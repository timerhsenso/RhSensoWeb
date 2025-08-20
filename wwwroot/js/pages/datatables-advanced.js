(function () {
    var exportTitle = document.title || 'Export';

    var table = $('#grid').DataTable({
        processing: true,
        serverSide: true,
        ajax: {
            url: '/DataTableAdvanced/GetData',
            type: 'POST'
        },
        columns: [
            { data: 'codigo',   name: 'Codigo' },
            { data: 'nome',     name: 'Nome' },
            { data: 'email',    name: 'Email' },
            { data: 'status',   name: 'Status' },
            { data: 'criadoEm', name: 'CriadoEm' }
        ],
        responsive: true,
        colReorder: true,
        stateSave: true,
        stateDuration: -1,
        dom: "<'row g-2 align-items-center'<'col-md-6'f><'col-md-6 text-md-end'B>>" +
             "tr" +
             "<'row mt-2'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>",
        language: {
            search: "",
            searchPlaceholder: "Pesquisar..."
        },
        buttons: [
            { extend: 'colvis', className: 'btn btn-secondary', text: '<i class="fa fa-columns"></i> Colunas' },
            {
                extend: 'collection',
                className: 'btn btn-primary',
                text: '<i class="fa fa-download"></i> Exportar',
                autoClose: true,
                buttons: [
                    { extend: 'excelHtml5', title: exportTitle, exportOptions: { columns: ':visible' } },
                    { extend: 'csvHtml5',   title: exportTitle, exportOptions: { columns: ':visible' } },
                    { extend: 'pdfHtml5',   title: exportTitle, orientation: 'landscape', pageSize: 'A4', exportOptions: { columns: ':visible' } },
                    { extend: 'print',      title: exportTitle, exportOptions: { columns: ':visible' } }
                ]
            },
            { text: '<i class="fa fa-star-o"></i> <span class="d-none d-sm-inline">Favoritar</span>',
              className: 'btn btn-outline-warning',
              attr: { id: 'btnFavorite' },
              action: function () { toggleFavorite(); }
            }
        ],
        initComplete: function () {
            $(document).on('click', function () {
                $('.dt-button-collection').css({ right: 0, left: 'auto' });
            });
            updateFavUI();
        }
    });

    var favKey = 'fav:' + window.location.pathname;

    function updateFavUI() {
        var favored = localStorage.getItem(favKey) === '1';
        var $btn = $('#btnFavorite');
        if (favored) {
            $btn.addClass('active btn-warning').removeClass('btn-outline-warning');
            $btn.html('<i class="fa fa-star"></i> <span class="d-none d-sm-inline">Favorito</span>');
            $btn.attr('title', 'Remover dos favoritos');
        } else {
            $btn.removeClass('active btn-warning').addClass('btn-outline-warning');
            $btn.html('<i class="fa fa-star-o"></i> <span class="d-none d-sm-inline">Favoritar</span>');
            $btn.attr('title', 'Favoritar esta tela');
        }
    }

    function toggleFavorite() {
        var favored = localStorage.getItem(favKey) === '1';
        localStorage.setItem(favKey, favored ? '0' : '1');
        updateFavUI();
    }
})();