-- =============================================
-- Inicialização do Banco de Dados e Configuração de Segurança
-- =============================================
-- Criando o banco de dados PortariaSaaS com suporte a UTF-8 para internacionalização
USE master;
GO

-- Criando o banco de dados se não existir
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'PortariaSaaS')
CREATE DATABASE PortariaSaaS 
    COLLATE Latin1_General_100_CI_AI_SC_UTF8;
GO

-- Selecionando o banco de dados para operações subsequentes
USE PortariaSaaS;
GO

-- Criando uma chave mestra para criptografia transparente de dados (TDE)
CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'ComplexPassword2025#';
GO

-- Criando um certificado para criptografia do banco de dados
CREATE CERTIFICATE CertificadoPortaria 
    WITH SUBJECT = 'Certificado de Criptografia do Banco de Dados Portaria';
GO

-- Habilitando TDE com a chave de criptografia do banco
CREATE DATABASE ENCRYPTION KEY
    WITH ALGORITHM = AES_256
    ENCRYPTION BY SERVER CERTIFICATE CertificadoPortaria;
GO

ALTER DATABASE PortariaSaaS
    SET ENCRYPTION ON;
GO

-- Configurando grupo de arquivos FILESTREAM para armazenar dados binários grandes
ALTER DATABASE PortariaSaaS 
    ADD FILEGROUP FsGroup CONTAINS FILESTREAM;
GO

ALTER DATABASE PortariaSaaS 
    ADD FILE (NAME = 'FsFiles', FILENAME = 'C:\FsData\PortariaSaaS') 
    TO FILEGROUP FsGroup;
GO

-- Comando de backup do certificado (execução manual requerida para recuperação)
-- BACKUP CERTIFICATE CertificadoPortaria TO FILE = 'C:\Backups\CertificadoPortaria.cer' 
-- WITH PRIVATE KEY (FILE = 'C:\Backups\CertificadoPortaria.pvk', ENCRYPTION BY PASSWORD = 'BackupPassword2025#');
GO

-- =============================================
-- Criação de Schemas
-- =============================================
-- Criando schemas para organizar objetos por funcionalidade
CREATE SCHEMA saas;          -- Schema para dados relacionados a tenants
GO
CREATE SCHEMA multitenant;   -- Schema para entidades multitenant como usuários e acessos
GO
CREATE SCHEMA audit;         -- Schema para dados de auditoria e logs
GO
CREATE SCHEMA billing;       -- Schema para dados de faturamento e assinaturas
GO
CREATE SCHEMA security;      -- Schema para políticas e predicados de segurança
GO
CREATE SCHEMA reporting;     -- Schema para visões de relatórios e dashboards
GO

-- =============================================
-- Tabelas Principais
-- =============================================

-- Tabela Tenants
-- Esta tabela armazena os dados dos tenants (empresas) que utilizam o sistema SaaS.
CREATE TABLE saas.Tenants (
    TenantId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada tenant.
    CompanyName NVARCHAR(200) NOT NULL, -- Campo de dados: Nome da empresa do tenant.
    CompanyDocument NVARCHAR(20) NOT NULL, -- Campo de dados: Número do documento legal (ex.: CNPJ) do tenant.
    ContactName NVARCHAR(100) NOT NULL, -- Campo de dados: Nome do contato principal do tenant.
    ContactEmail NVARCHAR(100) NOT NULL, -- Campo de dados: Endereço de e-mail do contato principal.
    ContactPhone NVARCHAR(20) NULL, -- Campo de dados: Número de telefone do contato principal (opcional).
    Address NVARCHAR(200) NULL, -- Campo de dados: Endereço físico do tenant (opcional).
    City NVARCHAR(100) NULL, -- Campo de dados: Cidade do tenant (opcional).
    State NVARCHAR(50) NULL, -- Campo de dados: Estado do tenant (opcional).
    Country NVARCHAR(50) NULL DEFAULT 'Brasil', -- Campo de dados: País do tenant (padrão: Brasil, opcional).
    TimeZone NVARCHAR(50) NULL DEFAULT 'E. South America Standard Time', -- Campo de dados: Fuso horário do tenant (padrão: horário de Brasília).
    LanguageCode NVARCHAR(10) NULL DEFAULT 'pt-BR', -- Campo de dados: Código do idioma da interface (padrão: português do Brasil).
    IsActive BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se o tenant está ativo.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de criação do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da última atualização (opcional).
    DeactivatedAt DATETIME2 NULL -- Campo de controle: Data e hora de desativação (opcional).
);
COMMENT ON TABLE saas.Tenants IS 'Esta tabela armazena os dados dos tenants (empresas) que utilizam o sistema SaaS.';
COMMENT ON COLUMN saas.Tenants.TenantId IS 'Chave primária: Identificador único de cada tenant.';
COMMENT ON COLUMN saas.Tenants.CompanyName IS 'Campo de dados: Nome da empresa do tenant.';
COMMENT ON COLUMN saas.Tenants.CompanyDocument IS 'Campo de dados: Número do documento legal (ex.: CNPJ) do tenant.';
COMMENT ON COLUMN saas.Tenants.ContactName IS 'Campo de dados: Nome do contato principal do tenant.';
COMMENT ON COLUMN saas.Tenants.ContactEmail IS 'Campo de dados: Endereço de e-mail do contato principal.';
COMMENT ON COLUMN saas.Tenants.ContactPhone IS 'Campo de dados: Número de telefone do contato principal (opcional).';
COMMENT ON COLUMN saas.Tenants.Address IS 'Campo de dados: Endereço físico do tenant (opcional).';
COMMENT ON COLUMN saas.Tenants.City IS 'Campo de dados: Cidade do tenant (opcional).';
COMMENT ON COLUMN saas.Tenants.State IS 'Campo de dados: Estado do tenant (opcional).';
COMMENT ON COLUMN saas.Tenants.Country IS 'Campo de dados: País do tenant (padrão: Brasil, opcional).';
COMMENT ON COLUMN saas.Tenants.TimeZone IS 'Campo de dados: Fuso horário do tenant (padrão: horário de Brasília).';
COMMENT ON COLUMN saas.Tenants.LanguageCode IS 'Campo de dados: Código do idioma da interface (padrão: português do Brasil).';
COMMENT ON COLUMN saas.Tenants.IsActive IS 'Campo de controle: Indica se o tenant está ativo.';
COMMENT ON COLUMN saas.Tenants.CreatedAt IS 'Campo de controle: Data e hora de criação do registro.';
COMMENT ON COLUMN saas.Tenants.UpdatedAt IS 'Campo de controle: Data e hora da última atualização (opcional).';
COMMENT ON COLUMN saas.Tenants.DeactivatedAt IS 'Campo de controle: Data e hora de desativação (opcional).';
GO

-- Tabela SubscriptionPlans
-- Esta tabela define os planos de assinatura disponíveis para os tenants.
CREATE TABLE billing.SubscriptionPlans (
    PlanId INT IDENTITY(1,1) PRIMARY KEY, -- Chave primária: Identificador único e auto-incremental de cada plano.
    PlanName NVARCHAR(50) NOT NULL, -- Campo de dados: Nome do plano de assinatura.
    Description NVARCHAR(500) NULL, -- Campo de dados: Descrição das funcionalidades do plano (opcional).
    MaxUsers INT NOT NULL, -- Campo de dados: Número máximo de usuários permitidos.
    MaxLocations INT NOT NULL, -- Campo de dados: Número máximo de locais permitidos.
    MaxDevices INT NOT NULL, -- Campo de dados: Número máximo de dispositivos permitidos.
    MaxVisitorsPerMonth INT NULL, -- Campo de dados: Número máximo de visitantes por mês (nulo para ilimitado).
    RetentionPeriodDays INT NOT NULL DEFAULT 90, -- Campo de dados: Período de retenção de dados em dias.
    HasBiometricIntegration BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se a integração biométrica está incluída.
    HasAPIAccess BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se o acesso à API está incluído.
    HasAdvancedReporting BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se os relatórios avançados estão incluídos.
    MonthlyPrice DECIMAL(10,2) NOT NULL, -- Campo de dados: Preço mensal do plano.
    IsActive BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se o plano está ativo.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME() -- Campo de controle: Data e hora de criação do plano.
);
COMMENT ON TABLE billing.SubscriptionPlans IS 'Esta tabela define os planos de assinatura disponíveis para os tenants.';
COMMENT ON COLUMN billing.SubscriptionPlans.PlanId IS 'Chave primária: Identificador único e auto-incremental de cada plano.';
COMMENT ON COLUMN billing.SubscriptionPlans.PlanName IS 'Campo de dados: Nome do plano de assinatura.';
COMMENT ON COLUMN billing.SubscriptionPlans.Description IS 'Campo de dados: Descrição das funcionalidades do plano (opcional).';
COMMENT ON COLUMN billing.SubscriptionPlans.MaxUsers IS 'Campo de dados: Número máximo de usuários permitidos.';
COMMENT ON COLUMN billing.SubscriptionPlans.MaxLocations IS 'Campo de dados: Número máximo de locais permitidos.';
COMMENT ON COLUMN billing.SubscriptionPlans.MaxDevices IS 'Campo de dados: Número máximo de dispositivos permitidos.';
COMMENT ON COLUMN billing.SubscriptionPlans.MaxVisitorsPerMonth IS 'Campo de dados: Número máximo de visitantes por mês (nulo para ilimitado).';
COMMENT ON COLUMN billing.SubscriptionPlans.RetentionPeriodDays IS 'Campo de dados: Período de retenção de dados em dias.';
COMMENT ON COLUMN billing.SubscriptionPlans.HasBiometricIntegration IS 'Campo de controle: Indica se a integração biométrica está incluída.';
COMMENT ON COLUMN billing.SubscriptionPlans.HasAPIAccess IS 'Campo de controle: Indica se o acesso à API está incluído.';
COMMENT ON COLUMN billing.SubscriptionPlans.HasAdvancedReporting IS 'Campo de controle: Indica se os relatórios avançados estão incluídos.';
COMMENT ON COLUMN billing.SubscriptionPlans.MonthlyPrice IS 'Campo de dados: Preço mensal do plano.';
COMMENT ON COLUMN billing.SubscriptionPlans.IsActive IS 'Campo de controle: Indica se o plano está ativo.';
COMMENT ON COLUMN billing.SubscriptionPlans.CreatedAt IS 'Campo de controle: Data e hora de criação do plano.';
GO

