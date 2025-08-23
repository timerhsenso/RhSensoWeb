import { secureFetch } from './http.js';
import { pick, isJsonResponse } from './json.js';
export function bindAjaxForm(formEl, { onSuccess, onError, redirectTo } = {}) {
  if (!formEl) return;
  formEl.addEventListener('submit', async (ev) => {
    ev.preventDefault();
    try {
      const res = await secureFetch(formEl.action, { method: 'POST', body: new FormData(formEl) });
      const data = (await (isJsonResponse(res) ? res.json() : Promise.resolve({})));
      const ok  = pick(data, 'success', 'Success');
      const msg = pick(data, 'message', 'Message') || '';
      if (!ok) throw new Error(msg || 'Falha ao salvar.');
      if (typeof onSuccess === 'function') onSuccess(data);
      if (redirectTo) window.location.href = redirectTo;
    } catch (e) {
      if (typeof onError === 'function') onError(e);
      else alert(e?.message || 'Falha ao salvar.');
    }
  }, { once: true });
}