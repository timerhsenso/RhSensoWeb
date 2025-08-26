// /js/app/startup.js
import '/js/core/config.js';
import { ensureAjaxSetup } from '/js/core/http.js';
import '/js/core/notify.js';
import '/js/core/modal.js';
import '/js/core/forms.js';
import { createCrudTable, bindCrudHandlers } from '/js/core/table.js';
import '/js/core/datatable.defaults.js';

ensureAjaxSetup();

document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('[data-crud]').forEach(el => {
    const cfgSelector = el.getAttribute('data-config');
    let cfg = {};
    if (cfgSelector) {
      const jsonEl = document.querySelector(cfgSelector);
      if (jsonEl) {
        try { cfg = JSON.parse(jsonEl.textContent || '{}'); }
        catch { console.warn('Config JSON inválido em', cfgSelector); }
      }
    }
    const endpoint = el.getAttribute('data-endpoint');
    if (endpoint && !cfg.ajax) cfg.ajax = { url: endpoint, type: 'GET' };

    const dt = createCrudTable(el, cfg);
    bindCrudHandlers(el, cfg, dt);
  });
});
