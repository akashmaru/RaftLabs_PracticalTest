using Microsoft.AspNetCore.Mvc;
using ReqRes.Client.DTOs;
using ReqRes.Client.Interface;


namespace ReqResWebApi.Controllers;

/// <summary>
/// Provides API endpoints for managing and retrieving user data.
/// </summary>
/// <remarks>This controller handles requests related to user information, including retrieving all users and
/// fetching details for a specific user. It relies on an external user service to perform the underlying
/// operations.</remarks>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    #region Private member declaration
    /// <summary>
    /// Represents a service used to interact with an external user API.
    /// </summary>
    /// <remarks>This field is intended to store a reference to an implementation of <see
    /// cref="IExternalUserService"/>  for performing operations related to external user data. It is read-only and
    /// initialized through  dependency injection or during object construction.</remarks>
    private readonly IExternalUserService _apiService; 
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="UsersController"/> class.
    /// </summary>
    /// <param name="apiService">The external user service used to interact with user-related data.</param>
    public UsersController(IExternalUserService apiService)
    {
        _apiService = apiService;
    }

    /// <summary>
    /// GET api/users/1
    /// Retrieves a list of all users.
    /// </summary>
    /// <remarks>This method sends an HTTP GET request to the "AllUsers" endpoint and asynchronously 
    /// retrieves the user data. The response is wrapped in an HTTP 200 OK status if successful.</remarks>
    /// <returns>An <see cref="ActionResult{T}"/> containing a <see cref="UserListResponse"/> object  with the details of all
    /// users. Returns an empty list if no users are found.</returns>
    [HttpGet("AllUsers")]
    public async Task<ActionResult<UserListResponse>> Get()
    {
        var users = await _apiService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Retrieves the details of a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to retrieve.</param>
    /// <returns>An <see cref="ActionResult{T}"/> containing the user details if found;  otherwise, a <see
    /// cref="NotFoundResult"/> with an error message.</returns>
    [HttpGet("details/{userId}")]
    public async Task<ActionResult<User>> GetUser(int userId)
    {
        try
        {
            var user = await _apiService.GetUserByIdAsync(userId);
            return Ok(user);
        }
        catch (HttpRequestException ex)
        {
            return NotFound(ex.Message);
        }
    }
}