/* dt-util.js — v4 */
window.DTUtil = (function () {
    /**
     * Configura header antifalsificação (RequestVerificationToken) em requests mutáveis.
     */
    function setupAjaxCsrf(metaSelector = 'meta[name="request-verification-token"]') {
        const token = document.querySelector(metaSelector)?.content || null;
        if (!token) return;
        $.ajaxSetup({
            beforeSend: function (xhr, s) {
                const m = (s.type || '').toUpperCase();
                if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(m)) xhr.setRequestHeader('RequestVerificationToken', token);
            }
        });
    }

    /**
     * Localização pt-BR para DataTables.
     */
    function dtLanguage() {
        return {
            decimal: ',', thousands: '.',
            processing: 'Processando...', search: 'Pesquisar:', lengthMenu: 'Mostrar _MENU_ registros',
            info: 'Mostrando _START_ a _END_ de _TOTAL_ registros', infoEmpty: 'Mostrando 0 a 0 de 0 registros',
            infoFiltered: '(filtrado de _MAX_ no total)', loadingRecords: 'Carregando...',
            zeroRecords: 'Nenhum registro encontrado', emptyTable: 'Nenhum dado disponível',
            paginate: { first: 'Primeiro', previous: 'Anterior', next: 'Próximo', last: 'Último' },
            aria: { sortAscending: ': ordenar crescente', sortDescending: ': ordenar decrescente' }
        };
    }

    /** Escapa HTML básico. */
    function escapeHtml(s) {
        return String(s ?? '').replace(/[&<>"']/g, m => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[m]));
    }

    /** Encontra índice de coluna pelo mData. */
    function findColumnIndexByData(dt, name) {
        const c = dt.settings()[0].aoColumns;
        for (let i = 0; i < c.length; i++) { if (c[i].mData === name) return i; }
        return -1;
    }

    /** Guarda metadados/API no elemento tabela. */
    function storeMeta($t, meta) { $t.data('dt-meta', meta); }
    function storeApi($t, api) { $t.data('dt-api', api); }
    function getApiFromEl(el) { return $(el).closest('table[data-dt-context]').data('dt-api') || null; }

    // versão mais tolerante: tenta target/data-targetTableId, depois sobe no DOM
    function getNearestTable(trigger) {
        const target = $(trigger).data('target'); if (target) return $(String(target));
        const tid = $(trigger).data('targetTableId'); if (tid) return $('#' + tid);
        const $from = $(trigger);
        const $closest = $from.closest('table[data-dt-context]');
        if ($closest.length) return $closest;
        return $from.parents().find('table[data-dt-context]').first();
    }

    /**
     * Helper de notificação padronizado.
     * Usa AppNotifier.<level>() se existir, senão cai em alert() + console.
     * @param {'info'|'warn'|'error'|'success'} level
     * @param {string} message
     * @param {'log'|'warn'|'error'} [fallbackConsole='warn']
     */
    function notify(level, message, fallbackConsole = 'warn') {
        if (window.AppNotifier && typeof AppNotifier[level] === 'function') {
            AppNotifier[level](message);
        } else {
            alert(message);
            if (console && typeof console[fallbackConsole] === 'function') {
                console[fallbackConsole]('DTUtil:', message);
            }
        }
    }

    /** Renderers úteis para colunas. */
    const renderers = {
        boolBadge: (val) => !!val ? '<span class="badge bg-success">Sim</span>' : '<span class="badge bg-secondary">Não</span>',
        dateBR: (val) => { if (!val) return ''; try { const d = new Date(val); return d.toLocaleDateString('pt-BR'); } catch { return String(val); } },
        currencyBRL: (val) => { try { return (val == null ? '' : Number(val).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })); } catch { return String(val ?? ''); } }
    };

    // registro de actions por contexto
    const registry = new Map();
    function on(ctx, action, fn) {
        if (!registry.has(ctx)) registry.set(ctx, new Map());
        registry.get(ctx).set(String(action).toLowerCase(), fn);
    }
    function onMany(ctx, map) { if (!map) return; Object.entries(map).forEach(([a, f]) => on(ctx, a, f)); }
    function resolve(ctx, action) {
        const k = String(action).toLowerCase();
        const c = registry.get(ctx); if (c && c.has(k)) return c.get(k);
        const all = registry.get('*'); if (all && all.has(k)) return all.get(k);
        return null;
    }

    // delegados globais
    let wired = false;
    function initGlobalDelegates(root = document) {
        if (wired) return; wired = true;

        // ações (editar, excluir, detalhes) e handlers custom
        $(root).on('click', '.dt-action', async function (e) {
            const $btn = $(e.currentTarget);
            const action = String($btn.data('action') || '').toLowerCase(); if (!action) return;
            const $table = $btn.closest('table[data-dt-context]'); const meta = $table.data('dt-meta') || {};
            const ctx = meta.context || $table.data('dt-context') || '*'; const id = $btn.data('id'); const dt = getApiFromEl($btn[0]);
            const handler = resolve(ctx, action);

            if (typeof handler === 'function')
                return handler({ id, event: e, context: ctx, button: $btn[0], dt, table: $table[0], meta });

            if (action === 'edit') {
                const urlGet = meta.routes?.editGet?.replace('{id}', encodeURIComponent(id));
                const urlPost = meta.routes?.editPost?.replace('{id}', encodeURIComponent(id)) || urlGet;
                if (meta.useModal && window.AppModal?.form && urlGet)
                    return AppModal.form({
                        title: `Editar: ${id}`, getUrl: urlGet, postUrl: urlPost,
                        onSuccess: () => {
                            dt?.ajax.reload(null, false);
                            //window.AppNotifier && AppNotifier.success('Registro atualizado.'); @* comentado para eviar duplicação  *@
                        }
                    });
                if (urlGet) return location.href = urlGet;
            }

            if (action === 'detail' && meta.routes?.detailsGet)
                return location.href = meta.routes.detailsGet.replace('{id}', encodeURIComponent(id));

            if (action === 'delete') {
                const rowData = dt.row($btn.closest('tr')).data() || {}; const token = rowData?.[meta.tokenField || 'token'];
                if (!token) return (window.AppNotifier ? AppNotifier.warn('Token ausente para exclusão.') : alert('Token ausente para exclusão.'));
                const proceed = window.AppModal?.confirm ? await AppModal.confirm('Excluir este registro?') : confirm('Excluir este registro?');
                if (!proceed) return;
                return $.ajax({ url: meta.routes.deletePost, type: 'POST', contentType: 'application/json; charset=UTF-8', data: JSON.stringify({ token }) })
                    .done(() => { dt.ajax.reload(null, false); window.AppNotifier && AppNotifier.success('Excluído com sucesso.'); })
                    .fail(xhr => (window.AppAjax ? AppAjax.handleError(xhr) : alert('Erro ao excluir.')));
            }
        });

        // export (usa botões internos invisíveis)
        $(root).on('click', '[data-export]', function () {
            const type = String($(this).data('export') || '').toLowerCase();
            const $t = getNearestTable(this); const dt = $t.data('dt-api');
            if (!dt) { notify('error', 'Grid não encontrado para exportação.'); return; }
            exportTable(dt, type);
        });

        // exclusão em lote (com toasts de erro/aviso)
        $(root).on('click', '[data-bulk="delete"]', async function () {
            const $t = getNearestTable(this);
            const dt = $t.data('dt-api');
            const meta = $t.data('dt-meta') || {};

            if (!$t.length || !dt) { notify('error', 'Grid não encontrado para exclusão.'); return; }
            if (!meta.routes?.deletePost) { notify('warn', 'Rota de exclusão não configurada.'); return; }

            const rows = getSelectedRows(dt);
            if (!rows.length) { notify('info', 'Nenhum registro selecionado.'); return; }

            const tokenField = meta.tokenField || 'token';
            const tokens = rows.map(r => r?.[tokenField]).filter(Boolean);
            if (!tokens.length) { notify('warn', 'Nenhum token encontrado nas linhas selecionadas.'); return; }

            const proceed = window.AppModal?.confirm ? await AppModal.confirm(`Excluir ${tokens.length} registro(s)?`) : confirm(`Excluir ${tokens.length} registro(s)?`);
            if (!proceed) return;

            if (meta.routes.deleteBatchPost) {
                $.ajax({ url: meta.routes.deleteBatchPost, type: 'POST', contentType: 'application/json; charset=UTF-8', data: JSON.stringify({ tokens }) })
                    .done((res) => {
                        dt.ajax.reload(null, false);
                        const msg = res && typeof res.ok === 'number' ? `Exclusão: OK ${res.ok} | Falhas ${res.fail || 0}` : 'Exclusão em lote concluída.';
                        window.AppNotifier ? AppNotifier.success(msg) : alert(msg);
                    })
                    .fail(xhr => (window.AppAjax ? AppAjax.handleError(xhr) : alert('Erro no lote.')));
            } else {
                let ok = 0, fail = 0;
                for (const t of tokens) { try { await $.ajax({ url: meta.routes.deletePost, type: 'POST', contentType: 'application/json; charset=UTF-8', data: JSON.stringify({ token: t }) }); ok++; } catch { fail++; } }
                dt.ajax.reload(null, false);
                const msg = `Exclusão concluída. Sucesso: ${ok} | Falhas: ${fail}`;
                window.AppNotifier ? (fail ? AppNotifier.warn(msg) : AppNotifier.success(msg)) : alert(msg);
            }
        });

        // toggle (ativo/inativo)
        $(root).on('change', '.dt-toggle', function () {
            const $tog = $(this); const $t = $tog.closest('table[data-dt-context]'); const meta = $t.data('dt-meta') || {};
            const id = $tog.data('id'); const ativo = $tog.is(':checked');
            if (!meta.routes?.updateAtivoPost) {
                $tog.prop('checked', !ativo);
                return (window.AppNotifier ? AppNotifier.warn('Rota de atualização de status não configurada.') : alert('Rota de atualização de status não configurada.'));
            }
            $.ajax({ url: meta.routes.updateAtivoPost, type: 'POST', data: $.param({ id, ativo }), contentType: 'application/x-www-form-urlencoded; charset=UTF-8' })
                .done(() => window.AppNotifier && AppNotifier.success('Status atualizado.'))
                .fail(xhr => { $tog.prop('checked', !ativo); (window.AppAjax ? AppAjax.handleError(xhr) : alert('Falha ao atualizar status.')); });
        });
    }

    /**
     * Retorna dados das linhas marcadas via checkbox .dt-row-select.
     */
    function getSelectedRows(dt) {
        const out = []; dt.rows().every(function () { const n = this.node(); const $c = $(n).find('.dt-row-select:checkbox'); if ($c.is(':checked')) out.push(this.data()); });
        return out;
    }

    /**
     * Cria DataTable com colunas padrão e ações delegadas.
     */
    function createAjaxDataTable(opt) {
        if (!$.fn.DataTable) throw new Error('DataTables não carregado.');
        if (!opt || !opt.table) throw new Error('DTUtil: Parâmetro "table" é obrigatório.');
        if (!(opt.routes && (opt.routes.list || opt.ajaxUrl))) throw new Error('DTUtil: Informe routes.list (ou ajaxUrl).');
        if (!Array.isArray(opt.fields)) throw new Error('DTUtil: Parâmetro "fields" deve ser um array.');

        const rowId = opt.rowId || 'id';
        const tokenField = opt.tokenField || 'token';
        const fields = opt.fields;
        const context = opt.context || '*';

        const actions = Array.isArray(opt.actions) && opt.actions.length ? opt.actions : [
            { name: 'edit', icon: 'fa-pen', class: 'btn-outline-primary', title: 'Editar' },
            { name: 'delete', icon: 'fa-trash', class: 'btn-outline-danger', title: 'Excluir' },
            { name: 'detail', icon: 'fa-circle-info', class: 'btn-outline-secondary', title: 'Detalhes' }
        ];

        const $table = $(opt.table).attr('data-dt-context', context);

        const firstCol = {
            data: null, title: '', width: '32px', orderable: false, searchable: false, className: 'text-center',
            render: (_, _2, row) => `<input type="checkbox" class="form-check-input dt-row-select" data-id="${escapeHtml(row?.[rowId])}" aria-label="Selecionar">`
        };

        const middle = fields.map(c => {
            if (c.type === 'toggle') {
                return {
                    data: c.data, title: c.title || c.data, width: c.width || '90px',
                    orderable: c.orderable !== false, searchable: c.searchable !== false, className: (c.className || '') + ' text-center',
                    render: (val, _t, row) => {
                        const checked = !!val ? 'checked' : '';
                        return `<div class="form-check form-switch d-inline-flex align-items-center justify-content-center">
              <input class="form-check-input dt-toggle" type="checkbox" role="switch" ${checked} data-id="${escapeHtml(row?.[rowId])}" aria-label="Alternar status">
            </div>`;
                    }
                };
            }
            let renderer = null;
            if (typeof c.renderer === 'function') renderer = c.renderer;
            if (typeof c.renderer === 'string' && renderers[c.renderer]) renderer = renderers[c.renderer];
            const base = { data: c.data, title: c.title || c.data, width: c.width, orderable: c.orderable !== false, searchable: c.searchable !== false, className: c.className || '' };
            if (renderer) base.render = (val, _t, row) => renderer(val, row);
            return base;
        });

        const lastCol = {
            data: null, title: 'Ações', width: 'auto', orderable: false, searchable: false, className: 'text-center',
            render: (_, _2, row) => {
                const id = row?.[rowId];
                const btns = actions
                    .filter(a => typeof a.visible !== 'function' || a.visible(row) !== false)
                    .map(a => {
                        const title = a.title || a.name; const cls = a.class || 'btn-outline-secondary'; const icon = a.icon || 'fa-circle';
                        return `<button type="button" class="btn btn-sm ${cls} dt-action" data-action="${a.name}" data-id="${escapeHtml(id)}" title="${title}"><i class="fa-solid ${icon}"></i></button>`;
                    }).join('');
                return `<div class="btn-group" role="group" aria-label="Ações">${btns}</div>`;
            }
        };

        const dt = $table.DataTable({
            dom: 'Btip',
            buttons: {
                dom: { container: { className: 'dt-buttons d-none' } },
                buttons: [
                    { extend: 'excelHtml5', title: opt.exportName || 'Export' },
                    { extend: 'csvHtml5', title: opt.exportName || 'Export' },
                    { extend: 'pdfHtml5', title: opt.exportName || 'Export' },
                    { extend: 'copyHtml5' },
                    { extend: 'print', title: opt.exportName || 'Export' }
                ]
            },
            ajax: { url: opt.routes?.list || opt.ajaxUrl, type: 'GET', dataSrc: (res) => res?.data ?? [] },
            columns: [firstCol, ...middle, lastCol],
            rowId: (row) => row?.[rowId],
            responsive: true, autoWidth: false, processing: true, serverSide: false,
            pageLength: opt.pageLength || 25, lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, 'Todos']],
            order: [[1, 'asc']], language: dtLanguage()
        });

        storeApi($table, dt);
        storeMeta($table, { context, rowId, tokenField, useModal: !!opt.useModal, routes: opt.routes || {}, settings: { rowId, tokenField } });
        return dt;
    }

    /**
     * Dispara export (excel|csv|pdf|copy|print) usando botões do DataTables.
     */
    function exportTable(dt, tipo) {
        if (!dt || !dt.button) return;
        const map = { excel: '.buttons-excel', pdf: '.buttons-pdf', csv: '.buttons-csv', copiar: '.buttons-copy', copy: '.buttons-copy', print: '.buttons-print' };
        const sel = map[(tipo || '').toLowerCase()];
        if (sel) dt.button(sel).trigger();
        else console.warn('Tipo inválido:', tipo);
    }

    /** Visibilidade de coluna. */
    function setColumnVisibility(dt, idxOrName, visible) {
        if (!dt) return;
        let idx = typeof idxOrName === 'number' ? idxOrName : findColumnIndexByData(dt, idxOrName);
        if (idx < 0) return console.warn('Coluna não encontrada:', idxOrName);
        dt.column(idx).visible(!!visible);
    }

    /** Atalho para várias colunas por nome. */
    function toggleColumnsByName(dt, names = [], visible) {
        names.forEach(n => setColumnVisibility(dt, n, visible));
    }

    // expõe APIs principais
    return {
        setupAjaxCsrf,
        initGlobalDelegates,
        createAjaxDataTable,
        actions: { on, onMany },
        exportTable,
        setColumnVisibility,
        toggleColumnsByName,
        getSelectedRows,
        renderers
    };
})();


