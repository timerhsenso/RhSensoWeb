# Prompt de Contexto — Projeto **RhSensoWeb** (ASP.NET Core MVC • .NET 8 • SQL Server 2019)

Você vai continuar o desenvolvimento de um ERP corporativo chamado **RhSensoWeb**. Abaixo está **apenas o contexto do sistema**: o que ele é, o que cada parte faz e **onde** ficam as coisas no projeto. **Não proponha mudanças** — use este contexto para compreender o padrão existente e seguir a mesma linha.

---

## 1) Visão geral do sistema

- **Nome**: RhSensoWeb  
- **Stack**: ASP.NET Core **MVC** (.NET 8), SQL Server 2019, Bootstrap 5, AdminLTE, jQuery, DataTables 2.x, módulos JS próprios (ES Modules).  
- **Objetivo**: ERP modular para ambiente corporativo/industrial (RH, segurança do trabalho, portaria/controle de acesso, fornecedores, treinamentos, EPI/fardamento, etc.).  
- **Multi-empresa/filial**: tabelas costumam incluir `cdempresa (int)`, `cdfilial (int)` e `nomatric (char/varchar)` quando o assunto é colaborador; visão futura de **SaaS** com campo `IdSaas`.  
- **Áreas principais (exemplos)**:  
  - **SEG** (Segurança/Sistema): login, grupos, permissões, funções, botões.  
  - Outras áreas planejadas/existentes conforme módulos (RHU, SDT, EPI, etc.).  
- **Padrão visual**: **AdminLTE + Bootstrap 5**; layout com **menu lateral**, **topbar** com menu do usuário (Perfil, Alterar Senha, Alterar Layout, Sair), **cards**, **gráficos** e **DataTables**.

---

## 2) Estrutura de pastas (padrão do projeto)

- **`/Areas/<AREA>/Controllers`**  
  Controllers MVC de cada área (ex.: `Areas/SEG/Controllers/TsistemaController.cs`, `BtfuncaoController.cs`, `UsuarioController.cs` etc.).

- **`/Areas/<AREA>/Models`**  
  Models das entidades mapeadas para o SQL Server (ex.: `Tsistema`, `Tuse1`, `Usrh1`, `Gurh1`, `Fucn1`, `Hbrh1`, `Btfuncao`…).

- **`/Areas/<AREA>/Services` e `/Areas/<AREA>/Repositories`**  
  Serviços e repositórios por área quando aplicável (ex.: `LoginService`, `PermissionService`, etc.).  
  Observação: alguns serviços são **compartilhados** (ex.: permissões, sessão, auditoria) e podem estar fora de Areas conforme convenção do projeto.

- **`/Areas/<AREA>/Views/<Entidade>`**  
  Views MVC por entidade (ex.: `Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Details.cshtml`, além de parciais como `_FormFields.cshtml`).

- **`/Views/Shared`**  
  - **Layouts** (ex.: `_VerticalLayout.cshtml`) — layout base **AdminLTE/Bootstrap**.  
  - **Parciais compartilhadas** (ex.: `~/Views/Shared/Partials/_TopBarShearhGrid.cshtml`, `_ExportButtons.cshtml`).

- **`/wwwroot/css`**  
  CSS do projeto (ex.: `crud.css` e folhas específicas).

- **`/wwwroot/js`**  
  Código front-end:
  - **`/wwwroot/js/core`**: módulos **ES Modules** reutilizáveis do projeto:  
    - `http.js` (setup global de AJAX/fetch, cabeçalhos, antiforgery) — função **`ensureAjaxSetup()`**.  
    - `json.js` (utilitários como `pick`).  
    - `modal.js` (carregar HTML em modal e fechar — `loadIntoModal`, `closeModal`).  
    - `forms.js` (binding de formulários AJAX — `bindAjaxForm`).  
    - `table.js` (fábrica/config de DataTables — `createCrudTable`).  
    - `notify.js` (notificações).  
  - **Outros JS específicos de telas** ficam ao lado da view ou organizados por módulo conforme necessidade.

- **`/Data`**  
  - `ApplicationDbContext.cs` — **EF Core DbContext** central, mapeando as DbSets principais (ex.: `Gurh1`, `Tuse1`, `Usrh1`, `Fucn1`, `Hbrh1`, `Tsistema`, `Const1`, e outros).  
  - **Boa prática do projeto**: **DbContext centralizado** e organização por **área** nas pastas de MVC/Services/Views.

- **Arquivos de configuração**  
  - `appsettings.json` / `appsettings.<Ambiente>.json` — connection strings e configs.  
  - `Program.cs` — registro de serviços DI, autenticação Cookie, Session, middlewares, rotas (com **Areas**).

---

## 3) Banco de dados (SQL Server 2019)

- **Tabelas base de segurança/permissões** (SEG):  
  - **`tuse1`**: usuários (login/senha e metadados).  
  - **`usrh1`**: vínculo usuário × grupo.  
  - **`gurh1`**: grupos/roles do sistema.  
  - **`fucn1`**: funções/telas/módulos que um grupo pode acessar.  
  - **`hbrh1`**: granularidade de permissões (ações/botões vinculados às funções).  
  - **`btfuncao`**: definição e vínculo de **botões por função** (ex.: habilitar/ocultar botões).
