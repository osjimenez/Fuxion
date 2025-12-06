using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Fuxion.Resources;

namespace Fuxion;

public static partial class Extensions
{
	#region IsNullOrDefault
	extension<T>([NotNullWhen(false)] T? me)
	{
		public bool IsNullOrDefault() => EqualityComparer<T>.Default.Equals(me!, default!);
	}
	extension<T>([NotNullWhen(false)] T? me) where T : struct
	{
		public bool IsNullOrDefault() => me == null || EqualityComparer<T>.Default.Equals(me.Value, default!);
	}
	#endregion

}