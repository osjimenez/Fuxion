﻿using Fuxion.Reflection;
using Fuxion.Application.Commands;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fuxion.AspNetCore.Controllers
{
	[Route("api/[controller]")]
	public class CommandController : ControllerBase
	{
		public CommandController(ICommandDispatcher commandDispatcher, TypeKeyDirectory typeKeyDirectory)
		{
			this.commandDispatcher = commandDispatcher;
			this.typeKeyDirectory = typeKeyDirectory;
		}

		readonly ICommandDispatcher commandDispatcher;
		readonly TypeKeyDirectory typeKeyDirectory;
		[HttpPost]
		public async Task<IActionResult> Post([FromBody] CommandPod pod)
		{
			if (!typeKeyDirectory.ContainsKey(pod.PayloadKey)) return BadRequest($"Command '{pod.PayloadKey}' is not expected");
			var com = pod.WithTypeKeyDirectory(typeKeyDirectory);
			if (com == null) throw new InvalidCastException($"Command with key '{pod.PayloadKey}' is not registered in '{nameof(TypeKeyDirectory)}'");
			await commandDispatcher.DispatchAsync(com);
			return Ok();
		}
	}
}
