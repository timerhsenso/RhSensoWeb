namespace RhSensoWeb.Common.Tokens
{
    /// <summary>
    /// Payload genérico para tokens opacos de ações por linha (Edit/Delete/etc.).
    /// Use em qualquer entidade: o serviço interpreta o campo Id.
    /// </summary>
    public sealed record RowKey(string Id);
}
