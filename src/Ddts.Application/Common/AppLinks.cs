namespace Ddts.Application.Common;

/// <summary>
/// E-postadaki bağlantıların açılacağı web adresi (ör. http://localhost:5173).
/// Değeri Program.cs'te "Web:BaseUrl" ayarından okunur.
/// </summary>
public sealed record AppLinks(string WebBaseUrl)
{
    public string PasswordReset(string token) => $"{WebBaseUrl.TrimEnd('/')}/reset-password?token={token}";
}
