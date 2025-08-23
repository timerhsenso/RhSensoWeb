import { ensureAjaxSetup, secureFetch } from '../core/http.js';
import { pick } from '../core/json.js';
import { loadIntoModal, closeModal } from '../core/modal.js';
import { bindAjaxForm } from '../core/forms.js';
import { createCrudTable } from '../core/table.js';
import { notify } from '../core/notify.js';

function renderToggle(field, key, row) {
  const id = row[key];
  const checked = row[field] ? 'checked' : '';
  return `<input type="checkbox" class="form-check-input crud-toggle" data-id="${id}" data-field="${field}" ${checked} />`;
}
export const builtins = {
  key: (field, title) => ({ data: field, title }),
  text: (field, title) => ({ data: field, title }),
  toggle: (field, title) => ({
    data: null, title,
    render: (_, __, row, meta) => renderToggle(field, meta.settings.oInit.__crudKey, row)
  }),
  actions: () => ({
    data: null, title: 'Ações', orderable: false,
    render: (_, __, row) => {
      const editToken = row.EditToken ?? row.editToken ?? '';
      const deleteToken = row.DeleteToken ?? row.deleteToken ?? '';
      return `<div class="btn-group btn-group-sm">
        <button class="btn btn-primary" data-open-edit data-edit-token="${editToken}">Editar</button>
        <button class="btn btn-danger"  data-del-token="${deleteToken}">Excluir</button>
      </div>`;
    }
  }),
};

export function createCrudPage(config) {
  const cfg = Object.assign({
    selectors: { table: '#tbl', modal: '#formModal', newButton: '#btnAdd' },
    texts: { newTitle: 'Novo Registro', editTitle: 'Editar Registro', confirmDelete: 'Confirma excluir?', saved: 'Registro salvo.', deleted: 'Excluído com sucesso.' }
  }, config || {});

  ensureAjaxSetup();
  const modEl = document.querySelector(cfg.selectors.modal);
  const columns = (cfg.columns || []).map(col => typeof col === 'function' ? col() : col);
  const dt = createCrudTable(cfg.selectors.table, {
    ajaxUrl: cfg.endpoints.list,
    columns,
    options: { __crudKey: cfg.key }
  });

  if (cfg.selectors.newButton) {
    $(cfg.selectors.newButton).on('click', () => {
      loadIntoModal(modEl, cfg.endpoints.createForm, {
        title: cfg.texts.newTitle,
        onLoaded: (_, form) => bindAjaxForm(form, {
          onSuccess: () => { closeModal(modEl); dt.ajax.reload(null, false); notify({ title:'Sucesso', message: cfg.texts.saved, variant:'success' }); }
        })
      });
    });
  }

  $(document).on('click', '[data-open-edit]', function () {
    const tokenAttr = this.getAttribute('data-edit-token');
    const row = dt.row($(this).closest('tr')).data();
    const url = cfg.endpoints.safeEditForm
      ? (typeof cfg.endpoints.safeEditForm === 'function' ? cfg.endpoints.safeEditForm(row, tokenAttr) : cfg.endpoints.safeEditForm)
      : (cfg.endpoints.editFormById ? cfg.endpoints.editFormById(row[cfg.key]) : null);
    if (!url) return console.warn('Nenhum endpoint de edição configurado.');

    loadIntoModal(modEl, url, {
      title: cfg.texts.editTitle,
      onLoaded: (_, form) => bindAjaxForm(form, {
        onSuccess: () => { closeModal(modEl); dt.ajax.reload(null, false); notify({ title:'Sucesso', message: cfg.texts.saved, variant:'success' }); }
      })
    });
  });

  $(document).on('change', '.crud-toggle', async function () {
    const el = this;
    const id = el.getAttribute('data-id');
    const desired = el.checked;
    if (!cfg.endpoints.updateAtivo) return;
    try {
      const params = new URLSearchParams();
      params.set(cfg.keyParamName || cfg.key, id);
      params.set('ativo', String(desired));
      const res = await secureFetch(cfg.endpoints.updateAtivo, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' },
        body: params
      });
      const json = await res.json();
      const ok = pick(json, 'success','Success'); const msg = pick(json, 'message','Message') || '';
      if (!ok) throw new Error(msg || 'Falha ao atualizar.');
      notify({ title:'Sucesso', message: msg || 'Atualizado.', variant:'success' });
    } catch (e) {
      el.checked = !desired;
      notify({ title:'Erro', message: e?.message || 'Falha ao atualizar.', variant:'danger' });
    }
  });

  $(document).on('click', '[data-del-token]', async function () {
    const token = this.getAttribute('data-del-token');
    if (!confirm(cfg.texts.confirmDelete)) return;
    try {
      const res = await secureFetch(cfg.endpoints.deleteByToken, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ token })
      });
      const json = await res.json();
      const ok = pick(json, 'success','Success'); const msg = pick(json, 'message','Message') || '';
      if (!ok) throw new Error(msg || 'Falha ao excluir.');
      dt.ajax.reload(null, false);
      notify({ title:'Sucesso', message: msg || cfg.texts.deleted, variant:'success' });
    } catch (e) {
      notify({ title:'Erro', message: e?.message || 'Falha ao excluir.', variant:'danger' });
    }
  });

  return dt;
}