using Ddts.Application.Abstractions.Persistence;
using Ddts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// =====================================================================
//  Composition root: hangi porta hangi adapter'ın takılacağı yalnızca burada seçilir.
//  Yeni bir port/adapter yazınca kaydını aşağıdaki ilgili bölüme ekleyin.
// =====================================================================

// ---- Web API altyapısı ----
builder.Services.AddControllers();
builder.Services.AddProblemDetails();   // Hatalar RFC 9457 ProblemDetails biçiminde döner
builder.Services.AddOpenApi();          // /openapi/v1.json (controller'lardaki XML yorumları da eklenir)
builder.Services.AddHealthChecks();

// ---- Veritabanı: EF Core + PostgreSQL, tablo ve sütun adları snake_case ----
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    // Bağlantı dizesi DbContext ilk istendiğinde okunur; böylece testler kendi
    // veritabanını (Testcontainers) ayarlardan verebilir.
    var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Default");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:Default ayarı boş. Yerelde 'dotnet user-secrets', Docker'da .env dosyasını kontrol edin.");
    }

    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
});

// ---- Repository'ler ve diğer giden portlar ----
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddSingleton(TimeProvider.System);   // testlerde FakeTimeProvider takılır

var app = builder.Build();

// ---- HTTP hattı (middleware) ----
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                 // http://localhost:8080/openapi/v1.json
    app.MapScalarApiReference();      // http://localhost:8080/scalar  (API'yi tarayıcıdan deneme arayüzü)
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
}

app.MapHealthChecks("/api/health");
app.MapControllers();

app.Run();

// Entegrasyon testlerindeki WebApplicationFactory<Program> için
public partial class Program { }
