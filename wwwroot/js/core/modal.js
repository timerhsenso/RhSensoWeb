import { reparseUnobtrusive } from './validation.js';
export async function loadIntoModal(modalEl, url, { title, onLoaded } = {}) {
  const body = modalEl.querySelector('.modal-body');
  const titleEl = modalEl.querySelector('.modal-title');
  if (title && titleEl) titleEl.textContent = title;
  $(body).html('<div class="p-3 small text-muted">Carregando...</div>');
  $(body).load(url, function () {
    const form = body.querySelector('form');
    reparseUnobtrusive(form);
    if (typeof onLoaded === 'function') onLoaded(modalEl, form);
  });
  const bsModal = bootstrap.Modal.getOrCreateInstance(modalEl);
  bsModal.show();
  return bsModal;
}
export function closeModal(modalEl) { bootstrap.Modal.getOrCreateInstance(modalEl).hide(); }