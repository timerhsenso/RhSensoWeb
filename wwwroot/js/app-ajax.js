/* app-ajax.js */
window.AppAjax = (function () {
  function getMessage(xhr, fallback = 'Erro inesperado.') {
    if (!xhr) return fallback;
    try {
      if (xhr.responseJSON && (xhr.responseJSON.message || xhr.responseJSON.error)) return xhr.responseJSON.message || xhr.responseJSON.error;
      if (xhr.responseText) {
        const t = xhr.responseText.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim();
        if (t) return t.substring(0, 300);
      }
      if (xhr.status && xhr.statusText) return `${xhr.status} - ${xhr.statusText}`;
    } catch {}
    return fallback;
  }
  function handleError(xhr) {
    const msg = getMessage(xhr);
    if (window.AppNotifier) AppNotifier.error(msg); else alert(msg);
  }
  return { getMessage, handleError };
})();
