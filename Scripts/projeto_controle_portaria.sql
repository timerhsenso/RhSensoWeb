-- =============================================
-- Inicializa��o do Banco de Dados e Configura��o de Seguran�a
-- =============================================
-- Criando o banco de dados PortariaSaaS com suporte a UTF-8 para internacionaliza��o
USE master;
GO

-- Criando o banco de dados se n�o existir
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'PortariaSaaS')
CREATE DATABASE PortariaSaaS 
    COLLATE Latin1_General_100_CI_AI_SC_UTF8;
GO

-- Selecionando o banco de dados para opera��es subsequentes
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

-- Configurando grupo de arquivos FILESTREAM para armazenar dados bin�rios grandes
ALTER DATABASE PortariaSaaS 
    ADD FILEGROUP FsGroup CONTAINS FILESTREAM;
GO

ALTER DATABASE PortariaSaaS 
    ADD FILE (NAME = 'FsFiles', FILENAME = 'C:\FsData\PortariaSaaS') 
    TO FILEGROUP FsGroup;
GO

-- Comando de backup do certificado (execu��o manual requerida para recupera��o)
-- BACKUP CERTIFICATE CertificadoPortaria TO FILE = 'C:\Backups\CertificadoPortaria.cer' 
-- WITH PRIVATE KEY (FILE = 'C:\Backups\CertificadoPortaria.pvk', ENCRYPTION BY PASSWORD = 'BackupPassword2025#');
GO

-- =============================================
-- Cria��o de Schemas
-- =============================================
-- Criando schemas para organizar objetos por funcionalidade
CREATE SCHEMA saas;          -- Schema para dados relacionados a tenants
GO
CREATE SCHEMA multitenant;   -- Schema para entidades multitenant como usu�rios e acessos
GO
CREATE SCHEMA audit;         -- Schema para dados de auditoria e logs
GO
CREATE SCHEMA billing;       -- Schema para dados de faturamento e assinaturas
GO
CREATE SCHEMA security;      -- Schema para pol�ticas e predicados de seguran�a
GO
CREATE SCHEMA reporting;     -- Schema para vis�es de relat�rios e dashboards
GO

-- =============================================
-- Tabelas Principais
-- =============================================

-- Tabela Tenants
-- Esta tabela armazena os dados dos tenants (empresas) que utilizam o sistema SaaS.
CREATE TABLE saas.Tenants (
    TenantId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada tenant.
    CompanyName NVARCHAR(200) NOT NULL, -- Campo de dados: Nome da empresa do tenant.
    CompanyDocument NVARCHAR(20) NOT NULL, -- Campo de dados: N�mero do documento legal (ex.: CNPJ) do tenant.
    ContactName NVARCHAR(100) NOT NULL, -- Campo de dados: Nome do contato principal do tenant.
    ContactEmail NVARCHAR(100) NOT NULL, -- Campo de dados: Endere�o de e-mail do contato principal.
    ContactPhone NVARCHAR(20) NULL, -- Campo de dados: N�mero de telefone do contato principal (opcional).
    Address NVARCHAR(200) NULL, -- Campo de dados: Endere�o f�sico do tenant (opcional).
    City NVARCHAR(100) NULL, -- Campo de dados: Cidade do tenant (opcional).
    State NVARCHAR(50) NULL, -- Campo de dados: Estado do tenant (opcional).
    Country NVARCHAR(50) NULL DEFAULT 'Brasil', -- Campo de dados: Pa�s do tenant (padr�o: Brasil, opcional).
    TimeZone NVARCHAR(50) NULL DEFAULT 'E. South America Standard Time', -- Campo de dados: Fuso hor�rio do tenant (padr�o: hor�rio de Bras�lia).
    LanguageCode NVARCHAR(10) NULL DEFAULT 'pt-BR', -- Campo de dados: C�digo do idioma da interface (padr�o: portugu�s do Brasil).
    IsActive BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se o tenant est� ativo.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de cria��o do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da �ltima atualiza��o (opcional).
    DeactivatedAt DATETIME2 NULL -- Campo de controle: Data e hora de desativa��o (opcional).
);
COMMENT ON TABLE saas.Tenants IS 'Esta tabela armazena os dados dos tenants (empresas) que utilizam o sistema SaaS.';
COMMENT ON COLUMN saas.Tenants.TenantId IS 'Chave prim�ria: Identificador �nico de cada tenant.';
COMMENT ON COLUMN saas.Tenants.CompanyName IS 'Campo de dados: Nome da empresa do tenant.';
COMMENT ON COLUMN saas.Tenants.CompanyDocument IS 'Campo de dados: N�mero do documento legal (ex.: CNPJ) do tenant.';
COMMENT ON COLUMN saas.Tenants.ContactName IS 'Campo de dados: Nome do contato principal do tenant.';
COMMENT ON COLUMN saas.Tenants.ContactEmail IS 'Campo de dados: Endere�o de e-mail do contato principal.';
COMMENT ON COLUMN saas.Tenants.ContactPhone IS 'Campo de dados: N�mero de telefone do contato principal (opcional).';
COMMENT ON COLUMN saas.Tenants.Address IS 'Campo de dados: Endere�o f�sico do tenant (opcional).';
COMMENT ON COLUMN saas.Tenants.City IS 'Campo de dados: Cidade do tenant (opcional).';
COMMENT ON COLUMN saas.Tenants.State IS 'Campo de dados: Estado do tenant (opcional).';
COMMENT ON COLUMN saas.Tenants.Country IS 'Campo de dados: Pa�s do tenant (padr�o: Brasil, opcional).';
COMMENT ON COLUMN saas.Tenants.TimeZone IS 'Campo de dados: Fuso hor�rio do tenant (padr�o: hor�rio de Bras�lia).';
COMMENT ON COLUMN saas.Tenants.LanguageCode IS 'Campo de dados: C�digo do idioma da interface (padr�o: portugu�s do Brasil).';
COMMENT ON COLUMN saas.Tenants.IsActive IS 'Campo de controle: Indica se o tenant est� ativo.';
COMMENT ON COLUMN saas.Tenants.CreatedAt IS 'Campo de controle: Data e hora de cria��o do registro.';
COMMENT ON COLUMN saas.Tenants.UpdatedAt IS 'Campo de controle: Data e hora da �ltima atualiza��o (opcional).';
COMMENT ON COLUMN saas.Tenants.DeactivatedAt IS 'Campo de controle: Data e hora de desativa��o (opcional).';
GO

-- Tabela SubscriptionPlans
-- Esta tabela define os planos de assinatura dispon�veis para os tenants.
CREATE TABLE billing.SubscriptionPlans (
    PlanId INT IDENTITY(1,1) PRIMARY KEY, -- Chave prim�ria: Identificador �nico e auto-incremental de cada plano.
    PlanName NVARCHAR(50) NOT NULL, -- Campo de dados: Nome do plano de assinatura.
    Description NVARCHAR(500) NULL, -- Campo de dados: Descri��o das funcionalidades do plano (opcional).
    MaxUsers INT NOT NULL, -- Campo de dados: N�mero m�ximo de usu�rios permitidos.
    MaxLocations INT NOT NULL, -- Campo de dados: N�mero m�ximo de locais permitidos.
    MaxDevices INT NOT NULL, -- Campo de dados: N�mero m�ximo de dispositivos permitidos.
    MaxVisitorsPerMonth INT NULL, -- Campo de dados: N�mero m�ximo de visitantes por m�s (nulo para ilimitado).
    RetentionPeriodDays INT NOT NULL DEFAULT 90, -- Campo de dados: Per�odo de reten��o de dados em dias.
    HasBiometricIntegration BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se a integra��o biom�trica est� inclu�da.
    HasAPIAccess BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se o acesso � API est� inclu�do.
    HasAdvancedReporting BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se os relat�rios avan�ados est�o inclu�dos.
    MonthlyPrice DECIMAL(10,2) NOT NULL, -- Campo de dados: Pre�o mensal do plano.
    IsActive BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se o plano est� ativo.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME() -- Campo de controle: Data e hora de cria��o do plano.
);
COMMENT ON TABLE billing.SubscriptionPlans IS 'Esta tabela define os planos de assinatura dispon�veis para os tenants.';
COMMENT ON COLUMN billing.SubscriptionPlans.PlanId IS 'Chave prim�ria: Identificador �nico e auto-incremental de cada plano.';
COMMENT ON COLUMN billing.SubscriptionPlans.PlanName IS 'Campo de dados: Nome do plano de assinatura.';
COMMENT ON COLUMN billing.SubscriptionPlans.Description IS 'Campo de dados: Descri��o das funcionalidades do plano (opcional).';
COMMENT ON COLUMN billing.SubscriptionPlans.MaxUsers IS 'Campo de dados: N�mero m�ximo de usu�rios permitidos.';
COMMENT ON COLUMN billing.SubscriptionPlans.MaxLocations IS 'Campo de dados: N�mero m�ximo de locais permitidos.';
COMMENT ON COLUMN billing.SubscriptionPlans.MaxDevices IS 'Campo de dados: N�mero m�ximo de dispositivos permitidos.';
COMMENT ON COLUMN billing.SubscriptionPlans.MaxVisitorsPerMonth IS 'Campo de dados: N�mero m�ximo de visitantes por m�s (nulo para ilimitado).';
COMMENT ON COLUMN billing.SubscriptionPlans.RetentionPeriodDays IS 'Campo de dados: Per�odo de reten��o de dados em dias.';
COMMENT ON COLUMN billing.SubscriptionPlans.HasBiometricIntegration IS 'Campo de controle: Indica se a integra��o biom�trica est� inclu�da.';
COMMENT ON COLUMN billing.SubscriptionPlans.HasAPIAccess IS 'Campo de controle: Indica se o acesso � API est� inclu�do.';
COMMENT ON COLUMN billing.SubscriptionPlans.HasAdvancedReporting IS 'Campo de controle: Indica se os relat�rios avan�ados est�o inclu�dos.';
COMMENT ON COLUMN billing.SubscriptionPlans.MonthlyPrice IS 'Campo de dados: Pre�o mensal do plano.';
COMMENT ON COLUMN billing.SubscriptionPlans.IsActive IS 'Campo de controle: Indica se o plano est� ativo.';
COMMENT ON COLUMN billing.SubscriptionPlans.CreatedAt IS 'Campo de controle: Data e hora de cria��o do plano.';
GO

