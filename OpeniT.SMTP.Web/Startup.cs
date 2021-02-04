using BlazorPro.BlazorSize;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpeniT.SMTP.Web.DataRepositories;
using OpeniT.SMTP.Web.Methods;
using Newtonsoft.Json;
using OpeniT.SMTP.Web.Models;
using System;
using System.Net.Http;
using Blazored.LocalStorage;
using MatBlazor;
using iTools.Utilities.JsRuntimeStream;
using OpeniT.SMTP.Web.Helpers;

namespace OpeniT.SMTP.Web
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables();
            this.Configuration = builder.Build();
        }
        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddServerSideBlazor();
            services.AddSingleton(this.Configuration);
            services.AddDbContext<DataContext>(options => options.UseSqlServer(Configuration.GetConnectionString("PortalConnection"), b => b.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

            services.AddTransient<AzureHelper>();
            services.AddScoped<IDataRepository, DataRepository>();
            services.AddScoped<SMTPMethods>();
            services.AddScoped<HttpClient>();
            services.AddScoped<ResizeListener>();
            services.AddJsRuntimeStream();

            services.AddLogging();
            services.AddHttpContextAccessor();

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                options.OnAppendCookie = cookieContext =>
                {
                    if (cookieContext.CookieOptions.SameSite == SameSiteMode.None) cookieContext.CookieOptions.SameSite = SameSiteMode.Unspecified;
                };
                options.OnDeleteCookie = cookieContext =>
                {
                    if (cookieContext.CookieOptions.SameSite == SameSiteMode.None) cookieContext.CookieOptions.SameSite = SameSiteMode.Unspecified;
                };
            });

            services.AddAuthentication(options =>
			{
				options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
			})
				.AddCookie(options =>
				{
					options.LoginPath = new PathString("/signin-oidc");
					options.AccessDeniedPath = new PathString("/Home/Index");
				})
				.AddOpenIdConnect(options =>
				{
					options.Authority = this.Configuration["Microsoft:Authority"];
					options.ClientId = this.Configuration["Microsoft:ClientId"];
					options.SignedOutCallbackPath = new PathString("/signout-callback-oidc");
				});

            
			services.Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme, options =>
			{
				options.Authority = options.Authority + "/v2.0/";

				options.TokenValidationParameters.ValidateIssuer = false;
			});

			services.AddIdentity<ApplicationUser, IdentityRole>(options =>
			{
				options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
			})
			.AddEntityFrameworkStores<DataContext>()
			.AddDefaultTokenProviders();

			services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 0;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireDigit = true;
            });

            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(10);
                options.Cookie.HttpOnly = true;
            });

            services.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            })
            .AddRazorRuntimeCompilation()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });

            services.Configure<FormOptions>(options =>
            {
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = int.MaxValue; // In case of multipart
            });

            services.AddRouting(options => options.LowercaseUrls = true);

            services.AddBlazoredLocalStorage();

            services.AddSignalR();

            // MatBlazor services
            services.AddMatBlazor();
            services.AddMatToaster(config =>
            {
                config.Position = MatToastPosition.BottomRight;
                config.PreventDuplicates = false;
                config.NewestOnTop = false;
                config.ShowCloseButton = false;
                config.MaximumOpacity = 100;
                config.VisibleStateDuration = 5000;
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            //Localization
            //var options = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
            //app.UseRequestLocalization(options.Value);

            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseSession();
            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("areaRoute", "{area:exists}/{controller=Admin}/{action=Index}/{id?}");
                endpoints.MapControllerRoute("default", "{controller=AccountController}/{action=Index}/{id?}");
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
