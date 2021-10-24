using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Avalon.Admin.EntityFramework.Shared.DbContexts;
using Avalon.Admin.EntityFramework.Shared.Entities.Identity;
using Avalon.STS.Identity.Configuration;
using Avalon.STS.Identity.Configuration.Constants;
using Avalon.STS.Identity.Configuration.Interfaces;
using Avalon.STS.Identity.Helpers;
using System;
using Microsoft.AspNetCore.DataProtection;
using Avalon.Shared.Helpers;
using Microsoft.AspNetCore.Http;
using Avalon.Shared.Configuration.Identity;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.HttpOverrides;

namespace Avalon.STS.Identity
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var rootConfiguration = CreateRootConfiguration();
            services.AddSingleton(rootConfiguration);
            // Register DbContexts for IdentityServer and Identity
            RegisterDbContexts(services);

            // Save data protection keys to db, using a common application name shared between Admin and STS
            services.AddDataProtection<IdentityServerDataProtectionDbContext>(Configuration);

            // Add email senders which is currently setup for SendGrid and SMTP
            services.AddEmailSenders(Configuration);

            // Add services for authentication, including Identity model and external providers
            RegisterAuthentication(services);

            // Add HSTS options
            RegisterHstsOptions(services);

            // Add all dependencies for Asp.Net Core Identity in MVC - these dependencies are injected into generic Controllers
            // Including settings for MVC and Localization
            // If you want to change primary keys or use another db model for Asp.Net Core Identity:
            services.AddMvcWithLocalization<UserIdentity, string>(Configuration);

            // Add authorization policies for MVC
            RegisterAuthorization(services);

            //services.Configure<ForwardedHeadersOptions>(options =>
            //{
            //    options.ForwardedHeaders =
            //        ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            //});


            services.AddIdSHealthChecks<IdentityServerConfigurationDbContext, IdentityServerPersistedGrantDbContext, AdminIdentityDbContext, IdentityServerDataProtectionDbContext>(Configuration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration configuration)
        {
            app.UseCookiePolicy();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            var advancedConfiguration = configuration.GetSection(nameof(AdvancedConfiguration)).Get<AdvancedConfiguration>();

            if ( advancedConfiguration != null && !string.IsNullOrWhiteSpace(advancedConfiguration.DockerInnerLocation) && !string.IsNullOrWhiteSpace(advancedConfiguration.DockerOuterLocation))
            {
                app.Use(async (httpcontext, next) =>
                {
                    httpcontext.SetIdentityServerOrigin(advancedConfiguration.IssuerUri);

                    await next();

                    if (httpcontext.Response.StatusCode == StatusCodes.Status302Found)
                    {
                        string location = httpcontext.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Location];
                        httpcontext.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Location] =
                                location.Replace(advancedConfiguration.DockerInnerLocation, advancedConfiguration.DockerOuterLocation);
                    }

                });
            }
            

            app.UsePathBase(Configuration.GetValue<string>("BasePath"));

            // Add custom security headers
            app.UseSecurityHeaders(Configuration);

            app.UseStaticFiles();
            UseAuthentication(app);
            app.UseMvcLocalizationServices();

            app.UseRouting();
            app.UseAuthorization();
            var forwardOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                RequireHeaderSymmetry = false
            };

            forwardOptions.KnownNetworks.Clear();
            forwardOptions.KnownProxies.Clear();

            // ref: https://github.com/aspnet/Docs/issues/2384
            app.UseForwardedHeaders(forwardOptions);

            app.UseEndpoints(endpoint =>
            {
                endpoint.MapDefaultControllerRoute();
                endpoint.MapHealthChecks("/health", new HealthCheckOptions
                {
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
            });
        }

        public virtual void RegisterDbContexts(IServiceCollection services)
        {
            services.RegisterDbContexts<AdminIdentityDbContext, IdentityServerConfigurationDbContext, IdentityServerPersistedGrantDbContext, IdentityServerDataProtectionDbContext>(Configuration);
        }

        public virtual void RegisterAuthentication(IServiceCollection services)
        {
            services.AddAuthenticationServices<AdminIdentityDbContext, UserIdentity, UserIdentityRole>(Configuration);
            services.AddIdentityServer<IdentityServerConfigurationDbContext, IdentityServerPersistedGrantDbContext, UserIdentity>(Configuration);
        }

        public virtual void RegisterAuthorization(IServiceCollection services)
        {
            var rootConfiguration = CreateRootConfiguration();
            services.AddAuthorizationPolicies(rootConfiguration);
        }

        public virtual void UseAuthentication(IApplicationBuilder app)
        {
            app.UseIdentityServer();
        }

        public virtual void RegisterHstsOptions(IServiceCollection services)
        {
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });
        }

        protected IRootConfiguration CreateRootConfiguration()
        {
            var rootConfiguration = new RootConfiguration();
            Configuration.GetSection(ConfigurationConsts.AdminConfigurationKey).Bind(rootConfiguration.AdminConfiguration);
            Configuration.GetSection(ConfigurationConsts.RegisterConfigurationKey).Bind(rootConfiguration.RegisterConfiguration);
            return rootConfiguration;
        }
    }
}






