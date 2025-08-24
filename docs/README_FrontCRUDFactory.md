# Fábrica Genérica de CRUD (Front-end)
Consulte aqui como configurar a Page Factory, endpoints e colunas. Inclua a partial `_AjaxSetup.cshtml` no layout da área e inicialize a página com:
```cshtml
@section Scripts {
  <script type="module">
    import { createCrudPage } from "/js/crud/page-factory.js";
    import config from "/js/crud/pages/tsistema.config.js";
    createCrudPage(config);
  </script>
}
```



# Estrutura MVC Otimizada

## 1. _TitleMeta.cshtml (SEM MUDANÇAS - está correto)
```html
@inject Microsoft.AspNetCore.Antiforgery.IAntiforgery Xsrf
@{
    var tokens = Xsrf.GetAndStoreTokens(Context);
}

<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">

@{
    var title = ViewBag.Title != null
    ? ViewBag.Title + " | RhSensoWEB - Responsive Bootstrap 5 Admin Dashboard"
    : "RhSensoWEB - Responsive Bootstrap 5 Admin Dashboard";
}

<title>@title</title>

<meta name="description" content="Inspinia is the #1 best-selling admin dashboard template on WrapBootstrap...">
<meta name="keywords" content="Inspinia, admin dashboard, WrapBootstrap...">
<meta name="author" content="WebAppLayers">
<meta name="request-verification-token" content="@tokens.RequestToken" />

<!-- App favicon -->
<link rel="shortcut icon" href="/images/favicon.ico">

<!-- Vendor css -->
<link href="~/css/vendors.min.css" rel="stylesheet" type="text/css">

<!-- App css -->
<link href="~/css/app.min.css" rel="stylesheet" type="text/css">
```

## 2. _HeadCSS.cshtml (SIMPLIFICADO - apenas configuração AJAX)
```html
<script type="module">
    import { ensureAjaxSetup } from "/js/core/http.js";
    ensureAjaxSetup();
</script>
```

## 3. _FooterScripts.cshtml (ORDEM CORRIGIDA)
```html
<!-- Theme Config Js -->
<script src="~/js/config.js"></script>

<!-- Vendors (jQuery, Bootstrap, etc.) -->
<script src="~/js/vendors.min.js"></script>

<!-- App Core -->
<script src="~/js/app.js"></script>

<!-- Security Setup -->
<script>
    (function () {
        var token = document.querySelector('meta[name="request-verification-token"]')?.getAttribute('content');
        if (!token) return;
        
        // jQuery AJAX Setup
        if (window.$ && $.ajaxSetup) {
            $.ajaxSetup({
                headers: { 'RequestVerificationToken': token }
            });
        }
        
        // Secure Fetch Wrapper
        window.secureFetch = async function (url, options) {
            options = options || {};
            options.headers = Object.assign({}, options.headers, { 'RequestVerificationToken': token });
            return fetch(url, options);
        };
    })();
</script>
```

