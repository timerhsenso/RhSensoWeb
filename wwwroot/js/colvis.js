// wwwroot/js/colvis.js
// Requisitos: jQuery + DataTables (opcional: ColReorder, Choices.js)
// Uso automático (se quiser): <div data-colvis data-table="#grid"></div> + autoInitColVis()
// Uso manual (recomendado p/ modal): initColVis({ table:'#grid', holder:'#colvis-holder' })

/* ---------------- Helpers ---------------- */

function jq() { return window.jQuery || window.$ || null; }
function hasChoices() { return typeof window.Choices === 'function'; }

function slug(s) {
    return String(s || '')
        .trim()
        .toLowerCase()
        .normalize('NFD').replace(/[\u0300-\u036f]/g, '')
        .replace(/[^\w]+/g, '-')
        .replace(/(^-|-$)/g, '');
}

function getExistingDT(tableSel) {
    const $ = jq();
    if (!$) return null;
    const $el = $(tableSel);
    if (!$el.length) return null;
    return ($.fn.dataTable && $.fn.dataTable.isDataTable($el)) ? $el.DataTable() : null;
}

function whenDTReady(tableSel, cb) {
    const $ = jq();
    if (!$) return;
    const attach = () => {
        const dt = getExistingDT(tableSel);
        if (dt) { cb(dt); return true; }
        return false;
    };
    if (attach()) return;
    $(tableSel).one('init.dt', () => attach());
}

/* --------------- Núcleo --------------- */

