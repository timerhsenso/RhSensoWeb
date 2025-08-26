/* app-notifier.js */
window.AppNotifier = (function () {
  const containerId = 'app-toast-container';
  function ensureContainer() {
    let c = document.getElementById(containerId);
    if (!c) {
      c = document.createElement('div');
      c.id = containerId;
      c.className = 'toast-container position-fixed bottom-0 end-0 p-3';
      c.setAttribute('aria-live', 'polite');
      c.setAttribute('aria-atomic', 'true');
      document.body.appendChild(c);
    }
    return c;
  }
  function icon(type) {
    return { success:'fa-circle-check', info:'fa-circle-info', warn:'fa-triangle-exclamation', error:'fa-circle-xmark' }[type] || 'fa-circle-info';
  }
  function show(type, message, delay = 3000) {
    try {
      const container = ensureContainer();
      const wrapper = document.createElement('div');
      wrapper.className = `toast align-items-center mb-2 border-0`;
      wrapper.setAttribute('role', 'alert');
      wrapper.setAttribute('aria-live', 'assertive');
      wrapper.setAttribute('aria-atomic', 'true');
      wrapper.innerHTML = `<div class="d-flex">
          <div class="toast-body"><i class="fa-solid ${icon(type)} me-2"></i>${message}</div>
          <button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast" aria-label="Fechar"></button>
        </div>`;
      container.appendChild(wrapper);
      if (window.bootstrap && bootstrap.Toast) {
        const t = new bootstrap.Toast(wrapper, { delay });
        t.show();
        wrapper.addEventListener('hidden.bs.toast', () => wrapper.remove());
      } else {
        console[type === 'error' ? 'error' : 'log'](message);
        setTimeout(() => wrapper.remove(), delay);
      }
    } catch (e) { alert(message); }
  }
  return {
    success: (m,d)=>show('success',m,d),
    info:    (m,d)=>show('info',m,d),
    warn:    (m,d)=>show('warn',m,d),
    error:   (m,d)=>show('error',m,d)
  };
})();
