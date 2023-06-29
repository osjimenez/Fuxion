using Fuxion.Domain;
using Handlers_QoL.API.Handlers.Movies;
using Microsoft.AspNetCore.Mvc;

namespace Handlers_QoL.API.Controllers;

[ApiController]
[Route("[controller]")]
public class MoviesController : ControllerBase
{
	public MoviesController(INexus nexus) => _nexus = nexus;
	readonly INexus _nexus;
	[HttpGet]
	public IActionResult Get()
	{
		_nexus.Publish(new GetMovieListQuery());
		return Ok();
	}
}