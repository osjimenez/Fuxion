using System;

namespace Fuxion;

public static class ExceptionExtensions
{
	extension(Exception me)
	{
		public Exception GetDeeperInnerException()
		{
			var inner = me;
			while (inner.InnerException is not null) inner = inner.InnerException;
			return inner;
		}
	}
}