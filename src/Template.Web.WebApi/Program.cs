using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Npgsql;
using Serilog;
using Template.Web.Domain.Shared;
using Template.Web.Domain.Shared.Utilities;
using Template.Web.Domain.Utilities;
using Template.Web.Domain.ValueObjects;
using Template.Web.Infrastructure.Repositories;
using Template.Web.WebApi.Auth;
using Template.Web.WebApi.Utilities;

var builder = WebApplication.CreateBuilder(args);

#region util Initialize

InitialConfiguration.Initialize(builder.Configuration);
CryptoUtil.Initialize(InitialConfiguration.Jwt.KeyFolder);

#endregion util Initialize

// Change container to autoFac
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(config =>
    config.RegisterAssemblyModules(
        Assembly.GetExecutingAssembly(),
        Assembly.Load("Template.Web." + nameof(Template.Web.Application))
    )
);

// Add services to the container.
builder.Host.UseSerilog(
    (context, logger) =>
    {
        logger.ReadFrom.Configuration(context.Configuration);
        logger.Enrich.FromLogContext();
    }
);

builder.Services.AddLogging();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder
    .Services.AddControllers()
    .AddJsonOptions(config =>
    {
        config.JsonSerializerOptions.DefaultIgnoreCondition = Options
            .CustomJsonSerializerOptions
            .DefaultIgnoreCondition;
        config.JsonSerializerOptions.PropertyNameCaseInsensitive = Options
            .CustomJsonSerializerOptions
            .PropertyNameCaseInsensitive;
    });
builder.Services.AddHttpContextAccessor();
builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(option =>
    {
        option.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new ECDsaSecurityKey(CryptoUtil.PublicECDsa), // Use ECDsa
            ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256],
            ValidateIssuer = true,
            ValidIssuer = InitialConfiguration.Jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = InitialConfiguration.Jwt.Audience,
            RequireExpirationTime = true,
            ValidateLifetime = true,
        };
    });

builder.Services.AddLocalization();

var scopes = Assembly
    .GetExecutingAssembly()
    .GetTypes()
    .Where(type => type.Namespace == "Template.Web.WebApi.Controllers")
    .SelectMany(type =>
        type.GetMethods()
            .Select(m => m.GetCustomAttribute<AuthorizeAttribute>())
            .Append(type.GetCustomAttribute<AuthorizeAttribute>())
    )
    .Where(t => t?.Policy is not null)
    .Select(t => t!.Policy!)
    .ToArray();

builder.Services.AddAuthorization(options => options.AddAllPolicies(scopes));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    option.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    option.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Description = InitialConfiguration.OpenApi.Description,
            Title = InitialConfiguration.OpenApi.Title,
            Contact = new OpenApiContact
            {
                Name = InitialConfiguration.OpenApi.Name,
                Email = InitialConfiguration.OpenApi.Email,
                Url = new Uri(InitialConfiguration.OpenApi.Url),
            },
        }
    );
    option.AddSecurityDefinition(
        JwtBearerDefaults.AuthenticationScheme,
        new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Description = "",
            Name = "Authentication",
            Scheme = JwtBearerDefaults.AuthenticationScheme,
        }
    );
    option.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference(
                JwtBearerDefaults.AuthenticationScheme,
                hostDocument: document,
                externalResource: null
            ),
            []
        },
    });
});

// Add dbContext pool
builder.Services.AddDbContextPool<ApiDbContext>(options =>
{
    options
        .UseNpgsql(
            new NpgsqlDataSourceBuilder(
                builder.Configuration.GetConnectionString("Postgres")
            ).Build()
        )
        .EnableDetailedErrors();
    options.UseSnakeCaseNamingConvention();
});

// Add mapper profiles
builder.Services.AddAutoMapper(config =>
    config.AddMaps(Assembly.Load("Template.Web." + nameof(Template.Web.Application)))
);

// Add mediatR
builder.Services.AddMediatR(config =>
    config.RegisterServicesFromAssemblies(
        Assembly.Load("Template.Web." + nameof(Template.Web.Application))
    )
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var initialDatabase = scope.ServiceProvider.GetRequiredService<InitialDatabase>();
    await initialDatabase.Initialize();
}

app.UseExceptionHandler(builder =>
    builder.Run(async context =>
        await ExceptionLocalizerExtension.LocalizeException(
            context,
            app.Services.GetService<IStringLocalizer<Exception>>()!
        )
    )
);

app.Run();
