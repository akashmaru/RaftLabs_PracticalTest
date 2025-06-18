using Microsoft.AspNetCore.Mvc;
using ReqRes.Client.DTOs;
using ReqRes.Client.Interface;


namespace ReqResWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IExternalUserService _apiService;

    public UsersController(IExternalUserService apiService)
    {
        _apiService = apiService;
    }

    // GET api/users/1
    [HttpGet("AllUsers")]
    public async Task<ActionResult<UserListResponse>> Get()
    {
        var users = await _apiService.GetAllUsersAsync();
        return Ok(users);
    }
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
