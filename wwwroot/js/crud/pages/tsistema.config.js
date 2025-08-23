// Config robusta para Tsistema (case-insensitive e com tokens)
export default {
    area: "SEG",
    entity: "Tsistema",

    // IDs que já existem na sua view/layout
    selectors: { table: "#tblTsistema", modal: "#formModal", newButton: "#BtnCreateNew" },

    // Endpoints (iguais aos que você já usa)
    endpoints: {
        list: "/SEG/Tsistema/GetData",
        createForm: "/SEG/Tsistema/Create?modal=1",
        safeEditForm: (row, token) => `/SEG/Tsistema/SafeEdit?token=${encodeURIComponent(token || row.EditToken || row.editToken || "")}`,
        updateAtivo: "/SEG/Tsistema/UpdateAtivo",
        deleteByToken: "/SEG/Tsistema/DeleteByToken"
    },

    // chave e nome do parâmetro que o UpdateAtivo espera
    key: "Cdsistema",
    keyParamName: "id",

    // Colunas tolerantes ao case, sem depender do builtins
    columns: [
        {
            title: "Código",
            data: (row) => row.Cdsistema ?? row.cdsistema ?? ""
        },
        {
            title: "Descrição",
            data: (row) => row.Dcsistema ?? row.dcsistema ?? ""
        },
        {
            title: "Ativo",
            orderable: false,
            data: null,
            render: (_, __, row) => {
                const id = row.Cdsistema ?? row.cdsistema ?? "";
                const ativo = (row.Ativo ?? row.ativo) ? "checked" : "";
                return `<input type="checkbox" class="form-check-input crud-toggle"
                       data-id="${id}" ${ativo} />`;
            }
        },
        {
            title: "Ações",
            orderable: false,
            data: null,
            render: (_, __, row) => {
                const editToken = row.EditToken ?? row.editToken ?? "";
                const deleteToken = row.DeleteToken ?? row.deleteToken ?? "";
                return `<div class="btn-group btn-group-sm">
                  <button class="btn btn-primary" data-open-edit data-edit-token="${editToken}">Editar</button>
                  <button class="btn btn-danger"  data-del-token="${deleteToken}">Excluir</button>
                </div>`;
            }
        }
    ],

    texts: {
        newTitle: "Novo Sistema",
        editTitle: "Editar Sistema",
        confirmDelete: "Confirma excluir este registro?",
        saved: "Registro salvo com sucesso.",
        deleted: "Excluído com sucesso."
    }
};
