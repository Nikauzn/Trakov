using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Ninject;
using Ninject.Activation;
using Ninject.Infrastructure.Disposal;
using System;
using System.Threading;
using Trakov.Backend.Classes;
using Trakov.Backend.Logic.PatreonAPI;
using Trakov.Backend.Logic.Tarkov;
using Trakov.Backend.Mongo;
using Trakov.Backend.Repositories;

namespace Trakov.Backend
{
    public class Startup
    {
        private sealed class Scope : DisposableObject { }
        private readonly AsyncLocal<Scope> scopeProvider = new AsyncLocal<Scope>();
        private static IKernel Kernel { get; set; }
        public static object Resolve(Type type) => Kernel.Get(type);
        private object RequestScope(IContext context) => scopeProvider.Value;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options => options.AddPolicy("CorsPolicy", builder =>
            {
                builder
                .WithOrigins("http://localhost:4200")
                    .AllowAnyMethod().AllowCredentials().AllowAnyHeader();
                builder
                .WithOrigins("https://trakov-alpha.web.app")
                    .AllowAnyMethod().AllowCredentials().AllowAnyHeader();
            }));
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddRequestScopingMiddleware(() => scopeProvider.Value = new Scope());
            services.AddControllers().AddNewtonsoftJson(x =>
            {
                x.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            });
            services.AddCustomControllerActivation(Resolve);
            services.AddCustomViewComponentActivation(Resolve);

            services.AddControllers();
            services.AddAuthentication(x => x.DefaultScheme = "Patreon")
                .AddScheme<PatreonSettings, PatreonAuthorizationMiddleware>("Patreon", x => { });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            Kernel = this.RegisterApplicationComponents(app, env.IsDevelopment());
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            } 
            else
            {
                app.UseHttpsRedirection();
                app.UseHsts();
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseCors("CorsPolicy");
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
        private IKernel RegisterApplicationComponents(IApplicationBuilder app, bool isDev)
        {
            var kernel = new StandardKernel();

            foreach (var ctrlType in app.GetControllerTypes())
            {
                kernel.Bind(ctrlType).ToSelf().InScope(RequestScope);
            }

            kernel.Bind<MongoService>().ToSelf().InSingletonScope()
                .WithConstructorArgument(Configuration.GetConnectionString("MongoDB"));

            kernel.Bind<IPatreonSettings>()
                .ToConstant(PatreonSettings.initFromSection(Configuration.GetSection("PatreonSettings")))
                .InSingletonScope();
            kernel.Bind<ICustomKey>()
                .ToConstant(CustomKey.initFromSection(Configuration.GetSection("PatreonSettings")))
                .InSingletonScope();
            kernel.Bind<ITarkovSyncerSettings>()
                .ToConstant(TarkovSyncerSettings.initFromSection(Configuration.GetSection("TarkovSyncerSettings")))
                .InSingletonScope();

            kernel.Bind<IUserRepository>().To<UserRepository>().InSingletonScope();
            kernel.Bind<IAuthService>().To<AuthService>().InSingletonScope();

            kernel.Bind<ItemsBaseRepo>().To<ItemsRepoMongo>().InSingletonScope();
            kernel.Bind<PricesBaseRepo>().To<PricesRepository>().InSingletonScope();
            kernel.Bind<RecipesRepositoryMongoBase>().To<RecipesRepositoryMongo>().InSingletonScope();
            kernel.Bind<TarkovCredsRepoBase>().To<TarkovCredsRepository>().InSingletonScope();
            kernel.Bind<LogRepositoryBase>().To<LogRepository>().InSingletonScope();
            kernel.Bind<ItemsMetricsRepositoryBase>().To<ItemsMetricsRepository>().InSingletonScope();

            kernel.Bind<ITarkovRepeater>().To<TarkovRepeaterTasks>().InSingletonScope();

            kernel.Bind<IPatreonService>().To<PatreonService>().InSingletonScope();
            kernel.Bind<IAuthenticationHandler>().To<PatreonAuthorizationMiddleware>().InScope(RequestScope);
            kernel.Bind<IPatreonCookieParser>().To<PatreonCookieParser>().InSingletonScope();

            kernel.Bind<MarketPricesTableViewBuilder>().ToSelf().InSingletonScope();

            repeaterService = kernel.Get<ITarkovRepeater>();
            //if (isDev == false)
                _ = repeaterService.launch();

            kernel.BindToMethod(app.GetRequestService<IViewBufferScope>);
            return kernel;
        }

        static public ITarkovRepeater repeaterService = null;
    }
}
