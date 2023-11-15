using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApi.Models;

namespace WebApi.Controllers;

[Route("api/v1/users")]
public class UserController: Controller
{
    private readonly ILogger<UserController> _logger;

    public UserController(ILogger<UserController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("index");

        return Ok("Hello World!");
    }


    [HttpPost("/create")]
    public async Task<IActionResult> CreateUser([FromBody]UserDto dto, CancellationToken token)
    {
        return Ok(
        new {
            User = dto,
            CorrelationId = Guid.NewGuid().ToString("N")
        });
    }
    
    [HttpPost("/error")]
    public async Task<IActionResult> Error([FromBody]UserDto dto, CancellationToken token)
    {
        throw new ArgumentNullException("error-from-controller");
    }
}