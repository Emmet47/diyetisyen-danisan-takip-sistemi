namespace Ddts.Application.Abstractions.Persistence;

/// <summary>
/// Giden (driven) port: bir use case boyunca yapılan değişiklikleri tek seferde kaydeder (Şekil 4.10).
/// Uygulaması Infrastructure katmanında: EfUnitOfWork.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Bekleyen değişiklikleri veritabanına yazar; etkilenen satır sayısını döner.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen işi tek bir veritabanı işleminde (transaction) çalıştırır; hata olursa hepsi geri alınır.
    /// Örnek (Şekil 5.1): abonelik satırı kilitlenir (SELECT … FOR UPDATE), limit kontrol edilir, danışan eklenir.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> work, CancellationToken cancellationToken = default);
}
