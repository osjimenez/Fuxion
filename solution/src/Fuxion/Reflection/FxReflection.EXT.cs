using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Fuxion.Reflection;

/// <summary>
/// Provides extension methods for reflection operations on types, members, methods, and assemblies.
/// Includes functionality for custom attribute retrieval, method signature generation, type inspection,
/// embedded resource access, and private member manipulation.
/// </summary>
/// <remarks>
/// This class extends various reflection types including <see cref="MemberInfo"/>, <see cref="MethodBase"/>, 
/// <see cref="Type"/>, and <see cref="Assembly"/> to provide convenient helper methods for common reflection tasks.
/// It also provides extensions for any object type through the Fuxion extensions framework to enable
/// accessing private fields and properties for testing and advanced scenarios.
/// <para>
/// Key features include:
/// <list type="bullet">
/// <item><description>Custom attribute retrieval with error handling options</description></item>
/// <item><description>Human-readable method and type signature generation</description></item>
/// <item><description>Type hierarchy inspection and generic type definition matching</description></item>
/// <item><description>Embedded resource loading from assemblies</description></item>
/// <item><description>Private member access (fields and properties) for testing</description></item>
/// <item><description>Special handling for async methods and compiler-generated types</description></item>
/// </list>
/// </para>
/// </remarks>
public static partial class ReflectionExtensions
{
	extension(MemberInfo me)
	{
		/// <summary>
		/// Retrieves a custom attribute of a specified type that is applied to a specified member,
		/// and optionally inspects the ancestors of that member.
		/// </summary>
		/// <typeparam name="TAttribute">The type of attribute to search for.</typeparam>
		/// <param name="inherit">
		/// <c>true</c> to inspect the ancestors of the member; otherwise, <c>false</c>.
		/// </param>
		/// <param name="exceptionIfNotFound">
		/// <c>true</c> to throw <see cref="AttributeNotFoundException"/> if the custom attribute is not found.
		/// </param>
		/// <param name="exceptionIfMoreThanOne">
		/// <c>true</c> to throw <see cref="AttributeMoreThanOneException"/> if the custom attribute is found more than once.
		/// </param>
		/// <returns>
		/// The custom attribute of type <typeparamref name="TAttribute"/> if found; otherwise, <c>null</c>.
		/// </returns>
		/// <exception cref="AttributeNotFoundException">
		/// Thrown when <paramref name="exceptionIfNotFound"/> is <c>true</c> and the attribute is not found.
		/// </exception>
		/// <exception cref="AttributeMoreThanOneException">
		/// Thrown when <paramref name="exceptionIfMoreThanOne"/> is <c>true</c> and more than one attribute is found.
		/// </exception>
		/// <example>
		/// <code>
		/// var method = typeof(MyClass).GetMethod("MyMethod");
		/// var attr = method.GetCustomAttribute&lt;ObsoleteAttribute&gt;();
		/// if (attr != null)
		/// {
		///     Console.WriteLine($"Method is obsolete: {attr.Message}");
		/// }
		/// 
		/// // Throw exception if not found
		/// var requiredAttr = method.GetCustomAttribute&lt;RequiredAttribute&gt;(exceptionIfNotFound: true);
		/// </code>
		/// </example>
		public TAttribute? GetCustomAttribute<TAttribute>(bool inherit = true, [DoesNotReturnIf(true)] bool exceptionIfNotFound = false, bool exceptionIfMoreThanOne = false)
			where TAttribute : Attribute
		{
			var attributes = me.GetCustomAttributes<TAttribute>(inherit).ToList();
			if(exceptionIfNotFound && attributes.Count == 0)
				throw new AttributeNotFoundException(me, typeof(TAttribute));
			if (exceptionIfMoreThanOne && attributes.Count > 1)
				throw new AttributeMoreThanOneException(me, typeof(TAttribute));
			return attributes.FirstOrDefault();
		}

		/// <summary>
		/// Determines whether the specified member has a custom attribute of the specified type.
		/// </summary>
		/// <typeparam name="TAttribute">The type of attribute to search for.</typeparam>
		/// <param name="inherit">
		/// <c>true</c> to inspect the ancestors of the member; otherwise, <c>false</c>.
		/// </param>
		/// <param name="exceptionIfMoreThanOne">
		/// <c>true</c> to throw <see cref="AttributeMoreThanOneException"/> if the custom attribute is found more than once.
		/// </param>
		/// <returns>
		/// <c>true</c> if the member has the specified attribute; otherwise, <c>false</c>.
		/// </returns>
		/// <exception cref="AttributeMoreThanOneException">
		/// Thrown when <paramref name="exceptionIfMoreThanOne"/> is <c>true</c> and more than one attribute is found.
		/// </exception>
		/// <example>
		/// <code>
		/// var method = typeof(MyClass).GetMethod("MyMethod");
		/// if (method.HasCustomAttribute&lt;ObsoleteAttribute&gt;())
		/// {
		///     Console.WriteLine("Method is marked as obsolete");
		/// }
		/// </code>
		/// </example>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool HasCustomAttribute<TAttribute>(bool inherit = true, [DoesNotReturnIf(true)] bool exceptionIfMoreThanOne = true)
			where TAttribute : Attribute
			=> me.GetCustomAttribute<TAttribute>(inherit, false, exceptionIfMoreThanOne) is not null;
	}

