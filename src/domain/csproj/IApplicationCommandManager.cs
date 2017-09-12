﻿using Fuxion.Domain.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fuxion.Domain
{
    public interface IApplicationCommandManager
    {
        Task DoAsync<TCommand>(TCommand command) where TCommand : ICommand;
    }
}
