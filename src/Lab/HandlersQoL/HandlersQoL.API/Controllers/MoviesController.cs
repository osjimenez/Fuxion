using Microsoft.AspNetCore.Mvc;

namespace Handlers_QoL.API.Controllers;

[ApiController]
[Route("[controller]")]
public class MoviesController : ControllerBase
{
	[HttpGet] 
	public IActionResult Get()
	{
		return Ok();
	} 
}