- **Outras tabelas exemplo**:  
  - **`tsistema`**: cadastro de sistemas/módulos (código/descrição…).  
  - **`func1`**: colaboradores (com `nomatric`, `cdempresa`, `cdfilial`), base para integrações de RH.
- **Padrões de campos**: seguir nomenclaturas legadas (ex.: `Cdsistema`, `Dcsistema`, `Nomatric`), incluindo validações por `DataAnnotations` nas **Models**.

---

## 4) Autenticação, sessão e permissões

- **Autenticação**: **Cookies** (ASP.NET Core). Login via **`tuse1`**.  
- **Serviços** típicos:
  - **`LoginService`**: autenticação, bloqueio por tentativas, força de senha, logging de eventos.
  - **`PermissionService`**: carrega permissões do usuário (menus/telas/botões), expõe helpers para verificação por módulo/ação.
  - **`IAuditService`** (quando presente): registra auditoria e eventos.
- **Sessão**: dados como **`UserSessionDto`**, **`UserPermissions`**, **`MenuPermissions`**, **`GrupoUsuario`** são serializados em **Session** (JSON) após o login e utilizados para renderização condicional (menus e **botões**).
- **Middleware**:
  - **`GroupValidationMiddleware`**: valida se o usuário autenticado possui **grupo** válido (lê dos **claims** gravados no login; integra com `PermissionService`).
- **Autorização na View**:  
  - Menus e **botões (ex.: Novo/Salvar/Excluir)** são exibidos/ocultados conforme as permissões em sessão.  
  - Há exemplos de **helpers**/checks para “HasMenuPermission/HasButtonPermission”.

---

## 5) Padrão de Controllers/Views (CRUD + DataTables)

- **Controllers** (ex.: `TsistemaController`, `BtfuncaoController`, `UsuarioController`):  
  - Ações típicas: `Index` (lista com **DataTables**), `GetData` (AJAX JSON para DataTables), `Create`/`Edit` (GET/POST), `Delete` (POST), endpoints auxiliares para **combos dependentes** e **toggles** (ex.: ativar/desativar).  
  - **[ValidateAntiForgeryToken]** aplicado em POSTs.

- **Views**:
  - **Layout**: `_VerticalLayout.cshtml` (AdminLTE/Bootstrap).  
    - Renderiza `@RenderSection("styles", required: false)` e `@RenderSection("scripts", required: false)`.  
    - Topbar do usuário (Perfil, Alterar Senha, Alterar Layout, Sair).  
  - **Index**: grade com **DataTables** (responsivo; exportações; colvis; colreorder, conforme tela).  
  - **Parciais**:  
    - `_FormFields.cshtml` para campos de Create/Edit.  
    - **Topbar/Toolbar** compartilhada: `~/Views/Shared/Partials/_TopBarShearhGrid.cshtml` (barra de pesquisa/ações da lista).  
    - **Botões de exportação**: `~/Views/Shared/Partials/_ExportButtons.cshtml`.

- **Seções por View**:
  - `@section styles { ... }` — CSSs/links adicionais da tela.  
  - `@section scripts { ... }` — jQuery, DataTables e JS da tela (ordem **importa**; ver seção 6).

---

## 6) Front-end: DataTables, bibliotecas e ordem de scripts

- **Bibliotecas usadas (CDNs)**, tipicamente em `@section scripts`:
  - **jQuery** (3.7.x)  
  - **DataTables 2.x** com extensões necessárias (Buttons, HTML5, Print, Responsive, ColVis/ColReorder, etc.)  
  - **JSZip** (para Excel)  
  - **pdfmake** (para PDF)  
- **CSS DataTables** é incluído em `@section styles`.  
- **Ordem típica de carregamento** (importante para a grade funcionar):
  1) jQuery  
  2) DataTables (bundle com as extensões utilizadas)  
  3) JSZip  
  4) pdfmake (+ vfs_fonts, quando necessário)  
  5) Scripts do projeto (módulos **ES Modules** e scripts da tela)
- **Padrão de inicialização**:  
  - O projeto usa **módulos ES** e funções utilitárias em `/wwwroot/js/core`.  
  - Exemplo comum no topo da view:  
    ```html
    <script type="module">
      import { ensureAjaxSetup } from "/js/core/http.js";
      ensureAjaxSetup(); // configura cabeçalhos AJAX + AntiForgery
      // demais imports e inicializações da grade/tela…
    </script>
    ```
- **Componentização de colunas (JS)**:  
  - Utilitário `builtins` (ex.: `key`, `text`, `toggle`, `actions`) para padronizar renderização de colunas na DataTable (checkbox/toggle, ações, etc.).

---

## 7) Anti-forgery e AJAX

