# RhSensoWeb

Este é um projeto ASP.NET Core MVC (.NET 8) modular, projetado para ser a base de um ERP corporativo.

## Estrutura do Projeto

O projeto segue uma estrutura modular com Áreas, separando funcionalidades por módulos (ex: RHU, FRE, SEG). Cada área contém seus próprios `Controllers`, `Models`, `Views`, `DTOs`, `Services`, `Repositories`, `Middleware` e `Helpers`.

## Configuração

### Pré-requisitos

- .NET SDK 8.0 ou superior
- SQL Server (ou outro banco de dados configurado na `ConnectionStrings`)

### Configuração do Banco de Dados

Atualize a `ConnectionStrings` no arquivo `appsettings.json` com as suas credenciais do SQL Server:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=1x2.xxx.x7.2;Database=bd_rhu_copenor;User Id=sa;Password=*#@0;TrustServerCertificate=true;"
}
```

### Executar o Projeto

Para executar o projeto localmente, navegue até a pasta `ERPModular` no terminal e execute:

```bash
dotnet run
```

O aplicativo estará disponível em `https://localhost:7000` (ou outra porta configurada).

## Testes Automatizados

O projeto inclui um projeto de testes unitários (`RhSensoWeb.Tests`). Para executar os testes, navegue até a raiz do projeto (`/home/ubuntu/RhSensoWeb`) e execute:

```bash
dotnet test
```

## CI/CD (Integração Contínua / Entrega Contínua)

Este projeto está configurado com um pipeline de CI/CD usando GitHub Actions. O workflow está definido no arquivo `.github/workflows/ci-cd.yml`.

### Build e Teste

O pipeline `build-and-test` é acionado em cada `push` e `pull_request` para a branch `main`. Ele executa as seguintes etapas:

1.  **Checkout do Código:** Clona o repositório.
2.  **Setup .NET:** Configura o ambiente .NET 8.
3.  **Restore dependencies:** Restaura as dependências do projeto.
4.  **Build:** Compila o projeto.
5.  **Test:** Executa os testes unitários definidos no projeto `ERPModular.Tests`.

Para visualizar o status do pipeline, acesse a aba "Actions" no seu repositório GitHub.

### Deploy (Exemplo Comentado)

Uma seção de `deploy` está incluída no workflow de CI/CD, mas está **comentada** por padrão. Este é um exemplo de como você poderia configurar o deploy para um serviço como o Azure Web Apps. Para habilitá-lo, você precisaria:

1.  Descomentar a seção `deploy` no arquivo `.github/workflows/ci-cd.yml`.
2.  Configurar as variáveis de ambiente e segredos necessários (ex: `AZURE_WEBAPP_PUBLISH_PROFILE`).

**Observação:** O deploy automatizado é um exemplo e deve ser adaptado às suas necessidades de infraestrutura e ambiente de deploy.


