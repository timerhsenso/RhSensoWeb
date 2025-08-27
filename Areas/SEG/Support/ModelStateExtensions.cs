using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RhSensoWeb.Support
{
    public static class ModelStateExtensions
    {
        public static Dictionary<string, string[]> ToErrorsDictionary(this ModelStateDictionary modelState)
        {
            var dict = new Dictionary<string, string[]>();
            foreach (var kv in modelState)
            {
                var key = kv.Key;
                var errors = kv.Value.Errors?.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Erro de validação." : e.ErrorMessage).ToArray()
                            ?? Array.Empty<string>();
                if (errors.Length > 0)
                    dict[key] = errors;
            }
            return dict;
        }
    }
}