-- Tabela Subscriptions
-- Esta tabela rastreia as assinaturas dos tenants aos planos.
CREATE TABLE billing.Subscriptions (
    SubscriptionId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada assinatura.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    PlanId INT NOT NULL, -- Chave estrangeira: Identificador do plano assinado.
    StartDate DATETIME2 NOT NULL, -- Campo de dados: Data de início da assinatura.
    EndDate DATETIME2 NOT NULL, -- Campo de dados: Data de término da assinatura.
    AutoRenew BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se a assinatura renova automaticamente.
    PaymentMethod NVARCHAR(20) NULL, -- Campo de dados: Método de pagamento utilizado (opcional).
    PaymentStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Campo de dados: Status atual do pagamento.
    StripeSubscriptionId NVARCHAR(100) NULL, -- Campo de dados: ID da assinatura no Stripe (opcional).
    StripeCustomerId NVARCHAR(100) NULL, -- Campo de dados: ID do cliente no Stripe (opcional).
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de criação da assinatura.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da última atualização (opcional).
    CONSTRAINT FK_Subscriptions_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com deleção em cascata.
    CONSTRAINT FK_Subscriptions_Plans FOREIGN KEY (PlanId) REFERENCES billing.SubscriptionPlans(PlanId) -- Chave estrangeira vinculada a SubscriptionPlans.
);
COMMENT ON TABLE billing.Subscriptions IS 'Esta tabela rastreia as assinaturas dos tenants aos planos.';
COMMENT ON COLUMN billing.Subscriptions.SubscriptionId IS 'Chave primária: Identificador único de cada assinatura.';
COMMENT ON COLUMN billing.Subscriptions.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN billing.Subscriptions.PlanId IS 'Chave estrangeira: Identificador do plano assinado.';
COMMENT ON COLUMN billing.Subscriptions.StartDate IS 'Campo de dados: Data de início da assinatura.';
COMMENT ON COLUMN billing.Subscriptions.EndDate IS 'Campo de dados: Data de término da assinatura.';
COMMENT ON COLUMN billing.Subscriptions.AutoRenew IS 'Campo de controle: Indica se a assinatura renova automaticamente.';
COMMENT ON COLUMN billing.Subscriptions.PaymentMethod IS 'Campo de dados: Método de pagamento utilizado (opcional).';
COMMENT ON COLUMN billing.Subscriptions.PaymentStatus IS 'Campo de dados: Status atual do pagamento.';
COMMENT ON COLUMN billing.Subscriptions.StripeSubscriptionId IS 'Campo de dados: ID da assinatura no Stripe (opcional).';
COMMENT ON COLUMN billing.Subscriptions.StripeCustomerId IS 'Campo de dados: ID do cliente no Stripe (opcional).';
COMMENT ON COLUMN billing.Subscriptions.CreatedAt IS 'Campo de controle: Data e hora de criação da assinatura.';
COMMENT ON COLUMN billing.Subscriptions.UpdatedAt IS 'Campo de controle: Data e hora da última atualização (opcional).';
GO

-- Tabela TenantConfigurations
-- Esta tabela armazena as configurações personalizáveis de cada tenant.
CREATE TABLE saas.TenantConfigurations (
    ConfigId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada configuração.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    Logo VARBINARY(MAX) FILESTREAM NULL, -- Campo de dados: Logotipo do tenant armazenado como dado binário (opcional).
    PrimaryColor NVARCHAR(7) NULL DEFAULT '#1976D2', -- Campo de dados: Cor primária da interface (padrão: azul, opcional).
    SecondaryColor NVARCHAR(7) NULL DEFAULT '#424242', -- Campo de dados: Cor secundária da interface (padrão: cinza, opcional).
    CheckinTimeoutMinutes INT NOT NULL DEFAULT 120, -- Campo de dados: Tempo limite para check-ins em minutos.
    RequireVisitorDocument BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se o documento do visitante é obrigatório.
    RequireVisitorPhoto BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se a foto do visitante é obrigatória.
    RequirePreAuthorization BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se a pré-autorização é obrigatória.
    EmailNotificationsEnabled BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se as notificações por e-mail estão ativadas.
    SMSNotificationsEnabled BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se as notificações por SMS estão ativadas.
    DefaultLanguage NVARCHAR(10) NOT NULL DEFAULT 'pt-BR', -- Campo de dados: Idioma padrão do tenant.
    DataRetentionDays INT NOT NULL DEFAULT 365, -- Campo de dados: Número de dias para retenção de dados.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de criação da configuração.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da última atualização (opcional).
    CONSTRAINT FK_TenantConfigurations_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com deleção em cascata.
    CONSTRAINT UK_TenantConfigurations_Tenant UNIQUE (TenantId) -- Restrição de unicidade: Garante uma configuração por tenant.
);
COMMENT ON TABLE saas.TenantConfigurations IS 'Esta tabela armazena as configurações personalizáveis de cada tenant.';
COMMENT ON COLUMN saas.TenantConfigurations.ConfigId IS 'Chave primária: Identificador único de cada configuração.';
COMMENT ON COLUMN saas.TenantConfigurations.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN saas.TenantConfigurations.Logo IS 'Campo de dados: Logotipo do tenant armazenado como dado binário (opcional).';
COMMENT ON COLUMN saas.TenantConfigurations.PrimaryColor IS 'Campo de dados: Cor primária da interface (padrão: azul, opcional).';
COMMENT ON COLUMN saas.TenantConfigurations.SecondaryColor IS 'Campo de dados: Cor secundária da interface (padrão: cinza, opcional).';
COMMENT ON COLUMN saas.TenantConfigurations.CheckinTimeoutMinutes IS 'Campo de dados: Tempo limite para check-ins em minutos.';
COMMENT ON COLUMN saas.TenantConfigurations.RequireVisitorDocument IS 'Campo de controle: Indica se o documento do visitante é obrigatório.';
COMMENT ON COLUMN saas.TenantConfigurations.RequireVisitorPhoto IS 'Campo de controle: Indica se a foto do visitante é obrigatória.';
COMMENT ON COLUMN saas.TenantConfigurations.RequirePreAuthorization IS 'Campo de controle: Indica se a pré-autorização é obrigatória.';
COMMENT ON COLUMN saas.TenantConfigurations.EmailNotificationsEnabled IS 'Campo de controle: Indica se as notificações por e-mail estão ativadas.';
COMMENT ON COLUMN saas.TenantConfigurations.SMSNotificationsEnabled IS 'Campo de controle: Indica se as notificações por SMS estão ativadas.';
COMMENT ON COLUMN saas.TenantConfigurations.DefaultLanguage IS 'Campo de dados: Idioma padrão do tenant.';
COMMENT ON COLUMN saas.TenantConfigurations.DataRetentionDays IS 'Campo de dados: Número de dias para retenção de dados.';
COMMENT ON COLUMN saas.TenantConfigurations.CreatedAt IS 'Campo de controle: Data e hora de criação da configuração.';
COMMENT ON COLUMN saas.TenantConfigurations.UpdatedAt IS 'Campo de controle: Data e hora da última atualização (opcional).';
GO

-- Tabela TenantSettings
-- Esta tabela armazena configurações dinâmicas chave-valor para os tenants.
CREATE TABLE saas.TenantSettings (
    SettingId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada configuração.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    SettingKey NVARCHAR(100) NOT NULL, -- Campo de dados: Chave da configuração dinâmica.
    SettingValue NVARCHAR(MAX) NOT NULL, -- Campo de dados: Valor da configuração dinâmica.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de criação da configuração.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da última atualização (opcional).
    CONSTRAINT FK_TenantSettings_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com deleção em cascata.
    CONSTRAINT UK_TenantSettings_Key UNIQUE (TenantId, SettingKey) -- Restrição de unicidade: Garante chave única por tenant.
);
COMMENT ON TABLE saas.TenantSettings IS 'Esta tabela armazena configurações dinâmicas chave-valor para os tenants.';
COMMENT ON COLUMN saas.TenantSettings.SettingId IS 'Chave primária: Identificador único de cada configuração.';
COMMENT ON COLUMN saas.TenantSettings.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN saas.TenantSettings.SettingKey IS 'Campo de dados: Chave da configuração dinâmica.';
COMMENT ON COLUMN saas.TenantSettings.SettingValue IS 'Campo de dados: Valor da configuração dinâmica.';
COMMENT ON COLUMN saas.TenantSettings.CreatedAt IS 'Campo de controle: Data e hora de criação da configuração.';
COMMENT ON COLUMN saas.TenantSettings.UpdatedAt IS 'Campo de controle: Data e hora da última atualização (opcional).';
GO

-- Tabela Locations
-- Esta tabela armazena os locais físicos gerenciados pelos tenants.
CREATE TABLE multitenant.Locations (
    LocationId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada local.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    Name NVARCHAR(100) NOT NULL, -- Campo de dados: Nome do local.
    Address NVARCHAR(200) NOT NULL, -- Campo de dados: Endereço físico do local.
    City NVARCHAR(100) NOT NULL, -- Campo de dados: Cidade do local.
    State NVARCHAR(50) NOT NULL, -- Campo de dados: Estado do local.
    Country NVARCHAR(50) NOT NULL DEFAULT 'Brasil', -- Campo de dados: País do local (padrão: Brasil).
    TimeZone NVARCHAR(50) NOT NULL DEFAULT 'E. South America Standard Time', -- Campo de dados: Fuso horário do local.
    IsActive BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se o local está ativo.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de criação do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da última atualização (opcional).
    CONSTRAINT FK_Locations_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE -- Chave estrangeira vinculada a Tenants com deleção em cascata.
);
COMMENT ON TABLE multitenant.Locations IS 'Esta tabela armazena os locais físicos gerenciados pelos tenants.';
COMMENT ON COLUMN multitenant.Locations.LocationId IS 'Chave primária: Identificador único de cada local.';
COMMENT ON COLUMN multitenant.Locations.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.Locations.Name IS 'Campo de dados: Nome do local.';
COMMENT ON COLUMN multitenant.Locations.Address IS 'Campo de dados: Endereço físico do local.';
COMMENT ON COLUMN multitenant.Locations.City IS 'Campo de dados: Cidade do local.';
COMMENT ON COLUMN multitenant.Locations.State IS 'Campo de dados: Estado do local.';
COMMENT ON COLUMN multitenant.Locations.Country IS 'Campo de dados: País do local (padrão: Brasil).';
COMMENT ON COLUMN multitenant.Locations.TimeZone IS 'Campo de dados: Fuso horário do local.';
COMMENT ON COLUMN multitenant.Locations.IsActive IS 'Campo de controle: Indica se o local está ativo.';
COMMENT ON COLUMN multitenant.Locations.CreatedAt IS 'Campo de controle: Data e hora de criação do registro.';
COMMENT ON COLUMN multitenant.Locations.UpdatedAt IS 'Campo de controle: Data e hora da última atualização (opcional).';
GO

