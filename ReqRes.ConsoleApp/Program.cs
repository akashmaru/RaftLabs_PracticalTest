using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using ReqRes.Client.DTOs;
using ReqRes.Client.Interface;
using ReqRes.Client.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        services.AddMemoryCache();

        services.AddHttpClient<IExternalUserService, ExternalUserService>(client =>
        {
            client.BaseAddress = new Uri(config["ReqResApi:BaseUrl"]);
            var apiKey = config["ReqResApi:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                // For x-api-key header
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);

                // OR if it's a Bearer token
                // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }
        })
        .AddPolicyHandler(HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(2)));
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build();

// Get service and run
var service = host.Services.GetRequiredService<IExternalUserService>();
var users = await service.GetAllUsersAsync();

if (users == null)
{
    Console.WriteLine("No users found.");
}
else
{
    foreach (var user in users)
    {
        Console.WriteLine($"{user.Id}: {user.First_Name}-{user.Last_Name}-{user.Email}");
    }
}

