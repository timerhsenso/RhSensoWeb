using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;                 // StatusCodes, SameSiteMode, CookieSecurePolicy
using Microsoft.AspNetCore.Mvc;                  // AutoValidateAntiforgeryTokenAttribute
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RhSensoWeb.Areas.SEG.Services;            // Serviços da área SEG
using RhSensoWeb.Areas.SYS.Services;            // Serviços da área SYS
using RhSensoWeb.Data;
using RhSensoWeb.Middleware;                    // Middleware global de exceções
using RhSensoWeb.Services.Security;             // RowTokenService / etc.
using Serilog;
using System.Text.Json;                          // JSON no RateLimiter
using System.Threading.RateLimiting;

namespace RhSensoWeb
{
    public partial class Program
    {
        public static void Main(string[] args)
        {
            // =========================================================
            //  SERILOG (logger do host) — CloseAndFlush no finally
            // =========================================================
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                var builder = WebApplication.CreateBuilder(args);
                builder.Host.UseSerilog();

                // =========================================================
                //  MVC + JSON
                //  - Anti-CSRF global em métodos mutáveis
                //  - Mantém PascalCase (compatível com seu front)
                // =========================================================
                builder.Services
                    .AddControllersWithViews(opt =>
                    {
                        opt.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                    })
                    .AddJsonOptions(o =>
                    {
                        o.JsonSerializerOptions.PropertyNamingPolicy = null;
                        o.JsonSerializerOptions.DictionaryKeyPolicy = null;
                    });

                // =========================================================
                //  INFRA & HELPERS
                // =========================================================
                builder.Services.AddHttpContextAccessor();
                builder.Services.AddScoped<SqlLoggingInterceptor>();     // seu interceptor de SQL
                builder.Services.AddMemoryCache();

                // =========================================================
                //  EF CORE / SQL SERVER
                //  - Retry on failure: resiliente a quedas momentâneas
                //  - Logs detalhados em DEV
                //  - Interceptor com contexto de request
                // =========================================================
                builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
                {
                    var cs = builder.Configuration.GetConnectionString("DefaultConnection");
                    options.UseSqlServer(cs, sql => sql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null));

                    if (builder.Environment.IsDevelopment())
                        options.EnableSensitiveDataLogging();

                    options.EnableDetailedErrors();
                    options.AddInterceptors(sp.GetRequiredService<SqlLoggingInterceptor>());
                    options.LogTo(Console.WriteLine, LogLevel.Information);
                });

