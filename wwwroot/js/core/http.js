export function getCsrfToken() {
  const meta = document.querySelector('meta[name="request-verification-token"]');
  if (meta && meta.content) return meta.content;
  const input = document.querySelector('input[name="__RequestVerificationToken"]');
  if (input && input.value) return input.value;
  return "";
}
export function ensureAjaxSetup() {
  if (!window.$ || !$.ajaxSetup) return;
  const token = getCsrfToken();
  $.ajaxSetup({
    headers: {
      'RequestVerificationToken': token,
      'X-Requested-With': 'XMLHttpRequest'
    }
  });
}
export async function secureFetch(url, options = {}) {
  const token = getCsrfToken();
  options.headers = Object.assign({}, options.headers, {
    'RequestVerificationToken': token,
    'X-Requested-With': 'XMLHttpRequest'
  });
  return fetch(url, options);
}