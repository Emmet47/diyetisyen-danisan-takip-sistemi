namespace Ddts.Application.Notifications;

/// <summary>Gelen port: zamanlanmış hatırlatmalar (FR-J1). ReminderWorker 10 dakikada bir çağırır (Şekil 5.7).</summary>
public interface IReminderService
{
    /// <summary>Zamanı gelen hatırlatmaları üretir. Aynı hatırlatma ikinci kez üretilmez (dedupKey).</summary>
    Task SendDueRemindersAsync(DateTime now, CancellationToken cancellationToken = default);
}