	const string AsyncMethodRegexPattern = @"<(?<method>.+?)>d__\d+`*(?<num_args>\d+)*";
#if STANDARD_OR_OLD_FRAMEWORKS
	internal static Regex AsyncMethodRegex() => new(AsyncMethodRegexPattern);
#else
	[GeneratedRegex(AsyncMethodRegexPattern)]
	internal static partial Regex AsyncMethodRegex();
#endif
	extension(MethodBase method)
	{
		/// <summary>
		/// Generates a human-readable signature string for the method with customizable formatting options.
		/// Supports async methods by detecting and resolving compiler-generated state machine types.
		/// </summary>
		/// <param name="includeAccessModifiers">
		/// <c>true</c> to include access modifiers (public, private, protected, internal, static, virtual, abstract); otherwise, <c>false</c>.
		/// </param>
		/// <param name="includeReturn">
		/// <c>true</c> to include the return type (only applies to <see cref="MethodInfo"/>); otherwise, <c>false</c>.
		/// </param>
		/// <param name="includeDeclaringType">
		/// <c>true</c> to prefix the method name with the declaring type; otherwise, <c>false</c>.
		/// </param>
		/// <param name="useFullNames">
		/// <c>true</c> to use fully-qualified type names; otherwise, <c>false</c> to use short names.
		/// </param>
		/// <param name="fullNamesOnlyInMethodName">
		/// <c>true</c> to use full names only in the method name and declaring type, but short names for parameters and return type; otherwise, <c>false</c>.
		/// </param>
		/// <param name="includeParameters">
		/// <c>true</c> to include the parameter list; otherwise, <c>false</c>.
		/// </param>
		/// <param name="includeParametersNames">
		/// <c>true</c> to include parameter names in addition to their types; otherwise, <c>false</c>.
		/// </param>
		/// <param name="parametersFunction">
		/// Optional custom function to generate the parameters string. 
		/// Receives: (useFullNames, includeParametersNames, method, parametersFunctionArguments) and returns the formatted parameters string.
		/// </param>
		/// <param name="parametersFunctionArguments">
		/// Optional arguments to pass to <paramref name="parametersFunction"/>.
		/// </param>
		/// <returns>
		/// A formatted string representing the method signature according to the specified options.
		/// </returns>
		/// <remarks>
		/// This method has special handling for async methods generated by the compiler. It detects the compiler-generated
		/// state machine class pattern and attempts to resolve back to the original async method signature.
		/// </remarks>
		/// <example>
		/// <code>
		/// var method = typeof(MyClass).GetMethod("ProcessDataAsync");
		/// 
		/// // Simple signature
		/// var simple = method.GetSignature();
		/// // Output: "MyClass.ProcessDataAsync(string, int)"
		/// 
		/// // Full signature with modifiers and return type
		/// var full = method.GetSignature(
		///     includeAccessModifiers: true,
		///     includeReturn: true,
		///     includeParametersNames: true);
		/// // Output: "public async Task MyClass.ProcessDataAsync(string data, int timeout)"
		/// 
		/// // Minimal signature without declaring type
		/// var minimal = method.GetSignature(includeDeclaringType: false);
		/// // Output: "ProcessDataAsync(string, int)"
		/// </code>
		/// </example>
		public string GetSignature(bool includeAccessModifiers = false,
			bool includeReturn = false,
			bool includeDeclaringType = true,
			bool useFullNames = false,
			bool fullNamesOnlyInMethodName = false,
			bool includeParameters = true,
			bool includeParametersNames = false,
			// PEND convert in a Delegate, declaring their params with names and documentation
			Func<bool, bool, MethodBase, object?, string>? parametersFunction = null,
			object? parametersFunctionArguments = null)
		{
			var res = new StringBuilder();