// === Busca global por input externo =============================
// Usa debounce e funciona com DataTables puro ou instância salva pelo DTUtil.
DTUtil.bindInputSearch = function (inputSelector, tableSelector, delayMs = 300) {
    // debounce simples
    const debounce = (fn, wait) => { let t; return (...args) => { clearTimeout(t); t = setTimeout(() => fn(...args), wait); }; };

    const $input = $(inputSelector);
    const $table = $(tableSelector || $input.attr('data-dt-target'));

    if (!$input.length) {
        if (window.AppNotifier) AppNotifier.error('Input de busca não encontrado.');
        else alert('Input de busca não encontrado.');
        console.warn('DTUtil.bindInputSearch: input não encontrado.', { inputSelector });
        return;
    }
    if (!$table.length) {
        if (window.AppNotifier) AppNotifier.error('Grid alvo da busca não encontrado.');
        else alert('Grid alvo da busca não encontrado.');
        console.warn('DTUtil.bindInputSearch: tabela não encontrada.', { tableSelector });
        return;
    }

    // pega a instância (via DTUtil ou DataTables puro)
    const dt = $table.data('dt-api') || ($.fn.dataTable.isDataTable($table) ? $table.DataTable() : null);
    if (!dt) {
        if (window.AppNotifier) AppNotifier.error('DataTable ainda não inicializado para busca.');
        else alert('DataTable ainda não inicializado para busca.');
        console.warn('DTUtil.bindInputSearch: DataTable não inicializado.', { tableSelector });
        return;
    }

    const doSearch = debounce(() => {
        const term = ($input.val() || '').toString();
        dt.search(term).draw();
    }, delayMs);

    // remove handlers antigos e adiciona novos (namespace .dtSearch)
    $input.off('.dtSearch')
        .on('input.dtSearch', doSearch)   // digitação
        .on('search.dtSearch', doSearch)  // clique no "X" de limpar no input[type=search]
        .on('keydown.dtSearch', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); doSearch(); } // Enter: busca imediata
            if (e.key === 'Escape') {                                   // Esc: limpa
                $input.val('');
                dt.search('').draw();
            }
        });
};


