using System.Security.Claims;
using DealW.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DealW.API.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController(IKeycloakAuthorizeApiClient keycloakAuthorizeApiClient) : ControllerBase
{
    // [HttpGet]
    // [Authorize]
    // public Dictionary<string, string> Get(ClaimsPrincipal claimsPrincipal)
    // {
    //     return claimsPrincipal.Claims.ToDictionary(c => c.Type, c => c.Value);
    // }
    
    [HttpGet("all")]
    [Authorize]
    public async Task<IActionResult> GetUsers([FromQuery] int take = 10, [FromQuery] int skip = 0)
    {
        var users = await keycloakAuthorizeApiClient.GetUsers(take, skip); 
        return Ok(users);
    }
}