			// Detect if method is a generated async method (MoveNext)
			if (method.Name == "MoveNext" && method.DeclaringType?.Name.Contains('<') == true)
			{
				// Try to extract original name between <>
				var match = AsyncMethodRegex()
					.Match(method.DeclaringType.Name);
				if (match.Success)
				{
					var methodName = match.Groups["method"].Value;

					// Extract number of generics arguments
					var methodNumArgs = match.Groups["num_args"].Value;
					if(methodNumArgs == string.Empty) methodNumArgs = "0";
					var numArgs = int.Parse(methodNumArgs);

					// Get the methods with the same name and number of generic arguments
					var methods = method.DeclaringType?.DeclaringType?.GetMethods()
						.Where(m => m.Name == methodName)
						.Where(m => m.GetGenericArguments().Length == numArgs);

					// Attempt to match the method by parameters
					var stateMachineFields = method.DeclaringType?
						.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
						.Where(f => !f.Name.StartsWith('<'))
						.ToList() ?? [];

					method = methods?.FirstOrDefault(m =>
					{
						var currentParams = m.GetParameters();
						// Check if all parameters of the original method are represented as fields in the state machine
						return currentParams.Length == stateMachineFields.Count
						       && currentParams.All(p => 
							       stateMachineFields.Any(f => f.Name == p.Name && f.FieldType == p.ParameterType));
					}) ?? method;
				}
			}

			// Access modifiers
			if (includeAccessModifiers)
			{
				if (method.IsPublic)
					res.Append("public ");
				else if (method.IsPrivate)
					res.Append("private ");
				else if (method.IsAssembly) res.Append("internal ");
				if (method.IsFamily) res.Append("protected ");
				if (method.IsStatic) res.Append("static ");
				if (method.IsVirtual) res.Append("virtual ");
				if (method.IsAbstract) res.Append("abstract ");
			}
			// Return type
			if (includeReturn && method is MethodInfo mi)
			{
				res.Append(mi.ReturnType.GetSignature(useFullNames && !fullNamesOnlyInMethodName) + " ");
			}

			// Method name
			if (includeDeclaringType) res.Append(method.DeclaringType?.GetSignature(useFullNames) + ".");
			res.Append(method.Name);

			// Generics arguments
			if (method.IsGenericMethod)
			{
				res.Append('<');
				var genericArgs = method.GetGenericArguments();
				for (var i = 0; i < genericArgs.Length; i++)
				res.Append((i > 0 ? ", " : "") + genericArgs[i]
						.GetSignature(useFullNames && !fullNamesOnlyInMethodName));
				res.Append('>');
			}
			// Parameters
			if (includeParameters)
			{
				res.Append('(');
				if (parametersFunction != null)
					res.Append(parametersFunction(useFullNames && !fullNamesOnlyInMethodName, includeParametersNames, method, parametersFunctionArguments));
				else
				{
					var pars = method.GetParameters();
					for (var i = 0; i < pars.Length; i++)
					{
						var par = pars[i];
						if (i == 0 && method.IsDefined(typeof(ExtensionAttribute), false)) res.Append("this ");
						if (par.ParameterType.IsByRef)
							res.Append("ref ");
						else if (par.IsOut) res.Append("out ");
						res.Append(par.ParameterType.GetSignature(useFullNames && !fullNamesOnlyInMethodName));
						if (includeParametersNames) res.Append(" " + par.Name);
						if (i < pars.Length - 1) res.Append(", ");
					}
				}
				res.Append(')');
			}
			return res.ToString();
		}
	}
	
	extension(Assembly assembly)
	{
		/// <summary>
		/// Retrieves an embedded resource stream from the assembly by folder path and file name.
		/// </summary>
		/// <param name="folder">
		/// The folder path within the assembly's embedded resources. 
		/// Path separators ('\\' or '/') are automatically converted to dots.
		/// </param>
		/// <param name="fileName">
		/// The name of the embedded resource file to retrieve.
		/// </param>
		/// <returns>
		/// A <see cref="IResponse{T}"/> containing the resource <see cref="Stream"/> if found;
		/// otherwise, an error response indicating the resource was not found or the assembly name is null.
		/// </returns>
		/// <remarks>
		/// The resource name is constructed as: <c>AssemblyName.Folder.FileName</c>
		/// where backslashes and forward slashes in the folder path are replaced with dots.
		/// </remarks>
		/// <example>
		/// <code>
		/// var assembly = typeof(MyClass).Assembly;
		/// var streamResponse = assembly.GetResourceStream("Resources\\Data", "config.json");
		/// if (streamResponse.IsSuccess)
		/// {
		///     using var stream = streamResponse.Payload;
		///     // Read from stream...
		/// }
		/// </code>
		/// </example>
		public IResponse<Stream> GetResourceStream(string folder, string fileName)
		{
			if (assembly.FullName is null)
				return Response.Get.Critical("Assembly.FullName is null").AsPayload<Stream>();
			var resourceName = assembly.FullName.Split(',')[0] + "." + folder.Replace("\\", ".").Replace("/", ".") + "." + fileName;
			var res = assembly.GetManifestResourceStream(resourceName);
			return res is null
				? Response.Get.NotFound($"Resource with name '{resourceName}' was not found on assembly").AsPayload<Stream>()
				: Response.Get.SuccessPayload(res);
		}

