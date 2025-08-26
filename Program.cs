using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RhSensoWeb.Data;
using RhSensoWeb.Services.Security;
using Serilog;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc; // NEW (para AutoValidateAntiforgeryTokenAttribute)
using RhSensoWeb.Areas.SEG.Services; // NEW (Service da área SEG)

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
                        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); // NEW (opcional, recomendado)
                    })
                    .AddJsonOptions(o =>
                    {
                        o.JsonSerializerOptions.PropertyNamingPolicy = null;      // PascalCase
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

                    // Log do EF (ajuste nível em prod)
                    options.LogTo(Console.WriteLine, LogLevel.Information);
                });

                // =======================
                // AUTENTICAÇÃO (Cookie)
                // =======================
                builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options =>
                    {
                        // FIX: alinhar rotas com a área SEG (você usa /SEG/Account/...)
                        options.LoginPath = "/SEG/Account/Login";
                        options.LogoutPath = "/SEG/Account/Logout";
                        //options.AccessDeniedPath = "/Account/AccessDenied";
                        options.AccessDeniedPath = "/Error/Error403";


                        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // mantido
                        options.SlidingExpiration = true;
                    });

                // =======================
                // SESSÃO
                // =======================
                // ADD: cache distribuído em memória (Session depende de IDistributedCache)
                builder.Services.AddDistributedMemoryCache();

                builder.Services.AddSession(options =>
                {
                    // FIX: sessão muito curta (2 min) causava "quedas" e AccessDenied; aumentamos para 2h
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
                // Nota: em produção/ambiente com mais de uma instância, considere persistir as chaves (FileSystem/Blob/Redis).

                // =======================
                // SERVIÇOS DE SEGURANÇA / UTIL
                // =======================
                builder.Services.AddSingleton<IRowTokenService, RowTokenService>();
                builder.Services.AddMemoryCache();

                // =======================
                // SEG – Services da área (NEW)
                // =======================
                builder.Services.AddScoped<ITsistemaService, TsistemaService>(); // NEW

                builder.Services.AddScoped<IUsuarioService, UsuarioService>();

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
                // ERROS
                // =======================
                var showOriginalErrors = builder.Configuration.GetValue<bool>("Errors:ShowOriginalErrors");
                if (app.Environment.IsDevelopment() || showOriginalErrors)
                {
                    app.UseDeveloperExceptionPage();
                }
                else
                {
                    app.UseExceptionHandler("/Error/500");
                    app.UseStatusCodePagesWithReExecute("/Error/{0}");
                    app.UseHsts();
                }

                // =======================
                // PIPELINE
                // =======================
                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseRouting();

                app.UseSession();          // OK: antes de Authentication/Authorization se você lê Session em filtros
                app.UseAuthentication();
                app.UseRateLimiter();
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
