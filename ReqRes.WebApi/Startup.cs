using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using ReqRes.Client.DTOs;
using ReqRes.Client.Interface;
using ReqRes.Client.Services;

public class Startup
{
    // This makes `Configuration` available throughout the class
    public IConfiguration Configuration { get; }

    // This constructor is automatically called by the framework
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }
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