		/// <summary>
		/// Retrieves an embedded resource from the assembly as a string by folder path and file name.
		/// </summary>
		/// <param name="folder">
		/// The folder path within the assembly's embedded resources.
		/// Path separators ('\\' or '/') are automatically converted to dots.
		/// </param>
		/// <param name="fileName">
		/// The name of the embedded resource file to retrieve.
		/// </param>
		/// <returns>
		/// A <see cref="IResponse{T}"/> containing the resource content as a string if found;
		/// otherwise, an error response indicating the resource was not found or the assembly name is null.
		/// </returns>
		/// <remarks>
		/// This method is a convenience wrapper around <see cref="GetResourceStream"/> that reads
		/// the entire stream content as a string using a <see cref="StreamReader"/>
		/// </remarks>
		/// <example>
		/// <code>
		/// var assembly = typeof(MyClass).Assembly;
		/// var contentResponse = assembly.GetResourceAsString("Resources\\Templates", "email.html");
		/// if (contentResponse.IsSuccess)
		/// {
		///     var htmlContent = contentResponse.Payload;
		///     Console.WriteLine(htmlContent);
		/// }
		/// </code>
		/// </example>
		public IResponse<string> GetResourceAsString(string folder, string fileName) =>
			GetResourceStream(assembly, folder, fileName).Match(
				r => Response.Get.SuccessPayload(new StreamReader(r.Payload!).ReadToEnd()),
				r => r.AsPayload<string>());

