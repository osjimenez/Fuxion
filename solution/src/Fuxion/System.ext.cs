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

/// <summary>
/// Provides general-purpose extension methods for common types in the System namespace.
/// </summary>
/// <remarks>
/// This class contains extension methods that enhance standard .NET types with additional functionality.
/// Current extensions include null/default checking for generic types and nullable value types.
/// </remarks>
public static partial class Extensions
{
	#region IsNullOrDefault
	/// <summary>
	/// Extension methods for generic nullable types to check for null or default values.
	/// </summary>
	extension<T>([NotNullWhen(false)] T? me)
	{
		/// <summary>
		/// Determines whether the value is <c>null</c> or equal to the default value for type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam>The type of the value to check.</typeparam>
		/// <returns>
		/// <c>true</c> if the value is <c>null</c> or equals <c>default(T)</c>; otherwise, <c>false</c>.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This method uses <see cref="EqualityComparer{T}.Default"/> to compare the value with the default
		/// for its type. For reference types, default is <c>null</c>. For value types, default is typically
		/// zero, false, or a struct with all fields set to their defaults.
		/// </para>
		/// <para>
		/// The <see cref="NotNullWhenAttribute"/> ensures that when this method returns <c>false</c>,
		/// the compiler knows the value is not <c>null</c>.
		/// </para>
		/// <para>
		/// <strong>Common default values:</strong>
		/// </para>
		/// <list type="bullet">
		/// <item><description>Reference types: <c>null</c></description></item>
		/// <item><description>int, long, etc.: <c>0</c></description></item>
		/// <item><description>bool: <c>false</c></description></item>
		/// <item><description>DateTime: <c>0001-01-01 00:00:00</c></description></item>
		/// <item><description>Guid: <c>00000000-0000-0000-0000-000000000000</c></description></item>
		/// <item><description>Nullable types (int?, bool?, etc.): <c>null</c></description></item>
		/// </list>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Reference types
		/// string? text = null;
		/// text.IsNullOrDefault()          // returns true (null)
		/// 
		/// text = "hello";
		/// text.IsNullOrDefault()          // returns false
		/// 
		/// // Value types with default values
		/// int number = 0;
		/// number.IsNullOrDefault()        // returns true (0 is default for int)
		/// 
		/// number = 42;
		/// number.IsNullOrDefault()        // returns false
		/// 
		/// bool flag = false;
		/// flag.IsNullOrDefault()          // returns true (false is default for bool)
		/// 
		/// flag = true;
		/// flag.IsNullOrDefault()          // returns false
		/// 
		/// // DateTime
		/// DateTime date = default;
		/// date.IsNullOrDefault()          // returns true (0001-01-01)
		/// 
		/// date = DateTime.Now;
		/// date.IsNullOrDefault()          // returns false
		/// 
		/// // Guid
		/// Guid id = Guid.Empty;
		/// id.IsNullOrDefault()            // returns true (all zeros)
		/// 
		/// id = Guid.NewGuid();
		/// id.IsNullOrDefault()            // returns false
		/// 
		/// // Null-safety with pattern matching
		/// if (!text.IsNullOrDefault())
		/// {
		///     // Compiler knows 'text' is not null here
		///     Console.WriteLine(text.ToUpper());
		/// }
		/// 
		/// // Use case: validation
		/// public Response ValidateUser(User user)
		/// {
		///     if (user.IsNullOrDefault())
		///         return Response.Get.InvalidData("User cannot be null");
		///     
		///     if (user.Id.IsNullOrDefault())
		///         return Response.Get.InvalidData("User ID is required");
		///     
		///     return Response.Get.Success();
		/// }
		/// </code>
		/// </example>
		public bool IsNullOrDefault() => EqualityComparer<T>.Default.Equals(me!, default!);
	}
	