-- Tabela Users
-- Esta tabela armazena as contas de usuários para operações dos tenants.
CREATE TABLE multitenant.Users (
    UserId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada usuário.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    LocationId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do local do usuário (opcional).
    Username NVARCHAR(50) NOT NULL, -- Campo de dados: Nome de usuário para login.
    PasswordHash NVARCHAR(MAX) NOT NULL, -- Campo de dados: Hash da senha para segurança.
    Email NVARCHAR(100) NOT NULL, -- Campo de dados: Endereço de e-mail do usuário.
    FirstName NVARCHAR(50) NOT NULL, -- Campo de dados: Primeiro nome do usuário.
    LastName NVARCHAR(100) NOT NULL, -- Campo de dados: Sobrenome do usuário.
    DocumentType NVARCHAR(20) NULL DEFAULT 'CPF', -- Campo de dados: Tipo de documento de identificação (padrão: CPF).
    DocumentNumber NVARCHAR(20) NULL, -- Campo de dados: Número do documento de identificação.
    PhoneNumber NVARCHAR(20) NULL, -- Campo de dados: Número de telefone do usuário (opcional).
    Photo VARBINARY(MAX) FILESTREAM NULL, -- Campo de dados: Foto do usuário armazenada como dado binário (opcional).
    Role NVARCHAR(20) NOT NULL DEFAULT 'Operator', -- Campo de dados: Papel do usuário (ex.: Operador, Admin).
    IsActive BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se o usuário está ativo.
    LastLogin DATETIME2 NULL, -- Campo de dados: Data e hora do último login (opcional).
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de criação do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da última atualização (opcional).
    CONSTRAINT FK_Users_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com deleção em cascata.
    CONSTRAINT FK_Users_Locations FOREIGN KEY (LocationId) REFERENCES multitenant.Locations(LocationId), -- Chave estrangeira vinculada a Locations.
    CONSTRAINT UK_Users_Username UNIQUE (TenantId, Username), -- Restrição de unicidade: Garante nome de usuário único por tenant.
    CONSTRAINT UK_Users_Email UNIQUE (TenantId, Email) -- Restrição de unicidade: Garante e-mail único por tenant.
);
COMMENT ON TABLE multitenant.Users IS 'Esta tabela armazena as contas de usuários para operações dos tenants.';
COMMENT ON COLUMN multitenant.Users.UserId IS 'Chave primária: Identificador único de cada usuário.';
COMMENT ON COLUMN multitenant.Users.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.Users.LocationId IS 'Chave estrangeira: Identificador do local do usuário (opcional).';
COMMENT ON COLUMN multitenant.Users.Username IS 'Campo de dados: Nome de usuário para login.';
COMMENT ON COLUMN multitenant.Users.PasswordHash IS 'Campo de dados: Hash da senha para segurança.';
COMMENT ON COLUMN multitenant.Users.Email IS 'Campo de dados: Endereço de e-mail do usuário.';
COMMENT ON COLUMN multitenant.Users.FirstName IS 'Campo de dados: Primeiro nome do usuário.';
COMMENT ON COLUMN multitenant.Users.LastName IS 'Campo de dados: Sobrenome do usuário.';
COMMENT ON COLUMN multitenant.Users.DocumentType IS 'Campo de dados: Tipo de documento de identificação (padrão: CPF).';
COMMENT ON COLUMN multitenant.Users.DocumentNumber IS 'Campo de dados: Número do documento de identificação.';
COMMENT ON COLUMN multitenant.Users.PhoneNumber IS 'Campo de dados: Número de telefone do usuário (opcional).';
COMMENT ON COLUMN multitenant.Users.Photo IS 'Campo de dados: Foto do usuário armazenada como dado binário (opcional).';
COMMENT ON COLUMN multitenant.Users.Role IS 'Campo de dados: Papel do usuário (ex.: Operador, Admin).';
COMMENT ON COLUMN multitenant.Users.IsActive IS 'Campo de controle: Indica se o usuário está ativo.';
COMMENT ON COLUMN multitenant.Users.LastLogin IS 'Campo de dados: Data e hora do último login (opcional).';
COMMENT ON COLUMN multitenant.Users.CreatedAt IS 'Campo de controle: Data e hora de criação do registro.';
COMMENT ON COLUMN multitenant.Users.UpdatedAt IS 'Campo de controle: Data e hora da última atualização (opcional).';
GO