## 4. Index.cshtml (LIMPO E ORGANIZADO)
```razor
@{
    ViewBag.Areas = "SEG";
    ViewBag.Views = "Tsistema";
    ViewBag.Controller = "Tsistema";
    ViewBag.Icon = "monitor-smartphone";
    ViewBag.Title = "Tabela de Sistemas";
    ViewBag.SubTitle = "Segurança";
    ViewBag.HabilitaBtnNovo = true;
    ViewBag.HabilitaBtnExportar = true;
    Layout = "~/Views/Shared/_VerticalLayout.cshtml";
}

@section Styles {
    <!-- DataTables CSS -->
    <link rel="stylesheet" href="https://cdn.datatables.net/1.13.8/css/dataTables.bootstrap5.min.css" />
    <link rel="stylesheet" href="https://cdn.datatables.net/responsive/2.5.0/css/responsive.bootstrap5.min.css" />
    <link rel="stylesheet" href="https://cdn.datatables.net/buttons/2.4.2/css/buttons.bootstrap5.min.css" />

    <!-- Custom CRUD Styles -->
    <link href="/css/crud.css" rel="stylesheet" asp-append-version="true" />
    <link href="~/css/datatables-advanced.css" rel="stylesheet" />

    <!-- Page-Specific Styles -->
    <style>
        .crud-actions { white-space: nowrap; }
        .status-badge { 
            font-size: 0.75rem; 
            padding: 0.25rem 0.5rem; 
        }
        .table-responsive {
            border-radius: 0.375rem;
            box-shadow: 0 0.125rem 0.25rem rgba(0, 0, 0, 0.075);
        }
        .custom-filters {
            background: #f8f9fa;
            padding: 1rem;
            border-radius: 0.375rem;
            margin-bottom: 1rem;
            border: 1px solid #dee2e6;
        }
        .filter-group {
            display: flex;
            gap: 0.5rem;
            align-items-end;
            flex-wrap: wrap;
        }
        .filter-item { min-width: 120px; }
        .loading-overlay {
            position: absolute;
            top: 0; left: 0; right: 0; bottom: 0;
            background: rgba(255, 255, 255, 0.8);
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 1000;
        }
        .loading-overlay.d-none { display: none !important; }
        
        /* Remove breadcrumb separator only on this page */
        .breadcrumb-item + .breadcrumb-item::before { content: ""; }
    </style>
}

<!-- Toast Container -->
@await Html.PartialAsync("~/Views/Shared/Partials/_ToastContainer.cshtml")

<!-- Page Title -->
@await Html.PartialAsync("~/Views/Shared/Partials/_PageTitle.cshtml")

<!-- Top Bar -->
@await Html.PartialAsync("~/Views/Shared/Partials/_TopBarShearhGrid.cshtml")

<!-- Custom Filters -->
<div class="container-fluid">
    <div class="custom-filters d-none" id="customFilters">
        <!-- [Filtros personalizados aqui - mantém o código atual] -->
    </div>
</div>

<!-- Main Table Container -->
<div class="container-fluid">
    <div class="card">
        <div class="card-header d-flex justify-content-between align-items-center">
            <!-- [Header da tabela - mantém o código atual] -->
        </div>
        
        <div class="card-body position-relative">
            <!-- Loading Overlay -->
            <div class="loading-overlay d-none" id="loadingOverlay">
                <div class="text-center">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Carregando...</span>
                    </div>
                    <p class="mt-2 text-muted">Carregando dados...</p>
                </div>
            </div>

            <!-- Main Table -->
            <div class="table-responsive">
                <table id="tblTsistema" class="table table-striped table-hover w-100">
                    <thead class="table-light">
                        <tr>
                            <th width="10%">Código</th>
                            <th>Descrição</th>
                            <th width="12%" class="text-center">Status</th>
                            <th width="120px" class="text-center">Ações</th>
                        </tr>
                    </thead>
                </table>
            </div>

            <!-- Additional Info -->
            <div class="row mt-3">
                <div class="col-md-6">
                    <small class="text-muted">
                        <i class="ti ti-info-circle me-1"></i>
                        Use Ctrl+N para novo registro, F5 para atualizar
                    </small>
                </div>
                <div class="col-md-6 text-end">
                    <small class="text-muted" id="tableInfo">
                        <!-- Será preenchido pelo JavaScript -->
                    </small>
                </div>
            </div>
        </div>
    </div>
</div>

<!-- Modals -->
<!-- [Mantém os modais atuais] -->

@section Scripts {
    <!-- DataTables JavaScript -->
    <script src="https://cdn.datatables.net/1.13.8/js/jquery.dataTables.min.js"></script>
    <script src="https://cdn.datatables.net/1.13.8/js/dataTables.bootstrap5.min.js"></script>
    <script src="https://cdn.datatables.net/responsive/2.5.0/js/dataTables.responsive.min.js"></script>
    <script src="https://cdn.datatables.net/responsive/2.5.0/js/responsive.bootstrap5.min.js"></script>
    <script src="https://cdn.datatables.net/buttons/2.4.2/js/dataTables.buttons.min.js"></script>
    <script src="https://cdn.datatables.net/buttons/2.4.2/js/buttons.bootstrap5.min.js"></script>

    <!-- Form Validation -->
    @await Html.PartialAsync("_ValidationScriptsPartial")

    <!-- CRUD Initialization -->
    <script type="module">
        import { createCrudPage } from "/js/crud/page-factory.js";
        import config from "/js/crud/pages/tsistema.config.js";

        window.addEventListener('DOMContentLoaded', () => {
            // Verify dependencies
            if (typeof $ === 'undefined') {
                console.error('❌ jQuery not loaded!');
                alert('Erro: jQuery não foi carregado.');
                return;
            }
            
            if (typeof $.fn.DataTable === 'undefined') {
                console.error('❌ DataTables not loaded!');
                alert('Erro: DataTables não foi carregado.');
                return;
            }

            try {
                // Create CRUD instance
                const crudInstance = createCrudPage(config);

                // Global exposure for debugging
                window.crudInstance = crudInstance;
                window.dataTable = crudInstance.dataTable;

                // Setup UI events
                setupUIEvents(crudInstance);

                console.log('✅ CRUD initialized successfully');
            } catch (error) {
                console.error('❌ Error initializing CRUD:', error);
                alert('Erro ao carregar a página. Tente recarregar.');
            }
        });

        // [Mantém as funções setupUIEvents, showLoading, etc. - código atual está correto]
    </script>
}
```

## 5. _VerticalLayout.cshtml (SEM MUDANÇAS - remover style duplicado)
```razor
<!-- Remover este bloco duplicado: -->
<!--
<style>
    .breadcrumb-item + .breadcrumb-item::before {
        content: "";
    }
</style>
-->
```

---

## Melhorias Implementadas:

### ✅ **Eliminação de Duplicações:**
- DataTables carregado apenas no Index.cshtml
- Configuração AJAX consolidada no _FooterScripts.cshtml
- Estilos breadcrumb apenas no Index.cshtml

### ✅ **Ordem de Carregamento Otimizada:**
1. _TitleMeta.cshtml: Meta tags + CSS básico
2. Index.cshtml @section Styles: CSS específico da página
3. _HeadCSS.cshtml: Configuração AJAX mínima
4. _FooterScripts.cshtml: Scripts base na ordem correta
5. Index.cshtml @section Scripts: Scripts específicos da página

### ✅ **Benefícios da Nova Estrutura:**
- Sem duplicações de código
- Carregamento mais rápido
- Melhor organização
- Fácil manutenção
- Debug mais simples

### ✅ **Validações Adicionadas:**
- Verificação se jQuery foi carregado
- Verificação se DataTables foi carregado
- Mensagens de erro mais claras

Esta estrutura elimina as duplicações e mantém a funcionalidade, com melhor performance e organização do código.