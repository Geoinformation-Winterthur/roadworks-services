// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
using roadwork_portal_service.Configuration;
using roadwork_portal_service.ElasticsearchLogger;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Npgsql;
using Prometheus;
using NetTopologySuite.IO.Converters;
using Microsoft.OpenApi.Models;



try
{

    Log.Information("Starting roadwork-portal service.");

    NpgsqlConnection.GlobalTypeMapper.UseNetTopologySuite();

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Add services to the container.

    string serviceDomain = AppConfig.Configuration.GetValue<string>("URL:ServiceDomain");
    string serviceBasePath = AppConfig.Configuration.GetValue<string>("URL:ServiceBasePath");
    string securityKey = AppConfig.Configuration.GetValue<string>("SecurityKey");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
        .AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = serviceDomain + serviceBasePath,
                ValidAudience = serviceDomain + serviceBasePath,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey))
            };
        });


    var useElastic = builder.Configuration.GetValue<bool>("ElasticLogger:Use");

    if (useElastic)
    {
        var elasticUrl = builder.Configuration.GetValue<string>("ElasticLogger:EndpointUrl");
        var elasticApplication = builder.Configuration.GetValue<string>("ElasticLogger:Application");

        var environment = !string.IsNullOrWhiteSpace(AppConfig.Configuration.GetValue<string>("Environment"))
                            ? AppConfig.Configuration.GetValue<string>("Environment")
                            : AppConfig.connectionString.IndexOf("wsstadt723", StringComparison.OrdinalIgnoreCase) > -1
                                ? "Test"
                                : AppConfig.connectionString.IndexOf("wsstadt724", StringComparison.OrdinalIgnoreCase) > -1
                                    ? "Production"
                                    : "Unknown";
        Console.WriteLine("I am configured to use Elasticsearch logging.", elasticApplication, environment);

        builder.Services.AddSingleton<IElasticsearchLogger>(new ElasticsearchLogger(elasticUrl, environment, elasticApplication));
    }
    else
    {
        builder.Services.AddSingleton<IElasticsearchLogger, ElasticsearchLoggerNotUsed>();
        Console.WriteLine("I am configured to NOT use Elasticsearch logging.");
    }

    builder.Services.AddControllers()
            .AddJsonOptions(options => {
                options.JsonSerializerOptions.Converters.Add(new GeoJsonConverterFactory());
            });
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options => {
        string serviceDescription = AppConfig.Configuration.GetValue<string>("ServiceDescription");
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Winterthur Roadworks Services API - V1",
            Version = "v1",
            Description = serviceDescription
        });
        var commentsXmlFile = Path.Combine(System.AppContext.BaseDirectory,
                        "roadworks-services.xml");
        options.IncludeXmlComments(commentsXmlFile);
    });

    string clientDomain = AppConfig.Configuration.GetValue<string>("URL:ClientDomain");
    string policyName = "AllowCorsOrigins";


    List<string> allowedOrigins = new List<string>();
    allowedOrigins.Add(clientDomain);

    builder.Services.AddCors(opt =>
    {
        opt.AddPolicy(policyName, policy =>
            policy.WithOrigins(allowedOrigins.ToArray())
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());
    });

    /*
    builder.Services.AddAuthorization(options => 
    {
        options.AddPolicy("BasicAuthenticationForPrometheus", policy => {
            policy.Requirements.Add(...); TODO
        });
    });
    */

    var app = builder.Build();

    var elasticLogger = app.Services.GetRequiredService<IElasticsearchLogger>();

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(AppConfig.Configuration)       
        .CreateLogger();

    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UsePathBase(serviceBasePath);
    }

    app.UseSwagger(options =>
    {
        if (!app.Environment.IsDevelopment())
        {
            options.PreSerializeFilters.Add((doc, httpRequest) =>
            {
                doc.Servers = new List<OpenApiServer> {
                    new OpenApiServer {
                        Url = serviceDomain + serviceBasePath
                        }
                        };

            });
        }
    });
    app.UseSwaggerUI();

    // app.UseHttpsRedirection();

    app.UseRouting();

    app.UseCors(policyName);

    app.UseAuthentication();

    app.UseAuthorization();

    app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            // endpoints.MapMetrics().RequireAuthorization("BasicAuthenticationForPrometheus");
        });

    app.Run();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "roadworks services stopped working with error.");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}