namespace Ddts.Application.Abstractions.Security;

/// <summary>Giden port (Şekil 4.4): parolayı geri çözülemeyecek şekilde hash'ler (PBKDF2, NFR-G2).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string hash, string password);
}