export function initColVis({
    table,                 // '#grid...' (obrigatório)
    holder,                // '#colvis-holder' ou HTMLElement (obrigatório)
    storageKeyPrefix = 'colvis:',
    placeholder = 'Colunas visíveis',
    // defina aqui quais colunas não devem aparecer no seletor:
    skip = (idx, title, th) => {
        const t = (title || '').trim().toLowerCase();
        if (t === '' || t === '#') return true;       // checkbox / vazia
        if (t.includes('ações')) return true;         // coluna de ações
        return th.classList.contains('no-colvis');    // marque <th class="no-colvis">
    }
} = {}) {
    const $ = jq();
    const holderEl = (typeof holder === 'string') ? document.querySelector(holder) : holder;
    if (!holderEl || !$) return;

    // Evita duplicar UI no mesmo holder
    if (holderEl.dataset.colvisInitialized === '1') return;
    holderEl.dataset.colvisInitialized = '1';

    // Render do select (id único por abertura)
    const selId = 'colVisSelect_' + Math.random().toString(36).slice(2);
    holderEl.innerHTML = `<select id="${selId}" multiple data-choices data-choices-removeItem></select>`;
    const selectEl = holderEl.querySelector('#' + selId);

    whenDTReady(table, (dt) => {
        const settings = dt.settings()[0];
        const tableId = settings.sTableId || slug(table) || 'datatable';
        const STORAGE_KEY = storageKeyPrefix + tableId;

        // 🔹 chave para o “padrão” desta tabela
        const DEFAULT_KEY = storageKeyPrefix + 'default:' + tableId;

        // helpers -------------------------------------------------

        function getVisibleKeys() {
            const keys = [];
            dt.columns().every(function (idx) {
                const th = this.header();
                const title = (th && th.textContent) ? th.textContent.trim() : '';
                if (skip(idx, title, th)) return;
                const key = th?.dataset?.colKey || slug(title) || ('col-' + idx);
                if (this.visible()) keys.push(key);
            });
            return keys;
        }

        // Mapeia colunas da tabela
        function collectColumns() {
            const cols = [];
            dt.columns().every(function (idx) {
                const th = this.header();
                const title = (th && th.textContent) ? th.textContent.trim() : '';
                if (skip(idx, title, th)) return;

                const key = th?.dataset?.colKey || slug(title) || ('col-' + idx);
                cols.push({
                    idx,
                    key,
                    title: title || `Coluna ${idx + 1}`,
                    visible: this.visible()
                });
            });
            return cols;
        }

        // Constrói UI com seleção inicial (savedKeys OU colunas visíveis atuais)
        function buildUI(selectedKeys) {
            selectEl.innerHTML = '';
            const cols = collectColumns();

            const initialKeys = (Array.isArray(selectedKeys) && selectedKeys.length)
                ? new Set(selectedKeys)
                : new Set(cols.filter(c => c.visible).map(c => c.key)); // <- visíveis agora

            if (hasChoices()) {
                const choices = new window.Choices(selectEl, {
                    removeItemButton: true,
                    shouldSort: false,
                    placeholderValue: placeholder
                });

                const data = cols.map(c => ({
                    value: String(c.idx),
                    label: c.title,
                    selected: initialKeys.has(c.key),
                    customProperties: { key: c.key }
                }));

                choices.setChoices(data, 'value', 'label', true);
                // Garante chips marcados
                choices.setChoiceByValue(data.filter(d => d.selected).map(d => d.value));

                // Sincroniza data-key no <option> para salvar/ler por "key"
                [...selectEl.options].forEach((o, i) => {
                    o.dataset.key = data[i].customProperties.key;
                });

                // Guarda a instância (opcional)
                selectEl._choices = choices;
            } else {
                cols.forEach(c => {
                    const opt = document.createElement('option');
                    opt.value = String(c.idx);
                    opt.dataset.key = c.key;
                    opt.textContent = c.title;
                    if (initialKeys.has(c.key)) opt.selected = true;
                    selectEl.appendChild(opt);
                });
            }
        }

        function syncSelectToKeys(keys) {
            const set = new Set(keys);
            [...selectEl.options].forEach(o => { o.selected = set.has(o.dataset.key); });
            if (selectEl._choices) {
                const valuesToSelect = [...selectEl.options].filter(o => o.selected).map(o => o.value);
                selectEl._choices.removeActiveItems();
                selectEl._choices.setChoiceByValue(valuesToSelect);
            }
        }

        // Aplica visibilidade conforme seleção atual do select
        function applyVisibility(fromStorage = false) {
            const selectedKeys = new Set([...selectEl.selectedOptions].map(o => o.dataset.key));
            dt.columns().every(function (idx) {
                const th = this.header();
                const title = (th && th.textContent) ? th.textContent.trim() : '';
                if (skip(idx, title, th)) return;
                const key = th?.dataset?.colKey || slug(title) || ('col-' + idx);
                this.visible(selectedKeys.has(key), false);
            });
            dt.columns.adjust().draw(false);

            if (!fromStorage) {
                try { localStorage.setItem(STORAGE_KEY, JSON.stringify([...selectedKeys])); } catch { }
            }
        }

        // ===== Inicialização =====

        // 🔹 Carrega/gera padrão 1x (estado visível ao abrir pela primeira vez)
        let defaultKeys = [];
        try { defaultKeys = JSON.parse(localStorage.getItem(DEFAULT_KEY) || '[]'); } catch { }
        if (!defaultKeys.length) {
            defaultKeys = getVisibleKeys();
            try { localStorage.setItem(DEFAULT_KEY, JSON.stringify(defaultKeys)); } catch { }
        }

        // Aplica seleção salva do usuário (ou as visíveis atuais) e sincroniza DT
        let savedKeys = [];
        try { savedKeys = JSON.parse(localStorage.getItem(STORAGE_KEY) || '[]'); } catch { }
        buildUI(savedKeys);   // <- já preenche o select com as colunas visíveis se não houver saved
        applyVisibility(true);

        // Eventos
        selectEl.addEventListener('change', () => applyVisibility(false));

        // Reordenou colunas → reconstrói e reaplica seleção atual
        $(table).on('column-reorder.dt', () => {
            // seleção atual por key antes de reconstruir
            const currentKeys = [...selectEl.selectedOptions].map(o => o.dataset.key);
            buildUI(currentKeys);
            applyVisibility(true);
        });

        // Alguma outra ação mudou visibilidade → reflete no select/choices
        $(table).on('column-visibility.dt', () => {
            const keysVisible = [];
            dt.columns().every(function (idx) {
                const th = this.header();
                const title = (th && th.textContent) ? th.textContent.trim() : '';
                if (skip(idx, title, th)) return;
                const key = th?.dataset?.colKey || slug(title) || ('col-' + idx);
                if (this.visible()) keysVisible.push(key);
            });

            // marca opções
            [...selectEl.options].forEach(o => { o.selected = keysVisible.includes(o.dataset.key); });

            // se Choices, sincroniza chips
            if (selectEl._choices) {
                const valuesToSelect = [...selectEl.options].filter(o => o.selected).map(o => o.value);
                selectEl._choices.removeActiveItems();
                selectEl._choices.setChoiceByValue(valuesToSelect);
            }
        });

        // 🔹 Mini-API no holder (opcional para quem quiser chamar de fora)
        holderEl._colvis = {
            getDefaultKeys: () => [...defaultKeys],
            setDefaultKeys: (keys = []) => {
                defaultKeys = Array.isArray(keys) ? [...keys] : [];
                try { localStorage.setItem(DEFAULT_KEY, JSON.stringify(defaultKeys)); } catch { }
            },
            getSelectedKeys: () => [...selectEl.selectedOptions].map(o => o.dataset.key),
            setSelectedKeys: (keys = [], { apply = true, save = true } = {}) => {
                syncSelectToKeys(keys);
                if (apply) applyVisibility(!save);
                if (save) try { localStorage.setItem(STORAGE_KEY, JSON.stringify(keys)); } catch { }
            },
            resetToDefault: () => {
                syncSelectToKeys(defaultKeys);
                // aplica e salva como seleção atual do usuário
                applyVisibility(false);
                try { localStorage.setItem(STORAGE_KEY, JSON.stringify(defaultKeys)); } catch { }
            }
        };

        // 🔹 Eventos opcionais
        holderEl.addEventListener('colvis:reset', () => holderEl._colvis.resetToDefault());
        holderEl.addEventListener('colvis:set', (e) => holderEl._colvis.setSelectedKeys(e?.detail?.keys || [], { apply: true, save: true }));
        holderEl.addEventListener('colvis:set-default', (e) => holderEl._colvis.setDefaultKeys(e?.detail?.keys || getVisibleKeys()));
    });
}

/* -------------- Auto-init opcional -------------- */

export function autoInitColVis() {
    document.querySelectorAll('[data-colvis][data-table]').forEach(el => {
        initColVis({ table: el.getAttribute('data-table'), holder: el });
    });
}

// Se precisar usar sem modules, descomente a linha abaixo:
// window.ColVis = { initColVis, autoInitColVis };
