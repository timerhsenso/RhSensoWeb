// Enhanced CRUD Page Factory with better error handling and flexibility
import { ensureAjaxSetup, secureFetch } from '../core/http.js';
import { pick } from '../core/json.js';
import { loadIntoModal, closeModal } from '../core/modal.js';
import { bindAjaxForm } from '../core/forms.js';
import { createCrudTable } from '../core/table.js';
import { notify } from '../core/notify.js';

// Built-in column types for common scenarios
export const columnTypes = {
    text: (field, title, options = {}) => ({
        data: field,
        title,
        className: options.className || '',
        render: options.render || null
    }),

    number: (field, title, options = {}) => ({
        data: field,
        title,
        className: 'text-end ' + (options.className || ''),
        render: (data) => {
            if (data == null) return '';
            return options.currency ? `R$ ${Number(data).toFixed(2)}` : Number(data).toLocaleString();
        }
    }),

    date: (field, title, options = {}) => ({
        data: field,
        title,
        className: 'text-center ' + (options.className || ''),
        render: (data) => {
            if (!data) return '';
            const date = new Date(data);
            return options.format === 'datetime'
                ? date.toLocaleString('pt-BR')
                : date.toLocaleDateString('pt-BR');
        }
    }),

    toggle: (field, title, keyField = 'id') => ({
        data: null,
        title,
        orderable: false,
        className: 'text-center',
        render: (_, __, row) => {
            const id = getSafeValue(row, keyField);
            const checked = getSafeValue(row, field) ? 'checked' : '';
            return `<div class="form-check form-switch d-flex justify-content-center">
        <input type="checkbox" class="form-check-input crud-toggle" 
               data-id="${id}" data-field="${field}" ${checked} 
               role="switch" />
      </div>`;
        }
    }),

    actions: (options = {}) => ({
        data: null,
        title: options.title || 'Ações',
        orderable: false,
        searchable: false,
        className: 'text-center',
        width: options.width || '120px',
        render: (_, __, row) => {
            const editToken = getSafeValue(row, 'EditToken', 'editToken');
            const deleteToken = getSafeValue(row, 'DeleteToken', 'deleteToken');

            let buttons = '';

            if (options.showEdit !== false) {
                buttons += `<button class="btn btn-sm btn-outline-primary me-1" 
                           data-open-edit data-edit-token="${editToken}"
                           title="Editar">
                      <i class="ti ti-edit"></i>
                    </button>`;
            }

            if (options.showDelete !== false) {
                buttons += `<button class="btn btn-sm btn-outline-danger" 
                           data-del-token="${deleteToken}"
                           title="Excluir">
                      <i class="ti ti-trash"></i>
                    </button>`;
            }

            if (options.customButtons) {
                buttons += options.customButtons(row);
            }

            return `<div class="btn-group" role="group">${buttons}</div>`;
        }
    })
};

// Helper function to safely get values from row data
function getSafeValue(row, ...keys) {
    for (const key of keys) {
        if (row && row[key] !== undefined && row[key] !== null) {
            return row[key];
        }
    }
    return '';
}

// Enhanced validation for configuration
function validateConfig(config) {
    const required = ['endpoints', 'selectors'];
    const errors = [];

    for (const field of required) {
        if (!config[field]) {
            errors.push(`Campo obrigatório '${field}' não encontrado na configuração`);
        }
    }

    if (config.endpoints && !config.endpoints.list) {
        errors.push("Endpoint 'list' é obrigatório");
    }

    if (config.selectors && !config.selectors.table) {
        errors.push("Seletor 'table' é obrigatório");
    }

    if (errors.length > 0) {
        throw new Error(`Configuração inválida:\n${errors.join('\n')}`);
    }
}

// Enhanced error handling with retry mechanism
async function executeWithRetry(operation, retries = 2, delay = 1000) {
    for (let i = 0; i <= retries; i++) {
        try {
            return await operation();
        } catch (error) {
            if (i === retries) throw error;

            console.warn(`Tentativa ${i + 1} falhou, tentando novamente em ${delay}ms...`, error);
            await new Promise(resolve => setTimeout(resolve, delay));
        }
    }
}