                // =========================================================
                //  AUTENTICAÇÃO (Cookies) + opções de cookie mais seguras
                // =========================================================
                builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options =>
                    {
                        options.LoginPath = "/SEG/Account/Login";
                        options.LogoutPath = "/SEG/Account/Logout";
                        options.AccessDeniedPath = "/Error/Error403";

                        options.Cookie.Name = ".RhSensoWeb.Auth";
                        options.Cookie.HttpOnly = true;
                        options.Cookie.SameSite = SameSiteMode.Lax;    // evita CSRF sem quebrar POSTs
                        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

                        options.ExpireTimeSpan = TimeSpan.FromHours(2);
                        options.SlidingExpiration = true;
                    });

                // =========================================================
                //  SESSÃO (usada nos seus filtros e helpers)
                // =========================================================
                builder.Services.AddDistributedMemoryCache();
                builder.Services.AddSession(options =>
                {
                    options.IdleTimeout = TimeSpan.FromHours(2);
                    options.Cookie.Name = ".RhSensoWeb.Session";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                });

                // =========================================================
                //  ANTI-FORGERY — cabeçalho usado pelo seu front (RequestVerificationToken)
                // =========================================================
                builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

                // =========================================================
                //  DATA PROTECTION — (persistir chaves em produção/multi-instância)
                // =========================================================
                builder.Services.AddDataProtection();

                // =========================================================
                //  DI: Serviços de domínio
                // =========================================================
                builder.Services.AddSingleton<IRowTokenService, RowTokenService>();

                // SEG
                builder.Services.AddScoped<IUsuarioService, UsuarioService>();
                builder.Services.AddScoped<ITsistemaService, TsistemaService>();
                builder.Services.AddScoped<IBtfuncaoService, BtfuncaoService>();

                // SYS
                builder.Services.AddScoped<ITaux1Service, Taux1Service>();
                builder.Services.AddScoped<ITaux2Service, Taux2Service>();

                // MIDDLEWARES via DI
                builder.Services.AddScoped<_ExceptionHandlingMiddleware>();

                // =========================================================
                //  RATE LIMITER — política para endpoints “sensíveis” (ex.: toggle)
                //  - Responde JSON amigável + Retry-After para o front iniciar cooldown
                // =========================================================
                builder.Services.AddRateLimiter(options =>
                {
                    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                    options.OnRejected = async (ctx, ct) =>
                    {
                        ctx.HttpContext.Response.ContentType = "application/json";

                        // Sugestão de espera vinda do limiter
                        int retryAfter = 2;
                        if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ra))
                            retryAfter = Math.Max(1, (int)Math.Ceiling(ra.TotalSeconds));

                        // Também adiciona header padrão (bom para proxies/clients)
                        ctx.HttpContext.Response.Headers["Retry-After"] = retryAfter.ToString();

                        var payload = new
                        {
                            success = false,
                            message = $"Muitas tentativas. Tente novamente em {retryAfter}s.",
                            retryAfter
                        };

                        await ctx.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(payload), ct);
                    };

                    options.AddPolicy("UpdateAtivoPolicy", httpContext =>
                    {
                        // chave por usuário autenticado; fallback IP quando anônimo
                        var key = httpContext.User?.Identity?.Name
                                  ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                  ?? "anon";

                        return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 1,                     // 1 ação por janela
                            TokensPerPeriod = 1,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(2),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                    });
                });

                // =========================================================
                //  BUILD DO APP
                // =========================================================
                var app = builder.Build();

                // =========================================================
                //  MIDDLEWARES GLOBAIS — ficam no topo do pipeline
                // =========================================================
                app.UseMiddleware<_ExceptionHandlingMiddleware>();   // tratamento unificado de exceções → JSON 500
                app.UseSerilogRequestLogging();                      // loga cada request (Status, Path, Tempo, etc.)

                // Header de ambiente em toda resposta (útil em debug/infra)
                app.Use(async (ctx, next) =>
                {
                    ctx.Response.Headers["X-Env"] = app.Environment.EnvironmentName;
                    await next();
                });

                // Exponhe HttpContext a helpers estáticos (seu helper usa isso)
                var accessor = app.Services.GetRequiredService<IHttpContextAccessor>();
                RhSensoWeb.Helpers.ConstanteHelper.Configure(accessor);

                // =========================================================
                //  STATUS PAGES + HSTS
                //  - StatusCodePagesWithReExecute trata 4xx/5xx NÃO-exceções (ex.: 404 de estático)
                //  - HSTS só em produção e ANTES do redirect
                // =========================================================
                app.UseStatusCodePagesWithReExecute("/Error/{0}");

                if (!app.Environment.IsDevelopment())
                {
                    app.UseHsts();
                }

                // =========================================================
                //  PIPELINE PRINCIPAL (ordem importa)
                //  - StaticFiles antes de Routing evita passar por filtros desnecessários
                //  - Session → Auth → RateLimiter → Authorization
                // =========================================================
                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseRouting();

                app.UseSession();
                app.UseAuthentication();
                app.UseRateLimiter();        // políticas por endpoint (ex.: [EnableRateLimiting("UpdateAtivoPolicy")])
                app.UseAuthorization();

                // =========================================================
                //  ROTAS
                //  - Áreas primeiro, depois rota padrão
                // =========================================================
                app.MapControllerRoute(
                    name: "areas",
                    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Falha crítica na inicialização");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
