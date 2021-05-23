﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fuxion.Shell
{
	public interface IModule
	{
		void PreRegister(IServiceCollection services) { }
		void Register(IServiceCollection services);
		void Initialize(IServiceProvider serviceProvider) { }
	}
}