		/// <summary>
		/// Asynchronously retrieves an embedded resource from the assembly as a string by folder path and file name.
		/// </summary>
		/// <param name="folder">
		/// The folder path within the assembly's embedded resources.
		/// Path separators ('\\' or '/') are automatically converted to dots.
		/// </param>
		/// <param name="fileName">
		/// The name of the embedded resource file to retrieve.
		/// </param>
		/// <param name="ct">
		/// A <see cref="CancellationToken"/> to observe while waiting for the task to complete.
		/// </param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a <see cref="IResponse{T}"/> 
		/// with the resource content as a string if found; otherwise, an error response indicating 
		/// the resource was not found or the assembly name is null.
		/// </returns>
		/// <remarks>
		/// This method is an asynchronous convenience wrapper around <see cref="GetResourceStream"/> that reads
		/// the entire stream content as a string using <see cref="StreamReader.ReadToEndAsync()"/>.
		/// <para>
		/// Note: The <paramref name="ct"/> parameter is only used on .NET 5.0 or greater. 
		/// On older frameworks (.NET Standard 2.0, .NET Framework 4.7.2), the cancellation token is ignored
		/// as <see cref="StreamReader.ReadToEndAsync()"/> does not accept a cancellation token in those versions.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// var assembly = typeof(MyClass).Assembly;
		/// var cts = new CancellationTokenSource();
		/// 
		/// var contentResponse = await assembly.GetResourceAsStringAsync(
		///     "Resources\\Templates", 
		///     "email.html", 
		///     cts.Token);
		///     
		/// if (contentResponse.IsSuccess)
		/// {
		///     var htmlContent = contentResponse.Payload;
		///     Console.WriteLine(htmlContent);
		/// }
		/// </code>
		/// </example>
		public async Task<IResponse<string>> GetResourceAsStringAsync(string folder, string fileName, CancellationToken ct = default) =>
			await GetResourceStream(assembly, folder, fileName).MatchAsync(
				async r => Response.Get.SuccessPayload(await new StreamReader(r.Payload!).ReadToEndAsync(
#if !STANDARD_OR_OLD_FRAMEWORKS
				ct
#endif
				)),
				r => r.AsPayload<string>());
	}
	const string FileScopeClassNameRegexPattern = "^(.*)<[a-zA-Z_]+>[A-F0-9]+__(.*)$";
#if !STANDARD_OR_OLD_FRAMEWORKS
	[GeneratedRegex(FileScopeClassNameRegexPattern)]
	private static partial Regex FileScopeClassNameRegex();
#endif
	extension(Type me)
	{
		/// <summary>
		/// Generates a human-readable signature string for the type with optional fully-qualified names.
		/// Handles generic types, nullable types, file-scoped types, and provides C# alias names for built-in types.
		/// </summary>
		/// <param name="useFullNames">
		/// <c>true</c> to use fully-qualified type names (including namespace); otherwise, <c>false</c> for short names.
		/// </param>
		/// <returns>
		/// A formatted string representing the type signature with C# syntax.
		/// Built-in types are represented with their C# aliases (e.g., "string" instead of "String", "int" instead of "Int32").
		/// Generic types are formatted as <c>TypeName&lt;T1, T2&gt;</c>.
		/// Nullable value types are formatted as <c>TypeName?</c>.
		/// </returns>
		/// <remarks>
		/// This method handles special cases:
		/// <list type="bullet">
		/// <item><description>File-scoped types (compiler-generated names are cleaned up)</description></item>
		/// <item><description>Nullable value types (displayed with <c>?</c> suffix)</description></item>
		/// <item><description>Generic types (formatted with angle brackets)</description></item>
		/// <item><description>Built-in C# types (replaced with aliases: string, int, bool, etc.)</description></item>
		/// </list>
		/// </remarks>
		/// <example>
		/// <code>
		/// var type = typeof(List&lt;string&gt;);
		/// var signature = type.GetSignature();
		/// // Returns: "List&lt;string&gt;"
		/// 
		/// var fullSignature = type.GetSignature(useFullNames: true);
		/// // Returns: "System.Collections.Generic.List&lt;string&gt;"
		/// 
		/// var nullableInt = typeof(int?);
		/// var nullableSig = nullableInt.GetSignature();
		/// // Returns: "int?"
		/// </code>
		/// </example>
		public string GetSignature(bool useFullNames = false)
		{
			var regex =
#if STANDARD_OR_OLD_FRAMEWORKS
				new Regex(FileScopeClassNameRegexPattern);
#else
			FileScopeClassNameRegex();
#endif
			var name = useFullNames && !string.IsNullOrWhiteSpace(me.FullName) ? me.FullName : me.Name;
			var match = regex.Match(name);
			if (match.Success && match.Groups.Count >= 3) name = match.Groups[1].Value + match.Groups[2].Value;
			//name = type.Name[(type.Name.IndexOf("__", StringComparison.Ordinal) + 2)..];

			var nullableType = Nullable.GetUnderlyingType(me);
			if (nullableType != null) return nullableType.GetSignature(useFullNames) + "?";
			//var name = useFullNames && !string.IsNullOrWhiteSpace(type.FullName) ? type.FullName : typeName;
			if (!me.GetTypeInfo()
				    .IsGenericType)
				return me.Name switch
				{
					"String" => "string",
					"String[]" => "string[]",
					"Boolean" => "bool",
					"Boolean[]" => "bool[]",
					"Int32" => "int",
					"Int32[]" => "int[]",
					"Int64" => "long",
					"Int64[]" => "long[]",
					"Decimal" => "decimal",
					"Decimal[]" => "decimal[]",
					"Byte" => "byte",
					"Byte[]" => "byte[]",
					"Object" => "object",
					"Object[]" => "object[]",
					"Void" => "void",
					var _ => name
				};
			StringBuilder sb = new(name.Contains('`') ? name[..name.IndexOf('`')] : name);
			sb.Append('<');
			var first = true;
			foreach (var t in me.GenericTypeArguments)
			{
				if (!first) sb.Append(',');
				sb.Append(t.GetSignature(useFullNames));
				first = false;
			}
			sb.Append('>');
			return sb.ToString();
		}

