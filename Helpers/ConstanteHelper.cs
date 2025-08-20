using RhSensoWeb.Models;

namespace RhSensoWeb.Helpers
{
    public static class ConstanteHelper
    {
        private static IHttpContextAccessor? _contextAccessor;

        public static void Configure(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public static string? GetValor(string cdconstante)
        {
            var constantes = _contextAccessor?
                .HttpContext?.Session.GetObject<List<Const1>>("Constantes");

            return constantes?
                .FirstOrDefault(x => x.Cdconstante.Equals(cdconstante, System.StringComparison.OrdinalIgnoreCase))
                ?.Dcconteudo;
        }
    }
}