-- Tabela Employees
-- Esta tabela armazena os registros de funcionários para controle de acesso.
CREATE TABLE multitenant.Employees (
    EmployeeId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada funcionário.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    LocationId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do local do funcionário (opcional).
    CompanyDepartment NVARCHAR(100) NULL, -- Campo de dados: Departamento da empresa (opcional).
    EmployeeNumber NVARCHAR(50) NULL, -- Campo de dados: Número de identificação do funcionário (opcional).
    FirstName NVARCHAR(50) NOT NULL, -- Campo de dados: Primeiro nome do funcionário.
    LastName NVARCHAR(100) NOT NULL, -- Campo de dados: Sobrenome do funcionário.
    DocumentType NVARCHAR(20) NOT NULL DEFAULT 'CPF', -- Campo de dados: Tipo de documento de identificação (padrão: CPF).
    DocumentNumber NVARCHAR(20) NOT NULL, -- Campo de dados: Número do documento de identificação.
    Email NVARCHAR(100) NULL, -- Campo de dados: Endereço de e-mail do funcionário (opcional).
    PhoneNumber NVARCHAR(20) NULL, -- Campo de dados: Número de telefone do funcionário (opcional).
    Photo VARBINARY(MAX) FILESTREAM NULL, -- Campo de dados: Foto do funcionário armazenada como dado binário (opcional).
    RFIDTag NVARCHAR(100) ENCRYPTED WITH (ENCRYPTION_TYPE = DETERMINISTIC, ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256', COLUMN_ENCRYPTION_KEY = CEK_Portaria) NULL, -- Campo de dados: Tag RFID para acesso (criptografado, opcional).
    BiometricData VARBINARY(MAX) ENCRYPTED WITH (ENCRYPTION_TYPE = DETERMINISTIC, ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256', COLUMN_ENCRYPTION_KEY = CEK_Portaria) NULL, -- Campo de dados: Dados biométricos (criptografados, opcionais).
    ConsentGiven BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se o consentimento para processamento de dados foi dado (LGPD/GDPR).
    DataExpirationDate DATETIME2 NULL, -- Campo de controle: Data de expiração dos dados do funcionário.
    IsActive BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se o funcionário está ativo.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de criação do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da última atualização (opcional).
    CONSTRAINT FK_Employees_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com deleção em cascata.
    CONSTRAINT FK_Employees_Locations FOREIGN KEY (LocationId) REFERENCES multitenant.Locations(LocationId), -- Chave estrangeira vinculada a Locations.
    CONSTRAINT UK_Employees_Document UNIQUE (TenantId, DocumentType, DocumentNumber), -- Restrição de unicidade: Garante documento único por tenant.
    CONSTRAINT UK_Employees_RFID UNIQUE (TenantId, RFIDTag) WHERE RFIDTag IS NOT NULL -- Restrição de unicidade: Garante tag RFID única por tenant.
);
COMMENT ON TABLE multitenant.Employees IS 'Esta tabela armazena os registros de funcionários para controle de acesso.';
COMMENT ON COLUMN multitenant.Employees.EmployeeId IS 'Chave primária: Identificador único de cada funcionário.';
COMMENT ON COLUMN multitenant.Employees.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.Employees.LocationId IS 'Chave estrangeira: Identificador do local do funcionário (opcional).';
COMMENT ON COLUMN multitenant.Employees.CompanyDepartment IS 'Campo de dados: Departamento da empresa (opcional).';
COMMENT ON COLUMN multitenant.Employees.EmployeeNumber IS 'Campo de dados: Número de identificação do funcionário (opcional).';
COMMENT ON COLUMN multitenant.Employees.FirstName IS 'Campo de dados: Primeiro nome do funcionário.';
COMMENT ON COLUMN multitenant.Employees.LastName IS 'Campo de dados: Sobrenome do funcionário.';
COMMENT ON COLUMN multitenant.Employees.DocumentType IS 'Campo de dados: Tipo de documento de identificação (padrão: CPF).';
COMMENT ON COLUMN multitenant.Employees.DocumentNumber IS 'Campo de dados: Número do documento de identificação.';
COMMENT ON COLUMN multitenant.Employees.Email IS 'Campo de dados: Endereço de e-mail do funcionário (opcional).';
COMMENT ON COLUMN multitenant.Employees.PhoneNumber IS 'Campo de dados: Número de telefone do funcionário (opcional).';
COMMENT ON COLUMN multitenant.Employees.Photo IS 'Campo de dados: Foto do funcionário armazenada como dado binário (opcional).';
COMMENT ON COLUMN multitenant.Employees.RFIDTag IS 'Campo de dados: Tag RFID para acesso (criptografado, opcional).';
COMMENT ON COLUMN multitenant.Employees.BiometricData IS 'Campo de dados: Dados biométricos (criptografados, opcionais).';
COMMENT ON COLUMN multitenant.Employees.ConsentGiven IS 'Campo de controle: Indica se o consentimento para processamento de dados foi dado (LGPD/GDPR).';
COMMENT ON COLUMN multitenant.Employees.DataExpirationDate IS 'Campo de controle: Data de expiração dos dados do funcionário.';
COMMENT ON COLUMN multitenant.Employees.IsActive IS 'Campo de controle: Indica se o funcionário está ativo.';
COMMENT ON COLUMN multitenant.Employees.CreatedAt IS 'Campo de controle: Data e hora de criação do registro.';
COMMENT ON COLUMN multitenant.Employees.UpdatedAt IS 'Campo de controle: Data e hora da última atualização (opcional).';
GO

-- Criando Chave de Criptografia de Coluna para Always Encrypted
CREATE COLUMN ENCRYPTION KEY CEK_Portaria
    WITH VALUES
    (
        COLUMN_MASTER_KEY = CMK_Portaria,
        ALGORITHM = 'RSA_OAEP',
        ENCRYPTED_VALUE = 0x0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF
    );
GO

-- Criando Chave Mestra de Coluna
CREATE COLUMN MASTER KEY CMK_Portaria
    WITH (
        KEY_STORE_PROVIDER_NAME = 'MSSQL_CERTIFICATE_STORE',
        KEY_PATH = 'CurrentUser/My/ChaveMestraPortaria'
    );
GO

-- Tabela Visitors
-- Esta tabela armazena os registros de visitantes para controle de acesso.
CREATE TABLE multitenant.Visitors (
    VisitorId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada visitante.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    FirstName NVARCHAR(50) NOT NULL, -- Campo de dados: Primeiro nome do visitante.
    LastName NVARCHAR(100) NOT NULL, -- Campo de dados: Sobrenome do visitante.
    DocumentType NVARCHAR(20) NOT NULL DEFAULT 'CPF', -- Campo de dados: Tipo de documento de identificação (padrão: CPF).
    DocumentNumber NVARCHAR(20) NOT NULL, -- Campo de dados: Número do documento de identificação.
    Email NVARCHAR(100) NULL, -- Campo de dados: Endereço de e-mail do visitante (opcional).
    PhoneNumber NVARCHAR(20) NULL, -- Campo de dados: Número de telefone do visitante (opcional).
    CompanyName NVARCHAR(200) NULL, -- Campo de dados: Nome da empresa que o visitante representa (opcional).
    Photo VARBINARY(MAX) FILESTREAM NULL, -- Campo de dados: Foto do visitante armazenada como dado binário (opcional).
    VehiclePlate NVARCHAR(10) NULL, -- Campo de dados: Placa do veículo do visitante (opcional).
    VehicleModel NVARCHAR(50) NULL, -- Campo de dados: Modelo do veículo do visitante (opcional).
    VehicleColor NVARCHAR(30) NULL, -- Campo de dados: Cor do veículo do visitante (opcional).
    IsElectricVehicle BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se o veículo é elétrico (sustentabilidade).
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de criação do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da última atualização (opcional).
    ConsentGiven BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se o consentimento para processamento de dados foi dado (LGPD/GDPR).
    DataExpirationDate DATETIME2 NULL, -- Campo de controle: Data de expiração dos dados do visitante.
    CONSTRAINT FK_Visitors_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com deleção em cascata.
    CONSTRAINT UK_Visitors_Document UNIQUE (TenantId, DocumentType, DocumentNumber) -- Restrição de unicidade: Garante documento único por tenant.
);
COMMENT ON TABLE multitenant.Visitors IS 'Esta tabela armazena os registros de visitantes para controle de acesso.';
COMMENT ON COLUMN multitenant.Visitors.VisitorId IS 'Chave primária: Identificador único de cada visitante.';
COMMENT ON COLUMN multitenant.Visitors.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.Visitors.FirstName IS 'Campo de dados: Primeiro nome do visitante.';
COMMENT ON COLUMN multitenant.Visitors.LastName IS 'Campo de dados: Sobrenome do visitante.';
COMMENT ON COLUMN multitenant.Visitors.DocumentType IS 'Campo de dados: Tipo de documento de identificação (padrão: CPF).';
COMMENT ON COLUMN multitenant.Visitors.DocumentNumber IS 'Campo de dados: Número do documento de identificação.';
COMMENT ON COLUMN multitenant.Visitors.Email IS 'Campo de dados: Endereço de e-mail do visitante (opcional).';
COMMENT ON COLUMN multitenant.Visitors.PhoneNumber IS 'Campo de dados: Número de telefone do visitante (opcional).';
COMMENT ON COLUMN multitenant.Visitors.CompanyName IS 'Campo de dados: Nome da empresa que o visitante representa (opcional).';
COMMENT ON COLUMN multitenant.Visitors.Photo IS 'Campo de dados: Foto do visitante armazenada como dado binário (opcional).';
COMMENT ON COLUMN multitenant.Visitors.VehiclePlate IS 'Campo de dados: Placa do veículo do visitante (opcional).';
COMMENT ON COLUMN multitenant.Visitors.VehicleModel IS 'Campo de dados: Modelo do veículo do visitante (opcional).';
COMMENT ON COLUMN multitenant.Visitors.VehicleColor IS 'Campo de dados: Cor do veículo do visitante (opcional).';
COMMENT ON COLUMN multitenant.Visitors.IsElectricVehicle IS 'Campo de controle: Indica se o veículo é elétrico (sustentabilidade).';
COMMENT ON COLUMN multitenant.Visitors.CreatedAt IS 'Campo de controle: Data e hora de criação do registro.';
COMMENT ON COLUMN multitenant.Visitors.UpdatedAt IS 'Campo de controle: Data e hora da última atualização (opcional).';
COMMENT ON COLUMN multitenant.Visitors.ConsentGiven IS 'Campo de controle: Indica se o consentimento para processamento de dados foi dado (LGPD/GDPR).';
COMMENT ON COLUMN multitenant.Visitors.DataExpirationDate IS 'Campo de controle: Data de expiração dos dados do visitante.';
GO

-- Tabela VisitorGroups
-- Esta tabela armazena grupos de visitantes para gerenciamento em massa.
CREATE TABLE multitenant.VisitorGroups (
    GroupId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada grupo.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    GroupName NVARCHAR(100) NOT NULL, -- Campo de dados: Nome do grupo de visitantes.
    Description NVARCHAR(500) NULL, -- Campo de dados: Descrição do grupo (opcional).
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de criação do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da última atualização (opcional).
    CONSTRAINT FK_VisitorGroups_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE -- Chave estrangeira vinculada a Tenants com deleção em cascata.
);
COMMENT ON TABLE multitenant.VisitorGroups IS 'Esta tabela armazena grupos de visitantes para gerenciamento em massa.';
COMMENT ON COLUMN multitenant.VisitorGroups.GroupId IS 'Chave primária: Identificador único de cada grupo.';
COMMENT ON COLUMN multitenant.VisitorGroups.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.VisitorGroups.GroupName IS 'Campo de dados: Nome do grupo de visitantes.';
COMMENT ON COLUMN multitenant.VisitorGroups.Description IS 'Campo de dados: Descrição do grupo (opcional).';
COMMENT ON COLUMN multitenant.VisitorGroups.CreatedAt IS 'Campo de controle: Data e hora de criação do registro.';
COMMENT ON COLUMN multitenant.VisitorGroups.UpdatedAt IS 'Campo de controle: Data e hora da última atualização (opcional).';
GO

CREATE TABLE multitenant.VisitorGroupMembers (
    MemberId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada membro.
    GroupId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do grupo de visitantes.
    VisitorId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do visitante.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de criação da associação.
    CONSTRAINT FK_VisitorGroupMembers_Groups FOREIGN KEY (GroupId) REFERENCES multitenant.VisitorGroups(GroupId) ON DELETE CASCADE, -- Chave estrangeira vinculada a VisitorGroups com deleção em cascata.
    CONSTRAINT FK_VisitorGroupMembers_Visitors FOREIGN KEY (VisitorId) REFERENCES multitenant.Visitors(VisitorId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Visitors com deleção em cascata.
    CONSTRAINT UK_VisitorGroupMembers UNIQUE (GroupId, VisitorId) -- Restrição de unicidade: Garante associação única por grupo.
);
COMMENT ON TABLE multitenant.VisitorGroupMembers IS 'Esta tabela armazena os membros dos grupos de visitantes.';
COMMENT ON COLUMN multitenant.VisitorGroupMembers.MemberId IS 'Chave primária: Identificador único de cada membro.';
COMMENT ON COLUMN multitenant.VisitorGroupMembers.GroupId IS 'Chave estrangeira: Identificador do grupo de visitantes.';
COMMENT ON COLUMN multitenant.VisitorGroupMembers.VisitorId IS 'Chave estrangeira: Identificador do visitante.';
COMMENT ON COLUMN multitenant.VisitorGroupMembers.CreatedAt IS 'Campo de controle: Data e hora de criação da associação.';
GO

-- Tabela AccessAuthorizations
-- Esta tabela armazena os detalhes de autorizações de acesso para visitantes e funcionários.
CREATE TABLE multitenant.AccessAuthorizations (
    AuthorizationId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada autorização.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    VisitorId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do visitante (opcional).
    EmployeeId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do funcionário (opcional).
    AuthorizedBy UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do usuário que autorizou.
    ValidFrom DATETIME2 NOT NULL, -- Campo de dados: Data e hora de início da validade.
    ValidUntil DATETIME2 NOT NULL, -- Campo de dados: Data e hora de término da validade.
    Purpose NVARCHAR(500) NOT NULL, -- Campo de dados: Propósito do acesso.
    AccessZones NVARCHAR(MAX) NOT NULL, -- Campo de dados: Zonas de acesso autorizadas em formato JSON.
    RequiresEscort BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se é necessário acompanhamento.
    QRCode VARBINARY(MAX) FILESTREAM NULL, -- Campo de dados: Código QR para acesso (armazenado como binário, opcional).
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Campo de dados: Status atual da autorização.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de criação do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da última atualização (opcional).
    CONSTRAINT FK_AccessAuthorizations_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com deleção em cascata.
    CONSTRAINT FK_AccessAuthorizations_Visitors FOREIGN KEY (VisitorId) REFERENCES multitenant.Visitors(VisitorId), -- Chave estrangeira vinculada a Visitors.
    CONSTRAINT FK_AccessAuthorizations_Employees FOREIGN KEY (EmployeeId) REFERENCES multitenant.Employees(EmployeeId), -- Chave estrangeira vinculada a Employees.
    CONSTRAINT FK_AccessAuthorizations_AuthorizedBy FOREIGN KEY (AuthorizedBy) REFERENCES multitenant.Users(UserId) -- Chave estrangeira vinculada a Users.
);
COMMENT ON TABLE multitenant.AccessAuthorizations IS 'Esta tabela armazena os detalhes de autorizações de acesso para visitantes e funcionários.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.AuthorizationId IS 'Chave primária: Identificador único de cada autorização.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.VisitorId IS 'Chave estrangeira: Identificador do visitante (opcional).';
COMMENT ON COLUMN multitenant.AccessAuthorizations.EmployeeId IS 'Chave estrangeira: Identificador do funcionário (opcional).';
COMMENT ON COLUMN multitenant.AccessAuthorizations.AuthorizedBy IS 'Chave estrangeira: Identificador do usuário que autorizou.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.ValidFrom IS 'Campo de dados: Data e hora de início da validade.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.ValidUntil IS 'Campo de dados: Data e hora de término da validade.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.Purpose IS 'Campo de dados: Propósito do acesso.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.AccessZones IS 'Campo de dados: Zonas de acesso autorizadas em formato JSON.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.RequiresEscort IS 'Campo de controle: Indica se é necessário acompanhamento.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.QRCode IS 'Campo de dados: Código QR para acesso (armazenado como binário, opcional).';
COMMENT ON COLUMN multitenant.AccessAuthorizations.Status IS 'Campo de dados: Status atual da autorização.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.CreatedAt IS 'Campo de controle: Data e hora de criação do registro.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.UpdatedAt IS 'Campo de controle: Data e hora da última atualização (opcional).';
GO

-- Tabela Devices
-- Esta tabela armazena informações sobre os dispositivos de controle de acesso.
CREATE TABLE multitenant.Devices (
    DeviceId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada dispositivo.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    LocationId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do local.
    DeviceName NVARCHAR(100) NOT NULL, -- Campo de dados: Nome do dispositivo.
    DeviceType NVARCHAR(50) NOT NULL, -- Campo de dados: Tipo de dispositivo (ex.: Catraca, Leitor RFID).
    DeviceCode NVARCHAR(50) NOT NULL, -- Campo de dados: Código único do dispositivo.
    IPAddress NVARCHAR(45) NULL, -- Campo de dados: Endereço IP do dispositivo (opcional).
    MACAddress NVARCHAR(17) NULL, -- Campo de dados: Endereço MAC do dispositivo (opcional).
    Status NVARCHAR(20) NOT NULL DEFAULT 'Active', -- Campo de dados: Status atual do dispositivo.
    LastCommunication DATETIME2 NULL, -- Campo de dados: Data e hora da última comunicação (opcional).
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de criação do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da última atualização (opcional).
    CONSTRAINT FK_Devices_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com deleção em cascata.
    CONSTRAINT FK_Devices_Locations FOREIGN KEY (LocationId) REFERENCES multitenant.Locations(LocationId), -- Chave estrangeira vinculada a Locations.
    CONSTRAINT UK_Devices_Code UNIQUE (TenantId, DeviceCode) -- Restrição de unicidade: Garante código único por tenant.
);
COMMENT ON TABLE multitenant.Devices IS 'Esta tabela armazena informações sobre os dispositivos de controle de acesso.';
COMMENT ON COLUMN multitenant.Devices.DeviceId IS 'Chave primária: Identificador único de cada dispositivo.';
COMMENT ON COLUMN multitenant.Devices.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.Devices.LocationId IS 'Chave estrangeira: Identificador do local.';
COMMENT ON COLUMN multitenant.Devices.DeviceName IS 'Campo de dados: Nome do dispositivo.';
COMMENT ON COLUMN multitenant.Devices.DeviceType IS 'Campo de dados: Tipo de dispositivo (ex.: Catraca, Leitor RFID).';
COMMENT ON COLUMN multitenant.Devices.DeviceCode IS 'Campo de dados: Código único do dispositivo.';
COMMENT ON COLUMN multitenant.Devices.IPAddress IS 'Campo de dados: Endereço IP do dispositivo (opcional).';
COMMENT ON COLUMN multitenant.Devices.MACAddress IS 'Campo de dados: Endereço MAC do dispositivo (opcional).';
COMMENT ON COLUMN multitenant.Devices.Status IS 'Campo de dados: Status atual do dispositivo.';
COMMENT ON COLUMN multitenant.Devices.LastCommunication IS 'Campo de dados: Data e hora da última comunicação (opcional).';
COMMENT ON COLUMN multitenant.Devices.CreatedAt IS 'Campo de controle: Data e hora de criação do registro.';
COMMENT ON COLUMN multitenant.Devices.UpdatedAt IS 'Campo de controle: Data e hora da última atualização (opcional).';
GO

-- Tabela AccessRecords com Particionamento
-- Esta tabela registra todos os eventos de acesso com particionamento por data.
CREATE PARTITION FUNCTION PF_AccessDateTime (DATETIME2) 
AS RANGE RIGHT FOR VALUES ('2025-09-01', '2025-10-01', '2025-11-01');
GO

CREATE PARTITION SCHEME PS_AccessDateTime
AS PARTITION PF_AccessDateTime ALL TO ([PRIMARY]);
GO

CREATE TABLE multitenant.AccessRecords (
    RecordId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada registro de acesso.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    LocationId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do local.
    DeviceId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do dispositivo.
    VisitorId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do visitante (opcional).
    EmployeeId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do funcionário (opcional).
    AuthorizationId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador da autorização (opcional).
    AccessType NVARCHAR(10) NOT NULL, -- Campo de dados: Tipo de acesso (ex.: Entrada, Saída).
    AccessDateTime DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de dados: Data e hora do evento de acesso.
    DocumentNumber NVARCHAR(20) NULL, -- Campo de dados: Número do documento utilizado no acesso (opcional).
    RFIDTag NVARCHAR(100) NULL, -- Campo de dados: Tag RFID utilizada no acesso (opcional).
    BiometricMatchConfidence DECIMAL(5,2) NULL, -- Campo de dados: Pontuação de confiança do reconhecimento biométrico (opcional).
    AnomalyScore DECIMAL(5,2) NULL, -- Campo de dados: Pontuação de anomalia detectada (opcional).
    PhotoTaken VARBINARY(MAX) FILESTREAM NULL, -- Campo de dados: Foto capturada durante o acesso (opcional).
    Temperature DECIMAL(4,1) NULL, -- Campo de dados: Leitura de temperatura (opcional).
    Status NVARCHAR(20) NOT NULL DEFAULT 'Granted', -- Campo de dados: Status da tentativa de acesso.
    DenialReason NVARCHAR(200) NULL, -- Campo de dados: Motivo da negação do acesso (opcional).
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de criação do registro.
    DataExpirationDate DATETIME2 NULL, -- Campo de controle: Data de expiração do registro.
    CONSTRAINT FK_AccessRecords_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com deleção em cascata.
    CONSTRAINT FK_AccessRecords_Locations FOREIGN KEY (LocationId) REFERENCES multitenant.Locations(LocationId), -- Chave estrangeira vinculada a Locations.
    CONSTRAINT FK_AccessRecords_Devices FOREIGN KEY (DeviceId) REFERENCES multitenant.Devices(DeviceId), -- Chave estrangeira vinculada a Devices.
    CONSTRAINT FK_AccessRecords_Visitors FOREIGN KEY (VisitorId) REFERENCES multitenant.Visitors(VisitorId), -- Chave estrangeira vinculada a Visitors.
    CONSTRAINT FK_AccessRecords_Employees FOREIGN KEY (EmployeeId) REFERENCES multitenant.Employees(EmployeeId), -- Chave estrangeira vinculada a Employees.
    CONSTRAINT FK_AccessRecords_Authorizations FOREIGN KEY (AuthorizationId) REFERENCES multitenant.AccessAuthorizations(AuthorizationId) -- Chave estrangeira vinculada a AccessAuthorizations.
) ON PS_AccessDateTime (AccessDateTime);
COMMENT ON TABLE multitenant.AccessRecords IS 'Esta tabela registra todos os eventos de acesso com particionamento por data.';
COMMENT ON COLUMN multitenant.AccessRecords.RecordId IS 'Chave primária: Identificador único de cada registro de acesso.';
COMMENT ON COLUMN multitenant.AccessRecords.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.AccessRecords.LocationId IS 'Chave estrangeira: Identificador do local.';
COMMENT ON COLUMN multitenant.AccessRecords.DeviceId IS 'Chave estrangeira: Identificador do dispositivo.';
COMMENT ON COLUMN multitenant.AccessRecords.VisitorId IS 'Chave estrangeira: Identificador do visitante (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.EmployeeId IS 'Chave estrangeira: Identificador do funcionário (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.AuthorizationId IS 'Chave estrangeira: Identificador da autorização (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.AccessType IS 'Campo de dados: Tipo de acesso (ex.: Entrada, Saída).';
COMMENT ON COLUMN multitenant.AccessRecords.AccessDateTime IS 'Campo de dados: Data e hora do evento de acesso.';
COMMENT ON COLUMN multitenant.AccessRecords.DocumentNumber IS 'Campo de dados: Número do documento utilizado no acesso (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.RFIDTag IS 'Campo de dados: Tag RFID utilizada no acesso (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.BiometricMatchConfidence IS 'Campo de dados: Pontuação de confiança do reconhecimento biométrico (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.AnomalyScore IS 'Campo de dados: Pontuação de anomalia detectada (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.PhotoTaken IS 'Campo de dados: Foto capturada durante o acesso (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.Temperature IS 'Campo de dados: Leitura de temperatura (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.Status IS 'Campo de dados: Status da tentativa de acesso.';
COMMENT ON COLUMN multitenant.AccessRecords.DenialReason IS 'Campo de dados: Motivo da negação do acesso (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.CreatedAt IS 'Campo de controle: Data e hora de criação do registro.';
COMMENT ON COLUMN multitenant.AccessRecords.DataExpirationDate IS 'Campo de controle: Data de expiração do registro.';
GO

-- Tabela RestrictedZones
-- Esta tabela define as áreas restritas dentro dos locais.
CREATE TABLE multitenant.RestrictedZones (
    ZoneId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada zona.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    LocationId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do local.
    ZoneName NVARCHAR(100) NOT NULL, -- Campo de dados: Nome da zona restrita.
    Description NVARCHAR(500) NULL, -- Campo de dados: Descrição da zona (opcional).
    AccessLevel NVARCHAR(20) NOT NULL DEFAULT 'Restricted', -- Campo de dados: Nível de acesso (ex.: Público, Restrito).
    IsActive BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se a zona está ativa.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de criação do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da última atualização (opcional).
    CONSTRAINT FK_RestrictedZones_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com deleção em cascata.
    CONSTRAINT FK_RestrictedZones_Locations FOREIGN KEY (LocationId) REFERENCES multitenant.Locations(LocationId) -- Chave estrangeira vinculada a Locations.
);
COMMENT ON TABLE multitenant.RestrictedZones IS 'Esta tabela define as áreas restritas dentro dos locais.';
COMMENT ON COLUMN multitenant.RestrictedZones.ZoneId IS 'Chave primária: Identificador único de cada zona.';
COMMENT ON COLUMN multitenant.RestrictedZones.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.RestrictedZones.LocationId IS 'Chave estrangeira: Identificador do local.';
COMMENT ON COLUMN multitenant.RestrictedZones.ZoneName IS 'Campo de dados: Nome da zona restrita.';
COMMENT ON COLUMN multitenant.RestrictedZones.Description IS 'Campo de dados: Descrição da zona (opcional).';
COMMENT ON COLUMN multitenant.RestrictedZones.AccessLevel IS 'Campo de dados: Nível de acesso (ex.: Público, Restrito).';
COMMENT ON COLUMN multitenant.RestrictedZones.IsActive IS 'Campo de controle: Indica se a zona está ativa.';
COMMENT ON COLUMN multitenant.RestrictedZones.CreatedAt IS 'Campo de controle: Data e hora de criação do registro.';
COMMENT ON COLUMN multitenant.RestrictedZones.UpdatedAt IS 'Campo de controle: Data e hora da última atualização (opcional).';
GO

-- Tabela ZonePermissions
-- Esta tabela define as permissões de acesso por papel dentro das zonas.
CREATE TABLE multitenant.ZonePermissions (
    PermissionId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada permissão.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    ZoneId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador da zona restrita.
    Role NVARCHAR(50) NOT NULL, -- Campo de dados: Papel que requer acesso (ex.: Visitante, Funcionário).
    AccessLevel NVARCHAR(20) NOT NULL DEFAULT 'Denied', -- Campo de dados: Nível de acesso concedido (ex.: Negado, Concedido).
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de criação da permissão.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da última atualização (opcional).
    CONSTRAINT FK_ZonePermissions_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com deleção em cascata.
    CONSTRAINT FK_ZonePermissions_Zones FOREIGN KEY (ZoneId) REFERENCES multitenant.RestrictedZones(ZoneId), -- Chave estrangeira vinculada a RestrictedZones.
    CONSTRAINT UK_ZonePermissions UNIQUE (TenantId, ZoneId, Role) -- Restrição de unicidade: Garante papel único por zona por tenant.
);
COMMENT ON TABLE multitenant.ZonePermissions IS 'Esta tabela define as permissões de acesso por papel dentro das zonas.';
COMMENT ON COLUMN multitenant.ZonePermissions.PermissionId IS 'Chave primária: Identificador único de cada permissão.';
COMMENT ON COLUMN multitenant.ZonePermissions.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.ZonePermissions.ZoneId IS 'Chave estrangeira: Identificador da zona restrita.';
COMMENT ON COLUMN multitenant.ZonePermissions.Role IS 'Campo de dados: Papel que requer acesso (ex.: Visitante, Funcionário).';
COMMENT ON COLUMN multitenant.ZonePermissions.AccessLevel IS 'Campo de dados: Nível de acesso concedido (ex.: Negado, Concedido).';
COMMENT ON COLUMN multitenant.ZonePermissions.CreatedAt IS 'Campo de controle: Data e hora de criação da permissão.';
COMMENT ON COLUMN multitenant.ZonePermissions.UpdatedAt IS 'Campo de controle: Data e hora da última atualização (opcional).';
GO

-- Tabela Alerts
-- Esta tabela armazena os alertas gerados pelo sistema.
CREATE TABLE multitenant.Alerts (
    AlertId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada alerta.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    LocationId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do local (opcional).
    AlertType NVARCHAR(50) NOT NULL, -- Campo de dados: Tipo de alerta (ex.: Segurança, Sistema).
    Severity NVARCHAR(20) NOT NULL DEFAULT 'Medium', -- Campo de dados: Nível de gravidade do alerta.
    Title NVARCHAR(200) NOT NULL, -- Campo de dados: Título do alerta.
    Description NVARCHAR(1000) NULL, -- Campo de dados: Descrição detalhada do alerta (opcional).
    RelatedRecordId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do registro relacionado (opcional).
    RelatedRecordType NVARCHAR(50) NULL, -- Campo de dados: Tipo de registro relacionado (opcional).
    Acknowledged BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se o alerta foi reconhecido.
    AcknowledgedBy UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do usuário que reconheceu (opcional).
    AcknowledgedAt DATETIME2 NULL, -- Campo de controle: Data e hora do reconhecimento (opcional).
    Resolved BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se o alerta foi resolvido.
    ResolvedBy UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do usuário que resolveu (opcional).
    ResolvedAt DATETIME2 NULL, -- Campo de controle: Data e hora da resolução (opcional).
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de criação do registro.
    CONSTRAINT FK_Alerts_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com deleção em cascata.
    CONSTRAINT FK_Alerts_Locations FOREIGN KEY (LocationId) REFERENCES multitenant.Locations(LocationId), -- Chave estrangeira vinculada a Locations.
    CONSTRAINT FK_Alerts_AcknowledgedBy FOREIGN KEY (AcknowledgedBy) REFERENCES multitenant.Users(UserId), -- Chave estrangeira vinculada a Users.
    CONSTRAINT FK_Alerts_ResolvedBy FOREIGN KEY (ResolvedBy) REFERENCES multitenant.Users(UserId) -- Chave estrangeira vinculada a Users.
);
COMMENT ON TABLE multitenant.Alerts IS 'Esta tabela armazena os alertas gerados pelo sistema.';
COMMENT ON COLUMN multitenant.Alerts.AlertId IS 'Chave primária: Identificador único de cada alerta.';
COMMENT ON COLUMN multitenant.Alerts.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.Alerts.LocationId IS 'Chave estrangeira: Identificador do local (opcional).';
COMMENT ON COLUMN multitenant.Alerts.AlertType IS 'Campo de dados: Tipo de alerta (ex.: Segurança, Sistema).';
COMMENT ON COLUMN multitenant.Alerts.Severity IS 'Campo de dados: Nível de gravidade do alerta.';
COMMENT ON COLUMN multitenant.Alerts.Title IS 'Campo de dados: Título do alerta.';
COMMENT ON COLUMN multitenant.Alerts.Description IS 'Campo de dados: Descrição detalhada do alerta (opcional).';
COMMENT ON COLUMN multitenant.Alerts.RelatedRecordId IS 'Chave estrangeira: Identificador do registro relacionado (opcional).';
COMMENT ON COLUMN multitenant.Alerts.RelatedRecordType IS 'Campo de dados: Tipo de registro relacionado (opcional).';
COMMENT ON COLUMN multitenant.Alerts.Acknowledged IS 'Campo de controle: Indica se o alerta foi reconhecido.';
COMMENT ON COLUMN multitenant.Alerts.AcknowledgedBy IS 'Chave estrangeira: Identificador do usuário que reconheceu (opcional).';
COMMENT ON COLUMN multitenant.Alerts.AcknowledgedAt IS 'Campo de controle: Data e hora do reconhecimento (opcional).';
COMMENT ON COLUMN multitenant.Alerts.Resolved IS 'Campo de controle: Indica se o alerta foi resolvido.';
COMMENT ON COLUMN multitenant.Alerts.ResolvedBy IS 'Chave estrangeira: Identificador do usuário que resolveu (opcional).';
COMMENT ON COLUMN multitenant.Alerts.ResolvedAt IS 'Campo de controle: Data e hora da resolução (opcional).';
COMMENT ON COLUMN multitenant.Alerts.CreatedAt IS 'Campo de controle: Data e hora de criação do registro.';
GO

-- Tabela CrisisPlans
-- Esta tabela armazena os planos de gerenciamento de crises e emergências.
CREATE TABLE multitenant.CrisisPlans (
    PlanId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada plano de crise.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    PlanName NVARCHAR(100) NOT NULL, -- Campo de dados: Nome do plano de crise.
    Description NVARCHAR(1000) NULL, -- Campo de dados: Descrição detalhada do plano (opcional).
    Active BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se o plano está ativo.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de criação do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da última atualização (opcional).
    CONSTRAINT FK_CrisisPlans_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE -- Chave estrangeira vinculada a Tenants com deleção em cascata.
);
COMMENT ON TABLE multitenant.CrisisPlans IS 'Esta tabela armazena os planos de gerenciamento de crises e emergências.';
COMMENT ON COLUMN multitenant.CrisisPlans.PlanId IS 'Chave primária: Identificador único de cada plano de crise.';
COMMENT ON COLUMN multitenant.CrisisPlans.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.CrisisPlans.PlanName IS 'Campo de dados: Nome do plano de crise.';
COMMENT ON COLUMN multitenant.CrisisPlans.Description IS 'Campo de dados: Descrição detalhada do plano (opcional).';
COMMENT ON COLUMN multitenant.CrisisPlans.Active IS 'Campo de controle: Indica se o plano está ativo.';
COMMENT ON COLUMN multitenant.CrisisPlans.CreatedAt IS 'Campo de controle: Data e hora de criação do registro.';
COMMENT ON COLUMN multitenant.CrisisPlans.UpdatedAt IS 'Campo de controle: Data e hora da última atualização (opcional).';
GO

-- Tabela AuditLogs (Tabela Temporal para Imutabilidade)
-- Esta tabela registra todas as alterações para auditoria e conformidade.
CREATE TABLE audit.AuditLogs (
    AuditId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada log de auditoria.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    UserId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do usuário que realizou a ação (opcional).
    ActionType NVARCHAR(50) NOT NULL, -- Campo de dados: Tipo de ação realizada (ex.: Criar, Atualizar).
    TableName NVARCHAR(100) NOT NULL, -- Campo de dados: Nome da tabela afetada.
    RecordId NVARCHAR(100) NOT NULL, -- Campo de dados: Identificador do registro afetado.
    OldValues NVARCHAR(MAX) NULL, -- Campo de dados: Valores antigos em formato JSON (opcional).
    NewValues NVARCHAR(MAX) NULL, -- Campo de dados: Novos valores em formato JSON (opcional).
    IpAddress NVARCHAR(45) NULL, -- Campo de dados: Endereço IP da origem da ação (opcional).
    UserAgent NVARCHAR(500) NULL, -- Campo de dados: Agente do usuário da origem da ação (opcional).
    CreatedAt DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL, -- Campo de controle: Data e hora de início da validade do registro.
    UpdatedAt DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL, -- Campo de controle: Data e hora de término da validade do registro.
    PERIOD FOR SYSTEM_TIME (CreatedAt, UpdatedAt) -- Define o período temporal para versionamento.
) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = audit.AuditLogs_History)); -- Habilita versionamento de sistema para imutabilidade.
COMMENT ON TABLE audit.AuditLogs IS 'Esta tabela registra todas as alterações para auditoria e conformidade.';
COMMENT ON COLUMN audit.AuditLogs.AuditId IS 'Chave primária: Identificador único de cada log de auditoria.';
COMMENT ON COLUMN audit.AuditLogs.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN audit.AuditLogs.UserId IS 'Chave estrangeira: Identificador do usuário que realizou a ação (opcional).';
COMMENT ON COLUMN audit.AuditLogs.ActionType IS 'Campo de dados: Tipo de ação realizada (ex.: Criar, Atualizar).';
COMMENT ON COLUMN audit.AuditLogs.TableName IS 'Campo de dados: Nome da tabela afetada.';
COMMENT ON COLUMN audit.AuditLogs.RecordId IS 'Campo de dados: Identificador do registro afetado.';
COMMENT ON COLUMN audit.AuditLogs.OldValues IS 'Campo de dados: Valores antigos em formato JSON (opcional).';
COMMENT ON COLUMN audit.AuditLogs.NewValues IS 'Campo de dados: Novos valores em formato JSON (opcional).';
COMMENT ON COLUMN audit.AuditLogs.IpAddress IS 'Campo de dados: Endereço IP da origem da ação (opcional).';
COMMENT ON COLUMN audit.AuditLogs.UserAgent IS 'Campo de dados: Agente do usuário da origem da ação (opcional).';
COMMENT ON COLUMN audit.AuditLogs.CreatedAt IS 'Campo de controle: Data e hora de início da validade do registro.';
COMMENT ON COLUMN audit.AuditLogs.UpdatedAt IS 'Campo de controle: Data e hora de término da validade do registro.';
GO

-- Tabela UsageMetrics
-- Esta tabela rastreia as métricas de uso para faturamento e monitoramento.
CREATE TABLE billing.UsageMetrics (
    MetricId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada métrica.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    SubscriptionId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador da assinatura.
    MetricDate DATE NOT NULL, -- Campo de dados: Data da métrica.
    ActiveUsers INT NOT NULL DEFAULT 0, -- Campo de dados: Número de usuários ativos.
    VisitorCheckins INT NOT NULL DEFAULT 0, -- Campo de dados: Número de check-ins de visitantes.
    EmployeeCheckins INT NOT NULL DEFAULT 0, -- Campo de dados: Número de check-ins de funcionários.
    APICalls INT NOT NULL DEFAULT 0, -- Campo de dados: Número de chamadas à API.
    StorageMB DECIMAL(10,2) NOT NULL DEFAULT 0, -- Campo de dados: Uso de armazenamento em megabytes.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de criação da métrica.
    CONSTRAINT FK_UsageMetrics_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com deleção em cascata.
    CONSTRAINT FK_UsageMetrics_Subscriptions FOREIGN KEY (SubscriptionId) REFERENCES billing.Subscriptions(SubscriptionId), -- Chave estrangeira vinculada a Subscriptions.
    CONSTRAINT UK_UsageMetrics UNIQUE (TenantId, MetricDate) -- Restrição de unicidade: Garante métrica única por tenant por dia.
);
COMMENT ON TABLE billing.UsageMetrics IS 'Esta tabela rastreia as métricas de uso para faturamento e monitoramento.';
COMMENT ON COLUMN billing.UsageMetrics.MetricId IS 'Chave primária: Identificador único de cada métrica.';
COMMENT ON COLUMN billing.UsageMetrics.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN billing.UsageMetrics.SubscriptionId IS 'Chave estrangeira: Identificador da assinatura.';
COMMENT ON COLUMN billing.UsageMetrics.MetricDate IS 'Campo de dados: Data da métrica.';
COMMENT ON COLUMN billing.UsageMetrics.ActiveUsers IS 'Campo de dados: Número de usuários ativos.';
COMMENT ON COLUMN billing.UsageMetrics.VisitorCheckins IS 'Campo de dados: Número de check-ins de visitantes.';
COMMENT ON COLUMN billing.UsageMetrics.EmployeeCheckins IS 'Campo de dados: Número de check-ins de funcionários.';
COMMENT ON COLUMN billing.UsageMetrics.APICalls IS 'Campo de dados: Número de chamadas à API.';
COMMENT ON COLUMN billing.UsageMetrics.StorageMB IS 'Campo de dados: Uso de armazenamento em megabytes.';
COMMENT ON COLUMN billing.UsageMetrics.CreatedAt IS 'Campo de controle: Data e hora de criação da métrica.';
GO

-- Tabela Integrations (continuação)
-- Esta tabela armazena os detalhes de integrações com sistemas externos.
CREATE TABLE multitenant.Integrations (
    IntegrationId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada integração.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    IntegrationType NVARCHAR(50) NOT NULL, -- Campo de dados: Tipo de integração (ex.: IoT, ML).
    Name NVARCHAR(100) NOT NULL, -- Campo de dados: Nome da integração.
    Configuration NVARCHAR(MAX) NOT NULL, -- Campo de dados: Configuração da integração em formato JSON.
    Status NVARCHAR(20) NOT NULL DEFAULT 'Inactive', -- Campo de dados: Status atual da integração.
    LastSync DATETIME2 NULL, -- Campo de dados: Data e hora da última sincronização (opcional).
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de criação do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da última atualização (opcional).
    CONSTRAINT FK_Integrations_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE -- Chave estrangeira vinculada a Tenants com deleção em cascata.
);
COMMENT ON TABLE multitenant.Integrations IS 'Esta tabela armazena os detalhes de integrações com sistemas externos.';
COMMENT ON COLUMN multitenant.Integrations.IntegrationId IS 'Chave primária: Identificador único de cada integração.';
COMMENT ON COLUMN multitenant.Integrations.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.Integrations.IntegrationType IS 'Campo de dados: Tipo de integração (ex.: IoT, ML).';
COMMENT ON COLUMN multitenant.Integrations.Name IS 'Campo de dados: Nome da integração.';
COMMENT ON COLUMN multitenant.Integrations.Configuration IS 'Campo de dados: Configuração da integração em formato JSON.';
COMMENT ON COLUMN multitenant.Integrations.Status IS 'Campo de dados: Status atual da integração.';
COMMENT ON COLUMN multitenant.Integrations.LastSync IS 'Campo de dados: Data e hora da última sincronização (opcional).';
COMMENT ON COLUMN multitenant.Integrations.CreatedAt IS 'Campo de controle: Data e hora de criação do registro.';
COMMENT ON COLUMN multitenant.Integrations.UpdatedAt IS 'Campo de controle: Data e hora da última atualização (opcional).';
GO

-- Tabela AnomalyDetections
-- Esta tabela armazena as anomalias detectadas para análise de segurança e ML.
CREATE TABLE multitenant.AnomalyDetections (
    AnomalyId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave primária: Identificador único de cada anomalia.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    RecordId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do registro de acesso relacionado.
    AnomalyType NVARCHAR(50) NOT NULL, -- Campo de dados: Tipo de anomalia (ex.: Padrão de Acesso).
    AnomalyScore DECIMAL(5,2) NOT NULL, -- Campo de dados: Pontuação que indica a gravidade da anomalia.
    DetectedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora da detecção da anomalia.
    Description NVARCHAR(500) NULL, -- Campo de dados: Descrição da anomalia (opcional).
    CONSTRAINT FK_AnomalyDetections_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com deleção em cascata.
    CONSTRAINT FK_AnomalyDetections_Records FOREIGN KEY (RecordId) REFERENCES multitenant.AccessRecords(RecordId) -- Chave estrangeira vinculada a AccessRecords.
);
COMMENT ON TABLE multitenant.AnomalyDetections IS 'Esta tabela armazena as anomalias detectadas para análise de segurança e ML.';
COMMENT ON COLUMN multitenant.AnomalyDetections.AnomalyId IS 'Chave primária: Identificador único de cada anomalia.';
COMMENT ON COLUMN multitenant.AnomalyDetections.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.AnomalyDetections.RecordId IS 'Chave estrangeira: Identificador do registro de acesso relacionado.';
COMMENT ON COLUMN multitenant.AnomalyDetections.AnomalyType IS 'Campo de dados: Tipo de anomalia (ex.: Padrão de Acesso).';
COMMENT ON COLUMN multitenant.AnomalyDetections.AnomalyScore IS 'Campo de dados: Pontuação que indica a gravidade da anomalia.';
COMMENT ON COLUMN multitenant.AnomalyDetections.DetectedAt IS 'Campo de controle: Data e hora da detecção da anomalia.';
COMMENT ON COLUMN multitenant.AnomalyDetections.Description IS 'Campo de dados: Descrição da anomalia (opcional).';
GO

-- =============================================
-- Índices para Otimização de Performance
-- =============================================

CREATE CLUSTERED INDEX IX_AccessRecords_Tenant_Date 
ON multitenant.AccessRecords (TenantId, AccessDateTime)
ON PS_AccessDateTime (AccessDateTime);
-- Comentário: Índice clusterizado para otimizar consultas por tenant e data de acesso.

CREATE NONCLUSTERED INDEX IX_AccessRecords_Visitor 
ON multitenant.AccessRecords (TenantId, VisitorId, AccessDateTime);
-- Comentário: Índice não clusterizado para otimizar consultas por visitante e data.

CREATE NONCLUSTERED INDEX IX_AccessRecords_Employee 
ON multitenant.AccessRecords (TenantId, EmployeeId, AccessDateTime);
-- Comentário: Índice não clusterizado para otimizar consultas por funcionário e data.

CREATE NONCLUSTERED INDEX IX_AccessRecords_Device 
ON multitenant.AccessRecords (TenantId, DeviceId, AccessDateTime);
-- Comentário: Índice não clusterizado para otimizar consultas por dispositivo e data.

CREATE NONCLUSTERED INDEX IX_Visitors_Tenant_Document 
ON multitenant.Visitors (TenantId, DocumentType, DocumentNumber);
-- Comentário: Índice não clusterizado para otimizar consultas por documento de visitantes.

CREATE NONCLUSTERED INDEX IX_Visitors_Tenant_Name 
ON multitenant.Visitors (TenantId, LastName, FirstName);
-- Comentário: Índice não clusterizado para otimizar consultas por nome de visitantes.

CREATE NONCLUSTERED INDEX IX_Employees_Tenant_Document 
ON multitenant.Employees (TenantId, DocumentType, DocumentNumber);
-- Comentário: Índice não clusterizado para otimizar consultas por documento de funcionários.

CREATE NONCLUSTERED INDEX IX_Employees_Tenant_Name 
ON multitenant.Employees (TenantId, LastName, FirstName);
-- Comentário: Índice não clusterizado para otimizar consultas por nome de funcionários.

CREATE NONCLUSTERED INDEX IX_AuditLogs_Tenant_Date 
ON audit.AuditLogs (TenantId, CreatedAt);
-- Comentário: Índice não clusterizado para otimizar consultas por tenant e data de auditoria.

CREATE NONCLUSTERED INDEX IX_AuditLogs_User 
ON audit.AuditLogs (TenantId, UserId, CreatedAt);
-- Comentário: Índice não clusterizado para otimizar consultas por usuário e data.

CREATE NONCLUSTERED INDEX IX_UsageMetrics_Tenant_Date 
ON billing.UsageMetrics (TenantId, MetricDate);
-- Comentário: Índice não clusterizado para otimizar consultas por tenant e data de métricas.

CREATE NONCLUSTERED COLUMNSTORE INDEX CSI_AccessRecords 
ON multitenant.AccessRecords (TenantId, AccessDateTime, Status);
-- Comentário: Índice columnstore para otimizar análises em massa de registros de acesso.
GO

-- =============================================
-- Visões Indexadas para Relatórios
-- =============================================

CREATE VIEW reporting.AccessDashboard
WITH SCHEMABINDING
AS
SELECT 
    ar.TenantId, -- Chave estrangeira: Identificador do tenant.
    ar.LocationId, -- Chave estrangeira: Identificador do local.
    CAST(ar.AccessDateTime AS DATE) AS AccessDate, -- Campo de dados: Data dos eventos de acesso.
    COUNT_BIG(*) AS TotalAccesses, -- Campo de dados: Total de eventos de acesso.
    SUM(CASE WHEN ar.AccessType = 'Entry' THEN 1 ELSE 0 END) AS Entries, -- Campo de dados: Número de entradas.
    SUM(CASE WHEN ar.AccessType = 'Exit' THEN 1 ELSE 0 END) AS Exits, -- Campo de dados: Número de saídas.
    SUM(CASE WHEN ar.Status = 'Denied' THEN 1 ELSE 0 END) AS DeniedAccesses -- Campo de dados: Número de acessos negados.
FROM multitenant.AccessRecords ar
GROUP BY ar.TenantId, ar.LocationId, CAST(ar.AccessDateTime AS DATE);
GO

CREATE UNIQUE CLUSTERED INDEX IX_AccessDashboard
ON reporting.AccessDashboard (TenantId, LocationId, AccessDate);
-- Comentário: Índice clusterizado único para otimizar a visão de dashboard de acessos.
GO

CREATE VIEW reporting.VisitorReport
WITH SCHEMABINDING
AS
SELECT 
    v.TenantId, -- Chave estrangeira: Identificador do tenant.
    v.VisitorId, -- Chave estrangeira: Identificador do visitante.
    v.FirstName, -- Campo de dados: Primeiro nome do visitante.
    v.LastName, -- Campo de dados: Sobrenome do visitante.
    v.DocumentType, -- Campo de dados: Tipo de documento de identificação.
    v.DocumentNumber, -- Campo de dados: Número do documento de identificação.
    v.CompanyName, -- Campo de dados: Nome da empresa do visitante.
    COUNT_BIG(ar.RecordId) AS TotalVisits, -- Campo de dados: Total de visitas.
    MIN(ar.AccessDateTime) AS FirstVisit, -- Campo de dados: Data da primeira visita.
    MAX(ar.AccessDateTime) AS LastVisit -- Campo de dados: Data da última visita.
FROM multitenant.Visitors v
LEFT JOIN multitenant.AccessRecords ar ON v.VisitorId = ar.VisitorId AND v.TenantId = ar.TenantId
GROUP BY v.TenantId, v.VisitorId, v.FirstName, v.LastName, v.DocumentType, v.DocumentNumber, v.CompanyName;
GO

CREATE UNIQUE CLUSTERED INDEX IX_VisitorReport
ON reporting.VisitorReport (TenantId, VisitorId);
-- Comentário: Índice clusterizado único para otimizar a visão de relatório de visitantes.
GO

-- =============================================
-- Procedimentos Armazenados
-- =============================================

CREATE PROCEDURE multitenant.RegisterAccess
    @TenantId UNIQUEIDENTIFIER,
    @DeviceId UNIQUEIDENTIFIER,
    @DocumentNumber NVARCHAR(20) = NULL,
    @RFIDTag NVARCHAR(100) = NULL,
    @AccessType NVARCHAR(10),
    @Photo VARBINARY(MAX) = NULL,
    @Temperature DECIMAL(4,1) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        DECLARE @VisitorId UNIQUEIDENTIFIER = NULL;
        DECLARE @EmployeeId UNIQUEIDENTIFIER = NULL;
        DECLARE @AuthorizationId UNIQUEIDENTIFIER = NULL;
        DECLARE @Status NVARCHAR(20) = 'Granted';
        DECLARE @DenialReason NVARCHAR(200) = NULL;
        DECLARE @LocationId UNIQUEIDENTIFIER;
        DECLARE @BiometricMatchConfidence DECIMAL(5,2) = NULL;
        DECLARE @AnomalyScore DECIMAL(5,2) = NULL;
        DECLARE @TimeZoneOffset NVARCHAR(50);
        
        -- Obtém localização e fuso horário a partir do dispositivo
        SELECT @LocationId = l.LocationId, @TimeZoneOffset = l.TimeZone
        FROM multitenant.Devices d
        JOIN multitenant.Locations l ON d.LocationId = l.LocationId
        WHERE d.DeviceId = @DeviceId AND d.TenantId = @TenantId;
        
        IF @LocationId IS NULL
            THROW 50001, 'Dispositivo ou Tenant inválido', 1;
        
        -- Converte para o fuso horário local
        SET @AccessDateTime = DATEADD(hour, CASE WHEN @TimeZoneOffset LIKE '%-03' THEN -3 ELSE 0 END, SYSUTCDATETIME());
        
        -- Identifica visitante/funcionário
        IF @DocumentNumber IS NOT NULL
        BEGIN
            SELECT @VisitorId = VisitorId 
            FROM multitenant.Visitors 
            WHERE TenantId = @TenantId AND DocumentNumber = @DocumentNumber;
            
            SELECT @EmployeeId = EmployeeId 
            FROM multitenant.Employees 
            WHERE TenantId = @TenantId AND DocumentNumber = @DocumentNumber;
        END
        
        IF @RFIDTag IS NOT NULL
        BEGIN
            SELECT @EmployeeId = EmployeeId 
            FROM multitenant.Employees 
            WHERE TenantId = @TenantId AND RFIDTag = @RFIDTag;
        END
        
        -- Verifica autorização e zonas
        IF @VisitorId IS NOT NULL
        BEGIN
            SELECT TOP 1 @AuthorizationId = AuthorizationId
            FROM multitenant.AccessAuthorizations
            WHERE TenantId = @TenantId 
              AND VisitorId = @VisitorId 
              AND ValidFrom <= @AccessDateTime 
              AND ValidUntil >= @AccessDateTime
              AND Status = 'Approved'
            ORDER BY CreatedAt DESC;
            
            IF @AuthorizationId IS NULL
            BEGIN
                SET @Status = 'Denied';
                SET @DenialReason = 'Nenhuma autorização válida encontrada';
            END
            ELSE
            BEGIN
                -- Verifica permissões de zona dinamicamente
                DECLARE @Role NVARCHAR(50) = 'Visitor';
                IF @EmployeeId IS NOT NULL SET @Role = 'Employee';
                
                IF NOT EXISTS (
                    SELECT 1 FROM multitenant.ZonePermissions zp
                    JOIN multitenant.RestrictedZones rz ON zp.ZoneId = rz.ZoneId
                    WHERE rz.TenantId = @TenantId AND zp.Role = @Role AND zp.AccessLevel = 'Granted'
                )
                BEGIN
                    SET @Status = 'Denied';
                    SET @DenialReason = 'Acesso à zona restrito';
                END
            END
        END
        
        -- Verifica emergência
        IF EXISTS (SELECT 1 FROM multitenant.Alerts WHERE TenantId = @TenantId AND Severity = 'Critical' AND Resolved = 0)
        BEGIN
            SET @Status = 'Denied';
            SET @DenialReason = 'Bloqueio de emergência em efeito';
        END
        
        -- Registra acesso
        INSERT INTO multitenant.AccessRecords (
            TenantId, LocationId, DeviceId, VisitorId, EmployeeId, 
            AuthorizationId, AccessType, DocumentNumber, RFIDTag, 
            BiometricMatchConfidence, AnomalyScore, PhotoTaken, Temperature, Status, DenialReason, AccessDateTime
        )
        VALUES (
            @TenantId, @LocationId, @DeviceId, @VisitorId, @EmployeeId,
            @AuthorizationId, @AccessType, @DocumentNumber, @RFIDTag,
            @BiometricMatchConfidence, @AnomalyScore, @Photo, @Temperature, @Status, @DenialReason, @AccessDateTime
        );
        
        -- Calcula pontuação de anomalia
        SET @AnomalyScore = CASE WHEN @Status = 'Denied' THEN 1.0 ELSE 0.0 END;
        IF @AnomalyScore > 0
        BEGIN
            INSERT INTO multitenant.AnomalyDetections (TenantId, RecordId, AnomalyType, AnomalyScore, Description)
            VALUES (@TenantId, SCOPE_IDENTITY(), 'AccessPattern', @AnomalyScore, @DenialReason);
        END
        
        COMMIT TRANSACTION;
        SELECT 
            @Status AS AccessStatus, 
            @DenialReason AS DenialReason,
            SCOPE_IDENTITY() AS RecordId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

CREATE PROCEDURE billing.UpdateUsageMetrics
    @TenantId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        DECLARE @SubscriptionId UNIQUEIDENTIFIER;
        SELECT @SubscriptionId = SubscriptionId 
        FROM billing.Subscriptions 
        WHERE TenantId = @TenantId AND PaymentStatus = 'Active';
        
        IF @SubscriptionId IS NOT NULL
        BEGIN
            MERGE billing.UsageMetrics AS target
            USING (
                SELECT 
                    @TenantId AS TenantId,
                    @SubscriptionId AS SubscriptionId,
                    CAST(SYSUTCDATETIME() AS DATE) AS MetricDate,
                    (SELECT COUNT(*) FROM multitenant.Users WHERE TenantId = @TenantId AND IsActive = 1) AS ActiveUsers,
                    (SELECT COUNT(*) FROM multitenant.AccessRecords WHERE TenantId = @TenantId AND AccessType = 'Entry' AND VisitorId IS NOT NULL AND CAST(AccessDateTime AS DATE) = CAST(SYSUTCDATETIME() AS DATE)) AS VisitorCheckins,
                    (SELECT COUNT(*) FROM multitenant.AccessRecords WHERE TenantId = @TenantId AND AccessType = 'Entry' AND EmployeeId IS NOT NULL AND CAST(AccessDateTime AS DATE) = CAST(SYSUTCDATETIME() AS DATE)) AS EmployeeCheckins,
                    0 AS APICalls, -- Placeholder para chamadas de API (a ser rastreado via aplicativo)
                    (SELECT ISNULL(SUM(DATALENGTH(PhotoTaken)) / 1024.0 / 1024.0, 0) FROM multitenant.AccessRecords WHERE TenantId = @TenantId) AS StorageMB
            ) AS source
            ON (target.TenantId = source.TenantId AND target.MetricDate = source.MetricDate)
            WHEN MATCHED THEN
                UPDATE SET 
                    target.ActiveUsers = source.ActiveUsers,
                    target.VisitorCheckins = source.VisitorCheckins,
                    target.EmployeeCheckins = source.EmployeeCheckins,
                    target.APICalls = source.APICalls,
                    target.StorageMB = source.StorageMB
            WHEN NOT MATCHED THEN
                INSERT (TenantId, SubscriptionId, MetricDate, ActiveUsers, VisitorCheckins, EmployeeCheckins, APICalls, StorageMB)
                VALUES (source.TenantId, source.SubscriptionId, source.MetricDate, source.ActiveUsers, source.VisitorCheckins, source.EmployeeCheckins, source.APICalls, source.StorageMB);
        END
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

CREATE PROCEDURE multitenant.CleanExpiredData
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        DELETE FROM multitenant.AccessRecords
        WHERE DataExpirationDate < SYSUTCDATETIME();
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- =============================================
-- Gatilhos
-- =============================================

CREATE TRIGGER multitenant.Visitors_AuditTrigger
ON multitenant.Visitors
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @TenantId UNIQUEIDENTIFIER;
    DECLARE @UserId UNIQUEIDENTIFIER;
    DECLARE cur CURSOR FOR 
        SELECT DISTINCT TenantId FROM inserted UNION SELECT DISTINCT TenantId FROM deleted;
    
    OPEN cur;
    FETCH NEXT FROM cur INTO @TenantId;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @UserId = CAST(COALESCE(SESSION_CONTEXT(N'UserId'), (SELECT TOP 1 UserId FROM multitenant.Users WHERE TenantId = @TenantId AND Role = 'Admin')) AS UNIQUEIDENTIFIER);
        
        IF @TenantId IS NULL OR @UserId IS NULL
            THROW 50002, 'Contexto de Tenant ou Usuário não definido', 1;
        
        INSERT INTO audit.AuditLogs (TenantId, UserId, ActionType, TableName, RecordId, OldValues, NewValues)
        SELECT
            @TenantId,
            @UserId,
            CASE 
                WHEN i.VisitorId IS NOT NULL AND d.VisitorId IS NOT NULL THEN 'Update'
                WHEN i.VisitorId IS NOT NULL THEN 'Create'
                ELSE 'Delete'
            END,
            'Visitors',
            COALESCE(i.VisitorId, d.VisitorId),
            (SELECT * FROM deleted WHERE TenantId = @TenantId FOR JSON AUTO),
            (SELECT * FROM inserted WHERE TenantId = @TenantId FOR JSON AUTO)
        FROM inserted i
        FULL OUTER JOIN deleted d ON i.VisitorId = d.VisitorId
        WHERE i.TenantId = @TenantId OR d.TenantId = @TenantId;
        
        FETCH NEXT FROM cur INTO @TenantId;
    END
    
    CLOSE cur;
    DEALLOCATE cur;
END
GO

CREATE TRIGGER multitenant.AccessRecords_DataExpirationTrigger
ON multitenant.AccessRecords
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE a
    SET DataExpirationDate = DATEADD(day, tc.DataRetentionDays, a.CreatedAt)
    FROM multitenant.AccessRecords a
    JOIN inserted i ON a.RecordId = i.RecordId
    JOIN saas.TenantConfigurations tc ON i.TenantId = tc.TenantId;
END
GO

CREATE TRIGGER multitenant.AccessRecords_AnomalyTrigger
ON multitenant.AccessRecords
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO multitenant.AnomalyDetections (TenantId, RecordId, AnomalyType, AnomalyScore, Description)
    SELECT 
        i.TenantId,
        i.RecordId,
        'AccessPattern',
        CASE WHEN i.Status = 'Denied' THEN 1.0 ELSE 0.0 END,
        i.DenialReason
    FROM inserted i
    WHERE i.Status = 'Denied';
END
GO

-- =============================================
-- Segurança de Nível de Linha (RLS)
-- =============================================

CREATE FUNCTION security.TenantAccessPredicate(@TenantId UNIQUEIDENTIFIER)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN SELECT 1 AS accessResult
WHERE @TenantId = CAST(COALESCE(SESSION_CONTEXT(N'TenantId'), (SELECT TOP 1 TenantId FROM saas.Tenants WHERE IsActive = 1)) AS UNIQUEIDENTIFIER)
OR DATABASE_PRINCIPAL_ID() = DATABASE_PRINCIPAL_ID('dbo');
GO

CREATE SECURITY POLICY security.TenantSecurityPolicy
ADD FILTER PREDICATE security.TenantAccessPredicate(TenantId) ON multitenant.Visitors,
ADD FILTER PREDICATE security.TenantAccessPredicate(TenantId) ON multitenant.Employees,
ADD FILTER PREDICATE security.TenantAccessPredicate(TenantId) ON multitenant.AccessRecords,
ADD FILTER PREDICATE security.TenantAccessPredicate(TenantId) ON multitenant.AccessAuthorizations,
ADD FILTER PREDICATE security.TenantAccessPredicate(TenantId) ON multitenant.Devices,
ADD FILTER PREDICATE security.TenantAccessPredicate(TenantId) ON multitenant.RestrictedZones,
ADD FILTER PREDICATE security.TenantAccessPredicate(TenantId) ON multitenant.ZonePermissions,
ADD FILTER PREDICATE security.TenantAccessPredicate(TenantId) ON multitenant.Alerts,
ADD FILTER PREDICATE security.TenantAccessPredicate(TenantId) ON multitenant.Integrations,
ADD FILTER PREDICATE security.TenantAccessPredicate(TenantId) ON audit.AuditLogs,
ADD FILTER PREDICATE security.TenantAccessPredicate(TenantId) ON billing.UsageMetrics,
ADD FILTER PREDICATE security.TenantAccessPredicate(TenantId) ON multitenant.AnomalyDetections,
ADD FILTER PREDICATE security.TenantAccessPredicate(TenantId) ON multitenant.VisitorGroups,
ADD FILTER PREDICATE security.TenantAccessPredicate(TenantId) ON multitenant.VisitorGroupMembers,
ADD FILTER PREDICATE security.TenantAccessPredicate(TenantId) ON multitenant.CrisisPlans
WITH (STATE = ON);
GO

-- =============================================
-- Dados de Teste Iniciais
-- =============================================

INSERT INTO saas.Tenants (CompanyName, CompanyDocument, ContactName, ContactEmail, ContactPhone)
VALUES 
('Indústria Exemplo', '12345678901234', 'João Silva', 'joao@exemplo.com', '11987654321'),
('Comércio Teste', '98765432101234', 'Maria Oliveira', 'maria@teste.com', '21976543210');
GO

INSERT INTO billing.SubscriptionPlans (PlanName, Description, MaxUsers, MaxLocations, MaxDevices, MaxVisitorsPerMonth, RetentionPeriodDays, HasBiometricIntegration, HasAPIAccess, HasAdvancedReporting, MonthlyPrice)
VALUES 
('Plano Básico', 'Plano com funcionalidades básicas', 10, 1, 2, 100, 90, 0, 0, 0, 99.90),
('Plano Premium', 'Plano com recursos avançados', 50, 5, 10, NULL, 365, 1, 1, 1, 299.90);
GO

INSERT INTO billing.Subscriptions (TenantId, PlanId, StartDate, EndDate, AutoRenew, PaymentMethod, PaymentStatus)
VALUES 
((SELECT TenantId FROM saas.Tenants WHERE CompanyName = 'Indústria Exemplo'), 1, '2025-08-01', '2025-08-31', 1, 'Credit Card', 'Active'),
((SELECT TenantId FROM saas.Tenants WHERE CompanyName = 'Comércio Teste'), 2, '2025-08-01', '2025-08-31', 1, 'Credit Card', 'Active');
GO

INSERT INTO multitenant.Locations (TenantId, Name, Address, City, State, Country)
VALUES 
((SELECT TenantId FROM saas.Tenants WHERE CompanyName = 'Indústria Exemplo'), 'Escritório Principal', 'Rua Exemplo 123', 'São Paulo', 'SP', 'Brasil'),
((SELECT TenantId FROM saas.Tenants WHERE CompanyName = 'Comércio Teste'), 'Loja Central', 'Av. Teste 456', 'Rio de Janeiro', 'RJ', 'Brasil');
GO

INSERT INTO multitenant.Users (TenantId, LocationId, Username, PasswordHash, Email, FirstName, LastName, DocumentType, DocumentNumber, PhoneNumber, Role)
VALUES 
((SELECT TenantId FROM saas.Tenants WHERE CompanyName = 'Indústria Exemplo'), (SELECT LocationId FROM multitenant.Locations WHERE Name = 'Escritório Principal'), 'joao.admin', 'hash123', 'joao.admin@exemplo.com', 'João', 'Silva', 'CPF', '12345678901', '11987654321', 'Admin'),
((SELECT TenantId FROM saas.Tenants WHERE CompanyName = 'Comércio Teste'), (SELECT LocationId FROM multitenant.Locations WHERE Name = 'Loja Central'), 'maria.op', 'hash456', 'maria.op@teste.com', 'Maria', 'Oliveira', 'CPF', '98765432101', '21976543210', 'Operator');
GO