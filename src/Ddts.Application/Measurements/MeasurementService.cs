using Ddts.Application.Abstractions.Auditing;
using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Abstractions.Security;
using Ddts.Application.Common;
using Ddts.Domain.Clients;
using Ddts.Domain.Measurements;

namespace Ddts.Application.Measurements;

/// <summary>
/// Ölçüm ve gelişim takibi (C modülü, FR-C1–C4, Şekil 5.6). Diyetisyen yalnızca kendi danışanının
/// ölçümlerine ulaşır: başka diyetisyenin danışanı "bulunamadı" (404) olur (Şekil 5.9).
/// Denetim kaydına yalnızca işlem yazılır, ölçüm değerleri (sağlık verisi) yazılmaz (FR-A5, KVKK).
/// </summary>
public sealed class MeasurementService(
    IClientRepository clients,
    IMeasurementRepository measurements,
    IUnitOfWork unitOfWork,
    IAuditLogger auditLogger,
    ICurrentUser currentUser) : IMeasurementService
{
    /// <summary>Denetim kayıtlarındaki kayıt türü (audit_logs.entity_type).</summary>
    public const string AuditEntityType = "Measurement";

    public async Task<Guid> AddAsync(Guid clientId, MeasurementCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetOwnedClientAsync(clientId, cancellationToken);
        await EnsureDayIsFreeAsync(client.Id, command.MeasuredOn, null, cancellationToken);

        var measurement = new Measurement(
            client.Id,
            command.MeasuredOn,
            command.WeightKg,
            command.HeightCm,
            command.WaistCm,
            command.HipCm,
            command.BodyFatPercent,
            command.Note);

        // Aynı anda gelen iki istekten biri tekil kısıta (client_id, measured_on) takılırsa API 409 döner.
        measurements.Add(measurement);
        await auditLogger.LogAsync("MeasurementAdded", AuditEntityType, measurement.Id, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return measurement.Id;
    }

    public async Task UpdateAsync(Guid clientId, Guid measurementId, MeasurementCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetOwnedClientAsync(clientId, cancellationToken);
        var measurement = await GetMeasurementAsync(client.Id, measurementId, cancellationToken);

        if (measurement.MeasuredOn != command.MeasuredOn)
        {
            await EnsureDayIsFreeAsync(client.Id, command.MeasuredOn, measurement.Id, cancellationToken);
        }

        measurement.Update(
            command.MeasuredOn,
            command.WeightKg,
            command.HeightCm,
            command.WaistCm,
            command.HipCm,
            command.BodyFatPercent,
            command.Note);

        await auditLogger.LogAsync("MeasurementUpdated", AuditEntityType, measurement.Id, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<MeasurementHistoryDto> ListAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var client = await GetOwnedClientAsync(clientId, cancellationToken);
        return await HistoryAsync(client, cancellationToken);
    }

    public async Task DeleteAsync(Guid clientId, Guid measurementId, CancellationToken cancellationToken = default)
    {
        var client = await GetOwnedClientAsync(clientId, cancellationToken);
        var measurement = await GetMeasurementAsync(client.Id, measurementId, cancellationToken);

        measurements.Remove(measurement);
        await auditLogger.LogAsync("MeasurementDeleted", AuditEntityType, measurement.Id, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<MeasurementHistoryDto> ListMyMeasurementsAsync(CancellationToken cancellationToken = default)
    {
        // Uç nokta yalnızca danışana açık; kimlikler token'daki claim'lerden gelir.
        var clientId = currentUser.ClientId
            ?? throw new InvalidOperationException("Bu işlem yalnızca danışan hesabıyla yapılabilir.");
        var dietitianId = currentUser.DietitianId
            ?? throw new InvalidOperationException("Danışanın bağlı olduğu diyetisyen token'da yok.");

        var client = await clients.GetAsync(dietitianId, clientId, cancellationToken)
            ?? throw new NotFoundException("Danışan bulunamadı.");

        return await HistoryAsync(client, cancellationToken);
    }

    /// <summary>
    /// Geçmişi eskiden yeniye dizer; her ölçümde bir öncekine göre farkı (FR-C2) ve son kiloya göre
    /// hedefe kalan kiloyu (FR-C3) hesaplar.
    /// </summary>
    private async Task<MeasurementHistoryDto> HistoryAsync(Client client, CancellationToken cancellationToken)
    {
        var list = await measurements.ListAsync(client.Id, DateOnly.MinValue, DateOnly.MaxValue, cancellationToken);
        var items = MeasurementTrend.Build(list);

        return new MeasurementHistoryDto(
            client.TargetWeightKg,
            MeasurementTrend.RemainingKg(client.TargetWeightKg, items.Count == 0 ? null : items[^1].WeightKg),
            items);
    }

    /// <summary>Aynı gün için tek ölçüm (FR-C1): o gün başka bir ölçüm varsa 409 (Senaryo 4, adım 1a).</summary>
    private async Task EnsureDayIsFreeAsync(Guid clientId, DateOnly day, Guid? exceptMeasurementId, CancellationToken cancellationToken)
    {
        var sameDay = await measurements.ListAsync(clientId, day, day, cancellationToken);
        if (sameDay.Any(m => m.Id != exceptMeasurementId))
        {
            throw new ConflictException("Bu tarih için zaten bir ölçüm var. Mevcut ölçümü düzenleyebilirsin.");
        }
    }

    private async Task<Client> GetOwnedClientAsync(Guid clientId, CancellationToken cancellationToken)
    {
        // Ölçüm uçları yalnızca diyetisyene açık ([Authorize(Roles = "Dietitian")]); kimlik token'dan gelir.
        var dietitianId = currentUser.DietitianId
            ?? throw new InvalidOperationException("Bu işlem yalnızca diyetisyen hesabıyla yapılabilir.");

        return await clients.GetAsync(dietitianId, clientId, cancellationToken)
            ?? throw new NotFoundException("Danışan bulunamadı.");
    }

    private async Task<Measurement> GetMeasurementAsync(Guid clientId, Guid measurementId, CancellationToken cancellationToken) =>
        await measurements.GetAsync(clientId, measurementId, cancellationToken)
        ?? throw new NotFoundException("Ölçüm bulunamadı.");
}
