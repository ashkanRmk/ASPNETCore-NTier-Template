using System;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using Liaro.DataLayer.Abstract;
using Liaro.DataLayer.Context;
using Liaro.DataLayer.Repository;
using Liaro.Mapping;
using Liaro.ModelLayer.Security;
using Liaro.ModelLayer.ShortLink;
using Liaro.ServiceLayer;
using Liaro.ServiceLayer.Contracts;
using Liaro.ServiceLayer.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddOptions<BearerTokensOptions>()
                .Bind(configuration.GetSection("BearerTokens"))
                .Validate(bearerTokens =>
                {
                    return bearerTokens.AccessTokenExpirationMinutes < bearerTokens.RefreshTokenExpirationMinutes;
                }, "RefreshTokenExpirationMinutes is less than AccessTokenExpirationMinutes. Obtaining new tokens using the refresh token should happen only if the access token has expired.");
services.AddOptions<ApiSettings>()
    .Bind(configuration.GetSection("ApiSettings"));

//Security Services Registrations
services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
services.AddSingleton<ISecurityService, SecurityService>();
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


// services.AddEntityFrameworkNpgsql();
services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("LiaroDb"),
            b => b.MigrationsAssembly("Liaro")));

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
            ValidIssuer = configuration["BearerTokens:Issuer"], // site that makes the token
            ValidateIssuer = false, // TODO: change this to avoid forwarding attacks
            ValidAudience = configuration["BearerTokens:Audience"], // site that consumes the token
            ValidateAudience = false, // TODO: change this to avoid forwarding attacks
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["BearerTokens:Key"])),
            ValidateIssuerSigningKey = true, // verify signature to avoid tampering
            ValidateLifetime = true, // validate the expiration
            ClockSkew = TimeSpan.Zero // tolerance for the expiration date
        };
        cfg.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(JwtBearerEvents));
                logger.LogError("Authentication failed. Exeption:{}", context.Exception);
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
                logger.LogError("OnChallenge error Exeption:{}, Description:{}", context.Error, context.ErrorDescription);
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

services.Configure<CookiePolicyOptions>(options =>
{
    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});

services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Liaro", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
    });
});

services.AddControllers();
services.AddEndpointsApiExplorer();




var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
    app.UseSwagger();
    app.UseSwaggerUI();
// }

var dbInitService = app.Services.GetService<IDbInitializerService>();
dbInitService.Initialize();
dbInitService.SeedData();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
