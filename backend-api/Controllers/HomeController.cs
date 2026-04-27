using Microsoft.AspNetCore.Mvc;
using WeatherAPI.DTOs;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("/")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(ApiResponse.SuccessResponse(
            "Welcome to the Weather API! Use the /weatherforecast endpoint to get the weather forecast."));
    }
}
