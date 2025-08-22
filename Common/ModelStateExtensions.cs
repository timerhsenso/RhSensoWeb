using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RhSensoWeb.Common;

public static class ModelStateExtensions
{
    public static Dictionary<string, string[]> ToErrorDictionary(this ModelStateDictionary modelState)
        => modelState.Where(kvp => kvp.Value?.Errors.Count > 0)
                     .ToDictionary(
                         kvp => kvp.Key,
                         kvp => kvp.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Inválido." : e.ErrorMessage).ToArray()
                     );
}
