using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ReqRes.Client.DTOs;
using ReqRes.Client.Interface;
using System.Net.Http.Json;
using System.Text.Json;

namespace ReqRes.Client.Services
{
    /// <summary>
    /// Provides methods for interacting with an external user service, including retrieving user data and caching
    /// results to improve performance.
    /// </summary>
    /// <remarks>This service communicates with an external API to fetch user information. It uses an <see
    /// cref="HttpClient"/> for making HTTP requests, an <see cref="IMemoryCache"/> for caching responses, and an <see
    /// cref="ILogger{T}"/> for logging operations. The service handles paginated user data and individual user lookups,
    /// with built-in error handling for common issues such as timeouts, deserialization errors, and HTTP
    /// failures.</remarks>
    public class ExternalUserService : IExternalUserService
    {
        #region Private member declaration
        /// <summary>
        /// Represents the <see cref="HttpClient"/> instance used to send HTTP requests and receive HTTP responses.
        /// </summary>
        /// <remarks>This field is intended for internal use only and is used to manage HTTP communication
        /// within the class. It is initialized and configured internally and should not be accessed or modified
        /// directly.</remarks>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Represents a memory cache used for storing and retrieving data in memory.
        /// </summary>
        /// <remarks>This field is a readonly instance of <see cref="IMemoryCache"/>, which provides
        /// methods for  caching data in memory. It is intended for internal use and cannot be modified after
        /// initialization.</remarks>
        private readonly IMemoryCache _cache;

        /// <summary>
        /// Provides logging functionality for the <see cref="ExternalUserService"/> class.
        /// </summary>
        /// <remarks>This logger is used to record diagnostic and operational information related to the 
        /// <see cref="ExternalUserService"/>. It is intended for internal use only and is not exposed  to external
        /// consumers of the class.</remarks>
        private readonly ILogger<ExternalUserService> _logger; 
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalUserService"/> class.
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance used to make HTTP requests to external user services. Cannot be null.</param>
        /// <param name="cache">The <see cref="IMemoryCache"/> instance used to cache user data for improved performance. Cannot be null.</param>
        /// <param name="logger">The <see cref="ILogger{TCategoryName}"/> instance used for logging diagnostic and error information. Cannot
        /// be null.</param>
        public ExternalUserService(HttpClient httpClient, IMemoryCache cache, ILogger<ExternalUserService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Asynchronously retrieves all users from the remote service, paginated if necessary.
        /// </summary>
        /// <remarks>This method fetches user data from a remote service, handling pagination
        /// automatically.  It caches results for individual pages to improve performance on subsequent requests.  If an
        /// error occurs during the request (e.g., network issues or deserialization errors),  an empty collection is
        /// returned, and the error is logged.</remarks>
        /// <returns>A task that represents the asynchronous operation. The task result contains an  <IEnumerable{T}> of <User>
        /// objects representing all users retrieved  from the remote service. If no users are found or an error occurs,
        /// the result is an empty collection.</returns>
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            var allUsers = new List<User>();
            int currentPage = 1;
            int totalPages = 1; // default to enter loop
            //int currentPage = 9999; // something high to force 404
            try
            {
                do
                {
                    string cacheKey = $"users_page_{currentPage}";
                    List<User> users;

                    if (_cache.TryGetValue(cacheKey, out users))
                    {
                        _logger.LogInformation("Cache hit for page {Page}", currentPage);
                    }
                    else
                    {
                        var response = await _httpClient.GetAsync($"users?page={currentPage}");

                        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            _logger.LogWarning("User page {Page} not found.", currentPage);
                            return new List<User>();
                        }

                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogWarning("Failed to get users page {Page}. StatusCode: {StatusCode}", currentPage, response.StatusCode);
                            return new List<User>(); 
                        }

                        UserListResponse userList;
                        try
                        {
                            userList = await response.Content.ReadFromJsonAsync<UserListResponse>();
                        }
                        catch (NotSupportedException ex)
                        {
                            _logger.LogError(ex, "Unsupported content type when deserializing users page {Page}", currentPage);
                            return new List<User>(); 
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError(ex, "JSON deserialization error on page {Page}", currentPage);
                            return new List<User>();
                        }

                        users = userList?.Data ?? new List<User>();

                        _cache.Set(cacheKey, users, TimeSpan.FromMinutes(5));

                        // Update total pages based on response
                        totalPages = userList?.Total_Pages ?? 1;
                    }

                    // If no users are found on the current page, exit the loop
                    if (users.Count == 0)
                    {
                        _logger.LogInformation("No users found on page {Page}", currentPage);
                        break; // Exit loop if no users found
                    }

                    _logger.LogInformation("Fetched {Count} users from page {Page}", users.Count, currentPage);
                    allUsers.AddRange(users);
                    currentPage++;

                } while (currentPage <= totalPages);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error when fetching users");
                return new List<User>();
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request timeout when fetching users");
                return new List<User>();
            }
            return allUsers;
        }

        /// <summary>
        /// Retrieves a user by their unique identifier asynchronously.
        /// </summary>
        /// <remarks>This method attempts to retrieve the user from a cache first. If the user is not
        /// found in the cache,  it makes an HTTP request to fetch the user data. The user data is cached for subsequent
        /// requests if  successfully retrieved.  <para> If the user does not exist, a <see
        /// cref="UserNotFoundException"/> is thrown. If the HTTP request fails  or the response cannot be deserialized,
        /// the method logs the error and returns <see langword="null"/>. </para></remarks>
        /// <param name="userId">The unique identifier of the user to retrieve.</param>
        /// <returns>A <see cref="User"/> object representing the user if found; otherwise, <see langword="null"/>.</returns>
        /// <exception cref="UserNotFoundException">Thrown if the user with the specified <paramref name="userId"/> does not exist.</exception>
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            string cacheKey = $"user_{userId}";

            if (_cache.TryGetValue(cacheKey, out User cachedUser))
            {
                return cachedUser;
            }

            try
            {
                var response = await _httpClient.GetAsync($"users/{userId}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("User does not exis {UserId}. StatusCode: {StatusCode}", userId, response.StatusCode);
                    // User does not exist
                    throw new UserNotFoundException(userId); // or throw custom NotFoundException if you prefer
                }
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get user {UserId}. StatusCode: {StatusCode}", userId, response.StatusCode);
                    return null;
                }

                try
                {
                    var userResponse = await response.Content.ReadFromJsonAsync<UserResponse>();
                    var user = userResponse?.Data;

                    if (user != null)
                    {
                        _cache.Set(cacheKey, user, TimeSpan.FromMinutes(5));
                    }

                    return user;
                }
                catch (NotSupportedException ex)
                {
                    _logger.LogError(ex, "Unsupported content type when deserializing user {UserId}", userId);
                    return null;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "JSON deserialization error when reading user {UserId}", userId);
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error when fetching user {UserId}", userId);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request timeout when fetching user {UserId}", userId);
                return null;
            }
        }
    }
}