﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fuxion.Web
{
    public interface IWebApiProxy
    {
        IWebApiProxyProvider Provider { get; }
    }
}
