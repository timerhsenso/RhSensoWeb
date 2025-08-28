using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RhSensoWeb.Data;
using RhSensoWeb.Services.Security;
using Serilog;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;       // AutoValidateAntiforgeryTokenAttribute
using RhSensoWeb.Middleware;          // Middleware global de exceções
using RhSensoWeb.Areas.SEG.Services;  // Services da área SEG
using RhSensoWeb.Areas.SYS.Services;  // << NEW: Service da área SYS/Taux1

namespace RhSensoWeb
{
    public partial class Program
    {
        public static void Main(string[] args)
        {
            // --- Serilog com CloseAndFlush no finally (boa prática) ---
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                var builder = WebApplication.CreateBuilder(args);

                // Config Serilog via Host
                builder.Host.UseSerilog();

                // =======================
                // MVC + JSON (uma única vez)
                // =======================
                builder.Services
                    .AddControllersWithViews(options =>
                    {
                        // Aplica Anti-CSRF globalmente a POST/PUT/PATCH/DELETE
                        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                    })
                    .AddJsonOptions(o =>
                    {
                        // Mantém padrão PascalCase (compatível com front atual)
                        o.JsonSerializerOptions.PropertyNamingPolicy = null;
                        o.JsonSerializerOptions.DictionaryKeyPolicy = null;
                    });

                // =======================
                // INFRA
                // =======================
                builder.Services.AddHttpContextAccessor();
                builder.Services.AddScoped<SqlLoggingInterceptor>();

                // =======================
                // DB CONTEXT (EF Core)
                // =======================
                builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
                {
                    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

                    if (builder.Environment.IsDevelopment())
                        options.EnableSensitiveDataLogging();

                    options.EnableDetailedErrors();

                    // Interceptor de SQL com contexto da requisição
                    options.AddInterceptors(sp.GetRequiredService<SqlLoggingInterceptor>());

                    // Log do EF (ajuste nível em produção)
                    options.LogTo(Console.WriteLine, LogLevel.Information);
                });

                // =======================
                // AUTENTICAÇÃO (Cookie)
                // =======================
                builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options =>
                    {
                        // Alinhar rotas com a área SEG
                        options.LoginPath = "/SEG/Account/Login";
                        options.LogoutPath = "/SEG/Account/Logout";
                        options.AccessDeniedPath = "/Error/Error403";

                        options.ExpireTimeSpan = TimeSpan.FromHours(2);
                        options.SlidingExpiration = true;
                    });

                // =======================
                // SESSÃO
                // =======================
                builder.Services.AddDistributedMemoryCache();
                builder.Services.AddSession(options =>
                {
                    options.IdleTimeout = TimeSpan.FromHours(2);
                    options.Cookie.Name = ".RhSensoWeb.Session";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                });

                // =======================
                // ANTI-FORGERY (CSRF)
                // =======================
                builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

                // =======================
                // DATA PROTECTION
                // =======================
                builder.Services.AddDataProtection();
                // Nota: em ambiente multi-instância, persista as chaves (FileSystem/Blob/Redis).

                // =======================
                // SERVIÇOS DE SEGURANÇA / UTIL
                // =======================
                builder.Services.AddSingleton<IRowTokenService, RowTokenService>();
                builder.Services.AddMemoryCache();

                // =======================
                // SEG – 
                // =======================
                builder.Services.AddScoped<ITsistemaService, TsistemaService>();
                builder.Services.AddScoped<IUsuarioService, UsuarioService>();     // Usa UpdateAtivoPolicy no controller de usuário. :contentReference[oaicite:2]{index=2}
                builder.Services.AddScoped<IBtfuncaoService, BtfuncaoService>();

                // =======================
                // SYS – 
                // =======================
                builder.Services.AddScoped<ITaux1Service, Taux1Service>();         // Controller revisado usa RequirePermission/tokens como Usuário.
                builder.Services.AddScoped<ITaux2Service, Taux2Service>();

                // (se o Taux2 usa token por linha, igual Taux1)
                //  builder.Services.AddScoped<IRowTokenService, RowTokenService>();

                // =======================
                // MIDDLEWARES (DI)
                // =======================
                builder.Services.AddScoped<_ExceptionHandlingMiddleware>();         // registra IMiddleware

                // =======================
                // RATE LIMITER
                // =======================
                builder.Services.AddRateLimiter(options =>
                {
                    options.RejectionStatusCode = 429;

                    options.AddPolicy("UpdateAtivoPolicy", httpContext =>
                    {
                        var key = httpContext.User?.Identity?.Name
                                  ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                  ?? "anon";

                        return RateLimitPartition.GetTokenBucketLimiter(
                            key,
                            _ => new TokenBucketRateLimiterOptions
                            {
                                TokenLimit = 8,
                                TokensPerPeriod = 4,
                                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                                AutoReplenishment = true,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 0
                            });
                    });
                });

                var app = builder.Build();

                // =======================
                // EXCEÇÕES (GLOBAL) — deve vir PRIMEIRO no pipeline
                // =======================
                app.UseMiddleware<_ExceptionHandlingMiddleware>();                  // JSON 500 padronizado
                app.UseSerilogRequestLogging();                                    // log de cada request

                // Header de ambiente em todas as respostas
                app.Use(async (ctx, next) =>
                {
                    ctx.Response.Headers["X-Env"] = app.Environment.EnvironmentName;
                    await next();
                });

                // Disponibiliza HttpContext a helpers globais
                var accessor = app.Services.GetRequiredService<IHttpContextAccessor>();
                RhSensoWeb.Helpers.ConstanteHelper.Configure(accessor);

                // =======================
                // ERROS HTTP (não-exceções) + HSTS
                // =======================
                app.UseStatusCodePagesWithReExecute("/Error/{0}");

                if (!app.Environment.IsDevelopment())
                {
                    app.UseHsts();                                               // << move para antes do redirect (recomendado)
                }

                // =======================
                // PIPELINE
                // =======================
                app.UseHttpsRedirection();
                app.UseStaticFiles();

                app.UseRouting();

                // Ordem: Session -> Auth -> RateLimiter -> Authorization
                app.UseSession();          // lê sessão em filtros
                app.UseAuthentication();
                app.UseRateLimiter();      // usa Identity.Name quando houver (vide Usuario/Tsistema) :contentReference[oaicite:3]{index=3} :contentReference[oaicite:4]{index=4}
                app.UseAuthorization();

                // =======================
                // ROTAS
                // =======================
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
                Log.CloseAndFlush(); // garante flush de logs
            }
        }
    }
}