// === Limpar pesquisa e recarregar DataTable a partir de um botão ===
DTUtil.bindClearSearch = function (btnSelector, inputSelectors, tableSelector, options) {
    const opts = Object.assign({
        reload: false,             // true = ajax.reload(); false = draw()
        resetPaging: true,         // volta para a primeira página
        resetOrdering: false,      // true = limpa ordenação (usa default)
        clearColumnFilters: false, // true = limpa pesquisas por coluna (if any)
        clearState: false,         // true = limpa stateSave
        triggerChange: true        // dispara change nos inputs após limpar
    }, options || {});

    const $btn = $(btnSelector);
    const $inputs = Array.isArray(inputSelectors) ? $(inputSelectors.join(',')) : $(inputSelectors);

    $btn.off('click.dtClear').on('click.dtClear', function () {
        // 1) Limpa inputs customizados (texto, select, data, etc.)
        if ($inputs && $inputs.length) {
            $inputs.each(function () {
                const $el = $(this);
                const tag = (this.tagName || '').toUpperCase();

                if (tag === 'SELECT') {
                    $el.val('');
                    // se usar select2:
                    if ($el.hasClass('select2') || $el.data('select2')) {
                        $el.trigger('change.select2');
                    }
                } else {
                    // inputs text/date/number
                    $el.val('');
                }

                if (opts.triggerChange) $el.trigger('change');
            });
        }

        // 2) Opera na DataTable com segurança
        const $table = $(tableSelector);
        const dt = $table.data('dt-api') || ($.fn.dataTable.isDataTable($table) ? $table.DataTable() : null);
        if (!dt) {
            if (window.AppNotifier) AppNotifier.error('Grid não encontrado para limpar a pesquisa.');
            else alert('Grid não encontrado para limpar a pesquisa.');
            console.warn('DTUtil.bindClearSearch: DataTable não encontrado.', { tableSelector });
            return;
        }

        if (opts.clearState && typeof dt.state?.clear === 'function') { try { dt.state.clear(); } catch (_) { } }

        // Limpa busca global
        dt.search('');

        // Se houver filtros por coluna (col-specific search), limpa também
        if (opts.clearColumnFilters) dt.columns().every(function () { this.search(''); });

        // Reset ordenação/página conforme opções
        if (opts.resetOrdering) dt.order([]);
        if (opts.resetPaging) dt.page('first');

        // Redesenha/recarrega
        if (opts.reload && dt.ajax) {
            dt.ajax.reload(null, !opts.resetPaging); // false mantém página, true reseta
        } else {
            dt.draw();
        }
    });
};


