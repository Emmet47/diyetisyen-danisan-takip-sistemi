namespace Ddts.Infrastructure.Persistence.Configurations;

/// <summary>
/// docs/db/schema.sql'deki DEFAULT ifadeleri. Kimlikleri normalde uygulama üretir (Guid.CreateVersion7);
/// DEFAULT'lar elle yazılan SQL'ler (test betikleri, demo verisi) için şemada duruyor.
/// </summary>
internal static class SqlDefaults
{
    public const string NewUuid = "gen_random_uuid()";

    public const string Now = "now()";
}
