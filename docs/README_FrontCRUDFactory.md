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