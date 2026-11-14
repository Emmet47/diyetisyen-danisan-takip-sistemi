using System.Globalization;
using System.Text.Json;
using Ddts.Application.Abstractions.Auditing;
using Ddts.Application.Abstractions.Email;
using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Abstractions.Security;
using Ddts.Application.Common;
using Ddts.Domain.Clients;
using Ddts.Domain.Common;
using Ddts.Domain.Subscriptions;
using Ddts.Domain.Users;

namespace Ddts.Application.Clients;

/// <summary>
/// Danışan profil yönetimi (B modülü, FR-B1–B4, FR-L2). Her işlem giriş yapmış diyetisyenin
/// kimliğiyle (tenant) yapılır: başka diyetisyenin danışanı "bulunamadı" (404) olur (Şekil 5.9).
/// </summary>
public sealed class ClientService(
    IClientRepository clients,
    IUserRepository users,
    ISubscriptionRepository subscriptions,
    IFoodItemRepository foodItems,
    IAuditLogRepository auditLogs,
    IUnitOfWork unitOfWork,
    IEmailSender emailSender,
    IAuditLogger auditLogger,
    ICurrentUser currentUser,
    AppLinks links,
    TimeProvider timeProvider) : IClientService
{
    /// <summary>Denetim kayıtlarındaki kayıt türü (audit_logs.entity_type).</summary>
    public const string AuditEntityType = "Client";

    public async Task<Guid> AddClientAsync(AddClientCommand command, CancellationToken cancellationToken = default)
    {
        var dietitianId = CurrentDietitianId();

        // KVKK: sağlık verisi açık rızaya dayanarak işlenir (belge 2.5, madde 2)
        if (!command.KvkkConsentGiven)
        {
            throw new DomainException("Danışandan açık rıza alındığını işaretlemeden danışan eklenemez.");
        }

        if (await users.EmailExistsAsync(command.Email, cancellationToken))
        {
            throw new ConflictException("Bu e-posta adresiyle kayıtlı bir hesap var.");
        }

        var now = UtcNow();
        var profile = new ClientProfile(
            command.FullName, command.Phone, command.GoalType, command.TargetWeightKg, command.Preferences, command.AllergyNotes);

        // Danışanın giriş hesabı parolasız açılır; parolayı davet bağlantısından kendisi belirler (FR-A3).
        var user = new User(command.Email, UserRole.Client);
        var client = new Client(dietitianId, user.Id, profile, command.BirthDate, command.Sex, now);
        var invitationToken = ResetTokens.NewToken();
        user.StartPasswordReset(ResetTokens.Hash(invitationToken), now.Add(ResetTokens.Lifetime));

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Abonelik satırı kilitlenir: aynı anda iki danışan eklenirken limit aşılamaz (Şekil 5.1).
            await EnsureCanAddClientAsync(dietitianId, cancellationToken);

            users.Add(user);
            clients.Add(client);
            await auditLogger.LogAsync("ClientAdded", AuditEntityType, client.Id, null, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Davet e-postası işlem kapanmadan gönderilir: gönderilemezse danışan kaydı da geri alınır
            // ve diyetisyen aynı formu tekrar gönderebilir.
            await emailSender.SendInvitationAsync(user.Email, links.PasswordReset(invitationToken), cancellationToken);
        }, cancellationToken);

        return client.Id;
    }

    public async Task<IReadOnlyList<ClientSummaryDto>> ListAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var list = await clients.ListAsync(CurrentDietitianId(), onlyActive, cancellationToken);
        return list.Select(c => new ClientSummaryDto(c.Id, c.FullName, c.Phone, c.GoalType, c.IsActive)).ToList();
    }

    public async Task<ClientDetailDto> GetAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var client = await GetOwnedClientAsync(clientId, cancellationToken);
        return await ToDetailAsync(client, cancellationToken);
    }

    public async Task UpdateClientAsync(Guid clientId, ClientProfile profile, CancellationToken cancellationToken = default)
    {
        var client = await GetOwnedClientAsync(clientId, cancellationToken);
        var before = ProfileValues(client);

        client.UpdateProfile(profile);

        var changes = Diff(before, ProfileValues(client));
        if (changes.Count == 0)
        {
            return;
        }

        await auditLogger.LogAsync("ClientUpdated", AuditEntityType, client.Id, ToDetailsJson(changes), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetRestrictionsAsync(Guid clientId, IReadOnlyList<RestrictionInput> restrictions, CancellationToken cancellationToken = default)
    {
        var dietitianId = CurrentDietitianId();
        var client = await GetOwnedClientAsync(clientId, cancellationToken);

        // Yalnızca diyetisyenin görebildiği besinler (ortak kayıtlar + kendi kayıtları) listeye girer.
        var requestedIds = restrictions.Select(r => r.FoodItemId).Distinct().ToList();
        var oldIds = client.Restrictions.Select(r => r.FoodItemId).ToList();
        var foods = await foodItems.GetManyAsync(dietitianId, requestedIds.Union(oldIds).ToList(), cancellationToken);
        var foodNames = foods.ToDictionary(f => f.Id, f => f.Name);

        if (requestedIds.Any(id => !foodNames.ContainsKey(id)))
        {
            throw new DomainException("Listedeki besinlerden bazıları bulunamadı.");
        }

        var before = DescribeRestrictions(client.Restrictions, foodNames);

        client.SetRestrictions(restrictions.Select(r => new FoodRestriction(r.FoodItemId, r.Type)));

        var after = DescribeRestrictions(client.Restrictions, foodNames);
        if (before == after)
        {
            return;
        }

        var changes = new Dictionary<string, (string? Old, string? New)> { ["restrictions"] = (before, after) };
        await auditLogger.LogAsync("ClientRestrictionsUpdated", AuditEntityType, client.Id, ToDetailsJson(changes), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClientHistoryEntryDto>> GetHistoryAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var client = await GetOwnedClientAsync(clientId, cancellationToken);
        var logs = await auditLogs.ListForEntityAsync(AuditEntityType, client.Id, cancellationToken);

        return logs.Select(log => new ClientHistoryEntryDto(log.CreatedAt, log.Action, log.UserId, ParseChanges(log.Details))).ToList();
    }

    public async Task DeactivateAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var client = await GetOwnedClientAsync(clientId, cancellationToken);
        if (!client.IsActive)
        {
            throw new ConflictException("Danışan zaten pasif.");
        }

        var user = await GetAccountAsync(client, cancellationToken);

        // Pasif danışan paket sayımına girmez ve giriş yapamaz; verileri silinmez (FR-B4).
        client.Deactivate();
        user.Deactivate();
        await auditLogger.LogAsync("ClientDeactivated", AuditEntityType, client.Id, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivateAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var dietitianId = CurrentDietitianId();

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var client = await GetOwnedClientAsync(clientId, cancellationToken);
            if (client.IsActive)
            {
                throw new ConflictException("Danışan zaten aktif.");
            }

            // Aktifleşen danışan paket sayımına yeniden girer: limit eklemedeki gibi kontrol edilir (FR-L2).
            await EnsureCanAddClientAsync(dietitianId, cancellationToken);

            var user = await GetAccountAsync(client, cancellationToken);
            client.Activate();
            user.Activate();
            await auditLogger.LogAsync("ClientActivated", AuditEntityType, client.Id, null, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task DeleteAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var client = await GetOwnedClientAsync(clientId, cancellationToken);
        var user = await GetAccountAsync(client, cancellationToken);

        // Denetim kayıtlarında "kim, ne zaman, hangi işlem" kalır; eski/yeni değerler silinir (belge 2.5, madde 4).
        var logs = await auditLogs.ListForEntityAsync(AuditEntityType, client.Id, cancellationToken);
        foreach (var log in logs)
        {
            log.ClearDetails();
        }

        // Hesap silinince veritabanı danışanı ve ona bağlı ölçüm, plan, öğün kaydı, randevu ve
        // bildirimleri zincirleme (ON DELETE CASCADE) siler.
        clients.Remove(client);
        users.Remove(user);
        await auditLogger.LogAsync("ClientDeleted", AuditEntityType, client.Id, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ClientDetailDto> GetMyProfileAsync(CancellationToken cancellationToken = default)
    {
        // Uç nokta yalnızca danışana açık; kimlikler token'daki claim'lerden gelir.
        var clientId = currentUser.ClientId
            ?? throw new InvalidOperationException("Bu işlem yalnızca danışan hesabıyla yapılabilir.");
        var dietitianId = currentUser.DietitianId
            ?? throw new InvalidOperationException("Danışanın bağlı olduğu diyetisyen token'da yok.");

        var client = await clients.GetAsync(dietitianId, clientId, cancellationToken)
            ?? throw new NotFoundException("Danışan bulunamadı.");

        return await ToDetailAsync(client, cancellationToken);
    }

    private async Task EnsureCanAddClientAsync(Guid dietitianId, CancellationToken cancellationToken)
    {
        var subscription = await subscriptions.GetActiveForUpdateAsync(dietitianId, cancellationToken);

        // Süresi dolmuş abonelikte yeni danışan eklenemez (FR-L4, BR-01)
        if (subscription is null || !subscription.IsActiveOn(DateOnly.FromDateTime(UtcNow())))
        {
            throw new SubscriptionLimitException("Aboneliğin aktif değil. Yeni danışan eklemek için aboneliğini yenile.");
        }

        var activeClients = await clients.CountActiveAsync(dietitianId, cancellationToken);
        subscription.EnsureCanAddClient(activeClients);
    }

    private async Task<Client> GetOwnedClientAsync(Guid clientId, CancellationToken cancellationToken) =>
        await clients.GetAsync(CurrentDietitianId(), clientId, cancellationToken)
        ?? throw new NotFoundException("Danışan bulunamadı.");

    private async Task<User> GetAccountAsync(Client client, CancellationToken cancellationToken) =>
        await users.GetAsync(client.UserId, cancellationToken)
        ?? throw new InvalidOperationException("Danışanın giriş hesabı bulunamadı.");

    private async Task<ClientDetailDto> ToDetailAsync(Client client, CancellationToken cancellationToken)
    {
        var user = await GetAccountAsync(client, cancellationToken);
        var foodIds = client.Restrictions.Select(r => r.FoodItemId).ToList();
        var foods = await foodItems.GetManyAsync(client.DietitianId, foodIds, cancellationToken);
        var foodNames = foods.ToDictionary(f => f.Id, f => f.Name);

        var restrictions = client.Restrictions
            .Select(r => new ClientRestrictionDto(r.FoodItemId, foodNames.GetValueOrDefault(r.FoodItemId, "?"), r.Type))
            .OrderBy(r => r.FoodName, StringComparer.Create(TurkishCulture, ignoreCase: true))
            .ToList();

        return new ClientDetailDto(
            client.Id,
            client.FullName,
            user.Email,
            client.Phone,
            client.BirthDate,
            client.Sex,
            client.GoalType,
            client.TargetWeightKg,
            client.Preferences,
            client.AllergyNotes,
            client.IsActive,
            client.KvkkConsentAt,
            restrictions);
    }

    // ---- Değişiklik geçmişi (FR-B3): audit_logs.details = {"alan": {"old": .., "new": ..}} ----

    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    private static Dictionary<string, string?> ProfileValues(Client client) => new()
    {
        ["fullName"] = client.FullName,
        ["phone"] = client.Phone,
        ["goalType"] = client.GoalType.ToString(),
        // numeric(5,1): veritabanından 65.0 gelir, istekten 65 gelebilir; aynı biçimde yazılır ki değişmemiş sayılsın.
        ["targetWeightKg"] = client.TargetWeightKg?.ToString("0.0", CultureInfo.InvariantCulture),
        ["preferences"] = client.Preferences,
        ["allergyNotes"] = client.AllergyNotes,
    };

    private static Dictionary<string, (string? Old, string? New)> Diff(
        Dictionary<string, string?> before, Dictionary<string, string?> after) =>
        before
            .Where(field => field.Value != after[field.Key])
            .ToDictionary(field => field.Key, field => (Old: field.Value, New: after[field.Key]));

    /// <summary>Kısıt listesinin okunur hali: "Süt (Dislike), Yer fıstığı (Allergy)"; boşsa null.</summary>
    private static string? DescribeRestrictions(IEnumerable<FoodRestriction> restrictions, Dictionary<Guid, string> foodNames)
    {
        var items = restrictions
            .Select(r => $"{foodNames.GetValueOrDefault(r.FoodItemId, "?")} ({r.Type})")
            .Order(StringComparer.Create(TurkishCulture, ignoreCase: true))
            .ToList();

        return items.Count == 0 ? null : string.Join(", ", items);
    }

    private static string ToDetailsJson(Dictionary<string, (string? Old, string? New)> changes) =>
        JsonSerializer.Serialize(changes.ToDictionary(
            change => change.Key,
            change => new Dictionary<string, string?> { ["old"] = change.Value.Old, ["new"] = change.Value.New }));

    private static IReadOnlyList<FieldChangeDto> ParseChanges(string? details)
    {
        if (string.IsNullOrEmpty(details))
        {
            return [];
        }

        using var document = JsonDocument.Parse(details);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var changes = new List<FieldChangeDto>();
        foreach (var field in document.RootElement.EnumerateObject())
        {
            if (field.Value.ValueKind == JsonValueKind.Object &&
                field.Value.TryGetProperty("old", out var oldValue) &&
                field.Value.TryGetProperty("new", out var newValue))
            {
                changes.Add(new FieldChangeDto(field.Name, AsText(oldValue), AsText(newValue)));
            }
        }

        return changes;
    }

    private static string? AsText(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.String => value.GetString(),
        _ => value.GetRawText(),
    };

    // Danışan uçları yalnızca diyetisyene açık ([Authorize(Roles = "Dietitian")]); kimlik token'dan gelir.
    private Guid CurrentDietitianId() =>
        currentUser.DietitianId ?? throw new InvalidOperationException("Bu işlem yalnızca diyetisyen hesabıyla yapılabilir.");

    private DateTime UtcNow() => timeProvider.GetUtcNow().UtcDateTime;
}
