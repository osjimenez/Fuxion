﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fuxion.Synchronization
{
    internal interface IPropertyRunner
    {
        string PropertyName { get; }
        object MasterValue { get; }
        object SideValue { get; }
        Func<object, string> MasterNamingFunction { get; }
        Func<object, string> SideNamingFunction { get; }

        IPropertyRunner Invert();
    }
}
