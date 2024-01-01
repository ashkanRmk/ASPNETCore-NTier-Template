using System;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Liaro.DataLayer.Abstract;
using Liaro.DataLayer.Context;
using Liaro.DataLayer.Repository;
using Liaro.Misc;
using Liaro.Mapping;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.AspNetCore.Http;
using Liaro.ServiceLayer.Security;
using Liaro.ModelLayer.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Diagnostics;
using Newtonsoft.Json;
using System.Collections.Generic;
using Liaro.ServiceLayer;
using Liaro.ServiceLayer.Contracts;
using FluentValidation.AspNetCore;
using FluentValidation;
using Liaro.ModelLayer.ShortLink;

namespace Liaro
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<BearerTokensOptions>()
                .Bind(Configuration.GetSection("BearerTokens"))
                .Validate(bearerTokens =>
                {
                    return bearerTokens.AccessTokenExpirationMinutes < bearerTokens.RefreshTokenExpirationMinutes;
                }, "RefreshTokenExpirationMinutes is less than AccessTokenExpirationMinutes. Obtaining new tokens using the refresh token should happen only if the access token has expired.");
            services.AddOptions<ApiSettings>()
                .Bind(Configuration.GetSection("ApiSettings"));

            //Security Services Registrations
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<ISecurityService, SecurityService>();
            services.AddScoped<IAntiForgeryCookieService, AntiForgeryCookieService>();
            services.AddScoped<IUsersService, UsersService>();
            services.AddScoped<IRolesService, RolesService>();
            services.AddScoped<IDbInitializerService, DbInitializerService>();
            services.AddScoped<ITokenStoreService, TokenStoreService>();
            services.AddScoped<ITokenValidatorService, TokenValidatorService>();
            services.AddScoped<ITokenFactoryService, TokenFactoryService>();

            //Liaro Services Registrations
            services.AddScoped<IKavenegarService, KavenegarService>();
            services.AddScoped<IShortLinksService, ShortLinksService>();

            //Model Validator Registrations
            services.AddTransient<IValidator<ShortLinkCreateVM>, ShortLinkValidator>();


            services.AddEntityFrameworkNpgsql();
            services.AddDbContextPool<ApplicationDbContext>((serviceProvider, optionsBuilder) =>
            {
                optionsBuilder.UseNpgsql(Configuration.GetConnectionString("LiaroDb"),
                    b => b.MigrationsAssembly("Liaro"));
            });

            services.AddTransient(typeof(IEntityBaseRepository<>), typeof(EntityBaseRepository<>));

            var config = new MapperConfiguration(cfg => { cfg.AddProfile(new AutoMapperConfiguration()); });
            services.AddSingleton<IMapper>(sp => config.CreateMapper());

            services.AddLogging(loggingBuilder =>
                loggingBuilder.AddSerilog(dispose: true));

            // Only needed for custom roles.
            services.AddAuthorization(options =>
                {
                    options.AddPolicy(CustomRoles.Admin, policy => policy.RequireRole(CustomRoles.Admin));
                    options.AddPolicy(CustomRoles.User, policy => policy.RequireRole(CustomRoles.User));
                    options.AddPolicy(CustomRoles.Editor, policy => policy.RequireRole(CustomRoles.Editor));
                });

            // Needed for jwt auth.
            services
                .AddAuthentication(options =>
                {
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(cfg =>
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = Configuration["BearerTokens:Issuer"], // site that makes the token
                        ValidateIssuer = false, // TODO: change this to avoid forwarding attacks
                        ValidAudience = Configuration["BearerTokens:Audience"], // site that consumes the token
                        ValidateAudience = false, // TODO: change this to avoid forwarding attacks
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["BearerTokens:Key"])),
                        ValidateIssuerSigningKey = true, // verify signature to avoid tampering
                        ValidateLifetime = true, // validate the expiration
                        ClockSkew = TimeSpan.Zero // tolerance for the expiration date
                    };
                    cfg.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(JwtBearerEvents));
                            logger.LogError("Authentication failed.", context.Exception);
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            var tokenValidatorService = context.HttpContext.RequestServices.GetRequiredService<ITokenValidatorService>();
                            return tokenValidatorService.ValidateAsync(context);
                        },
                        OnMessageReceived = context =>
                        {
                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(JwtBearerEvents));
                            logger.LogError("OnChallenge error", context.Error, context.ErrorDescription);
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
            });

            // services.AddAntiforgery(x => x.HeaderName = "X-XSRF-TOKEN");

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddSwaggerGen(c =>
            {
                // c.AddSecurityDefinition("Bearer", new ApiKeyScheme()
                // {
                //     In = "header",
                //     Description = "JWT Authorization header using the Bearer scheme. Example: \"bearer {token}\"",
                //     Name = "Authorization",
                //     Type = "apiKey"
                // });

                // c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                // {
                //     {"Bearer", new string[] { }}
                // });
                //
                // c.SwaggerDoc("v1", new Info
                // {
                //     Version = "v1",
                //     Title = "Liaro | لیارو",
                //     Description = "My Liaro Open API",
                //     TermsOfService = "None"
                // });
                // c.OperationFilter<SwaggerFileUploadOperation>();
            });

            services.AddMvc(options =>
                    {
                        options.Filters.Add(typeof(ValidateModelStateAttribute));
                    })
                    .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                    .AddFluentValidation();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseExceptionHandler(appBuilder =>
            {
                appBuilder.Use(async (context, next) =>
                {
                    var error = context.Features[typeof(IExceptionHandlerFeature)] as IExceptionHandlerFeature;
                    if (error != null && error.Error is SecurityTokenExpiredException)
                    {
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonConvert.SerializeObject(new
                        {
                            State = 401,
                            Msg = "token expired"
                        }));
                    }
                    else if (error != null && error.Error != null)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonConvert.SerializeObject(new
                        {
                            State = 500,
                            Msg = error.Error.Message
                        }));
                    }
                    else
                    {
                        await next();
                    }
                });
            });

            app.UseAuthentication();

            var scopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            using (var scope = scopeFactory.CreateScope())
            {
                var dbInitializer = scope.ServiceProvider.GetService<IDbInitializerService>();
                dbInitializer.Initialize();
                dbInitializer.SeedData();
            }
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Liaro");
                c.DocumentTitle = "My Liaro Open API";
            });
        }
    }
}
