namespace Fuxion;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

public static class FuxionExtensions
{
	extension<T>(T me)
	{
		public FuxionExtensions<T?> Fx
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me);
		}
	}
}

/// <summary>
/// Extension container for Fuxion functionality.
/// </summary>
public class FuxionExtensions<T>(T me) : Extensions<T>(me);

public abstract class Extensions<T>(T me)
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public T Value
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => me;
	}
}
