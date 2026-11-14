using Ddts.Application.Abstractions.Auditing;
using Ddts.Application.Abstractions.Email;
using Ddts.Application.Abstractions.Payments;
using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Abstractions.Security;
using Ddts.Application.Common;
using Ddts.Domain.Common;
using Ddts.Domain.Dietitians;
using Ddts.Domain.Subscriptions;
using Ddts.Domain.Users;

namespace Ddts.Application.Auth;

/// <summary>Kayıt, giriş ve parola sıfırlama (FR-A1–A3, FR-A5).</summary>
public sealed class AuthService(
    IUserRepository users,
    IDietitianRepository dietitians,
    ISubscriptionPlanRepository subscriptionPlans,
    ISubscriptionRepository subscriptions,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IEmailSender emailSender,
    IPaymentGateway paymentGateway,
    IAuditLogger auditLogger,
    AppLinks links,
    TimeProvider timeProvider) : IAuthService
{
    public const int MinPasswordLength = 8;

    public const int MaxPasswordLength = 100;

    public async Task<LoginResult> RegisterDietitianAsync(RegisterDietitianCommand command, CancellationToken cancellationToken = default)
    {
        if (await users.EmailExistsAsync(command.Email, cancellationToken))
        {
            throw new ConflictException("Bu e-posta adresiyle kayıtlı bir hesap var. Giriş yapabilir ya da parolanı sıfırlayabilirsin.");
        }

        var plan = await subscriptionPlans.GetAsync(command.SubscriptionPlanId, cancellationToken);
        if (plan is null || !plan.IsActive)
        {
            throw new DomainException("Seçilen paket satışta değil.");
        }

        EnsureValidPassword(command.Password);

        var user = new User(command.Email, UserRole.Dietitian);
        user.SetPassword(passwordHasher.Hash(command.Password));
        var dietitian = new Dietitian(user.Id, command.FullName, command.Phone);

        // Ödeme simülasyonu: kart bilgisi alınmaz (FR-L4)
        var payment = await paymentGateway.ChargeAsync(dietitian.Id, plan.MonthlyPrice, cancellationToken);
        if (!payment.Succeeded)
        {
            throw new DomainException(payment.Error ?? "Ödeme alınamadı.");
        }

        users.Add(user);
        dietitians.Add(dietitian);
        subscriptions.Add(new Subscription(dietitian.Id, plan, Today()));
        await auditLogger.LogAsync("DietitianRegistered", "User", user.Id, null, cancellationToken);

        // Kullanıcı, diyetisyen, abonelik ve denetim kaydı tek veritabanı işleminde yazılır.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateLoginResult(user);
    }

    public async Task<LoginResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByEmailAsync(command.Email, cancellationToken);

        // Hangisinin yanlış olduğunu söylemiyoruz: "e-posta ya da parola hatalı" (belge 7.5)
        if (user?.PasswordHash is null || !passwordHasher.Verify(user.PasswordHash, command.Password))
        {
            await auditLogger.LogAsync("LoginFailed", "User", user?.Id, null, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new AuthenticationFailedException("E-posta ya da parola hatalı.");
        }

        if (!user.IsActive)
        {
            throw new AuthenticationFailedException("Hesabın pasif durumda. Diyetisyeninle ya da yöneticiyle iletişime geç.");
        }

        await auditLogger.LogAsync("LoginSucceeded", "User", user.Id, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateLoginResult(user);
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByEmailAsync(email, cancellationToken);

        // Kayıtlı olmayan e-postada da aynı yanıt (202) döner: hangi e-postaların kayıtlı olduğu öğrenilemez.
        if (user is null || !user.IsActive)
        {
            return;
        }

        // Bağlantıdaki token rastgele; veritabanına yalnızca hash'i yazılır.
        var token = ResetTokens.NewToken();
        user.StartPasswordReset(ResetTokens.Hash(token), timeProvider.GetUtcNow().UtcDateTime.Add(ResetTokens.Lifetime));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailSender.SendPasswordResetAsync(user.Email, links.PasswordReset(token), cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var tokenHash = ResetTokens.Hash(command.Token);
        var user = await users.GetByResetTokenHashAsync(tokenHash, cancellationToken);

        if (user is null || !user.IsResetTokenValid(tokenHash, timeProvider.GetUtcNow().UtcDateTime))
        {
            throw new DomainException("Bağlantı geçersiz ya da süresi dolmuş. Yeniden parola sıfırlama isteği gönder.");
        }

        EnsureValidPassword(command.NewPassword);

        // SetPassword token'ı da siler: bağlantı tek kullanımlık.
        user.SetPassword(passwordHasher.Hash(command.NewPassword));
        await auditLogger.LogAsync("PasswordReset", "User", user.Id, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureValidPassword(string password)
    {
        if (password.Length is < MinPasswordLength or > MaxPasswordLength)
        {
            throw new DomainException($"Parola {MinPasswordLength} ile {MaxPasswordLength} karakter arasında olmalı.");
        }
    }

    private LoginResult CreateLoginResult(User user)
    {
        var expiresAt = timeProvider.GetUtcNow().UtcDateTime.Add(AuthTokenDefaults.Lifetime);
        return new LoginResult(tokenService.CreateToken(user), expiresAt, user.Role);
    }

    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
