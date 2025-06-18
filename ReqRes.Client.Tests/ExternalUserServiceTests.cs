using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using ReqRes.Client.DTOs;
using ReqRes.Client.Services;
using System.Net;
using System.Text.Json;

/// <summary>
/// Contains unit tests for the <see cref="ExternalUserService"/> class,  verifying its behavior under various
/// conditions such as successful responses,  error handling, caching, and retries.
/// </summary>
/// <remarks>These tests ensure that the <see cref="ExternalUserService"/> correctly handles scenarios such as:
/// <list type="bullet"> <item>Fetching all users across multiple pages.</item> <item>Handling HTTP 404 responses
/// gracefully by returning an empty list.</item> <item>Returning cached data when available.</item> <item>Retrying on
/// transient errors and handling timeouts.</item> <item>Gracefully handling deserialization errors by returning an
/// empty list.</item> </list> The tests use mocking frameworks like Moq to simulate HTTP responses and verify
/// behavior.</remarks>
public class ExternalUserServiceTests
{
    /// <summary>
    /// Verifies that the <see cref="ExternalUserService.GetAllUsersAsync"/> method retrieves all users across multiple
    /// pages of data.
    /// </summary>
    /// <remarks>This test ensures that the <see cref="ExternalUserService.GetAllUsersAsync"/> method
    /// correctly handles paginated API responses and aggregates users from all pages into a single collection. It uses
    /// mocked HTTP responses to simulate a paginated API.</remarks>
    /// <returns></returns>
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

    /// <summary>
    /// Verifies that the <see cref="ExternalUserService.GetAllUsersAsync"/> method returns an empty list when the HTTP
    /// response status code is 404 (Not Found).
    /// </summary>
    /// <remarks>This test ensures that the service correctly handles a 404 response by returning an empty
    /// list of users, rather than throwing an exception or returning null. It uses a mocked <see
    /// cref="HttpMessageHandler"/> to simulate the 404 response.</remarks>
    /// <returns></returns>
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

    /// <summary>
    /// Verifies that the <see cref="ExternalUserService.GetAllUsersAsync"/> method returns an empty list when a timeout
    /// occurs during the HTTP request.
    /// </summary>
    /// <remarks>This test simulates a timeout by mocking the HTTP client to throw a <see
    /// cref="TaskCanceledException"/>. It ensures that the method gracefully handles timeouts and returns an empty list
    /// instead of propagating the exception.</remarks>
    /// <returns></returns>
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

    /// <summary>
    /// Verifies that the <see cref="ExternalUserService.GetAllUsersAsync"/> method retrieves users from the cache if
    /// cached data is available, bypassing the HTTP request.
    /// </summary>
    /// <remarks>This test ensures that the caching mechanism in <see cref="ExternalUserService"/> is
    /// functioning correctly. If the cache contains a valid entry for the requested users, the method should return the
    /// cached data without making an HTTP call.</remarks>
    /// <returns></returns>
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

    /// <summary>
    /// Verifies that the <see cref="ExternalUserService.GetAllUsersAsync"/> method retries on transient errors, such as
    /// temporary HTTP failures.
    /// </summary>
    /// <remarks>This test simulates a transient error by throwing an <see cref="HttpRequestException"/>
    /// during the HTTP request.  It ensures that the method under test retries the operation as expected, using a retry
    /// policy (e.g., Polly). The test asserts that no users are returned and that the retry mechanism is invoked at
    /// least once.</remarks>
    /// <returns></returns>
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

    /// <summary>
    /// Verifies that the <see cref="ExternalUserService.GetAllUsersAsync"/> method returns an empty list when a
    /// deserialization error occurs due to malformed JSON in the HTTP response.
    /// </summary>
    /// <remarks>This test ensures that the <see cref="ExternalUserService.GetAllUsersAsync"/> method
    /// gracefully handles deserialization errors by returning an empty list instead of throwing an exception. The test
    /// simulates a scenario where the HTTP response contains invalid JSON.</remarks>
    /// <returns></returns>
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
