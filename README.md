# ReqResDemo

## Overview

ReqResDemo is an ASP.NET Core Web API project that demonstrates consuming the ReqRes API using best practices such as dependency injection, configuration via `appsettings.json`, and resilient HTTP client usage.

---

## Build & Run Instructions

### Prerequisites

- [.NET 6 SDK or later](https://dotnet.microsoft.com/download)
- Visual Studio 2022+ or Visual Studio Code

### Steps

1. **Clone the Repository**
    ```sh
    git clone <your-repo-url>
    cd ReqResDemo
    ```

2. **Restore Dependencies**
    ```sh
    dotnet restore
    ```

3. **Build the Solution**
    ```sh
    dotnet build
    ```

4. **Run the Application**
    ```sh
    dotnet run --project ReqResWebApi
    ```
    The API will be available at `https://localhost:5001` or `http://localhost:5000`.

---

## Testing

1. **Run Unit Tests**
    ```sh
    dotnet test
    ```
    This will execute all tests in the solution and display the results.

---

## Configuration

- All configuration is managed via `appsettings.json`.  
- Example:
    ```json
    {
      "ReqResApi": {
        "BaseUrl": "https://reqres.in/api/",
        "TimeoutSeconds": 30
      },
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      },
      "AllowedHosts": "*"
    }
    ```

---

## Design Decisions

- **Dependency Injection:**  
  All services (including `IExternalUserService`) are registered using ASP.NET Core's built-in DI for testability and maintainability.

- **HttpClientFactory:**  
  `AddHttpClient` is used for `ExternalUserService` to manage HTTP connections efficiently and support policies like retries.

- **Configuration Binding:**  
  Strongly-typed options (`ReqResOptions`) are used for API settings, bound from `appsettings.json`.

- **Resilience:**  
  Polly is used for transient HTTP error handling with retry policies.

- **Caching:**  
  `IMemoryCache` is injected for optional in-memory caching of API responses.

---

## Troubleshooting

- **Invalid URI:**  
  Ensure `ReqResApi:BaseUrl` is set in `appsettings.json`.

- **Dependency Injection Errors:**  
  Confirm all services are registered in `Startup.cs` or `Program.cs`.

---

## Project Structure

- `ReqResWebApi/Controllers/` - API controllers
- `ReqRes.Client/` - Service interfaces, implementations, and options
- `appsettings.json` - Configuration file
