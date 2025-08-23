// Configuração CORRIGIDA para Tsistema
import { columnTypes } from '../page-factory.js';

export default {
    // Informações da entidade
    entity: "Sistema",
    key: "Cdsistema",
    keyParamName: "id",

    // Seletores DOM
    selectors: {
        table: "#tblTsistema",
        modal: "#formModal",
        newButton: "#BtnCreateNew",
        exportButton: "#BtnExport"
    },

    // Endpoints da API
    endpoints: {
        list: "/SEG/Tsistema/GetData",
        createForm: "/SEG/Tsistema/Create?modal=1",
        safeEditForm: (row, token) =>
            `/SEG/Tsistema/SafeEdit?token=${encodeURIComponent(token || row.EditToken || row.editToken || "")}`,
        updateStatus: "/SEG/Tsistema/UpdateAtivo",
        deleteByToken: "/SEG/Tsistema/DeleteByToken",
        export: "/SEG/Tsistema/Export"
    },

    // CONFIGURAÇÃO CORRIGIDA DAS COLUNAS
    columns: [
        {
            title: "Código",
            data: "cdsistema", // MINÚSCULO - padrão JSON
            className: "text-center",
            width: "10%",
            render: function (data, type, row) {
                return data ? `<code class="text-primary">${data}</code>` : '';
            }
        },

        {
            title: "Descrição",
            data: "dcsistema", // MINÚSCULO - padrão JSON
            width: "60%",
            render: function (data, type, row) {
                if (!data) return '<em class="text-muted">Sem descrição</em>';

                if (data.length > 50) {
                    return `<span title="${data}" class="text-truncate d-inline-block" style="max-width: 300px;">
                        ${data}
                    </span>`;
                }
                return data;
            }
        },

        {
            title: "Status",
            data: "ativo", // MINÚSCULO - padrão JSON
            orderable: false,
            searchable: false,
            className: "text-center",
            width: "15%",
            render: function (data, type, row) {
                // Usar nomes minúsculos (padrão JSON)
                const id = row.cdsistema || '';
                const ativo = data === true || data === 'true';

                const checked = ativo ? 'checked' : '';
                const badgeClass = ativo ? 'bg-success' : 'bg-secondary';
                const badgeText = ativo ? 'Ativo' : 'Inativo';

                return `
                    <div class="d-flex flex-column align-items-center gap-1">
                        <div class="form-check form-switch">
                            <input type="checkbox" class="form-check-input crud-toggle" 
                                   data-id="${id}" data-field="ativo" ${checked} 
                                   role="switch" />
                        </div>
                        <span class="badge ${badgeClass} badge-sm">${badgeText}</span>
                    </div>
                `;
            }
        },

        {
            title: "Ações",
            data: null,
            orderable: false,
            searchable: false,
            className: "text-center",
            width: "15%",
            render: function (data, type, row) {
                // Usar nomes minúsculos (padrão JSON)
                const editToken = row.editToken || '';
                const deleteToken = row.deleteToken || '';

                return `
                    <div class="btn-group btn-group-sm" role="group">
                        <button class="btn btn-outline-primary" 
                                data-open-edit 
                                data-edit-token="${editToken}"
                                title="Editar Sistema"
                                ${!editToken ? 'disabled' : ''}>
                            <i class="ti ti-edit"></i>
                        </button>
                        <button class="btn btn-outline-danger" 
                                data-del-token="${deleteToken}"
                                title="Excluir Sistema"
                                ${!deleteToken ? 'disabled' : ''}>
                            <i class="ti ti-trash"></i>
                        </button>
                    </div>
                `;
            }
        }
    ],

    // Textos localizados
    texts: {
        newTitle: "Novo Sistema",
        editTitle: "Editar Sistema",
        confirmDelete: "Tem certeza que deseja excluir este sistema?",
        saved: "Sistema salvo com sucesso",
        deleted: "Sistema excluído com sucesso",
        updated: "Status do sistema atualizado",
        loading: "Carregando sistemas...",
        error: "Erro ao processar operação",
        noData: "Nenhum sistema encontrado"
    },

    // CONFIGURAÇÃO CORRIGIDA DA DATATABLE
    tableOptions: {
        pageLength: 25,
        order: [[0, 'asc']],
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.8/i18n/pt-BR.json',
            emptyTable: "Nenhum sistema encontrado",
            loadingRecords: "Carregando sistemas...",
            processing: "Processando...",
            search: "Pesquisar:",
            lengthMenu: "Mostrar _MENU_ registros por página",
            info: "Mostrando _START_ a _END_ de _TOTAL_ registros",
            infoEmpty: "Mostrando 0 a 0 de 0 registros",
            infoFiltered: "(filtrado de _MAX_ registros)",
            paginate: {
                first: "Primeiro",
                last: "Último",
                next: "Próximo",
                previous: "Anterior"
            }
        },
        responsive: true,
        processing: true,
        serverSide: false,

        // Layout da tabela
        dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>rt<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',

        // Configurações das colunas
        columnDefs: [
            { targets: [0], width: "10%" },      // Código
            { targets: [1], width: "60%" },      // Descrição  
            { targets: [2], width: "15%" },      // Status
            { targets: [3], width: "15%" },      // Ações
            { targets: [2, 3], orderable: false } // Status e Ações não ordenáveis
        ],

        // REMOVER CONFIGURAÇÃO AJAX DUPLICADA - deixar só no page-factory
        // A configuração AJAX será feita automaticamente pelo page-factory
    },

    // Hooks para customização
    hooks: {
        beforeSave: async (data, mode) => {
            const descricao = data.Dcsistema || data.dcsistema || data.descricao;
            if (!descricao || descricao.trim().length < 2) {
                throw new Error("Descrição deve ter pelo menos 2 caracteres");
            }
            return true;
        },

        afterSave: (data, mode) => {
            console.log(`Sistema ${mode === 'create' ? 'criado' : 'editado'}:`, data);
        },

        beforeDelete: async (row) => {
            const codigo = row.cdsistema || row.Cdsistema || row.codigo;
            console.log('Tentando excluir sistema:', codigo);
            return true;
        },

        afterDelete: (row) => {
            console.log("Sistema excluído:", row);
        },

        onTableReady: (dataTable) => {
            console.log("Tabela de sistemas inicializada com sucesso");

            // Debug: Log dos dados recebidos
            dataTable.on('xhr', function () {
                var json = dataTable.ajax.json();
                console.log('Dados recebidos do servidor:', json);
            });

            // Adicionar filtro personalizado
            addStatusFilter(dataTable);
        }
    }
};

// Função auxiliar para adicionar filtro de status
function addStatusFilter(dataTable) {
    const filterContainer = document.querySelector('#tblTsistema_wrapper .row:first-child .col-md-6:last-child');

    if (filterContainer) {
        const filterHtml = `
            <div class="d-flex align-items-center gap-2 mt-2">
                <label class="form-label mb-0">Status:</label>
                <select class="form-select form-select-sm" id="statusFilter" style="width: auto;">
                    <option value="">Todos</option>
                    <option value="ativo">Ativo</option>
                    <option value="inativo">Inativo</option>
                </select>
            </div>
        `;

        filterContainer.insertAdjacentHTML('beforeend', filterHtml);

        document.getElementById('statusFilter').addEventListener('change', function () {
            const value = this.value;
            let searchValue = '';

            if (value === 'ativo') {
                searchValue = 'checked';
            } else if (value === 'inativo') {
                searchValue = '^((?!checked).)*$';
            }

            dataTable.column(2).search(searchValue, true, false).draw();
        });
    }
}