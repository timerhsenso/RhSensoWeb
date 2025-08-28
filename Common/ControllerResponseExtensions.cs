using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RhSensoWeb.Common
{
    public static class ControllerResponseExtensions
    {
        /// 200 OK com envelope ApiResponse.
        public static IActionResult OkResp(this Controller controller, string? message = null, object? data = null)
            => controller.Ok(ApiResponse.Ok(message, data));

        /// 400 BadRequest com envelope ApiResponse.
        public static IActionResult BadResp(this Controller controller, string? message = null, object? data = null)
            => controller.BadRequest(ApiResponse.Fail(message, data));

        /// 400 BadRequest com erros de validação do ModelState.
        public static IActionResult BadResp(this Controller controller, ModelStateDictionary modelState, string? message = null)
            => controller.BadRequest(ApiResponse.Fail(message ?? "Verifique os dados informados.", modelState.ToErrorsDictionary()));

        /// 201 Created com envelope ApiResponse.
        public static IActionResult CreatedResp(this Controller controller, string locationUrl, string? message = null, object? data = null)
            => controller.Created(locationUrl, ApiResponse.Ok(message, data));
    }
}