	/// <summary>
	/// Extension methods for nullable value types to check for null or default values.
	/// </summary>
	extension<T>([NotNullWhen(false)] T? me) where T : struct
	{
		/// <summary>
		/// Determines whether the nullable value type is <c>null</c> or equal to the default value for type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam>The underlying value type (must be a struct).</typeparam>
		/// <returns>
		/// <c>true</c> if the value is <c>null</c> or its <see cref="Nullable{T}.Value"/> equals <c>default(T)</c>;
		/// otherwise, <c>false</c>.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This overload is specifically for nullable value types (<c>int?</c>, <c>bool?</c>, <c>DateTime?</c>, etc.).
		/// It checks both for <c>null</c> and for the default value of the underlying type.
		/// </para>
		/// <para>
		/// The <see cref="NotNullWhenAttribute"/> ensures that when this method returns <c>false</c>,
		/// the compiler knows the value is not <c>null</c> and <see cref="Nullable{T}.Value"/> can be safely accessed.
		/// </para>
		/// <para>
		/// <strong>Why this overload exists:</strong> Without this overload, nullable value types would use
		/// the generic version which treats <c>null</c> as the default. This overload provides explicit handling
		/// for the nullable wrapper, checking both the nullable state and the underlying value.
		/// </para>
		/// <para>
		/// <strong>Comparison with HasValue:</strong>
		/// </para>
		/// <list type="bullet">
		/// <item><description><c>HasValue</c>: Only checks if not null</description></item>
		/// <item><description><c>IsNullOrDefault()</c>: Checks if null OR if the value is the type's default</description></item>
		/// </list>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Nullable int
		/// int? number = null;
		/// number.IsNullOrDefault()        // returns true (null)
		/// 
		/// number = 0;
		/// number.IsNullOrDefault()        // returns true (0 is default for int)
		/// number.HasValue                 // returns true (different from IsNullOrDefault!)
		/// 
		/// number = 42;
		/// number.IsNullOrDefault()        // returns false
		/// 
		/// // Nullable bool
		/// bool? flag = null;
		/// flag.IsNullOrDefault()          // returns true (null)
		/// 
		/// flag = false;
		/// flag.IsNullOrDefault()          // returns true (false is default for bool)
		/// 
		/// flag = true;
		/// flag.IsNullOrDefault()          // returns false
		/// 
		/// // Nullable DateTime
		/// DateTime? date = null;
		/// date.IsNullOrDefault()          // returns true (null)
		/// 
		/// date = default(DateTime);       // 0001-01-01
		/// date.IsNullOrDefault()          // returns true (default DateTime)
		/// 
		/// date = DateTime.Now;
		/// date.IsNullOrDefault()          // returns false
		/// 
		/// // Nullable Guid
		/// Guid? id = null;
		/// id.IsNullOrDefault()            // returns true (null)
		/// 
		/// id = Guid.Empty;
		/// id.IsNullOrDefault()            // returns true (empty GUID)
		/// 
		/// id = Guid.NewGuid();
		/// id.IsNullOrDefault()            // returns false
		/// 
		/// // Null-safety with pattern matching
		/// if (!number.IsNullOrDefault())
		/// {
		///     // Compiler knows 'number' is not null here
		///     Console.WriteLine($"Valid number: {number.Value}");
		/// }
		/// 
		/// // Use case: distinguishing unset vs. default values
		/// public class Settings
		/// {
		///     public int? MaxRetries { get; set; }
		///     
		///     public int GetEffectiveRetries()
		///     {
		///         // Distinguish between:
		///         // - Not set (null) ? use system default (3)
		///         // - Set to 0 (explicit disable) ? use 0
		///         // - Set to positive value ? use that value
		///         
		///         if (MaxRetries == null)
		///             return 3;  // System default
		///         
		///         return MaxRetries.Value;  // Use explicit value (even if 0)
		///     }
		///     
		///     public bool IsRetryConfigured()
		///     {
		///         // Using IsNullOrDefault treats null and 0 the same
		///         return !MaxRetries.IsNullOrDefault();
		///     }
		/// }
		/// 
		/// // Comparison: HasValue vs IsNullOrDefault
		/// int? age = 0;
		/// 
		/// if (age.HasValue)
		///     Console.WriteLine("Age was provided");  // This executes (value is 0)
		/// 
		/// if (!age.IsNullOrDefault())
		///     Console.WriteLine("Age is meaningful");  // This does NOT execute (0 is default)
		/// </code>
		/// </example>
		public bool IsNullOrDefault() => me == null || EqualityComparer<T>.Default.Equals(me.Value, default!);
	}
	#endregion

}