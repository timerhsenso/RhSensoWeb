using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RhSensoWeb.Data;
using RhSensoWeb.Services.Security;
using Serilog;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
// using RhSensoWeb.Infrastructure; // ← (opcional) caso seu SqlLoggingInterceptor esteja em um namespace específico

namespace RhSensoWeb
{
    /// <summary>
    /// Ponto de entrada da aplicação ASP.NET Core.
    /// Configura DI (serviços) e a ordem do pipeline HTTP.
    /// </summary>
    public partial class Program
    {
        public static void Main(string[] args)
        {
            // Builder padrão do host + DI.
            var builder = WebApplication.CreateBuilder(args);


            builder.Services
    .AddControllersWithViews() // ou .AddControllers(), se for API
    .AddJsonOptions(o =>
    {
        // mantém PascalCase no JSON
        o.JsonSerializerOptions.PropertyNamingPolicy = null;
        o.JsonSerializerOptions.DictionaryKeyPolicy = null;
    });

            // =======================
            // LOGGING (Serilog)
            // =======================
            // Lê config de appsettings, enriquece com contexto,
            // e escreve no console + arquivo diário "logs/log-YYYYMMDD.txt".
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // Usa Serilog como provider de logging da aplicação.
            builder.Host.UseSerilog();

            // =======================================================
            // INFRA: HttpContext + Interceptor de SQL para auditoria
            // =======================================================
            // Acesso ao HttpContext via DI (para o interceptor capturar usuário/rota).
            builder.Services.AddHttpContextAccessor();

            // Registra o interceptor que loga SQL + usuário + rota + RequestId.
            builder.Services.AddScoped<SqlLoggingInterceptor>();
                        

            // =======================
            // DB CONTEXT (EF Core)
            // =======================
            // Overload com "sp" para resolver serviços do DI dentro do AddDbContext,
            // permitindo injetar o SqlLoggingInterceptor no pipeline do EF.
            builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

                // Em DEV, loga valores de parâmetros (nunca use em PROD).
                if (builder.Environment.IsDevelopment())
                    options.EnableSensitiveDataLogging();

                // Erros detalhados de consultas/relacionamentos (útil em DEV).
                options.EnableDetailedErrors();

                // >>> Ativa o interceptor (log estruturado de SQL com contexto da requisição).
                options.AddInterceptors(sp.GetRequiredService<SqlLoggingInterceptor>());

                // Log "bruto" do EF no console (opcional: ajuste nível em prod).
                options.LogTo(Console.WriteLine, LogLevel.Information);
            });

            // =======================
            // AUTENTICAÇÃO (Cookie)
            // =======================
            // Cookie de autenticação:
            // - Paths de login/logout/denied.
            // - Expira em 30 min + sliding (renova ao usar).
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                    options.SlidingExpiration = true;
                });

            // =======================
            // SESSÃO
            // =======================
            // - Timeout 30m
            // - HttpOnly: JS não lê o cookie
            // - IsEssential: essencial para o app (consent/GDPR)
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // MVC tradicional (Controllers + Views).
            builder.Services.AddControllersWithViews();

            // =======================
            // ANTI-FORGERY (CSRF)
            // =======================
            // Permite enviar o token anti-CSRF no cabeçalho "RequestVerificationToken"
            // para facilitar chamadas AJAX seguras.
            builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

            // =======================
            // DATA PROTECTION
            // =======================
            // Usado por antiforgery/cookies/etc.
            // Dica: em produção persista chaves (ex.: file share/Blob/Redis) para
            // suportar múltiplas instâncias e reinícios sem invalidar cookies.
            builder.Services.AddDataProtection();

            // Serviço de token por linha (edits/deletes seguros com “propósito” e TTL).
            builder.Services.AddSingleton<IRowTokenService, RowTokenService>();

            // Cache em memória (ex.: cooldown por registro no update de “Ativo”).
            builder.Services.AddMemoryCache();

            // =======================
            // RATE LIMITER
            // =======================
            // Limita frequência de ações por usuário/IP (mitiga flood/cliques repetidos).
            // Política: até 4 req/seg com burst de 8 por chave (user/IP).
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = 429; // Too Many Requests

                options.AddPolicy("UpdateAtivoPolicy", httpContext =>
                {
                    var key = httpContext.User?.Identity?.Name
                              ?? httpContext.Connection.RemoteIpAddress?.ToString()
                              ?? "anon";

                    return RateLimitPartition.GetTokenBucketLimiter(
                        key,
                        _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 8,                  // capacidade do “balde” (burst)
                            TokensPerPeriod = 4,             // reposição por período
                            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                            AutoReplenishment = true,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0                   // sem fila: excedeu -> 429 direto
                        });
                });
            });

            // =======================
            // CONSTRUÇÃO DO APP
            // =======================
            var app = builder.Build();

            // Adiciona um header com o ambiente em TODAS as respostas HTTP
            app.Use(async (ctx, next) =>
            {
                ctx.Response.Headers["X-Env"] = app.Environment.EnvironmentName;
                await next();
            });

            // Disponibiliza HttpContext a helpers globais (ex.: seu ConstanteHelper).
            var accessor = app.Services.GetRequiredService<IHttpContextAccessor>();
            RhSensoWeb.Helpers.ConstanteHelper.Configure(accessor);

            // =======================
            // TRATAMENTO DE ERROS
            // =======================
            // Em DEV (ou se configurado), mostra erros detalhados.
            // Em PROD, redireciona para páginas amigáveis.
            var showOriginalErrors = builder.Configuration.GetValue<bool>("Errors:ShowOriginalErrors");

            if (app.Environment.IsDevelopment() || showOriginalErrors)
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error/500");
                app.UseStatusCodePagesWithReExecute("/Error/{0}");
            }

            // HSTS (HTTPS estrito) fora de DEV.
            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            // =======================
            // MIDDLEWARE PIPELINE (ORDEM IMPORTA!)
            // =======================
            app.UseHttpsRedirection(); // força HTTPS
            app.UseStaticFiles();      // wwwroot
            app.UseRouting();          // endpoint routing

            app.UseSession();          // sessão (antes da auth se você depende dela)
            app.UseAuthentication();   // valida cookie e popula User
            app.UseRateLimiter();      // aplica políticas de rate limit
            app.UseAuthorization();    // [Authorize] e políticas

            // =======================
            // ENDPOINTS / ROTAS
            // =======================
            // Áreas primeiro: /{area}/{controller}/{action}/{id?}
            app.MapControllerRoute(
                name: "areaRoute",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            // Rota padrão: /{controller=Home}/{action=Index}/{id?}
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Sobe o servidor.
            app.Run();
        }
    }
}
