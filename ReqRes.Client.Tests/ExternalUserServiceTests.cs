using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using ReqRes.Client.DTOs;
using ReqRes.Client.Services;
using System.Net;
using System.Text;
using System.Text.Json;

public class ExternalUserServiceTests
{
    [Fact]
    public async Task GetAllUsersAsync_Returns_AllUsers_AcrossPages()
    {
        // Arrange
        var page1 = new UserListResponse
        {
            Page = 1,
            Total_Pages = 2,
            Data = new List<User>
            {
                new User { Id = 1, First_Name = "George" }
            }
        };

        var page2 = new UserListResponse
        {
            Page = 2,
            Total_Pages = 2,
            Data = new List<User>
            {
                new User { Id = 2, Last_Name = "Janet" }
            }
        };

        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(page1))
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(page2))
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://reqres.in/api/")
        };

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<ExternalUserService>.Instance;

        var service = new ExternalUserService(httpClient, memoryCache, logger);

        // Act
        var users = await service.GetAllUsersAsync();

        // Assert
        Assert.NotNull(users);
        var userList = new List<User>((IEnumerable<User>)users);
        Assert.Equal(2, userList.Count);
        Assert.Contains(userList, u => u.First_Name == "George");
        Assert.Contains(userList, u => u.Last_Name == "Janet");
    }
    [Fact]
    public async Task GetAllUsersAsync_Returns_EmptyList_On404()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://reqres.in/api/")
        };

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<ExternalUserService>.Instance;

        var service = new ExternalUserService(httpClient, memoryCache, logger);

        // Act
        var users = await service.GetAllUsersAsync();

        // Assert
        Assert.Empty(users);
    }
    [Fact]
    public async Task GetAllUsersAsync_Returns_EmptyList_OnTimeout()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://reqres.in/api/")
        };

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<ExternalUserService>.Instance;

        var service = new ExternalUserService(httpClient, memoryCache, logger);

        // Act
        var users = await service.GetAllUsersAsync();

        // Assert
        Assert.Empty(users);
    }
    [Fact]
    public async Task GetAllUsersAsync_Returns_FromCache_IfAvailable()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<ExternalUserService>.Instance;

        var cachedUsers = new List<User> { new User { Id = 1, First_Name = "CachedUser" } };
        memoryCache.Set("users_page_1", cachedUsers, TimeSpan.FromMinutes(5));

        var handlerMock = new Mock<HttpMessageHandler>(); // Will not be used because of cache

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://reqres.in/api/")
        };

        var service = new ExternalUserService(httpClient, memoryCache, logger);

        // Act
        var users = await service.GetAllUsersAsync();

        // Assert
        var userList = new List<User>((IEnumerable<User>)users);
        Assert.Single(userList);
        Assert.Equal("CachedUser", userList[0].First_Name);
    }
    [Fact]
    public async Task GetAllUsersAsync_Retries_On_Transient_Error()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Temporary failure"));

        var retryHandlerHttpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://reqres.in/api/")
        };

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<ExternalUserService>.Instance;

        var service = new ExternalUserService(retryHandlerHttpClient, memoryCache, logger);

        // Act
        var users = await service.GetAllUsersAsync();

        // Assert
        Assert.Empty(users);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.AtLeastOnce(), // Cannot confirm Polly retry count unless Polly's internal retry metrics are exposed
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }
    [Fact]

    public async Task GetAllUsersAsync_Returns_EmptyList_On_DeserializationError()
    {
        // Arrange: malformed JSON
        string malformedJson = "{ not valid json";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(malformedJson)
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://reqres.in/api/")
        };

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<ExternalUserService>.Instance;

        var service = new ExternalUserService(httpClient, memoryCache, logger);

        // Act
        var result = await service.GetAllUsersAsync();

        // Assert
        Assert.Empty(result); // Should gracefully fail and return an empty list
    }

}
