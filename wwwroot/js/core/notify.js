export function ensureToastContainer() {
  if (document.getElementById('toast-container')) return;
  const div = document.createElement('div');
  div.id = 'toast-container';
  div.className = 'toast-container position-fixed top-0 end-0 p-3';
  document.body.appendChild(div);
}
export function notify({ title = 'Info', message = '', variant = 'primary', delay = 2500 } = {}) {
  ensureToastContainer();
  const container = document.getElementById('toast-container');
  const wrapper = document.createElement('div');
  wrapper.className = `toast align-items-center text-bg-${variant}`;
  wrapper.setAttribute('role', 'alert');
  wrapper.setAttribute('aria-live', 'assertive');
  wrapper.setAttribute('aria-atomic', 'true');
  wrapper.innerHTML = `
    <div class="d-flex">
      <div class="toast-body">
        <strong class="me-2">${title}</strong> ${message}
      </div>
      <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
    </div>`;
  container.appendChild(wrapper);
  const toast = new bootstrap.Toast(wrapper, { delay }); toast.show();
  wrapper.addEventListener('hidden.bs.toast', () => wrapper.remove());
}