- **Na View/Layout**: é gerado um **meta** com o token AntiForgery:  
  ```cshtml
  @inject Microsoft.AspNetCore.Antiforgery.IAntiforgery Xsrf
  @{ var tokens = Xsrf.GetAndStoreTokens(Context); }
  <meta name="request-verification-token" content="@tokens.RequestToken" />
No JS: ensureAjaxSetup() (em /js/core/http.js) lê esse meta e injeta o header apropriado (ex.: RequestVerificationToken) em fetch/AJAX do projeto.

8) Rotas e Áreas
Padrão de roteamento com Areas habilitadas em Program.cs.

URL exemplo:

/SEG/Tsistema → Areas/SEG/Controllers/TsistemaController.cs → Index()

Controllers e Views seguem convenção MVC dentro de cada Area.

9) Layout, menus e usuário (UI/UX)
Layout base: _VerticalLayout.cshtml

Menu lateral (AdminLTE), topbar com menu de usuário (Perfil, Alterar Senha, Alterar Layout, Sair).

Responsividade baseada em Bootstrap 5.

Ambiente: as Views podem injetar IWebHostEnvironment para exibir o EnvironmentName quando necessário (ex.: HOMOLOGAÇÃO/PRODUÇÃO).

Botões padrão (exibição sujeita às permissões): btnNovo, btnSalvar, btnExcluir, btnPesquisar, etc.

10) Telas/Entidades de referência (SEG)
Tsistema

Local: Areas/SEG/Models/Tsistema.cs

Controller: Areas/SEG/Controllers/TsistemaController.cs

Views: Areas/SEG/Views/Tsistema/ (Index, Create, Edit, parciais)

Uso: cadastro de sistemas/módulos; já serve como padrão de CRUD (validações por DataAnnotations, DataTable com AJAX, exportações).

Btfuncao (Botões por Função)

Local: Areas/SEG/Models/Btfuncao.cs

Controller/Views: em Areas/SEG/.../Btfuncao*

Uso: vincula botões a funções (telas), controlando habilitação/visibilidade por permissão.

Usuario (usuários do sistema)

Local: Areas/SEG/Models/Tuse1.cs (modelo de usuário legado) + lógicas em UsuarioController e serviços.

Relacionamentos: usrh1 (usuário×grupo), gurh1 (grupos), fucn1/hbrh1 (funções e ações/botões).

11) Sessão, claims e renderização condicional
Após login, o grupo do usuário e suas permissões são armazenados em claims e Session.

Menus e botões nas Views verificam os dados da sessão para exibir/ocultar elementos da UI.

Páginas de listagem (DataTables) e telas de formulário respeitam essas permissões (ex.: ocultar btnExcluir se o usuário não tiver a ação).

12) Logs e diagnóstico (quando aplicável)
O projeto contempla logging detalhado (ex.: via Serilog/ILogger) e captura de exceções, com mensagens padronizadas; pode gravar eventos de segurança, tentativas de login, errors internos ("001 - Erro interno do sistema", etc.).

Também há auditoria planejada/implementada em serviços específicos (dependendo do módulo).

13) Conexão com o banco
Connection strings ficam em appsettings*.json.

EF Core configurado em Program.cs com ApplicationDbContext.

Pacote: Microsoft.Data.SqlClient como provider de banco.

Comentários/Extended Properties no SQL são adotados para documentar tabelas/campos.

14) Convenções de UI e reutilização
Toolbar (Topbar) de grids: parcial compartilhada ~/Views/Shared/Partials/_TopBarShearhGrid.cshtml.

Exportações (PDF/Excel/Impressão): DataTables Buttons usando pdfmake e xlsx; há parcial _ExportButtons.cshtml para reaproveitar.

Combos dependentes (ex.: Sistema → Função) e toggles (checkbox de ativo) são padrões recorrentes com endpoints específicos no Controller.

15) Segurança no front-end
AntiForgery: meta tag + header em todas as requisições POST/AJAX via ensureAjaxSetup().

jQuery Validate + Unobtrusive: reativação após carregamento dinâmico (ex.: em modais) para manter validações no client, com normalizer para trim() de campos obrigatórios.

16) Rotina de logout
Opção Sair no menu do usuário: remove sessão e cookies de autenticação, limpa cookies de login e redireciona para a tela de login, impedindo retorno via botão “voltar”.

17) Resumo de como “tudo se conecta”
Usuário acessa /SEG/Tsistema (exemplo).

Controller TsistemaController.Index() renderiza a View Index.cshtml usando _VerticalLayout.cshtml.

A View inclui a Topbar parcial e inicializa a DataTable.

A DataTable chama via AJAX o endpoint GetData do mesmo Controller.

O JS core (módulos) garante os headers e antiforgery (ensureAjaxSetup), cuida de modais, formulários, notificações e padrão visual.

Permissões em sessão/claims definem quais menus/botões aparecem.

POSTs (Create/Edit/Delete/Toggle) validam AntiForgery e aplicam regras de grupo/permissão via serviços do back-end.

Logs/Auditoria registram eventos relevantes.