-- Tabela Subscriptions
-- Esta tabela rastreia as assinaturas dos tenants aos planos.
CREATE TABLE billing.Subscriptions (
    SubscriptionId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada assinatura.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    PlanId INT NOT NULL, -- Chave estrangeira: Identificador do plano assinado.
    StartDate DATETIME2 NOT NULL, -- Campo de dados: Data de in�cio da assinatura.
    EndDate DATETIME2 NOT NULL, -- Campo de dados: Data de t�rmino da assinatura.
    AutoRenew BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se a assinatura renova automaticamente.
    PaymentMethod NVARCHAR(20) NULL, -- Campo de dados: M�todo de pagamento utilizado (opcional).
    PaymentStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Campo de dados: Status atual do pagamento.
    StripeSubscriptionId NVARCHAR(100) NULL, -- Campo de dados: ID da assinatura no Stripe (opcional).
    StripeCustomerId NVARCHAR(100) NULL, -- Campo de dados: ID do cliente no Stripe (opcional).
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de cria��o da assinatura.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da �ltima atualiza��o (opcional).
    CONSTRAINT FK_Subscriptions_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com dele��o em cascata.
    CONSTRAINT FK_Subscriptions_Plans FOREIGN KEY (PlanId) REFERENCES billing.SubscriptionPlans(PlanId) -- Chave estrangeira vinculada a SubscriptionPlans.
);
COMMENT ON TABLE billing.Subscriptions IS 'Esta tabela rastreia as assinaturas dos tenants aos planos.';
COMMENT ON COLUMN billing.Subscriptions.SubscriptionId IS 'Chave prim�ria: Identificador �nico de cada assinatura.';
COMMENT ON COLUMN billing.Subscriptions.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN billing.Subscriptions.PlanId IS 'Chave estrangeira: Identificador do plano assinado.';
COMMENT ON COLUMN billing.Subscriptions.StartDate IS 'Campo de dados: Data de in�cio da assinatura.';
COMMENT ON COLUMN billing.Subscriptions.EndDate IS 'Campo de dados: Data de t�rmino da assinatura.';
COMMENT ON COLUMN billing.Subscriptions.AutoRenew IS 'Campo de controle: Indica se a assinatura renova automaticamente.';
COMMENT ON COLUMN billing.Subscriptions.PaymentMethod IS 'Campo de dados: M�todo de pagamento utilizado (opcional).';
COMMENT ON COLUMN billing.Subscriptions.PaymentStatus IS 'Campo de dados: Status atual do pagamento.';
COMMENT ON COLUMN billing.Subscriptions.StripeSubscriptionId IS 'Campo de dados: ID da assinatura no Stripe (opcional).';
COMMENT ON COLUMN billing.Subscriptions.StripeCustomerId IS 'Campo de dados: ID do cliente no Stripe (opcional).';
COMMENT ON COLUMN billing.Subscriptions.CreatedAt IS 'Campo de controle: Data e hora de cria��o da assinatura.';
COMMENT ON COLUMN billing.Subscriptions.UpdatedAt IS 'Campo de controle: Data e hora da �ltima atualiza��o (opcional).';
GO

-- Tabela TenantConfigurations
-- Esta tabela armazena as configura��es personaliz�veis de cada tenant.
CREATE TABLE saas.TenantConfigurations (
    ConfigId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada configura��o.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    Logo VARBINARY(MAX) FILESTREAM NULL, -- Campo de dados: Logotipo do tenant armazenado como dado bin�rio (opcional).
    PrimaryColor NVARCHAR(7) NULL DEFAULT '#1976D2', -- Campo de dados: Cor prim�ria da interface (padr�o: azul, opcional).
    SecondaryColor NVARCHAR(7) NULL DEFAULT '#424242', -- Campo de dados: Cor secund�ria da interface (padr�o: cinza, opcional).
    CheckinTimeoutMinutes INT NOT NULL DEFAULT 120, -- Campo de dados: Tempo limite para check-ins em minutos.
    RequireVisitorDocument BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se o documento do visitante � obrigat�rio.
    RequireVisitorPhoto BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se a foto do visitante � obrigat�ria.
    RequirePreAuthorization BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se a pr�-autoriza��o � obrigat�ria.
    EmailNotificationsEnabled BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se as notifica��es por e-mail est�o ativadas.
    SMSNotificationsEnabled BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se as notifica��es por SMS est�o ativadas.
    DefaultLanguage NVARCHAR(10) NOT NULL DEFAULT 'pt-BR', -- Campo de dados: Idioma padr�o do tenant.
    DataRetentionDays INT NOT NULL DEFAULT 365, -- Campo de dados: N�mero de dias para reten��o de dados.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de cria��o da configura��o.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da �ltima atualiza��o (opcional).
    CONSTRAINT FK_TenantConfigurations_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com dele��o em cascata.
    CONSTRAINT UK_TenantConfigurations_Tenant UNIQUE (TenantId) -- Restri��o de unicidade: Garante uma configura��o por tenant.
);
COMMENT ON TABLE saas.TenantConfigurations IS 'Esta tabela armazena as configura��es personaliz�veis de cada tenant.';
COMMENT ON COLUMN saas.TenantConfigurations.ConfigId IS 'Chave prim�ria: Identificador �nico de cada configura��o.';
COMMENT ON COLUMN saas.TenantConfigurations.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN saas.TenantConfigurations.Logo IS 'Campo de dados: Logotipo do tenant armazenado como dado bin�rio (opcional).';
COMMENT ON COLUMN saas.TenantConfigurations.PrimaryColor IS 'Campo de dados: Cor prim�ria da interface (padr�o: azul, opcional).';
COMMENT ON COLUMN saas.TenantConfigurations.SecondaryColor IS 'Campo de dados: Cor secund�ria da interface (padr�o: cinza, opcional).';
COMMENT ON COLUMN saas.TenantConfigurations.CheckinTimeoutMinutes IS 'Campo de dados: Tempo limite para check-ins em minutos.';
COMMENT ON COLUMN saas.TenantConfigurations.RequireVisitorDocument IS 'Campo de controle: Indica se o documento do visitante � obrigat�rio.';
COMMENT ON COLUMN saas.TenantConfigurations.RequireVisitorPhoto IS 'Campo de controle: Indica se a foto do visitante � obrigat�ria.';
COMMENT ON COLUMN saas.TenantConfigurations.RequirePreAuthorization IS 'Campo de controle: Indica se a pr�-autoriza��o � obrigat�ria.';
COMMENT ON COLUMN saas.TenantConfigurations.EmailNotificationsEnabled IS 'Campo de controle: Indica se as notifica��es por e-mail est�o ativadas.';
COMMENT ON COLUMN saas.TenantConfigurations.SMSNotificationsEnabled IS 'Campo de controle: Indica se as notifica��es por SMS est�o ativadas.';
COMMENT ON COLUMN saas.TenantConfigurations.DefaultLanguage IS 'Campo de dados: Idioma padr�o do tenant.';
COMMENT ON COLUMN saas.TenantConfigurations.DataRetentionDays IS 'Campo de dados: N�mero de dias para reten��o de dados.';
COMMENT ON COLUMN saas.TenantConfigurations.CreatedAt IS 'Campo de controle: Data e hora de cria��o da configura��o.';
COMMENT ON COLUMN saas.TenantConfigurations.UpdatedAt IS 'Campo de controle: Data e hora da �ltima atualiza��o (opcional).';
GO

-- Tabela TenantSettings
-- Esta tabela armazena configura��es din�micas chave-valor para os tenants.
CREATE TABLE saas.TenantSettings (
    SettingId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada configura��o.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    SettingKey NVARCHAR(100) NOT NULL, -- Campo de dados: Chave da configura��o din�mica.
    SettingValue NVARCHAR(MAX) NOT NULL, -- Campo de dados: Valor da configura��o din�mica.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de cria��o da configura��o.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da �ltima atualiza��o (opcional).
    CONSTRAINT FK_TenantSettings_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com dele��o em cascata.
    CONSTRAINT UK_TenantSettings_Key UNIQUE (TenantId, SettingKey) -- Restri��o de unicidade: Garante chave �nica por tenant.
);
COMMENT ON TABLE saas.TenantSettings IS 'Esta tabela armazena configura��es din�micas chave-valor para os tenants.';
COMMENT ON COLUMN saas.TenantSettings.SettingId IS 'Chave prim�ria: Identificador �nico de cada configura��o.';
COMMENT ON COLUMN saas.TenantSettings.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN saas.TenantSettings.SettingKey IS 'Campo de dados: Chave da configura��o din�mica.';
COMMENT ON COLUMN saas.TenantSettings.SettingValue IS 'Campo de dados: Valor da configura��o din�mica.';
COMMENT ON COLUMN saas.TenantSettings.CreatedAt IS 'Campo de controle: Data e hora de cria��o da configura��o.';
COMMENT ON COLUMN saas.TenantSettings.UpdatedAt IS 'Campo de controle: Data e hora da �ltima atualiza��o (opcional).';
GO

-- Tabela Locations
-- Esta tabela armazena os locais f�sicos gerenciados pelos tenants.
CREATE TABLE multitenant.Locations (
    LocationId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada local.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    Name NVARCHAR(100) NOT NULL, -- Campo de dados: Nome do local.
    Address NVARCHAR(200) NOT NULL, -- Campo de dados: Endere�o f�sico do local.
    City NVARCHAR(100) NOT NULL, -- Campo de dados: Cidade do local.
    State NVARCHAR(50) NOT NULL, -- Campo de dados: Estado do local.
    Country NVARCHAR(50) NOT NULL DEFAULT 'Brasil', -- Campo de dados: Pa�s do local (padr�o: Brasil).
    TimeZone NVARCHAR(50) NOT NULL DEFAULT 'E. South America Standard Time', -- Campo de dados: Fuso hor�rio do local.
    IsActive BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se o local est� ativo.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de cria��o do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da �ltima atualiza��o (opcional).
    CONSTRAINT FK_Locations_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE -- Chave estrangeira vinculada a Tenants com dele��o em cascata.
);
COMMENT ON TABLE multitenant.Locations IS 'Esta tabela armazena os locais f�sicos gerenciados pelos tenants.';
COMMENT ON COLUMN multitenant.Locations.LocationId IS 'Chave prim�ria: Identificador �nico de cada local.';
COMMENT ON COLUMN multitenant.Locations.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.Locations.Name IS 'Campo de dados: Nome do local.';
COMMENT ON COLUMN multitenant.Locations.Address IS 'Campo de dados: Endere�o f�sico do local.';
COMMENT ON COLUMN multitenant.Locations.City IS 'Campo de dados: Cidade do local.';
COMMENT ON COLUMN multitenant.Locations.State IS 'Campo de dados: Estado do local.';
COMMENT ON COLUMN multitenant.Locations.Country IS 'Campo de dados: Pa�s do local (padr�o: Brasil).';
COMMENT ON COLUMN multitenant.Locations.TimeZone IS 'Campo de dados: Fuso hor�rio do local.';
COMMENT ON COLUMN multitenant.Locations.IsActive IS 'Campo de controle: Indica se o local est� ativo.';
COMMENT ON COLUMN multitenant.Locations.CreatedAt IS 'Campo de controle: Data e hora de cria��o do registro.';
COMMENT ON COLUMN multitenant.Locations.UpdatedAt IS 'Campo de controle: Data e hora da �ltima atualiza��o (opcional).';
GO

