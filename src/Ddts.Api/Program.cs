using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Ddts.Api.Errors;
using Ddts.Api.Security;
using Ddts.Application.Abstractions.Auditing;
using Ddts.Application.Abstractions.Email;
using Ddts.Application.Abstractions.Payments;
using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Abstractions.Security;
using Ddts.Application.Auth;
using Ddts.Application.Clients;
using Ddts.Application.Common;
using Ddts.Application.Subscriptions;
using Ddts.Infrastructure.Auditing;
using Ddts.Infrastructure.Email;
using Ddts.Infrastructure.Payments;
using Ddts.Infrastructure.Persistence;
using Ddts.Infrastructure.Persistence.Repositories;
using Ddts.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// =====================================================================
//  Composition root: hangi porta hangi adapter'ın takılacağı yalnızca burada seçilir.
//  Yeni bir port/adapter yazınca kaydını aşağıdaki ilgili bölüme ekleyin.
//  Not: ayarlar (bağlantı dizesi, JWT anahtarı ...) burada değil, servis ilk istendiğinde okunur;
//  böylece entegrasyon testleri kendi ayarlarını verebilir.
// =====================================================================

// ---- Web API altyapısı ----
builder.Services
    .AddControllers()
    // Enum'lar JSON'da metin olarak gider/gelir: "role": "Dietitian" (docs/api.md 8.3)
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();   // Hatalar RFC 9457 ProblemDetails biçiminde döner
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();   // servis hataları → 400/401/404/409
builder.Services.AddOpenApi();          // /openapi/v1.json (controller'lardaki XML yorumları da eklenir)
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();

// ---- Veritabanı: EF Core + PostgreSQL, tablo ve sütun adları snake_case ----
builder.Services.AddDbContext<DdtsDbContext>((serviceProvider, options) =>
{
    var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Default");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:Default ayarı boş. Yerelde 'dotnet user-secrets', Docker'da .env dosyasını kontrol edin.");
    }

    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
});

// ---- Kimlik doğrulama: JWT (ADR-0006) ----
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .Validate(
        jwt => jwt.SigningKey.Length >= JwtOptions.MinSigningKeyLength,
        $"Jwt:SigningKey ayarı boş ya da {JwtOptions.MinSigningKeyLength} karakterden kısa. " +
        "Yerelde 'dotnet user-secrets set \"Jwt:SigningKey\" ...', Docker'da .env içindeki JWT_SIGNING_KEY.")
    .ValidateOnStart();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
    {
        bearer.MapInboundClaims = false;   // claim adları token'daki gibi kalsın: "sub", "role"
        bearer.TokenValidationParameters = jwt.Value.CreateValidationParameters();
        bearer.Events = new JwtBearerEvents { OnTokenValidated = ActiveAccountCheck.OnTokenValidatedAsync };
    });

// Varsayılan: her uç nokta giriş ister. Herkese açık olanlar [AllowAnonymous] ile işaretli.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// ---- Giriş ucuna hız sınırı: IP başına dakikada 5 deneme, 6.'da 429 (NFR-G2) ----
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.Login, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        var problemDetailsService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Çok fazla giriş denemesi",
                Detail = "Bir dakika sonra tekrar dene.",
            },
        });
    };
});

// ---- Repository'ler ve diğer giden portlar (driven adapter'lar) ----
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<IDietitianRepository, EfDietitianRepository>();
builder.Services.AddScoped<IClientRepository, EfClientRepository>();
builder.Services.AddScoped<ISubscriptionPlanRepository, EfSubscriptionPlanRepository>();
builder.Services.AddScoped<ISubscriptionRepository, EfSubscriptionRepository>();
builder.Services.AddScoped<IFoodItemRepository, EfFoodItemRepository>();
builder.Services.AddScoped<IAuditLogRepository, EfAuditLogRepository>();

builder.Services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<IAuditLogger, EfAuditLogger>();
builder.Services.AddScoped<IPaymentGateway, FakePaymentAdapter>();   // ilk sürüm: ödeme simülasyonu
builder.Services.AddScoped<IEmailSender, SmtpEmailAdapter>();
builder.Services.AddOptions<SmtpOptions>().BindConfiguration(SmtpOptions.SectionName);
builder.Services.AddSingleton(TimeProvider.System);   // testlerde FakeTimeProvider takılır

// E-postadaki bağlantıların açılacağı web adresi
builder.Services.AddSingleton(serviceProvider => new AppLinks(
    serviceProvider.GetRequiredService<IConfiguration>()["Web:BaseUrl"] ?? "http://localhost:5173"));

// ---- Use case servisleri (gelen portlar) ----
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IClientService, ClientService>();

var app = builder.Build();

// ---- HTTP hattı (middleware) ----
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();                 // http://localhost:8080/openapi/v1.json
    app.MapScalarApiReference().AllowAnonymous();      // http://localhost:8080/scalar  (API'yi tarayıcıdan deneme arayüzü)
}

if (app.Environment.IsProduction())
{
    // Yayında tüm trafik HTTPS (NFR / 7.5 Güvenlik önlemleri)
    app.UseHsts();
    app.UseHttpsRedirection();
}

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.Services.ApplyMigrationsAsync();
    await app.Services.SeedAdminAsync();   // Seed:AdminEmail ve Seed:AdminPassword ayarlıysa ilk yönetici
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/api/health").AllowAnonymous();
app.MapControllers();

app.Run();

// Entegrasyon testlerindeki WebApplicationFactory<Program> için
public partial class Program { }
