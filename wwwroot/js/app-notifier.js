/* app-notifier.js (v2) */
window.AppNotifier = (function () {
    // -------------------- Defaults globais (pode mudar em runtime) --------------------
    const defaults = {
        position: 'bottom-right',    // top-right | top-left | bottom-right | bottom-left | center
        template: 'basic',           // basic | minimal | rich
        delay: 3000,                 // ms
        autohide: true,
        className: ''                // classes extras no .toast
    };

    const POS = {
        'top-right': 'top-0 end-0',
        'top-left': 'top-0 start-0',
        'bottom-right': 'bottom-0 end-0',
        'bottom-left': 'bottom-0 start-0',
        'center': 'top-50 start-50 translate-middle'
    };

    // Ícones (Font Awesome). Ajuste para seu set se quiser.
    const ICON = {
        success: 'fa-circle-check',
        info: 'fa-circle-info',
        warn: 'fa-triangle-exclamation',
        error: 'fa-circle-xmark'
    };

    const BG = {
        success: 'text-bg-success',
        info: 'text-bg-info',
        warn: 'text-bg-warning',
        error: 'text-bg-danger'
    };

    // -------------------- Templates --------------------
    const TPL = {
        // header + body
        basic: ({ title, message, type }) => `
      <div class="toast shadow border-0" role="alert" aria-live="assertive" aria-atomic="true">
        <div class="toast-header">
          <i class="fa-solid ${ICON[type] || ICON.info} me-2"></i>
          <strong class="me-auto">${title || ''}</strong>
          <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Fechar"></button>
        </div>
        <div class="toast-body">${message || ''}</div>
      </div>`,

        // bloco único com cor do tipo
        minimal: ({ message, type }) => `
      <div class="toast ${BG[type] || ''} text-white border-0 shadow" role="alert" aria-live="assertive" aria-atomic="true">
        <div class="d-flex align-items-center p-3">
          <i class="fa-solid ${ICON[type] || ICON.info} me-2"></i>
          <div class="flex-grow-1">${message || ''}</div>
          <button type="button" class="btn-close btn-close-white ms-2" data-bs-dismiss="toast" aria-label="Fechar"></button>
        </div>
      </div>`,

        // header colorido + progress
        rich: ({ title, message, type }) => `
      <div class="toast shadow border-0" role="alert" aria-live="assertive" aria-atomic="true">
        <div class="toast-header ${BG[type] || ''} text-white">
          <i class="fa-solid ${ICON[type] || ICON.info} me-2"></i>
          <strong class="me-auto">${title || ''}</strong>
          <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Fechar"></button>
        </div>
        <div class="toast-body">
          ${message || ''}
          <div class="progress mt-2" style="height:3px;">
            <div class="progress-bar ${BG[type] || ''} w-100"></div>
          </div>
        </div>
      </div>`
    };

    // -------------------- Helpers --------------------
    function getContainer(position) {
        const posKey = position || defaults.position;
        const id = `app-toast-container-${posKey}`;
        let c = document.getElementById(id);
        if (!c) {
            c = document.createElement('div');
            c.id = id;
            c.className = `toast-container position-fixed ${POS[posKey] || POS['bottom-right']} p-3`;
            c.setAttribute('aria-live', 'polite');
            c.setAttribute('aria-atomic', 'true');
            c.style.zIndex = 1080;
            c.style.maxWidth = '420px';
            document.body.appendChild(c);
        }
        return c;
    }

    function buildOptions(messageOrOpts, maybeOpts) {
        // Retrocompat: AppNotifier.success('msg', 2000) / AppNotifier.success('msg', { ... })
        if (typeof messageOrOpts === 'string') {
            const o = (typeof maybeOpts === 'number')
                ? { delay: maybeOpts }
                : (maybeOpts || {});
            return { message: messageOrOpts, ...o };
        }
        return messageOrOpts || {};
    }

    // -------------------- Núcleo --------------------
    function show(type, messageOrOpts, maybeOpts) {
        try {
            const opts = {
                ...defaults,
                ...buildOptions(messageOrOpts, maybeOpts)
            };
            const { message = '', title = '', template, position, delay, autohide, className } = opts;

            const container = getContainer(position);
            const html = (TPL[template] || TPL.basic)({ title, message, type });

            const wrapper = document.createElement('div');
            wrapper.innerHTML = html.trim();
            const toastEl = wrapper.firstElementChild;
            if (className) toastEl.classList.add(...String(className).split(' ').filter(Boolean));

            container.appendChild(toastEl);

            if (window.bootstrap && bootstrap.Toast) {
                const t = bootstrap.Toast.getOrCreateInstance(toastEl, { delay, autohide });
                toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
                t.show();
                return t;
            } else {
                console[type === 'error' ? 'error' : 'log'](message);
                setTimeout(() => toastEl.remove(), delay);
                return null;
            }
        } catch (e) {
            alert(typeof messageOrOpts === 'string' ? messageOrOpts : (messageOrOpts?.message || 'Notificação'));
        }
    }

    // -------------------- API Pública --------------------
    function config(newDefaults = {}) { Object.assign(defaults, newDefaults || {}); }
    function removeAll() {
        document.querySelectorAll('[id^="app-toast-container-"] .toast.show, [id^="app-toast-container-"] .toast')
            .forEach(el => {
                try {
                    const t = bootstrap.Toast.getInstance(el);
                    t ? t.hide() : el.remove();
                } catch { el.remove(); }
            });
    }

    return {
        config, removeAll,
        success: (m, o) => show('success', m, o),
        info: (m, o) => show('info', m, o),
        warn: (m, o) => show('warn', m, o),
        error: (m, o) => show('error', m, o)
    };
})();



/* app-notifier.js */


/*
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
*/