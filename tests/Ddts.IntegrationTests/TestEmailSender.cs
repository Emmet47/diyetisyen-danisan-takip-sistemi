using System.Collections.Concurrent;
using Ddts.Application.Abstractions.Email;

namespace Ddts.IntegrationTests;

/// <summary>Testlerde SMTP yerine kullanılır: e-postaları göndermez, listede tutar.</summary>
public sealed class TestEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public IReadOnlyCollection<SentEmail> Sent => _sent.ToArray();

    public Task SendInvitationAsync(string to, string link, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new SentEmail(to, "Invitation", link));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string to, string link, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new SentEmail(to, "PasswordReset", link));
        return Task.CompletedTask;
    }

    /// <summary>Bu adrese giden son bağlantıdaki token değeri.</summary>
    public string LastTokenFor(string to)
    {
        var link = _sent.Last(e => e.To == to).Link;
        return link[(link.IndexOf("token=", StringComparison.Ordinal) + "token=".Length)..];
    }
}

public sealed record SentEmail(string To, string Kind, string Link);
