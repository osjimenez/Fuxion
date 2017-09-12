﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Fuxion.Factories
{
    public interface IFactoryInjector
    {
        object Get(Type type);
		IEnumerable<object> GetMany(Type type);
	}
}