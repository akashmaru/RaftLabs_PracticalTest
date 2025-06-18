using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using ReqRes.Client.DTOs;
using ReqRes.Client.Interface;
using ReqRes.Client.Services;

/// <summary>
/// Configures the application's services and request pipeline.
/// </summary>
/// <remarks>The <see cref="Startup"/> class is responsible for configuring the application's dependency injection
/// container, middleware pipeline, and other application-level settings. It is used by the ASP.NET Core runtime during
/// application startup.</remarks>
public class Startup
{
    /// <summary>
    /// Gets the application's configuration settings.
    /// </summary>
    public IConfiguration Configuration { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Startup"/> class with the specified configuration.
    /// </summary>
    /// <param name="configuration">The application configuration settings.</param>
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    /// <summary>
    /// Configures the application's services and dependencies.
    /// </summary>
    /// <remarks>This method registers controllers, configures options for the ReqRes API, sets up an HTTP
    /// client for external user services with retry policies, and adds memory caching and Swagger generation.</remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to which services are added.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.Configure<ReqResOptions>(
            Configuration.GetSection("ReqResApi"));

        services.AddHttpClient<IExternalUserService, ExternalUserService>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<ReqResOptions>>().Value;

                if (string.IsNullOrWhiteSpace(options.BaseUrl))
                {
                    throw new InvalidOperationException("BaseUrl not configured!");
                }
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                client.DefaultRequestHeaders.Add("x-api-key", options.ApiKey);

            })
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(2)));

        services.AddMemoryCache();
        services.AddSwaggerGen();
    }

    /// <summary>
    /// Configures the application's request pipeline.
    /// </summary>
    /// <remarks>This method sets up middleware components for handling requests and responses in the
    /// application. It configures the developer exception page in the development environment, routing, authorization, 
    /// endpoint mapping for controllers, and Swagger for API documentation.</remarks>
    /// <param name="app">An <see cref="IApplicationBuilder"/> instance used to configure the application's middleware.</param>
    /// <param name="env">An <see cref="IWebHostEnvironment"/> instance that provides information about the web hosting environment.</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
        
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "ReqRes API V1");
        });
    }
}