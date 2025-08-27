using System;
using System.Linq;
using System.Reflection;
using RhSensoWeb.Services.Security;

namespace RhSensoWeb.Support
{
    public static class RowTokenServiceExtensions
    {
        /// <summary>
        /// Tenta decodificar o token chamando Unprotect<T> do serviço, seja qual for a assinatura
        /// (token) | (token, purpose) | (token, purpose, userId).
        /// O parâmetro userId é mantido por compatibilidade mesmo que a implementação não use.
        /// </summary>
        public static bool TryUnprotect<T>(this IRowTokenService svc, string token, string purpose, string userId, out T? payload)
        {
            payload = default;
            try
            {
                var type = svc.GetType();
                var candidates = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.Name == "Unprotect" && m.IsGenericMethodDefinition);

                foreach (var method in candidates)
                {
                    var gm = method.MakeGenericMethod(typeof(T));
                    var pars = gm.GetParameters();

                    object? result = null;

                    // Unprotect<T>(string token)
                    if (pars.Length == 1 && pars[0].ParameterType == typeof(string))
                    {
                        result = gm.Invoke(svc, new object[] { token });
                    }
                    // Unprotect<T>(string token, string purpose)
                    else if (pars.Length == 2 &&
                             pars[0].ParameterType == typeof(string) &&
                             pars[1].ParameterType == typeof(string))
                    {
                        result = gm.Invoke(svc, new object[] { token, purpose });
                    }
                    // Unprotect<T>(string token, string purpose, string userId)
                    else if (pars.Length == 3 &&
                             pars[0].ParameterType == typeof(string) &&
                             pars[1].ParameterType == typeof(string) &&
                             pars[2].ParameterType == typeof(string))
                    {
                        result = gm.Invoke(svc, new object[] { token, purpose, userId });
                    }

                    if (result is T ok)
                    {
                        payload = ok;
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                payload = default;
                return false;
            }
        }
    }
}