		/// <summary>
		/// Determines whether the current type is a subclass of the specified raw generic type.
		/// </summary>
		/// <param name="genericDefinition">
		/// The generic type definition to check against (e.g., <c>typeof(List&lt;&gt;)</c>).
		/// </param>
		/// <returns>
		/// <c>true</c> if the current type is a subclass of or implements the specified generic type; otherwise, <c>false</c>.
		/// </returns>
		/// <remarks>
		/// This method checks the entire type hierarchy, including base classes and implemented interfaces,
		/// to determine if any match the specified generic type definition.
		/// </remarks>
		/// <example>
		/// <code>
		/// var listType = typeof(List&lt;int&gt;);
		/// var isSubclass = listType.IsSubclassOfRawGeneric(typeof(IEnumerable&lt;&gt;));
		/// // Returns: true
		/// 
		/// var stringType = typeof(string);
		/// var isListSubclass = stringType.IsSubclassOfRawGeneric(typeof(List&lt;&gt;));
		/// // Returns: false
		/// </code>
		/// </example>
		public bool IsSubclassOfGenericDefinition(Type genericDefinition) => me.GetSubclassOfGenericDefinition(genericDefinition) is not null;
		/// <summary>
		/// Gets the concrete type in the inheritance hierarchy that matches the specified raw generic type.
		/// </summary>
		/// <param name="genericDefinition">
		/// The generic type definition to search for (e.g., <c>typeof(List&lt;&gt;)</c>).
		/// </param>
		/// <returns>
		/// The concrete type that matches the generic type definition if found; otherwise, <c>null</c>.
		/// For example, if searching for <c>IEnumerable&lt;&gt;</c> on <c>List&lt;int&gt;</c>, 
		/// returns <c>IEnumerable&lt;int&gt;</c>.
		/// </returns>
		/// <remarks>
		/// This method performs a breadth-first search through the type hierarchy (base classes and interfaces)
		/// to find the first type that matches the specified generic type definition.
		/// </remarks>
		/// <example>
		/// <code>
		/// var listType = typeof(List&lt;int&gt;);
		/// var enumerableType = listType.GetSubclassOfRawGeneric(typeof(IEnumerable&lt;&gt;));
		/// // Returns: typeof(IEnumerable&lt;int&gt;)
		/// 
		/// var matchingType = listType.GetSubclassOfRawGeneric(typeof(ICollection&lt;&gt;));
		/// // Returns: typeof(ICollection&lt;int&gt;)
		/// </code>
		/// </example>
		public Type? GetSubclassOfGenericDefinition(Type genericDefinition)
		{
			Queue<Type> toProcess = new([me]);
			while (toProcess.Count > 0)
			{
				var actual = toProcess.Dequeue();
				var cur = actual.GetTypeInfo().IsGenericType
					? actual.GetGenericTypeDefinition()
					: actual;
				if (cur.GetTypeInfo().IsGenericType && genericDefinition.GetGenericTypeDefinition() == cur.GetGenericTypeDefinition())
					return actual;
				foreach (var inter in actual.GetTypeInfo().ImplementedInterfaces)
					toProcess.Enqueue(inter);
				var baseType = actual.GetTypeInfo().BaseType;
				if (baseType != null) toProcess.Enqueue(baseType);
			}
			return null;
		}

		/// <summary>
		/// Determines whether the type can be assigned a <c>null</c> value.
		/// </summary>
		/// <param name="includeNullableValueTypes">
		/// <c>true</c> to consider value types (including enums) as non-nullable even though they're technically nullable via <see cref="Nullable{T}"/>;
		/// <c>false</c> to return <c>true</c> for <see cref="Nullable{T}"/> value types.
		/// </param>
		/// <returns>
		/// <c>true</c> if the type is a reference type (class or interface) or a <see cref="Nullable{T}"/> value type; otherwise, <c>false</c>.
		/// </returns>
		/// <remarks>
		/// This method considers the following as nullable:
		/// <list type="bullet">
		/// <item><description>Reference types (classes and interfaces)</description></item>
		/// <item><description><see cref="Nullable{T}"/> value types (e.g., <c>int?</c>, <c>DateTime?</c>)</description></item>
		/// </list>
		/// When <paramref name="includeNullableValueTypes"/> is <c>true</c>, the <see cref="Enum"/> type itself is treated as non-nullable.
		/// </remarks>
		/// <example>
		/// <code>
		/// var stringType = typeof(string);
		/// var isNullable1 = stringType.IsNullable();
		/// // Returns: true (reference type)
		/// 
		/// var intType = typeof(int);
		/// var isNullable2 = intType.IsNullable();
		/// // Returns: false (value type)
		/// 
		/// var nullableIntType = typeof(int?);
		/// var isNullable3 = nullableIntType.IsNullable();
		/// // Returns: true (Nullable&lt;int&gt;)
		/// </code>
		/// </example>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool CanBeNull(bool includeNullableValueTypes = true)
			=> me switch
			{
				_ when includeNullableValueTypes && me.IsEnum => false,
				_ when me.IsClass || me.IsInterface || me.IsGenericType && me.GetGenericTypeDefinition() == typeof(Nullable<>) => true,
				_ => false
			};

