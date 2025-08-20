using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;

namespace RhSensoWeb.Services.Security
{
    public interface IRowTokenService
    {
        string Protect(object payload, string purpose, string userId, TimeSpan ttl);
        (T Payload, string Purpose, string UserId) Unprotect<T>(string token);
    }

    /// <summary>
    /// Gera e valida tokens opacos assinados (com expiração e binding ao usuário/propósito).
    /// Use para abrir edição e excluir via AJAX sem expor IDs.
    /// </summary>
    public sealed class RowTokenService : IRowTokenService
    {
        private readonly IDataProtector _protector;

        public RowTokenService(IDataProtectionProvider provider)
        {
            // "purpose" do Data Protection separa as chaves lógicas (isola usos diferentes)
            _protector = provider.CreateProtector("RowToken.v1");
        }

        private sealed record Envelope(object d, string p, string u, DateTimeOffset exp);

        public string Protect(object payload, string purpose, string userId, TimeSpan ttl)
        {
            var env = new Envelope(payload, purpose, userId, DateTimeOffset.UtcNow.Add(ttl));
            var json = JsonSerializer.Serialize(env);
            return _protector.Protect(json);
        }

        public (T, string, string) Unprotect<T>(string token)
        {
            var json = _protector.Unprotect(token);
            var env = JsonSerializer.Deserialize<Envelope>(json)!;

            if (DateTimeOffset.UtcNow > env.exp)
                throw new InvalidOperationException("Token expirado.");

            var payload = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(env.d))!;
            return (payload, env.p, env.u);
        }
    }
}