// Main factory function with enhanced features
export function createCrudPage(config) {
    // Validate configuration
    validateConfig(config);

    // Default configuration with better structure
    const cfg = {
        // Entity info
        entity: 'Record',
        key: 'id',
        keyParamName: 'id',

        // Selectors
        selectors: {
            table: '#dataTable',
            modal: '#formModal',
            newButton: '#btnNew',
            exportButton: '#btnExport'
        },

        // Texts for internationalization
        texts: {
            newTitle: 'Novo Registro',
            editTitle: 'Editar Registro',
            confirmDelete: 'Tem certeza que deseja excluir este registro?',
            saved: 'Registro salvo com sucesso',
            deleted: 'Registro excluído com sucesso',
            updated: 'Status atualizado com sucesso',
            loading: 'Carregando...',
            error: 'Erro na operação',
            noData: 'Nenhum registro encontrado'
        },

        // Table options
        tableOptions: {
            pageLength: 25,
            lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "Todos"]],
            language: {
                url: '//cdn.datatables.net/plug-ins/1.13.8/i18n/pt-BR.json'
            },
            responsive: true,
            processing: true,
            serverSide: false
        },

        // Hooks for customization
        hooks: {
            beforeSave: null,
            afterSave: null,
            beforeDelete: null,
            afterDelete: null,
            onTableReady: null
        },

        // Merge with user config
        ...config
    };

    // Initialize AJAX setup
    ensureAjaxSetup();

    // Get DOM elements
    const elements = {
        table: document.querySelector(cfg.selectors.table),
        modal: document.querySelector(cfg.selectors.modal),
        newButton: document.querySelector(cfg.selectors.newButton),
        exportButton: document.querySelector(cfg.selectors.exportButton)
    };

    if (!elements.table) {
        throw new Error(`Tabela não encontrada: ${cfg.selectors.table}`);
    }

    // Process columns
    const processedColumns = (cfg.columns || []).map(col => {
        if (typeof col === 'function') return col();
        return col;
    });

    // Create DataTable with enhanced options
    const tableConfig = {
        ajax: {
            url: cfg.endpoints.list,
            dataSrc: 'data',
            error: function (xhr, error, thrown) {
                console.error('Erro ao carregar dados:', error, thrown);
                notify({
                    title: 'Erro',
                    message: 'Falha ao carregar os dados da tabela',
                    variant: 'danger'
                });
            }
        },
        columns: processedColumns,
        order: cfg.defaultOrder || [],
        __crudKey: cfg.key,
        ...cfg.tableOptions
    };

    const dt = createCrudTable(cfg.selectors.table, {
        ajaxUrl: cfg.endpoints.list,
        columns: processedColumns,
        options: tableConfig,
        onReady: (dataTable) => {
            if (cfg.hooks.onTableReady) {
                cfg.hooks.onTableReady(dataTable);
            }
        }
    });

    // New record handler
    if (elements.newButton && cfg.endpoints.createForm) {
        elements.newButton.addEventListener('click', () => {
            loadIntoModal(elements.modal, cfg.endpoints.createForm, {
                title: cfg.texts.newTitle,
                onLoaded: (modal, form) => {
                    bindAjaxForm(form, {
                        onSuccess: (data) => {
                            if (cfg.hooks.afterSave) cfg.hooks.afterSave(data, 'create');
                            closeModal(elements.modal);
                            dt.ajax.reload(null, false);
                            notify({
                                title: 'Sucesso',
                                message: cfg.texts.saved,
                                variant: 'success'
                            });
                        },
                        onError: (error) => {
                            notify({
                                title: 'Erro',
                                message: error.message || 'Falha ao salvar o registro',
                                variant: 'danger'
                            });
                        }
                    });
                }
            });
        });
    }

    // Edit record handler
    $(document).on('click', '[data-open-edit]', function () {
        const button = this;
        const tokenAttr = button.getAttribute('data-edit-token');
        const row = dt.row($(button).closest('tr')).data();

        let url;
        if (cfg.endpoints.safeEditForm) {
            url = typeof cfg.endpoints.safeEditForm === 'function'
                ? cfg.endpoints.safeEditForm(row, tokenAttr)
                : cfg.endpoints.safeEditForm;
        } else if (cfg.endpoints.editFormById) {
            url = cfg.endpoints.editFormById(getSafeValue(row, cfg.key));
        } else {
            console.error('Nenhum endpoint de edição configurado');
            return;
        }

        loadIntoModal(elements.modal, url, {
            title: cfg.texts.editTitle,
            onLoaded: (modal, form) => {
                bindAjaxForm(form, {
                    onSuccess: (data) => {
                        if (cfg.hooks.afterSave) cfg.hooks.afterSave(data, 'edit');
                        closeModal(elements.modal);
                        dt.ajax.reload(null, false);
                        notify({
                            title: 'Sucesso',
                            message: cfg.texts.saved,
                            variant: 'success'
                        });
                    },
                    onError: (error) => {
                        notify({
                            title: 'Erro',
                            message: error.message || 'Falha ao salvar o registro',
                            variant: 'danger'
                        });
                    }
                });
            }
        });
    });

    // Toggle status handler with enhanced error handling
    $(document).on('change', '.crud-toggle', async function () {
        const checkbox = this;
        const originalState = !checkbox.checked;
        const id = checkbox.getAttribute('data-id');
        const field = checkbox.getAttribute('data-field') || 'ativo';

        if (!cfg.endpoints.updateStatus && !cfg.endpoints.updateAtivo) {
            console.warn('Endpoint de atualização de status não configurado');
            checkbox.checked = originalState;
            return;
        }

        const endpoint = cfg.endpoints.updateStatus || cfg.endpoints.updateAtivo;

        try {
            await executeWithRetry(async () => {
                const params = new URLSearchParams();
                params.set(cfg.keyParamName || cfg.key, id);
                params.set(field, String(checkbox.checked));

                const response = await secureFetch(endpoint, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' },
                    body: params
                });

                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                }

                const json = await response.json();
                const success = pick(json, 'success', 'Success');
                const message = pick(json, 'message', 'Message') || '';

                if (!success) {
                    throw new Error(message || 'Falha ao atualizar o status');
                }

                notify({
                    title: 'Sucesso',
                    message: message || cfg.texts.updated,
                    variant: 'success'
                });
            });
        } catch (error) {
            console.error('Erro ao atualizar status:', error);
            checkbox.checked = originalState;
            notify({
                title: 'Erro',
                message: error.message || 'Falha ao atualizar o status',
                variant: 'danger'
            });
        }
    });

    // Delete record handler with enhanced confirmation
    $(document).on('click', '[data-del-token]', async function () {
        const button = this;
        const token = button.getAttribute('data-del-token');
        const row = dt.row($(button).closest('tr')).data();

        if (cfg.hooks.beforeDelete) {
            const shouldContinue = await cfg.hooks.beforeDelete(row);
            if (!shouldContinue) return;
        }

        // Enhanced confirmation dialog
        const confirmMessage = typeof cfg.texts.confirmDelete === 'function'
            ? cfg.texts.confirmDelete(row)
            : cfg.texts.confirmDelete;

        if (!confirm(confirmMessage)) return;

        try {
            button.disabled = true;

            await executeWithRetry(async () => {
                const response = await secureFetch(cfg.endpoints.deleteByToken, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ token })
                });

                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                }

                const json = await response.json();
                const success = pick(json, 'success', 'Success');
                const message = pick(json, 'message', 'Message') || '';

                if (!success) {
                    throw new Error(message || 'Falha ao excluir o registro');
                }

                if (cfg.hooks.afterDelete) cfg.hooks.afterDelete(row);

                dt.ajax.reload(null, false);
                notify({
                    title: 'Sucesso',
                    message: message || cfg.texts.deleted,
                    variant: 'success'
                });
            });
        } catch (error) {
            console.error('Erro ao excluir:', error);
            notify({
                title: 'Erro',
                message: error.message || 'Falha ao excluir o registro',
                variant: 'danger'
            });
        } finally {
            button.disabled = false;
        }
    });

    // Export functionality
    if (elements.exportButton && cfg.endpoints.export) {
        elements.exportButton.addEventListener('click', async () => {
            try {
                const response = await secureFetch(cfg.endpoints.export);
                if (!response.ok) throw new Error('Falha na exportação');

                const blob = await response.blob();
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `${cfg.entity}_${new Date().toISOString().split('T')[0]}.xlsx`;
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                document.body.removeChild(a);

                notify({
                    title: 'Sucesso',
                    message: 'Arquivo exportado com sucesso',
                    variant: 'success'
                });
            } catch (error) {
                notify({
                    title: 'Erro',
                    message: 'Falha ao exportar dados',
                    variant: 'danger'
                });
            }
        });
    }

    // Return API for external access
    return {
        dataTable: dt,
        config: cfg,
        elements,
        refresh: () => dt.ajax.reload(null, false),
        destroy: () => {
            if (dt) dt.destroy();
        }
    };
}