		/// <summary>
		/// Gets the default value for the type. For value types, returns an instance created with the default constructor.
		/// For reference types and nullable value types, returns <c>null</c>.
		/// </summary>
		/// <returns>
		/// The default value for the type: <c>null</c> for reference types and nullable value types,
		/// or a new instance for non-nullable value types (e.g., <c>0</c> for <c>int</c>, <c>false</c> for <c>bool</c>).
		/// </returns>
		/// <example>
		/// <code>
		/// var intType = typeof(int);
		/// var defaultInt = intType.GetDefaultValue();
		/// // Returns: 0
		/// 
		/// var stringType = typeof(string);
		/// var defaultString = stringType.GetDefaultValue();
		/// // Returns: null
		/// 
		/// var nullableIntType = typeof(int?);
		/// var defaultNullableInt = nullableIntType.GetDefaultValue();
		/// // Returns: null
		/// 
		/// var dateTimeType = typeof(DateTime);
		/// var defaultDateTime = dateTimeType.GetDefaultValue();
		/// // Returns: DateTime.MinValue (01/01/0001 00:00:00)
		/// </code>
		/// </example>
		public object? GetDefaultValue()
			=> me.GetTypeInfo().IsValueType && Nullable.GetUnderlyingType(me) == null
				? Activator.CreateInstance(me)
				: null;
	}
	extension<T>(FuxionExtensions<T?> me)
	{
		/// <summary>
		/// Provides reflection-related extension operations for this value.
		/// </summary>
		/// <returns>
		/// A <see cref="ReflectionExtensions{T}"/> instance that provides access to reflection extension methods.
		/// </returns>
		/// <example>
		/// <code>
		/// var myObject = new MyClass();
		/// var privateField = myObject.Fx.Reflection.GetPrivateFieldValue&lt;string&gt;("_internalData");
		/// </code>
		/// </example>
		public ReflectionExtensions<T?> Reflection
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me.Value);
		}
	}

	extension<T>(ReflectionExtensions<T?> me)
	{
		// Source: https://stackoverflow.com/questions/1565734/is-it-possible-to-set-private-property-via-reflection
		/// <summary>
		/// Returns a private or non-public property value from the underlying object using Reflection.
		/// </summary>
		/// <typeparam name="TValue">The type of the property value to retrieve.</typeparam>
		/// <param name="propertyName">The name of the property to retrieve.</param>
		/// <returns>
		/// The value of the property cast to type <typeparamref name="TValue"/>, or <c>null</c> if the property value is <c>null</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the underlying object is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when the property with the specified name is not found in the object's type.
		/// </exception>
		/// <remarks>
		/// This method searches for both public and non-public instance properties on the object's type.
		/// It's useful for accessing private properties during testing or when working with types you don't control.
		/// </remarks>
		/// <example>
		/// <code>
		/// class MyClass
		/// {
		///     private string SecretData { get; set; } = "Hidden";
		/// }
		/// 
		/// var obj = new MyClass();
		/// var secret = obj.Fx.Reflection.GetPrivatePropertyValue&lt;string&gt;("SecretData");
		/// Console.WriteLine(secret);  // Output: "Hidden"
		/// </code>
		/// </example>
		public TValue? GetPrivatePropertyValue<TValue>(string propertyName)
		{
#pragma warning disable CA2208
			if (me.Value == null) throw new ArgumentNullException(nameof(me.Value));
#pragma warning restore CA2208
			var pi = me.Value.GetType()
				.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			return pi == null
				? throw new ArgumentOutOfRangeException(nameof(propertyName),
					$"Property {propertyName} was not found in Type {me.Value.GetType()
						.FullName}")
				: (TValue?)pi.GetValue(me.Value, null);
		}

		/// <summary>
		/// Returns a private or non-public field value from the underlying object using Reflection.
		/// Searches through the entire inheritance hierarchy to find the field.
		/// </summary>
		/// <typeparam name="TValue">The type of the field value to retrieve.</typeparam>
		/// <param name="propertyName">The name of the field to retrieve.</param>
		/// <returns>
		/// The value of the field cast to type <typeparamref name="TValue"/>, or <c>null</c> if the field value is <c>null</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the underlying object is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when the field with the specified name is not found in the object's type or its base types.
		/// </exception>
		/// <remarks>
		/// This method searches for both public and non-public instance fields, traversing up the inheritance hierarchy
		/// until the field is found or the top of the hierarchy is reached.
		/// It's useful for accessing private fields during testing or when working with types you don't control.
		/// </remarks>
		/// <example>
		/// <code>
		/// class MyClass
		/// {
		///     private string _internalData = "Secret";
		/// }
		/// 
		/// var obj = new MyClass();
		/// var data = obj.Fx.Reflection.GetPrivateFieldValue&lt;string&gt;("_internalData");
		/// Console.WriteLine(data);  // Output: "Secret"
		/// </code>
		/// </example>
		public TValue? GetPrivateFieldValue<TValue>(string propertyName)
		{
#pragma warning disable CA2208
			if (me.Value == null) throw new ArgumentNullException(nameof(me.Value));
#pragma warning restore CA2208
			var t = me.Value.GetType();
			FieldInfo? fi = null;
			while (fi == null && t != null)
			{
				fi = t.GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				t = t.BaseType;
			}
			if (fi == null)
				throw new ArgumentOutOfRangeException(nameof(propertyName), string.Format("Field {0} was not found in Type {1}", propertyName, me.Value.GetType()
					.FullName));
			return (TValue?)fi.GetValue(me.Value);
		}

		/// <summary>
		/// Sets a private or non-public property value on the underlying object using Reflection.
		/// </summary>
		/// <param name="propertyName">The name of the property to set.</param>
		/// <param name="value">The value to assign to the property.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the underlying object is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when the property with the specified name is not found in the object's type or its inheritance hierarchy.
		/// </exception>
		/// <remarks>
		/// This method searches for both public and non-public instance properties, including those inherited from base types
		/// (<see cref="BindingFlags.FlattenHierarchy"/>). It's useful for setting private properties during testing
		/// or when initializing objects with non-public setters.
		/// </remarks>
		/// <example>
		/// <code>
		/// class MyClass
		/// {
		///     private string SecretData { get; set; } = "Hidden";
		///     public string GetSecret() => SecretData;
		/// }
		/// 
		/// var obj = new MyClass();
		/// obj.Fx.Reflection.SetPrivatePropertyValue("SecretData", "NewSecret");
		/// Console.WriteLine(obj.GetSecret());  // Output: "NewSecret"
		/// </code>
		/// </example>
		public void SetPrivatePropertyValue(string propertyName, object? value)
		{
#pragma warning disable CA2208
			if (me.Value == null) throw new ArgumentNullException(nameof(me.Value));
#pragma warning restore CA2208
			var prop = me.Value.GetType()
				.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy) ?? throw new ArgumentOutOfRangeException(nameof(propertyName),
				$"Property {propertyName} was not found in Type {me.Value.GetType()
					.FullName}");
			prop.DeclaringType?.InvokeMember(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.FlattenHierarchy, Type.DefaultBinder,
				me.Value, [
					value
				]);
		}

		/// <summary>
		/// Sets a private or non-public field value on the underlying object using Reflection.
		/// Searches through the entire inheritance hierarchy to find the field.
		/// </summary>
		/// <param name="propertyName">The name of the field to set.</param>
		/// <param name="value">The value to assign to the field.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the underlying object is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when the field with the specified name is not found in the object's type or its base types.
		/// </exception>
		/// <remarks>
		/// This method searches for both public and non-public instance fields, traversing up the inheritance hierarchy
		/// until the field is found or the top of the hierarchy is reached.
		/// It's useful for setting private fields during testing or when initializing objects with non-public fields.
		/// </remarks>
		/// <example>
		/// <code>
		/// class MyClass
		/// {
		///     private string _internalData = "Secret";
		///     public string GetData() => _internalData;
		/// }
		/// 
		/// var obj = new MyClass();
		/// obj.Fx.Reflection.SetPrivateFieldValue("_internalData", "NewData");
		/// Console.WriteLine(obj.GetData());  // Output: "NewData"
		/// </code>
		/// </example>
		public void SetPrivateFieldValue(string propertyName, object? value)
		{
#pragma warning disable CA2208
			if (me.Value == null) throw new ArgumentNullException(nameof(me.Value));
#pragma warning restore CA2208
			var t = me.Value.GetType();
			FieldInfo? fi = null;
			while (fi == null && t != null)
			{
				fi = t.GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				t = t.BaseType;
			}
			if (fi == null)
				throw new ArgumentOutOfRangeException(nameof(propertyName), string.Format("Field {0} was not found in Type {1}", propertyName, me.Value.GetType()
					.FullName));
			fi.SetValue(me.Value, value);
		}
	}
}