-- Tabela Users
-- Esta tabela armazena as contas de usu�rios para opera��es dos tenants.
CREATE TABLE multitenant.Users (
    UserId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada usu�rio.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    LocationId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do local do usu�rio (opcional).
    Username NVARCHAR(50) NOT NULL, -- Campo de dados: Nome de usu�rio para login.
    PasswordHash NVARCHAR(MAX) NOT NULL, -- Campo de dados: Hash da senha para seguran�a.
    Email NVARCHAR(100) NOT NULL, -- Campo de dados: Endere�o de e-mail do usu�rio.
    FirstName NVARCHAR(50) NOT NULL, -- Campo de dados: Primeiro nome do usu�rio.
    LastName NVARCHAR(100) NOT NULL, -- Campo de dados: Sobrenome do usu�rio.
    DocumentType NVARCHAR(20) NULL DEFAULT 'CPF', -- Campo de dados: Tipo de documento de identifica��o (padr�o: CPF).
    DocumentNumber NVARCHAR(20) NULL, -- Campo de dados: N�mero do documento de identifica��o.
    PhoneNumber NVARCHAR(20) NULL, -- Campo de dados: N�mero de telefone do usu�rio (opcional).
    Photo VARBINARY(MAX) FILESTREAM NULL, -- Campo de dados: Foto do usu�rio armazenada como dado bin�rio (opcional).
    Role NVARCHAR(20) NOT NULL DEFAULT 'Operator', -- Campo de dados: Papel do usu�rio (ex.: Operador, Admin).
    IsActive BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se o usu�rio est� ativo.
    LastLogin DATETIME2 NULL, -- Campo de dados: Data e hora do �ltimo login (opcional).
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de cria��o do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da �ltima atualiza��o (opcional).
    CONSTRAINT FK_Users_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com dele��o em cascata.
    CONSTRAINT FK_Users_Locations FOREIGN KEY (LocationId) REFERENCES multitenant.Locations(LocationId), -- Chave estrangeira vinculada a Locations.
    CONSTRAINT UK_Users_Username UNIQUE (TenantId, Username), -- Restri��o de unicidade: Garante nome de usu�rio �nico por tenant.
    CONSTRAINT UK_Users_Email UNIQUE (TenantId, Email) -- Restri��o de unicidade: Garante e-mail �nico por tenant.
);
COMMENT ON TABLE multitenant.Users IS 'Esta tabela armazena as contas de usu�rios para opera��es dos tenants.';
COMMENT ON COLUMN multitenant.Users.UserId IS 'Chave prim�ria: Identificador �nico de cada usu�rio.';
COMMENT ON COLUMN multitenant.Users.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.Users.LocationId IS 'Chave estrangeira: Identificador do local do usu�rio (opcional).';
COMMENT ON COLUMN multitenant.Users.Username IS 'Campo de dados: Nome de usu�rio para login.';
COMMENT ON COLUMN multitenant.Users.PasswordHash IS 'Campo de dados: Hash da senha para seguran�a.';
COMMENT ON COLUMN multitenant.Users.Email IS 'Campo de dados: Endere�o de e-mail do usu�rio.';
COMMENT ON COLUMN multitenant.Users.FirstName IS 'Campo de dados: Primeiro nome do usu�rio.';
COMMENT ON COLUMN multitenant.Users.LastName IS 'Campo de dados: Sobrenome do usu�rio.';
COMMENT ON COLUMN multitenant.Users.DocumentType IS 'Campo de dados: Tipo de documento de identifica��o (padr�o: CPF).';
COMMENT ON COLUMN multitenant.Users.DocumentNumber IS 'Campo de dados: N�mero do documento de identifica��o.';
COMMENT ON COLUMN multitenant.Users.PhoneNumber IS 'Campo de dados: N�mero de telefone do usu�rio (opcional).';
COMMENT ON COLUMN multitenant.Users.Photo IS 'Campo de dados: Foto do usu�rio armazenada como dado bin�rio (opcional).';
COMMENT ON COLUMN multitenant.Users.Role IS 'Campo de dados: Papel do usu�rio (ex.: Operador, Admin).';
COMMENT ON COLUMN multitenant.Users.IsActive IS 'Campo de controle: Indica se o usu�rio est� ativo.';
COMMENT ON COLUMN multitenant.Users.LastLogin IS 'Campo de dados: Data e hora do �ltimo login (opcional).';
COMMENT ON COLUMN multitenant.Users.CreatedAt IS 'Campo de controle: Data e hora de cria��o do registro.';
COMMENT ON COLUMN multitenant.Users.UpdatedAt IS 'Campo de controle: Data e hora da �ltima atualiza��o (opcional).';
GO

