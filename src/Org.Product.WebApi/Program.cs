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
using Org.Product.Application.Options;
using Org.Product.Domain.Shared;
using Org.Product.Domain.Utilities;
using Org.Product.Infrastructure.Repositories;
using Org.Product.WebApi.Auth;
using Org.Product.WebApi.Auth.AuthHandlers;
using Org.Product.WebApi.Options;
using Org.Product.WebApi.Utilities;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var jwtAuthenticationOptions =
    builder
        .Configuration.GetRequiredSection(JwtAuthenticationOptions.SectionName)
        .Get<JwtAuthenticationOptions>()
    ?? throw new InvalidOperationException("Missing Jwt configuration.");
var openApiOptions =
    builder.Configuration.GetRequiredSection(OpenApiOptions.SectionName).Get<OpenApiOptions>()
    ?? throw new InvalidOperationException("Missing OpenApiInfo configuration.");

CryptoUtil.Initialize(jwtAuthenticationOptions.KeyFolder);

builder.Services.AddAllOptions();

// Change container to autoFac
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(config =>
    config.RegisterAssemblyModules(
        Assembly.GetExecutingAssembly(),
        typeof(AccessTokenOptions).Assembly
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
        config.JsonSerializerOptions.DefaultIgnoreCondition = SharedOptions
            .CustomJsonSerializerOptions
            .DefaultIgnoreCondition;
        config.JsonSerializerOptions.PropertyNameCaseInsensitive = SharedOptions
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
        option.EventsType = typeof(SessionJwtBearerEvents);
        option.SaveToken = true;
        option.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new ECDsaSecurityKey(CryptoUtil.PublicECDsa), // Use ECDsa
            ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256],
            ValidateIssuer = true,
            ValidIssuer = jwtAuthenticationOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtAuthenticationOptions.Audience,
            RequireExpirationTime = true,
            ValidateLifetime = true,
        };
    });

builder.Services.AddLocalization();

builder.Services.AddAuthorization(options => options.AddAllPolicies());

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
            Description = openApiOptions.Description,
            Title = openApiOptions.Title,
            Contact = new OpenApiContact
            {
                Name = openApiOptions.Name,
                Email = openApiOptions.Email,
                Url = new Uri(openApiOptions.Url),
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
builder.Services.AddAutoMapper(config => config.AddMaps(typeof(AccessTokenOptions).Assembly));

// Add mediatR
builder.Services.AddMediatR(config =>
    config.RegisterServicesFromAssemblies(typeof(AccessTokenOptions).Assembly)
);

var app = builder.Build();
var exceptionLocalizer = app.Services.GetRequiredService<IStringLocalizer<Exception>>();
var includeExceptionDetails = app.Environment.IsDevelopment();

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
            exceptionLocalizer,
            includeExceptionDetails
        )
    )
);

app.Run();
