using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Abstractions.Security;
using Ddts.Application.Abstractions.Storage;
using Ddts.Application.Common;
using Ddts.Application.Plans;
using Ddts.Domain.Common;
using Ddts.Domain.MealLogs;
using Ddts.Domain.Users;

namespace Ddts.Application.MealLogs;

/// <summary>
/// Öğün günlüğü ve fotoğraf (G modülü, Şekil 5.5). Danışan yalnızca yayımlanmış planındaki öğünü, bugün ya da
/// geçmiş bir gün için, günde bir kez kaydeder (BR-11). Fotoğraf herkese kapalı depoda durur; görüntülemek için
/// 10 dakikalık imzalı bağlantı üretilir (BR-10, NFR-G3).
/// </summary>
public sealed class MealLogService(
    IMealLogRepository mealLogs,
    INutritionPlanRepository plans,
    IClientRepository clients,
    IPhotoStorage photoStorage,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IMealLogService
{
    public static readonly TimeSpan PhotoLinkLifetime = TimeSpan.FromMinutes(10);
    public const int MaxComparisonDays = 31;
    public const string InvalidPhotoMessage = "Sadece JPEG/PNG, en fazla 5 MB yükleyebilirsin.";

    public async Task<TodayDto> GetTodayAsync(CancellationToken cancellationToken = default)
    {
        var clientId = ClientId();
        var today = LocalTime.Today(timeProvider);

        var plan = await plans.GetPublishedForClientAsync(clientId, cancellationToken);
        if (plan is null)
        {
            return new TodayDto(today, null, null, []);
        }

        var logs = await mealLogs.ListAsync(clientId, today, today, cancellationToken);
        var meals = PlannedMeals.For(plan, today)
            .Select(meal => new TodayMealDto(
                PlanMapping.ToDto(meal),
                logs.Where(l => l.MealId == meal.Id).Select(ToDto).FirstOrDefault()))
            .ToList();

        return new TodayDto(today, plan.Id, plan.Title, meals);
    }

    public async Task<Guid> LogMealAsync(LogMealCommand command, CancellationToken cancellationToken = default)
    {
        var clientId = ClientId();
        var today = LocalTime.Today(timeProvider);

        if (command.LogDate > today)
        {
            throw new DomainException("İleri bir tarih için öğün kaydı yapılamaz.");
        }

        var plan = await plans.GetPublishedForClientAsync(clientId, cancellationToken);
        var meal = plan?.Meals.FirstOrDefault(m => m.Id == command.MealId)
            ?? throw new NotFoundException("Bu öğün yayımlanmış planında yok.");

        if (meal.DayNo != PlannedMeals.DayNoFor(plan!.PlanType, command.LogDate))
        {
            throw new DomainException("Bu öğün seçilen günün planında yok.");
        }

        var sameDay = await mealLogs.ListAsync(clientId, command.LogDate, command.LogDate, cancellationToken);
        if (sameDay.Any(l => l.MealId == meal.Id))
        {
            throw new ConflictException("Bu öğünü o gün için zaten kaydettin; kaydı güncelleyebilirsin.");
        }

        var log = new MealLog(clientId, meal.Id, command.LogDate, command.Status, command.Note);
        mealLogs.Add(log);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return log.Id;
    }

    public async Task UpdateLogAsync(Guid mealLogId, UpdateMealLogCommand command, CancellationToken cancellationToken = default)
    {
        var log = await GetMyLogAsync(mealLogId, cancellationToken);
        log.Update(command.Status, command.Note);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MealLogDto>> ListMyLogsAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var (start, end) = Range(from, to);
        var logs = await mealLogs.ListAsync(ClientId(), start, end, cancellationToken);
        return logs.Select(ToDto).ToList();
    }

    public async Task AttachPhotoAsync(Guid mealLogId, Stream content, CancellationToken cancellationToken = default)
    {
        var log = await GetMyLogAsync(mealLogId, cancellationToken);
        if (log.Photo is not null)
        {
            throw new ConflictException("Bu öğün kaydında zaten bir fotoğraf var.");
        }

        // En fazla 5 MB + 1 bayt okunur: sınırı aşan dosya belleğe tamamen alınmadan reddedilir.
        using var buffer = new MemoryStream();
        await CopyAtMostAsync(content, buffer, PhotoInfo.MaxSizeBytes + 1, cancellationToken);

        var contentType = DetectImageType(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
        if (contentType is null || buffer.Length == 0 || buffer.Length > PhotoInfo.MaxSizeBytes)
        {
            throw new InvalidPhotoException(InvalidPhotoMessage);
        }

        var key = $"{log.ClientId:N}/{Guid.NewGuid():N}{(contentType == "image/png" ? ".png" : ".jpg")}";
        var photo = PhotoInfo.Create(key, contentType, (int)buffer.Length);

        buffer.Position = 0;
        await photoStorage.UploadAsync(key, buffer, contentType, cancellationToken);

        try
        {
            log.AttachPhoto(photo);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Kayıt yazılamadıysa depoda sahipsiz dosya kalmasın.
            await photoStorage.DeleteAsync(key, CancellationToken.None);
            throw;
        }
    }

    public async Task<PhotoUrlDto> GetPhotoUrlAsync(Guid mealLogId, CancellationToken cancellationToken = default)
    {
        var log = await mealLogs.GetAsync(mealLogId, cancellationToken)
            ?? throw new NotFoundException("Öğün kaydı bulunamadı.");

        // Sahiplik: danışan yalnızca kendi kaydını, diyetisyen yalnızca kendi danışanının kaydını görür (IT-10).
        var allowed = currentUser.Role switch
        {
            UserRole.Client => currentUser.ClientId == log.ClientId,
            UserRole.Dietitian => currentUser.DietitianId is { } dietitianId
                && await clients.GetAsync(dietitianId, log.ClientId, cancellationToken) is not null,
            _ => false,
        };

        if (!allowed)
        {
            throw new NotFoundException("Öğün kaydı bulunamadı.");
        }

        var photo = log.Photo ?? throw new NotFoundException("Bu öğün kaydında fotoğraf yok.");
        var url = photoStorage.GetPresignedUrl(photo.Key, PhotoLinkLifetime);

        return new PhotoUrlDto(url, timeProvider.GetUtcNow().UtcDateTime + PhotoLinkLifetime);
    }

    public async Task<MealComparisonDto> GetComparisonAsync(
        Guid clientId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var dietitianId = currentUser.DietitianId
            ?? throw new InvalidOperationException("Bu işlem yalnızca diyetisyen hesabıyla yapılabilir.");
        var client = await clients.GetAsync(dietitianId, clientId, cancellationToken)
            ?? throw new NotFoundException("Danışan bulunamadı.");

        var (start, end) = Range(from, to);
        var clientPlans = await plans.ListForClientAsync(client.Id, cancellationToken);
        var logs = await mealLogs.ListAsync(client.Id, start, end, cancellationToken);

        var days = PlannedMeals.Between(clientPlans, start, end)
            .Select(day => new ComparisonDayDto(
                day.Date,
                day.Meals
                    .Select(meal => new ComparisonMealDto(
                        meal.Id,
                        meal.Name,
                        meal.Time,
                        logs.Where(l => l.MealId == meal.Id && l.LogDate == day.Date).Select(ToDto).FirstOrDefault()))
                    .ToList()))
            .ToList();

        return new MealComparisonDto(start, end, days);
    }

    /// <summary>Varsayılan: son 7 gün. En fazla 31 gün.</summary>
    private (DateOnly From, DateOnly To) Range(DateOnly? from, DateOnly? to)
    {
        var end = to ?? LocalTime.Today(timeProvider);
        var start = from ?? end.AddDays(-6);

        if (start > end)
        {
            throw new DomainException("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
        }

        if (end.DayNumber - start.DayNumber + 1 > MaxComparisonDays)
        {
            throw new DomainException($"En fazla {MaxComparisonDays} günlük aralık seçilebilir.");
        }

        return (start, end);
    }

    private async Task<MealLog> GetMyLogAsync(Guid mealLogId, CancellationToken cancellationToken)
    {
        var log = await mealLogs.GetAsync(mealLogId, cancellationToken);
        if (log is null || log.ClientId != ClientId())
        {
            throw new NotFoundException("Öğün kaydı bulunamadı.");
        }

        return log;
    }

    /// <summary>Dosya türüne uzantıdan değil dosya imzasından (ilk baytlar) bakılır (BR-10).</summary>
    private static string? DetectImageType(ReadOnlySpan<byte> bytes)
    {
        ReadOnlySpan<byte> png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        ReadOnlySpan<byte> jpeg = [0xFF, 0xD8, 0xFF];

        if (bytes.StartsWith(png))
        {
            return "image/png";
        }

        return bytes.StartsWith(jpeg) ? "image/jpeg" : null;
    }

    private static async Task CopyAtMostAsync(Stream source, Stream destination, int maxBytes, CancellationToken cancellationToken)
    {
        var chunk = new byte[81920];
        var total = 0;
        int read;
        while (total < maxBytes && (read = await source.ReadAsync(chunk.AsMemory(0, Math.Min(chunk.Length, maxBytes - total)), cancellationToken)) > 0)
        {
            await destination.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
            total += read;
        }
    }

    private static MealLogDto ToDto(MealLog log) =>
        new(log.Id, log.MealId, log.LogDate, log.Status, log.Note, log.Photo is not null, log.LoggedAt);

    private Guid ClientId() =>
        currentUser.ClientId ?? throw new InvalidOperationException("Bu işlem yalnızca danışan hesabıyla yapılabilir.");
}