/// <summary>
/// Generic wrapper class that provides reflection extension methods for values of type <typeparamref name="T"/>.
/// This class is used internally by the Fuxion extensions framework to enable fluent API syntax for reflection operations.
/// </summary>
/// <typeparam name="T">The type of the value being wrapped.</typeparam>
/// <remarks>
/// This class inherits from <see cref="Extensions{T}"/> and serves as a specialized container for reflection-related operations.
/// Users typically access reflection functionality through extension methods rather than instantiating this class directly.
/// The primary operations provided include:
/// <list type="bullet">
/// <item><description>Getting private property values</description></item>
/// <item><description>Getting private field values</description></item>
/// <item><description>Setting private property values</description></item>
/// <item><description>Setting private field values</description></item>
/// </list>
/// These operations are particularly useful for unit testing and scenarios where you need to access or modify
/// non-public members of objects.
/// </remarks>
/// <example>
/// <code>
/// // Accessed through extension syntax
/// var myObject = new MyClass();
/// 
/// // Get private field
/// var privateData = myObject.Fx.Reflection.GetPrivateFieldValue&lt;string&gt;("_secretData");
/// 
/// // Set private property
/// myObject.Fx.Reflection.SetPrivatePropertyValue("InternalState", 42);
/// 
/// // Get private property
/// var state = myObject.Fx.Reflection.GetPrivatePropertyValue&lt;int&gt;("InternalState");
/// 
/// // Set private field
/// myObject.Fx.Reflection.SetPrivateFieldValue("_counter", 100);
/// </code>
/// </example>
public class ReflectionExtensions<T>(T me) : Extensions<T>(me);
