
function confirmarExclusao(id) {
    $('#deleteId').val(id);
    $('#modalExcluir').modal('show');
}

function excluirRegistro() {
    var id = $('#deleteId').val();
    $.ajax({
        url: '/SGC_ColaboradorTreinamento/DeleteConfirmed',
        type: 'POST',
        data: { id: id },
        success: function (response) {
            if (response.success) {
                $('#modalExcluir').modal('hide');
                location.reload();
            } else {
                alert('Erro ao excluir');
            }
        },
        error: function () {
            alert('Erro ao processar requisição');
        }
    });
}
