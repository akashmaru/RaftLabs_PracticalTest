using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ReqRes.Client.DTOs;
using ReqRes.Client.Interface;
using System.Net.Http.Json;
using System.Text.Json;

namespace ReqRes.Client.Services
{
    public class ExternalUserService : IExternalUserService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ExternalUserService> _logger;
        public ExternalUserService(HttpClient httpClient, IMemoryCache cache, ILogger<ExternalUserService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
        }

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