-- Tabela Employees
-- Esta tabela armazena os registros de funcion�rios para controle de acesso.
CREATE TABLE multitenant.Employees (
    EmployeeId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada funcion�rio.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    LocationId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do local do funcion�rio (opcional).
    CompanyDepartment NVARCHAR(100) NULL, -- Campo de dados: Departamento da empresa (opcional).
    EmployeeNumber NVARCHAR(50) NULL, -- Campo de dados: N�mero de identifica��o do funcion�rio (opcional).
    FirstName NVARCHAR(50) NOT NULL, -- Campo de dados: Primeiro nome do funcion�rio.
    LastName NVARCHAR(100) NOT NULL, -- Campo de dados: Sobrenome do funcion�rio.
    DocumentType NVARCHAR(20) NOT NULL DEFAULT 'CPF', -- Campo de dados: Tipo de documento de identifica��o (padr�o: CPF).
    DocumentNumber NVARCHAR(20) NOT NULL, -- Campo de dados: N�mero do documento de identifica��o.
    Email NVARCHAR(100) NULL, -- Campo de dados: Endere�o de e-mail do funcion�rio (opcional).
    PhoneNumber NVARCHAR(20) NULL, -- Campo de dados: N�mero de telefone do funcion�rio (opcional).
    Photo VARBINARY(MAX) FILESTREAM NULL, -- Campo de dados: Foto do funcion�rio armazenada como dado bin�rio (opcional).
    RFIDTag NVARCHAR(100) ENCRYPTED WITH (ENCRYPTION_TYPE = DETERMINISTIC, ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256', COLUMN_ENCRYPTION_KEY = CEK_Portaria) NULL, -- Campo de dados: Tag RFID para acesso (criptografado, opcional).
    BiometricData VARBINARY(MAX) ENCRYPTED WITH (ENCRYPTION_TYPE = DETERMINISTIC, ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256', COLUMN_ENCRYPTION_KEY = CEK_Portaria) NULL, -- Campo de dados: Dados biom�tricos (criptografados, opcionais).
    ConsentGiven BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se o consentimento para processamento de dados foi dado (LGPD/GDPR).
    DataExpirationDate DATETIME2 NULL, -- Campo de controle: Data de expira��o dos dados do funcion�rio.
    IsActive BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se o funcion�rio est� ativo.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de cria��o do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da �ltima atualiza��o (opcional).
    CONSTRAINT FK_Employees_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com dele��o em cascata.
    CONSTRAINT FK_Employees_Locations FOREIGN KEY (LocationId) REFERENCES multitenant.Locations(LocationId), -- Chave estrangeira vinculada a Locations.
    CONSTRAINT UK_Employees_Document UNIQUE (TenantId, DocumentType, DocumentNumber), -- Restri��o de unicidade: Garante documento �nico por tenant.
    CONSTRAINT UK_Employees_RFID UNIQUE (TenantId, RFIDTag) WHERE RFIDTag IS NOT NULL -- Restri��o de unicidade: Garante tag RFID �nica por tenant.
);
COMMENT ON TABLE multitenant.Employees IS 'Esta tabela armazena os registros de funcion�rios para controle de acesso.';
COMMENT ON COLUMN multitenant.Employees.EmployeeId IS 'Chave prim�ria: Identificador �nico de cada funcion�rio.';
COMMENT ON COLUMN multitenant.Employees.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.Employees.LocationId IS 'Chave estrangeira: Identificador do local do funcion�rio (opcional).';
COMMENT ON COLUMN multitenant.Employees.CompanyDepartment IS 'Campo de dados: Departamento da empresa (opcional).';
COMMENT ON COLUMN multitenant.Employees.EmployeeNumber IS 'Campo de dados: N�mero de identifica��o do funcion�rio (opcional).';
COMMENT ON COLUMN multitenant.Employees.FirstName IS 'Campo de dados: Primeiro nome do funcion�rio.';
COMMENT ON COLUMN multitenant.Employees.LastName IS 'Campo de dados: Sobrenome do funcion�rio.';
COMMENT ON COLUMN multitenant.Employees.DocumentType IS 'Campo de dados: Tipo de documento de identifica��o (padr�o: CPF).';
COMMENT ON COLUMN multitenant.Employees.DocumentNumber IS 'Campo de dados: N�mero do documento de identifica��o.';
COMMENT ON COLUMN multitenant.Employees.Email IS 'Campo de dados: Endere�o de e-mail do funcion�rio (opcional).';
COMMENT ON COLUMN multitenant.Employees.PhoneNumber IS 'Campo de dados: N�mero de telefone do funcion�rio (opcional).';
COMMENT ON COLUMN multitenant.Employees.Photo IS 'Campo de dados: Foto do funcion�rio armazenada como dado bin�rio (opcional).';
COMMENT ON COLUMN multitenant.Employees.RFIDTag IS 'Campo de dados: Tag RFID para acesso (criptografado, opcional).';
COMMENT ON COLUMN multitenant.Employees.BiometricData IS 'Campo de dados: Dados biom�tricos (criptografados, opcionais).';
COMMENT ON COLUMN multitenant.Employees.ConsentGiven IS 'Campo de controle: Indica se o consentimento para processamento de dados foi dado (LGPD/GDPR).';
COMMENT ON COLUMN multitenant.Employees.DataExpirationDate IS 'Campo de controle: Data de expira��o dos dados do funcion�rio.';
COMMENT ON COLUMN multitenant.Employees.IsActive IS 'Campo de controle: Indica se o funcion�rio est� ativo.';
COMMENT ON COLUMN multitenant.Employees.CreatedAt IS 'Campo de controle: Data e hora de cria��o do registro.';
COMMENT ON COLUMN multitenant.Employees.UpdatedAt IS 'Campo de controle: Data e hora da �ltima atualiza��o (opcional).';
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
    VisitorId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada visitante.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    FirstName NVARCHAR(50) NOT NULL, -- Campo de dados: Primeiro nome do visitante.
    LastName NVARCHAR(100) NOT NULL, -- Campo de dados: Sobrenome do visitante.
    DocumentType NVARCHAR(20) NOT NULL DEFAULT 'CPF', -- Campo de dados: Tipo de documento de identifica��o (padr�o: CPF).
    DocumentNumber NVARCHAR(20) NOT NULL, -- Campo de dados: N�mero do documento de identifica��o.
    Email NVARCHAR(100) NULL, -- Campo de dados: Endere�o de e-mail do visitante (opcional).
    PhoneNumber NVARCHAR(20) NULL, -- Campo de dados: N�mero de telefone do visitante (opcional).
    CompanyName NVARCHAR(200) NULL, -- Campo de dados: Nome da empresa que o visitante representa (opcional).
    Photo VARBINARY(MAX) FILESTREAM NULL, -- Campo de dados: Foto do visitante armazenada como dado bin�rio (opcional).
    VehiclePlate NVARCHAR(10) NULL, -- Campo de dados: Placa do ve�culo do visitante (opcional).
    VehicleModel NVARCHAR(50) NULL, -- Campo de dados: Modelo do ve�culo do visitante (opcional).
    VehicleColor NVARCHAR(30) NULL, -- Campo de dados: Cor do ve�culo do visitante (opcional).
    IsElectricVehicle BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se o ve�culo � el�trico (sustentabilidade).
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de cria��o do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da �ltima atualiza��o (opcional).
    ConsentGiven BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se o consentimento para processamento de dados foi dado (LGPD/GDPR).
    DataExpirationDate DATETIME2 NULL, -- Campo de controle: Data de expira��o dos dados do visitante.
    CONSTRAINT FK_Visitors_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com dele��o em cascata.
    CONSTRAINT UK_Visitors_Document UNIQUE (TenantId, DocumentType, DocumentNumber) -- Restri��o de unicidade: Garante documento �nico por tenant.
);
COMMENT ON TABLE multitenant.Visitors IS 'Esta tabela armazena os registros de visitantes para controle de acesso.';
COMMENT ON COLUMN multitenant.Visitors.VisitorId IS 'Chave prim�ria: Identificador �nico de cada visitante.';
COMMENT ON COLUMN multitenant.Visitors.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.Visitors.FirstName IS 'Campo de dados: Primeiro nome do visitante.';
COMMENT ON COLUMN multitenant.Visitors.LastName IS 'Campo de dados: Sobrenome do visitante.';
COMMENT ON COLUMN multitenant.Visitors.DocumentType IS 'Campo de dados: Tipo de documento de identifica��o (padr�o: CPF).';
COMMENT ON COLUMN multitenant.Visitors.DocumentNumber IS 'Campo de dados: N�mero do documento de identifica��o.';
COMMENT ON COLUMN multitenant.Visitors.Email IS 'Campo de dados: Endere�o de e-mail do visitante (opcional).';
COMMENT ON COLUMN multitenant.Visitors.PhoneNumber IS 'Campo de dados: N�mero de telefone do visitante (opcional).';
COMMENT ON COLUMN multitenant.Visitors.CompanyName IS 'Campo de dados: Nome da empresa que o visitante representa (opcional).';
COMMENT ON COLUMN multitenant.Visitors.Photo IS 'Campo de dados: Foto do visitante armazenada como dado bin�rio (opcional).';
COMMENT ON COLUMN multitenant.Visitors.VehiclePlate IS 'Campo de dados: Placa do ve�culo do visitante (opcional).';
COMMENT ON COLUMN multitenant.Visitors.VehicleModel IS 'Campo de dados: Modelo do ve�culo do visitante (opcional).';
COMMENT ON COLUMN multitenant.Visitors.VehicleColor IS 'Campo de dados: Cor do ve�culo do visitante (opcional).';
COMMENT ON COLUMN multitenant.Visitors.IsElectricVehicle IS 'Campo de controle: Indica se o ve�culo � el�trico (sustentabilidade).';
COMMENT ON COLUMN multitenant.Visitors.CreatedAt IS 'Campo de controle: Data e hora de cria��o do registro.';
COMMENT ON COLUMN multitenant.Visitors.UpdatedAt IS 'Campo de controle: Data e hora da �ltima atualiza��o (opcional).';
COMMENT ON COLUMN multitenant.Visitors.ConsentGiven IS 'Campo de controle: Indica se o consentimento para processamento de dados foi dado (LGPD/GDPR).';
COMMENT ON COLUMN multitenant.Visitors.DataExpirationDate IS 'Campo de controle: Data de expira��o dos dados do visitante.';
GO

-- Tabela VisitorGroups
-- Esta tabela armazena grupos de visitantes para gerenciamento em massa.
CREATE TABLE multitenant.VisitorGroups (
    GroupId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada grupo.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    GroupName NVARCHAR(100) NOT NULL, -- Campo de dados: Nome do grupo de visitantes.
    Description NVARCHAR(500) NULL, -- Campo de dados: Descri��o do grupo (opcional).
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de cria��o do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da �ltima atualiza��o (opcional).
    CONSTRAINT FK_VisitorGroups_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE -- Chave estrangeira vinculada a Tenants com dele��o em cascata.
);
COMMENT ON TABLE multitenant.VisitorGroups IS 'Esta tabela armazena grupos de visitantes para gerenciamento em massa.';
COMMENT ON COLUMN multitenant.VisitorGroups.GroupId IS 'Chave prim�ria: Identificador �nico de cada grupo.';
COMMENT ON COLUMN multitenant.VisitorGroups.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.VisitorGroups.GroupName IS 'Campo de dados: Nome do grupo de visitantes.';
COMMENT ON COLUMN multitenant.VisitorGroups.Description IS 'Campo de dados: Descri��o do grupo (opcional).';
COMMENT ON COLUMN multitenant.VisitorGroups.CreatedAt IS 'Campo de controle: Data e hora de cria��o do registro.';
COMMENT ON COLUMN multitenant.VisitorGroups.UpdatedAt IS 'Campo de controle: Data e hora da �ltima atualiza��o (opcional).';
GO

CREATE TABLE multitenant.VisitorGroupMembers (
    MemberId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada membro.
    GroupId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do grupo de visitantes.
    VisitorId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do visitante.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de cria��o da associa��o.
    CONSTRAINT FK_VisitorGroupMembers_Groups FOREIGN KEY (GroupId) REFERENCES multitenant.VisitorGroups(GroupId) ON DELETE CASCADE, -- Chave estrangeira vinculada a VisitorGroups com dele��o em cascata.
    CONSTRAINT FK_VisitorGroupMembers_Visitors FOREIGN KEY (VisitorId) REFERENCES multitenant.Visitors(VisitorId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Visitors com dele��o em cascata.
    CONSTRAINT UK_VisitorGroupMembers UNIQUE (GroupId, VisitorId) -- Restri��o de unicidade: Garante associa��o �nica por grupo.
);
COMMENT ON TABLE multitenant.VisitorGroupMembers IS 'Esta tabela armazena os membros dos grupos de visitantes.';
COMMENT ON COLUMN multitenant.VisitorGroupMembers.MemberId IS 'Chave prim�ria: Identificador �nico de cada membro.';
COMMENT ON COLUMN multitenant.VisitorGroupMembers.GroupId IS 'Chave estrangeira: Identificador do grupo de visitantes.';
COMMENT ON COLUMN multitenant.VisitorGroupMembers.VisitorId IS 'Chave estrangeira: Identificador do visitante.';
COMMENT ON COLUMN multitenant.VisitorGroupMembers.CreatedAt IS 'Campo de controle: Data e hora de cria��o da associa��o.';
GO

-- Tabela AccessAuthorizations
-- Esta tabela armazena os detalhes de autoriza��es de acesso para visitantes e funcion�rios.
CREATE TABLE multitenant.AccessAuthorizations (
    AuthorizationId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada autoriza��o.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    VisitorId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do visitante (opcional).
    EmployeeId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do funcion�rio (opcional).
    AuthorizedBy UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do usu�rio que autorizou.
    ValidFrom DATETIME2 NOT NULL, -- Campo de dados: Data e hora de in�cio da validade.
    ValidUntil DATETIME2 NOT NULL, -- Campo de dados: Data e hora de t�rmino da validade.
    Purpose NVARCHAR(500) NOT NULL, -- Campo de dados: Prop�sito do acesso.
    AccessZones NVARCHAR(MAX) NOT NULL, -- Campo de dados: Zonas de acesso autorizadas em formato JSON.
    RequiresEscort BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se � necess�rio acompanhamento.
    QRCode VARBINARY(MAX) FILESTREAM NULL, -- Campo de dados: C�digo QR para acesso (armazenado como bin�rio, opcional).
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Campo de dados: Status atual da autoriza��o.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de cria��o do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da �ltima atualiza��o (opcional).
    CONSTRAINT FK_AccessAuthorizations_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com dele��o em cascata.
    CONSTRAINT FK_AccessAuthorizations_Visitors FOREIGN KEY (VisitorId) REFERENCES multitenant.Visitors(VisitorId), -- Chave estrangeira vinculada a Visitors.
    CONSTRAINT FK_AccessAuthorizations_Employees FOREIGN KEY (EmployeeId) REFERENCES multitenant.Employees(EmployeeId), -- Chave estrangeira vinculada a Employees.
    CONSTRAINT FK_AccessAuthorizations_AuthorizedBy FOREIGN KEY (AuthorizedBy) REFERENCES multitenant.Users(UserId) -- Chave estrangeira vinculada a Users.
);
COMMENT ON TABLE multitenant.AccessAuthorizations IS 'Esta tabela armazena os detalhes de autoriza��es de acesso para visitantes e funcion�rios.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.AuthorizationId IS 'Chave prim�ria: Identificador �nico de cada autoriza��o.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.VisitorId IS 'Chave estrangeira: Identificador do visitante (opcional).';
COMMENT ON COLUMN multitenant.AccessAuthorizations.EmployeeId IS 'Chave estrangeira: Identificador do funcion�rio (opcional).';
COMMENT ON COLUMN multitenant.AccessAuthorizations.AuthorizedBy IS 'Chave estrangeira: Identificador do usu�rio que autorizou.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.ValidFrom IS 'Campo de dados: Data e hora de in�cio da validade.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.ValidUntil IS 'Campo de dados: Data e hora de t�rmino da validade.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.Purpose IS 'Campo de dados: Prop�sito do acesso.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.AccessZones IS 'Campo de dados: Zonas de acesso autorizadas em formato JSON.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.RequiresEscort IS 'Campo de controle: Indica se � necess�rio acompanhamento.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.QRCode IS 'Campo de dados: C�digo QR para acesso (armazenado como bin�rio, opcional).';
COMMENT ON COLUMN multitenant.AccessAuthorizations.Status IS 'Campo de dados: Status atual da autoriza��o.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.CreatedAt IS 'Campo de controle: Data e hora de cria��o do registro.';
COMMENT ON COLUMN multitenant.AccessAuthorizations.UpdatedAt IS 'Campo de controle: Data e hora da �ltima atualiza��o (opcional).';
GO

-- Tabela Devices
-- Esta tabela armazena informa��es sobre os dispositivos de controle de acesso.
CREATE TABLE multitenant.Devices (
    DeviceId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada dispositivo.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    LocationId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do local.
    DeviceName NVARCHAR(100) NOT NULL, -- Campo de dados: Nome do dispositivo.
    DeviceType NVARCHAR(50) NOT NULL, -- Campo de dados: Tipo de dispositivo (ex.: Catraca, Leitor RFID).
    DeviceCode NVARCHAR(50) NOT NULL, -- Campo de dados: C�digo �nico do dispositivo.
    IPAddress NVARCHAR(45) NULL, -- Campo de dados: Endere�o IP do dispositivo (opcional).
    MACAddress NVARCHAR(17) NULL, -- Campo de dados: Endere�o MAC do dispositivo (opcional).
    Status NVARCHAR(20) NOT NULL DEFAULT 'Active', -- Campo de dados: Status atual do dispositivo.
    LastCommunication DATETIME2 NULL, -- Campo de dados: Data e hora da �ltima comunica��o (opcional).
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de cria��o do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da �ltima atualiza��o (opcional).
    CONSTRAINT FK_Devices_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com dele��o em cascata.
    CONSTRAINT FK_Devices_Locations FOREIGN KEY (LocationId) REFERENCES multitenant.Locations(LocationId), -- Chave estrangeira vinculada a Locations.
    CONSTRAINT UK_Devices_Code UNIQUE (TenantId, DeviceCode) -- Restri��o de unicidade: Garante c�digo �nico por tenant.
);
COMMENT ON TABLE multitenant.Devices IS 'Esta tabela armazena informa��es sobre os dispositivos de controle de acesso.';
COMMENT ON COLUMN multitenant.Devices.DeviceId IS 'Chave prim�ria: Identificador �nico de cada dispositivo.';
COMMENT ON COLUMN multitenant.Devices.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.Devices.LocationId IS 'Chave estrangeira: Identificador do local.';
COMMENT ON COLUMN multitenant.Devices.DeviceName IS 'Campo de dados: Nome do dispositivo.';
COMMENT ON COLUMN multitenant.Devices.DeviceType IS 'Campo de dados: Tipo de dispositivo (ex.: Catraca, Leitor RFID).';
COMMENT ON COLUMN multitenant.Devices.DeviceCode IS 'Campo de dados: C�digo �nico do dispositivo.';
COMMENT ON COLUMN multitenant.Devices.IPAddress IS 'Campo de dados: Endere�o IP do dispositivo (opcional).';
COMMENT ON COLUMN multitenant.Devices.MACAddress IS 'Campo de dados: Endere�o MAC do dispositivo (opcional).';
COMMENT ON COLUMN multitenant.Devices.Status IS 'Campo de dados: Status atual do dispositivo.';
COMMENT ON COLUMN multitenant.Devices.LastCommunication IS 'Campo de dados: Data e hora da �ltima comunica��o (opcional).';
COMMENT ON COLUMN multitenant.Devices.CreatedAt IS 'Campo de controle: Data e hora de cria��o do registro.';
COMMENT ON COLUMN multitenant.Devices.UpdatedAt IS 'Campo de controle: Data e hora da �ltima atualiza��o (opcional).';
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
    RecordId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada registro de acesso.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    LocationId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do local.
    DeviceId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do dispositivo.
    VisitorId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do visitante (opcional).
    EmployeeId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do funcion�rio (opcional).
    AuthorizationId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador da autoriza��o (opcional).
    AccessType NVARCHAR(10) NOT NULL, -- Campo de dados: Tipo de acesso (ex.: Entrada, Sa�da).
    AccessDateTime DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de dados: Data e hora do evento de acesso.
    DocumentNumber NVARCHAR(20) NULL, -- Campo de dados: N�mero do documento utilizado no acesso (opcional).
    RFIDTag NVARCHAR(100) NULL, -- Campo de dados: Tag RFID utilizada no acesso (opcional).
    BiometricMatchConfidence DECIMAL(5,2) NULL, -- Campo de dados: Pontua��o de confian�a do reconhecimento biom�trico (opcional).
    AnomalyScore DECIMAL(5,2) NULL, -- Campo de dados: Pontua��o de anomalia detectada (opcional).
    PhotoTaken VARBINARY(MAX) FILESTREAM NULL, -- Campo de dados: Foto capturada durante o acesso (opcional).
    Temperature DECIMAL(4,1) NULL, -- Campo de dados: Leitura de temperatura (opcional).
    Status NVARCHAR(20) NOT NULL DEFAULT 'Granted', -- Campo de dados: Status da tentativa de acesso.
    DenialReason NVARCHAR(200) NULL, -- Campo de dados: Motivo da nega��o do acesso (opcional).
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de cria��o do registro.
    DataExpirationDate DATETIME2 NULL, -- Campo de controle: Data de expira��o do registro.
    CONSTRAINT FK_AccessRecords_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com dele��o em cascata.
    CONSTRAINT FK_AccessRecords_Locations FOREIGN KEY (LocationId) REFERENCES multitenant.Locations(LocationId), -- Chave estrangeira vinculada a Locations.
    CONSTRAINT FK_AccessRecords_Devices FOREIGN KEY (DeviceId) REFERENCES multitenant.Devices(DeviceId), -- Chave estrangeira vinculada a Devices.
    CONSTRAINT FK_AccessRecords_Visitors FOREIGN KEY (VisitorId) REFERENCES multitenant.Visitors(VisitorId), -- Chave estrangeira vinculada a Visitors.
    CONSTRAINT FK_AccessRecords_Employees FOREIGN KEY (EmployeeId) REFERENCES multitenant.Employees(EmployeeId), -- Chave estrangeira vinculada a Employees.
    CONSTRAINT FK_AccessRecords_Authorizations FOREIGN KEY (AuthorizationId) REFERENCES multitenant.AccessAuthorizations(AuthorizationId) -- Chave estrangeira vinculada a AccessAuthorizations.
) ON PS_AccessDateTime (AccessDateTime);
COMMENT ON TABLE multitenant.AccessRecords IS 'Esta tabela registra todos os eventos de acesso com particionamento por data.';
COMMENT ON COLUMN multitenant.AccessRecords.RecordId IS 'Chave prim�ria: Identificador �nico de cada registro de acesso.';
COMMENT ON COLUMN multitenant.AccessRecords.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.AccessRecords.LocationId IS 'Chave estrangeira: Identificador do local.';
COMMENT ON COLUMN multitenant.AccessRecords.DeviceId IS 'Chave estrangeira: Identificador do dispositivo.';
COMMENT ON COLUMN multitenant.AccessRecords.VisitorId IS 'Chave estrangeira: Identificador do visitante (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.EmployeeId IS 'Chave estrangeira: Identificador do funcion�rio (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.AuthorizationId IS 'Chave estrangeira: Identificador da autoriza��o (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.AccessType IS 'Campo de dados: Tipo de acesso (ex.: Entrada, Sa�da).';
COMMENT ON COLUMN multitenant.AccessRecords.AccessDateTime IS 'Campo de dados: Data e hora do evento de acesso.';
COMMENT ON COLUMN multitenant.AccessRecords.DocumentNumber IS 'Campo de dados: N�mero do documento utilizado no acesso (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.RFIDTag IS 'Campo de dados: Tag RFID utilizada no acesso (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.BiometricMatchConfidence IS 'Campo de dados: Pontua��o de confian�a do reconhecimento biom�trico (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.AnomalyScore IS 'Campo de dados: Pontua��o de anomalia detectada (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.PhotoTaken IS 'Campo de dados: Foto capturada durante o acesso (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.Temperature IS 'Campo de dados: Leitura de temperatura (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.Status IS 'Campo de dados: Status da tentativa de acesso.';
COMMENT ON COLUMN multitenant.AccessRecords.DenialReason IS 'Campo de dados: Motivo da nega��o do acesso (opcional).';
COMMENT ON COLUMN multitenant.AccessRecords.CreatedAt IS 'Campo de controle: Data e hora de cria��o do registro.';
COMMENT ON COLUMN multitenant.AccessRecords.DataExpirationDate IS 'Campo de controle: Data de expira��o do registro.';
GO

-- Tabela RestrictedZones
-- Esta tabela define as �reas restritas dentro dos locais.
CREATE TABLE multitenant.RestrictedZones (
    ZoneId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada zona.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    LocationId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do local.
    ZoneName NVARCHAR(100) NOT NULL, -- Campo de dados: Nome da zona restrita.
    Description NVARCHAR(500) NULL, -- Campo de dados: Descri��o da zona (opcional).
    AccessLevel NVARCHAR(20) NOT NULL DEFAULT 'Restricted', -- Campo de dados: N�vel de acesso (ex.: P�blico, Restrito).
    IsActive BIT NOT NULL DEFAULT 1, -- Campo de controle: Indica se a zona est� ativa.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de cria��o do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da �ltima atualiza��o (opcional).
    CONSTRAINT FK_RestrictedZones_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com dele��o em cascata.
    CONSTRAINT FK_RestrictedZones_Locations FOREIGN KEY (LocationId) REFERENCES multitenant.Locations(LocationId) -- Chave estrangeira vinculada a Locations.
);
COMMENT ON TABLE multitenant.RestrictedZones IS 'Esta tabela define as �reas restritas dentro dos locais.';
COMMENT ON COLUMN multitenant.RestrictedZones.ZoneId IS 'Chave prim�ria: Identificador �nico de cada zona.';
COMMENT ON COLUMN multitenant.RestrictedZones.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.RestrictedZones.LocationId IS 'Chave estrangeira: Identificador do local.';
COMMENT ON COLUMN multitenant.RestrictedZones.ZoneName IS 'Campo de dados: Nome da zona restrita.';
COMMENT ON COLUMN multitenant.RestrictedZones.Description IS 'Campo de dados: Descri��o da zona (opcional).';
COMMENT ON COLUMN multitenant.RestrictedZones.AccessLevel IS 'Campo de dados: N�vel de acesso (ex.: P�blico, Restrito).';
COMMENT ON COLUMN multitenant.RestrictedZones.IsActive IS 'Campo de controle: Indica se a zona est� ativa.';
COMMENT ON COLUMN multitenant.RestrictedZones.CreatedAt IS 'Campo de controle: Data e hora de cria��o do registro.';
COMMENT ON COLUMN multitenant.RestrictedZones.UpdatedAt IS 'Campo de controle: Data e hora da �ltima atualiza��o (opcional).';
GO

-- Tabela ZonePermissions
-- Esta tabela define as permiss�es de acesso por papel dentro das zonas.
CREATE TABLE multitenant.ZonePermissions (
    PermissionId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada permiss�o.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    ZoneId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador da zona restrita.
    Role NVARCHAR(50) NOT NULL, -- Campo de dados: Papel que requer acesso (ex.: Visitante, Funcion�rio).
    AccessLevel NVARCHAR(20) NOT NULL DEFAULT 'Denied', -- Campo de dados: N�vel de acesso concedido (ex.: Negado, Concedido).
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de cria��o da permiss�o.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da �ltima atualiza��o (opcional).
    CONSTRAINT FK_ZonePermissions_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com dele��o em cascata.
    CONSTRAINT FK_ZonePermissions_Zones FOREIGN KEY (ZoneId) REFERENCES multitenant.RestrictedZones(ZoneId), -- Chave estrangeira vinculada a RestrictedZones.
    CONSTRAINT UK_ZonePermissions UNIQUE (TenantId, ZoneId, Role) -- Restri��o de unicidade: Garante papel �nico por zona por tenant.
);
COMMENT ON TABLE multitenant.ZonePermissions IS 'Esta tabela define as permiss�es de acesso por papel dentro das zonas.';
COMMENT ON COLUMN multitenant.ZonePermissions.PermissionId IS 'Chave prim�ria: Identificador �nico de cada permiss�o.';
COMMENT ON COLUMN multitenant.ZonePermissions.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.ZonePermissions.ZoneId IS 'Chave estrangeira: Identificador da zona restrita.';
COMMENT ON COLUMN multitenant.ZonePermissions.Role IS 'Campo de dados: Papel que requer acesso (ex.: Visitante, Funcion�rio).';
COMMENT ON COLUMN multitenant.ZonePermissions.AccessLevel IS 'Campo de dados: N�vel de acesso concedido (ex.: Negado, Concedido).';
COMMENT ON COLUMN multitenant.ZonePermissions.CreatedAt IS 'Campo de controle: Data e hora de cria��o da permiss�o.';
COMMENT ON COLUMN multitenant.ZonePermissions.UpdatedAt IS 'Campo de controle: Data e hora da �ltima atualiza��o (opcional).';
GO

-- Tabela Alerts
-- Esta tabela armazena os alertas gerados pelo sistema.
CREATE TABLE multitenant.Alerts (
    AlertId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada alerta.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    LocationId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do local (opcional).
    AlertType NVARCHAR(50) NOT NULL, -- Campo de dados: Tipo de alerta (ex.: Seguran�a, Sistema).
    Severity NVARCHAR(20) NOT NULL DEFAULT 'Medium', -- Campo de dados: N�vel de gravidade do alerta.
    Title NVARCHAR(200) NOT NULL, -- Campo de dados: T�tulo do alerta.
    Description NVARCHAR(1000) NULL, -- Campo de dados: Descri��o detalhada do alerta (opcional).
    RelatedRecordId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do registro relacionado (opcional).
    RelatedRecordType NVARCHAR(50) NULL, -- Campo de dados: Tipo de registro relacionado (opcional).
    Acknowledged BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se o alerta foi reconhecido.
    AcknowledgedBy UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do usu�rio que reconheceu (opcional).
    AcknowledgedAt DATETIME2 NULL, -- Campo de controle: Data e hora do reconhecimento (opcional).
    Resolved BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se o alerta foi resolvido.
    ResolvedBy UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do usu�rio que resolveu (opcional).
    ResolvedAt DATETIME2 NULL, -- Campo de controle: Data e hora da resolu��o (opcional).
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de cria��o do registro.
    CONSTRAINT FK_Alerts_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com dele��o em cascata.
    CONSTRAINT FK_Alerts_Locations FOREIGN KEY (LocationId) REFERENCES multitenant.Locations(LocationId), -- Chave estrangeira vinculada a Locations.
    CONSTRAINT FK_Alerts_AcknowledgedBy FOREIGN KEY (AcknowledgedBy) REFERENCES multitenant.Users(UserId), -- Chave estrangeira vinculada a Users.
    CONSTRAINT FK_Alerts_ResolvedBy FOREIGN KEY (ResolvedBy) REFERENCES multitenant.Users(UserId) -- Chave estrangeira vinculada a Users.
);
COMMENT ON TABLE multitenant.Alerts IS 'Esta tabela armazena os alertas gerados pelo sistema.';
COMMENT ON COLUMN multitenant.Alerts.AlertId IS 'Chave prim�ria: Identificador �nico de cada alerta.';
COMMENT ON COLUMN multitenant.Alerts.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.Alerts.LocationId IS 'Chave estrangeira: Identificador do local (opcional).';
COMMENT ON COLUMN multitenant.Alerts.AlertType IS 'Campo de dados: Tipo de alerta (ex.: Seguran�a, Sistema).';
COMMENT ON COLUMN multitenant.Alerts.Severity IS 'Campo de dados: N�vel de gravidade do alerta.';
COMMENT ON COLUMN multitenant.Alerts.Title IS 'Campo de dados: T�tulo do alerta.';
COMMENT ON COLUMN multitenant.Alerts.Description IS 'Campo de dados: Descri��o detalhada do alerta (opcional).';
COMMENT ON COLUMN multitenant.Alerts.RelatedRecordId IS 'Chave estrangeira: Identificador do registro relacionado (opcional).';
COMMENT ON COLUMN multitenant.Alerts.RelatedRecordType IS 'Campo de dados: Tipo de registro relacionado (opcional).';
COMMENT ON COLUMN multitenant.Alerts.Acknowledged IS 'Campo de controle: Indica se o alerta foi reconhecido.';
COMMENT ON COLUMN multitenant.Alerts.AcknowledgedBy IS 'Chave estrangeira: Identificador do usu�rio que reconheceu (opcional).';
COMMENT ON COLUMN multitenant.Alerts.AcknowledgedAt IS 'Campo de controle: Data e hora do reconhecimento (opcional).';
COMMENT ON COLUMN multitenant.Alerts.Resolved IS 'Campo de controle: Indica se o alerta foi resolvido.';
COMMENT ON COLUMN multitenant.Alerts.ResolvedBy IS 'Chave estrangeira: Identificador do usu�rio que resolveu (opcional).';
COMMENT ON COLUMN multitenant.Alerts.ResolvedAt IS 'Campo de controle: Data e hora da resolu��o (opcional).';
COMMENT ON COLUMN multitenant.Alerts.CreatedAt IS 'Campo de controle: Data e hora de cria��o do registro.';
GO

-- Tabela CrisisPlans
-- Esta tabela armazena os planos de gerenciamento de crises e emerg�ncias.
CREATE TABLE multitenant.CrisisPlans (
    PlanId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada plano de crise.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    PlanName NVARCHAR(100) NOT NULL, -- Campo de dados: Nome do plano de crise.
    Description NVARCHAR(1000) NULL, -- Campo de dados: Descri��o detalhada do plano (opcional).
    Active BIT NOT NULL DEFAULT 0, -- Campo de controle: Indica se o plano est� ativo.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de cria��o do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da �ltima atualiza��o (opcional).
    CONSTRAINT FK_CrisisPlans_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE -- Chave estrangeira vinculada a Tenants com dele��o em cascata.
);
COMMENT ON TABLE multitenant.CrisisPlans IS 'Esta tabela armazena os planos de gerenciamento de crises e emerg�ncias.';
COMMENT ON COLUMN multitenant.CrisisPlans.PlanId IS 'Chave prim�ria: Identificador �nico de cada plano de crise.';
COMMENT ON COLUMN multitenant.CrisisPlans.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.CrisisPlans.PlanName IS 'Campo de dados: Nome do plano de crise.';
COMMENT ON COLUMN multitenant.CrisisPlans.Description IS 'Campo de dados: Descri��o detalhada do plano (opcional).';
COMMENT ON COLUMN multitenant.CrisisPlans.Active IS 'Campo de controle: Indica se o plano est� ativo.';
COMMENT ON COLUMN multitenant.CrisisPlans.CreatedAt IS 'Campo de controle: Data e hora de cria��o do registro.';
COMMENT ON COLUMN multitenant.CrisisPlans.UpdatedAt IS 'Campo de controle: Data e hora da �ltima atualiza��o (opcional).';
GO

-- Tabela AuditLogs (Tabela Temporal para Imutabilidade)
-- Esta tabela registra todas as altera��es para auditoria e conformidade.
CREATE TABLE audit.AuditLogs (
    AuditId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada log de auditoria.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    UserId UNIQUEIDENTIFIER NULL, -- Chave estrangeira: Identificador do usu�rio que realizou a a��o (opcional).
    ActionType NVARCHAR(50) NOT NULL, -- Campo de dados: Tipo de a��o realizada (ex.: Criar, Atualizar).
    TableName NVARCHAR(100) NOT NULL, -- Campo de dados: Nome da tabela afetada.
    RecordId NVARCHAR(100) NOT NULL, -- Campo de dados: Identificador do registro afetado.
    OldValues NVARCHAR(MAX) NULL, -- Campo de dados: Valores antigos em formato JSON (opcional).
    NewValues NVARCHAR(MAX) NULL, -- Campo de dados: Novos valores em formato JSON (opcional).
    IpAddress NVARCHAR(45) NULL, -- Campo de dados: Endere�o IP da origem da a��o (opcional).
    UserAgent NVARCHAR(500) NULL, -- Campo de dados: Agente do usu�rio da origem da a��o (opcional).
    CreatedAt DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL, -- Campo de controle: Data e hora de in�cio da validade do registro.
    UpdatedAt DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL, -- Campo de controle: Data e hora de t�rmino da validade do registro.
    PERIOD FOR SYSTEM_TIME (CreatedAt, UpdatedAt) -- Define o per�odo temporal para versionamento.
) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = audit.AuditLogs_History)); -- Habilita versionamento de sistema para imutabilidade.
COMMENT ON TABLE audit.AuditLogs IS 'Esta tabela registra todas as altera��es para auditoria e conformidade.';
COMMENT ON COLUMN audit.AuditLogs.AuditId IS 'Chave prim�ria: Identificador �nico de cada log de auditoria.';
COMMENT ON COLUMN audit.AuditLogs.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN audit.AuditLogs.UserId IS 'Chave estrangeira: Identificador do usu�rio que realizou a a��o (opcional).';
COMMENT ON COLUMN audit.AuditLogs.ActionType IS 'Campo de dados: Tipo de a��o realizada (ex.: Criar, Atualizar).';
COMMENT ON COLUMN audit.AuditLogs.TableName IS 'Campo de dados: Nome da tabela afetada.';
COMMENT ON COLUMN audit.AuditLogs.RecordId IS 'Campo de dados: Identificador do registro afetado.';
COMMENT ON COLUMN audit.AuditLogs.OldValues IS 'Campo de dados: Valores antigos em formato JSON (opcional).';
COMMENT ON COLUMN audit.AuditLogs.NewValues IS 'Campo de dados: Novos valores em formato JSON (opcional).';
COMMENT ON COLUMN audit.AuditLogs.IpAddress IS 'Campo de dados: Endere�o IP da origem da a��o (opcional).';
COMMENT ON COLUMN audit.AuditLogs.UserAgent IS 'Campo de dados: Agente do usu�rio da origem da a��o (opcional).';
COMMENT ON COLUMN audit.AuditLogs.CreatedAt IS 'Campo de controle: Data e hora de in�cio da validade do registro.';
COMMENT ON COLUMN audit.AuditLogs.UpdatedAt IS 'Campo de controle: Data e hora de t�rmino da validade do registro.';
GO

-- Tabela UsageMetrics
-- Esta tabela rastreia as m�tricas de uso para faturamento e monitoramento.
CREATE TABLE billing.UsageMetrics (
    MetricId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada m�trica.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    SubscriptionId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador da assinatura.
    MetricDate DATE NOT NULL, -- Campo de dados: Data da m�trica.
    ActiveUsers INT NOT NULL DEFAULT 0, -- Campo de dados: N�mero de usu�rios ativos.
    VisitorCheckins INT NOT NULL DEFAULT 0, -- Campo de dados: N�mero de check-ins de visitantes.
    EmployeeCheckins INT NOT NULL DEFAULT 0, -- Campo de dados: N�mero de check-ins de funcion�rios.
    APICalls INT NOT NULL DEFAULT 0, -- Campo de dados: N�mero de chamadas � API.
    StorageMB DECIMAL(10,2) NOT NULL DEFAULT 0, -- Campo de dados: Uso de armazenamento em megabytes.
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de cria��o da m�trica.
    CONSTRAINT FK_UsageMetrics_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com dele��o em cascata.
    CONSTRAINT FK_UsageMetrics_Subscriptions FOREIGN KEY (SubscriptionId) REFERENCES billing.Subscriptions(SubscriptionId), -- Chave estrangeira vinculada a Subscriptions.
    CONSTRAINT UK_UsageMetrics UNIQUE (TenantId, MetricDate) -- Restri��o de unicidade: Garante m�trica �nica por tenant por dia.
);
COMMENT ON TABLE billing.UsageMetrics IS 'Esta tabela rastreia as m�tricas de uso para faturamento e monitoramento.';
COMMENT ON COLUMN billing.UsageMetrics.MetricId IS 'Chave prim�ria: Identificador �nico de cada m�trica.';
COMMENT ON COLUMN billing.UsageMetrics.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN billing.UsageMetrics.SubscriptionId IS 'Chave estrangeira: Identificador da assinatura.';
COMMENT ON COLUMN billing.UsageMetrics.MetricDate IS 'Campo de dados: Data da m�trica.';
COMMENT ON COLUMN billing.UsageMetrics.ActiveUsers IS 'Campo de dados: N�mero de usu�rios ativos.';
COMMENT ON COLUMN billing.UsageMetrics.VisitorCheckins IS 'Campo de dados: N�mero de check-ins de visitantes.';
COMMENT ON COLUMN billing.UsageMetrics.EmployeeCheckins IS 'Campo de dados: N�mero de check-ins de funcion�rios.';
COMMENT ON COLUMN billing.UsageMetrics.APICalls IS 'Campo de dados: N�mero de chamadas � API.';
COMMENT ON COLUMN billing.UsageMetrics.StorageMB IS 'Campo de dados: Uso de armazenamento em megabytes.';
COMMENT ON COLUMN billing.UsageMetrics.CreatedAt IS 'Campo de controle: Data e hora de cria��o da m�trica.';
GO

-- Tabela Integrations (continua��o)
-- Esta tabela armazena os detalhes de integra��es com sistemas externos.
CREATE TABLE multitenant.Integrations (
    IntegrationId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada integra��o.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    IntegrationType NVARCHAR(50) NOT NULL, -- Campo de dados: Tipo de integra��o (ex.: IoT, ML).
    Name NVARCHAR(100) NOT NULL, -- Campo de dados: Nome da integra��o.
    Configuration NVARCHAR(MAX) NOT NULL, -- Campo de dados: Configura��o da integra��o em formato JSON.
    Status NVARCHAR(20) NOT NULL DEFAULT 'Inactive', -- Campo de dados: Status atual da integra��o.
    LastSync DATETIME2 NULL, -- Campo de dados: Data e hora da �ltima sincroniza��o (opcional).
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora de cria��o do registro.
    UpdatedAt DATETIME2 NULL, -- Campo de controle: Data e hora da �ltima atualiza��o (opcional).
    CONSTRAINT FK_Integrations_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE -- Chave estrangeira vinculada a Tenants com dele��o em cascata.
);
COMMENT ON TABLE multitenant.Integrations IS 'Esta tabela armazena os detalhes de integra��es com sistemas externos.';
COMMENT ON COLUMN multitenant.Integrations.IntegrationId IS 'Chave prim�ria: Identificador �nico de cada integra��o.';
COMMENT ON COLUMN multitenant.Integrations.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.Integrations.IntegrationType IS 'Campo de dados: Tipo de integra��o (ex.: IoT, ML).';
COMMENT ON COLUMN multitenant.Integrations.Name IS 'Campo de dados: Nome da integra��o.';
COMMENT ON COLUMN multitenant.Integrations.Configuration IS 'Campo de dados: Configura��o da integra��o em formato JSON.';
COMMENT ON COLUMN multitenant.Integrations.Status IS 'Campo de dados: Status atual da integra��o.';
COMMENT ON COLUMN multitenant.Integrations.LastSync IS 'Campo de dados: Data e hora da �ltima sincroniza��o (opcional).';
COMMENT ON COLUMN multitenant.Integrations.CreatedAt IS 'Campo de controle: Data e hora de cria��o do registro.';
COMMENT ON COLUMN multitenant.Integrations.UpdatedAt IS 'Campo de controle: Data e hora da �ltima atualiza��o (opcional).';
GO

-- Tabela AnomalyDetections
-- Esta tabela armazena as anomalias detectadas para an�lise de seguran�a e ML.
CREATE TABLE multitenant.AnomalyDetections (
    AnomalyId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY, -- Chave prim�ria: Identificador �nico de cada anomalia.
    TenantId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do tenant.
    RecordId UNIQUEIDENTIFIER NOT NULL, -- Chave estrangeira: Identificador do registro de acesso relacionado.
    AnomalyType NVARCHAR(50) NOT NULL, -- Campo de dados: Tipo de anomalia (ex.: Padr�o de Acesso).
    AnomalyScore DECIMAL(5,2) NOT NULL, -- Campo de dados: Pontua��o que indica a gravidade da anomalia.
    DetectedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Campo de controle: Data e hora da detec��o da anomalia.
    Description NVARCHAR(500) NULL, -- Campo de dados: Descri��o da anomalia (opcional).
    CONSTRAINT FK_AnomalyDetections_Tenants FOREIGN KEY (TenantId) REFERENCES saas.Tenants(TenantId) ON DELETE CASCADE, -- Chave estrangeira vinculada a Tenants com dele��o em cascata.
    CONSTRAINT FK_AnomalyDetections_Records FOREIGN KEY (RecordId) REFERENCES multitenant.AccessRecords(RecordId) -- Chave estrangeira vinculada a AccessRecords.
);
COMMENT ON TABLE multitenant.AnomalyDetections IS 'Esta tabela armazena as anomalias detectadas para an�lise de seguran�a e ML.';
COMMENT ON COLUMN multitenant.AnomalyDetections.AnomalyId IS 'Chave prim�ria: Identificador �nico de cada anomalia.';
COMMENT ON COLUMN multitenant.AnomalyDetections.TenantId IS 'Chave estrangeira: Identificador do tenant.';
COMMENT ON COLUMN multitenant.AnomalyDetections.RecordId IS 'Chave estrangeira: Identificador do registro de acesso relacionado.';
COMMENT ON COLUMN multitenant.AnomalyDetections.AnomalyType IS 'Campo de dados: Tipo de anomalia (ex.: Padr�o de Acesso).';
COMMENT ON COLUMN multitenant.AnomalyDetections.AnomalyScore IS 'Campo de dados: Pontua��o que indica a gravidade da anomalia.';
COMMENT ON COLUMN multitenant.AnomalyDetections.DetectedAt IS 'Campo de controle: Data e hora da detec��o da anomalia.';
COMMENT ON COLUMN multitenant.AnomalyDetections.Description IS 'Campo de dados: Descri��o da anomalia (opcional).';
GO

-- =============================================
-- �ndices para Otimiza��o de Performance
-- =============================================

CREATE CLUSTERED INDEX IX_AccessRecords_Tenant_Date 
ON multitenant.AccessRecords (TenantId, AccessDateTime)
ON PS_AccessDateTime (AccessDateTime);
-- Coment�rio: �ndice clusterizado para otimizar consultas por tenant e data de acesso.

CREATE NONCLUSTERED INDEX IX_AccessRecords_Visitor 
ON multitenant.AccessRecords (TenantId, VisitorId, AccessDateTime);
-- Coment�rio: �ndice n�o clusterizado para otimizar consultas por visitante e data.

CREATE NONCLUSTERED INDEX IX_AccessRecords_Employee 
ON multitenant.AccessRecords (TenantId, EmployeeId, AccessDateTime);
-- Coment�rio: �ndice n�o clusterizado para otimizar consultas por funcion�rio e data.

CREATE NONCLUSTERED INDEX IX_AccessRecords_Device 
ON multitenant.AccessRecords (TenantId, DeviceId, AccessDateTime);
-- Coment�rio: �ndice n�o clusterizado para otimizar consultas por dispositivo e data.

CREATE NONCLUSTERED INDEX IX_Visitors_Tenant_Document 
ON multitenant.Visitors (TenantId, DocumentType, DocumentNumber);
-- Coment�rio: �ndice n�o clusterizado para otimizar consultas por documento de visitantes.

CREATE NONCLUSTERED INDEX IX_Visitors_Tenant_Name 
ON multitenant.Visitors (TenantId, LastName, FirstName);
-- Coment�rio: �ndice n�o clusterizado para otimizar consultas por nome de visitantes.

CREATE NONCLUSTERED INDEX IX_Employees_Tenant_Document 
ON multitenant.Employees (TenantId, DocumentType, DocumentNumber);
-- Coment�rio: �ndice n�o clusterizado para otimizar consultas por documento de funcion�rios.

CREATE NONCLUSTERED INDEX IX_Employees_Tenant_Name 
ON multitenant.Employees (TenantId, LastName, FirstName);
-- Coment�rio: �ndice n�o clusterizado para otimizar consultas por nome de funcion�rios.

CREATE NONCLUSTERED INDEX IX_AuditLogs_Tenant_Date 
ON audit.AuditLogs (TenantId, CreatedAt);
-- Coment�rio: �ndice n�o clusterizado para otimizar consultas por tenant e data de auditoria.

CREATE NONCLUSTERED INDEX IX_AuditLogs_User 
ON audit.AuditLogs (TenantId, UserId, CreatedAt);
-- Coment�rio: �ndice n�o clusterizado para otimizar consultas por usu�rio e data.

CREATE NONCLUSTERED INDEX IX_UsageMetrics_Tenant_Date 
ON billing.UsageMetrics (TenantId, MetricDate);
-- Coment�rio: �ndice n�o clusterizado para otimizar consultas por tenant e data de m�tricas.

CREATE NONCLUSTERED COLUMNSTORE INDEX CSI_AccessRecords 
ON multitenant.AccessRecords (TenantId, AccessDateTime, Status);
-- Coment�rio: �ndice columnstore para otimizar an�lises em massa de registros de acesso.
GO

-- =============================================
-- Vis�es Indexadas para Relat�rios
-- =============================================

CREATE VIEW reporting.AccessDashboard
WITH SCHEMABINDING
AS
SELECT 
    ar.TenantId, -- Chave estrangeira: Identificador do tenant.
    ar.LocationId, -- Chave estrangeira: Identificador do local.
    CAST(ar.AccessDateTime AS DATE) AS AccessDate, -- Campo de dados: Data dos eventos de acesso.
    COUNT_BIG(*) AS TotalAccesses, -- Campo de dados: Total de eventos de acesso.
    SUM(CASE WHEN ar.AccessType = 'Entry' THEN 1 ELSE 0 END) AS Entries, -- Campo de dados: N�mero de entradas.
    SUM(CASE WHEN ar.AccessType = 'Exit' THEN 1 ELSE 0 END) AS Exits, -- Campo de dados: N�mero de sa�das.
    SUM(CASE WHEN ar.Status = 'Denied' THEN 1 ELSE 0 END) AS DeniedAccesses -- Campo de dados: N�mero de acessos negados.
FROM multitenant.AccessRecords ar
GROUP BY ar.TenantId, ar.LocationId, CAST(ar.AccessDateTime AS DATE);
GO

CREATE UNIQUE CLUSTERED INDEX IX_AccessDashboard
ON reporting.AccessDashboard (TenantId, LocationId, AccessDate);
-- Coment�rio: �ndice clusterizado �nico para otimizar a vis�o de dashboard de acessos.
GO

CREATE VIEW reporting.VisitorReport
WITH SCHEMABINDING
AS
SELECT 
    v.TenantId, -- Chave estrangeira: Identificador do tenant.
    v.VisitorId, -- Chave estrangeira: Identificador do visitante.
    v.FirstName, -- Campo de dados: Primeiro nome do visitante.
    v.LastName, -- Campo de dados: Sobrenome do visitante.
    v.DocumentType, -- Campo de dados: Tipo de documento de identifica��o.
    v.DocumentNumber, -- Campo de dados: N�mero do documento de identifica��o.
    v.CompanyName, -- Campo de dados: Nome da empresa do visitante.
    COUNT_BIG(ar.RecordId) AS TotalVisits, -- Campo de dados: Total de visitas.
    MIN(ar.AccessDateTime) AS FirstVisit, -- Campo de dados: Data da primeira visita.
    MAX(ar.AccessDateTime) AS LastVisit -- Campo de dados: Data da �ltima visita.
FROM multitenant.Visitors v
LEFT JOIN multitenant.AccessRecords ar ON v.VisitorId = ar.VisitorId AND v.TenantId = ar.TenantId
GROUP BY v.TenantId, v.VisitorId, v.FirstName, v.LastName, v.DocumentType, v.DocumentNumber, v.CompanyName;
GO

CREATE UNIQUE CLUSTERED INDEX IX_VisitorReport
ON reporting.VisitorReport (TenantId, VisitorId);
-- Coment�rio: �ndice clusterizado �nico para otimizar a vis�o de relat�rio de visitantes.
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
        
        -- Obt�m localiza��o e fuso hor�rio a partir do dispositivo
        SELECT @LocationId = l.LocationId, @TimeZoneOffset = l.TimeZone
        FROM multitenant.Devices d
        JOIN multitenant.Locations l ON d.LocationId = l.LocationId
        WHERE d.DeviceId = @DeviceId AND d.TenantId = @TenantId;
        
        IF @LocationId IS NULL
            THROW 50001, 'Dispositivo ou Tenant inv�lido', 1;
        
        -- Converte para o fuso hor�rio local
        SET @AccessDateTime = DATEADD(hour, CASE WHEN @TimeZoneOffset LIKE '%-03' THEN -3 ELSE 0 END, SYSUTCDATETIME());
        
        -- Identifica visitante/funcion�rio
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
        
        -- Verifica autoriza��o e zonas
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
                SET @DenialReason = 'Nenhuma autoriza��o v�lida encontrada';
            END
            ELSE
            BEGIN
                -- Verifica permiss�es de zona dinamicamente
                DECLARE @Role NVARCHAR(50) = 'Visitor';
                IF @EmployeeId IS NOT NULL SET @Role = 'Employee';
                
                IF NOT EXISTS (
                    SELECT 1 FROM multitenant.ZonePermissions zp
                    JOIN multitenant.RestrictedZones rz ON zp.ZoneId = rz.ZoneId
                    WHERE rz.TenantId = @TenantId AND zp.Role = @Role AND zp.AccessLevel = 'Granted'
                )
                BEGIN
                    SET @Status = 'Denied';
                    SET @DenialReason = 'Acesso � zona restrito';
                END
            END
        END
        
        -- Verifica emerg�ncia
        IF EXISTS (SELECT 1 FROM multitenant.Alerts WHERE TenantId = @TenantId AND Severity = 'Critical' AND Resolved = 0)
        BEGIN
            SET @Status = 'Denied';
            SET @DenialReason = 'Bloqueio de emerg�ncia em efeito';
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
        
        -- Calcula pontua��o de anomalia
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
            THROW 50002, 'Contexto de Tenant ou Usu�rio n�o definido', 1;
        
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
-- Seguran�a de N�vel de Linha (RLS)
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
('Ind�stria Exemplo', '12345678901234', 'Jo�o Silva', 'joao@exemplo.com', '11987654321'),
('Com�rcio Teste', '98765432101234', 'Maria Oliveira', 'maria@teste.com', '21976543210');
GO

INSERT INTO billing.SubscriptionPlans (PlanName, Description, MaxUsers, MaxLocations, MaxDevices, MaxVisitorsPerMonth, RetentionPeriodDays, HasBiometricIntegration, HasAPIAccess, HasAdvancedReporting, MonthlyPrice)
VALUES 
('Plano B�sico', 'Plano com funcionalidades b�sicas', 10, 1, 2, 100, 90, 0, 0, 0, 99.90),
('Plano Premium', 'Plano com recursos avan�ados', 50, 5, 10, NULL, 365, 1, 1, 1, 299.90);
GO

INSERT INTO billing.Subscriptions (TenantId, PlanId, StartDate, EndDate, AutoRenew, PaymentMethod, PaymentStatus)
VALUES 
((SELECT TenantId FROM saas.Tenants WHERE CompanyName = 'Ind�stria Exemplo'), 1, '2025-08-01', '2025-08-31', 1, 'Credit Card', 'Active'),
((SELECT TenantId FROM saas.Tenants WHERE CompanyName = 'Com�rcio Teste'), 2, '2025-08-01', '2025-08-31', 1, 'Credit Card', 'Active');
GO

INSERT INTO multitenant.Locations (TenantId, Name, Address, City, State, Country)
VALUES 
((SELECT TenantId FROM saas.Tenants WHERE CompanyName = 'Ind�stria Exemplo'), 'Escrit�rio Principal', 'Rua Exemplo 123', 'S�o Paulo', 'SP', 'Brasil'),
((SELECT TenantId FROM saas.Tenants WHERE CompanyName = 'Com�rcio Teste'), 'Loja Central', 'Av. Teste 456', 'Rio de Janeiro', 'RJ', 'Brasil');
GO

INSERT INTO multitenant.Users (TenantId, LocationId, Username, PasswordHash, Email, FirstName, LastName, DocumentType, DocumentNumber, PhoneNumber, Role)
VALUES 
((SELECT TenantId FROM saas.Tenants WHERE CompanyName = 'Ind�stria Exemplo'), (SELECT LocationId FROM multitenant.Locations WHERE Name = 'Escrit�rio Principal'), 'joao.admin', 'hash123', 'joao.admin@exemplo.com', 'Jo�o', 'Silva', 'CPF', '12345678901', '11987654321', 'Admin'),
((SELECT TenantId FROM saas.Tenants WHERE CompanyName = 'Com�rcio Teste'), (SELECT LocationId FROM multitenant.Locations WHERE Name = 'Loja Central'), 'maria.op', 'hash456', 'maria.op@teste.com', 'Maria', 'Oliveira', 'CPF', '98765432101', '21976543210', 'Operator');
GO