/**
* Recarrega/redesenha o DataTable informado.
* @param {string|HTMLElement|jQuery} tableSelector - seletor ou elemento da tabela
* @param {Object} [options]
* @param {boolean} [options.resetPaging=false] - true volta para a 1ª página
* @param {boolean} [options.clientSideDraw=false] - força draw() mesmo sem ajax
*/
DTUtil.refresh = function (tableSelector, options) {
    const opts = Object.assign({ resetPaging: false, clientSideDraw: false }, options || {});
    const $table = $(tableSelector);
    const dt = $table.data('dt-api') || ($.fn.dataTable.isDataTable($table) ? $table.DataTable() : null);

    if (!dt) {
        if (window.AppNotifier) { AppNotifier.error('Grid não encontrado para atualização.'); }
        else { alert('Grid não encontrado para atualização.'); }
        console.warn('DTUtil.refresh: DataTable não encontrado.', { tableSelector });
        return;
    }

    if (dt.ajax && typeof dt.ajax.reload === 'function') {
        dt.ajax.reload(null, opts.resetPaging);
    } else if (opts.clientSideDraw) {
        if (opts.resetPaging) dt.page('first').draw(false);
        else dt.draw(false);
    }
};

// Delegate para anchors com atributo data-dt-refresh (plug & play)
$(document).on('click', '[data-dt-refresh]', function (e) {
    e.preventDefault();
    const target = $(this).data('dt-refresh') || $(this).data('target');
    DTUtil.refresh(target);
});
