using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Fuxion.Linq.Expressions;
using Fuxion.Reflection;
using Microsoft.CSharp.RuntimeBinder;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace Fuxion.Web;

/// <summary>
/// Defines how the patcher handles properties that don't exist in the target type.
/// </summary>
public enum NonExistingPropertiesMode
{
	/// <summary>
	/// Non-existing properties are not allowed. Attempting to set or get them will throw an exception.
	/// </summary>
	NotAllowed = 0,
	
	/// <summary>
	/// Non-existing properties can only be set, but not retrieved.
	/// </summary>
	OnlySet = 1,
	
	/// <summary>
	/// Non-existing properties can be both set and retrieved.
	/// </summary>
	GetAndSet = 2
}

/// <summary>
/// Provides a dynamic object for partially updating (patching) objects of type <typeparamref name="T"/>.
/// This class is useful for scenarios like HTTP PATCH operations where only specified properties should be updated.
/// </summary>
/// <typeparam name="T">The target type that this patcher can update. Must be a non-nullable reference or value type.</typeparam>
/// <remarks>
/// The Patcher class allows you to:
/// <list type="bullet">
/// <item><description>Dynamically set properties without knowing them at compile time</description></item>
/// <item><description>Track which properties have been set</description></item>
/// <item><description>Apply only the set properties to a target object</description></item>
/// <item><description>Handle properties that may not exist in the target type</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// dynamic patcher = new Patcher&lt;Person&gt;();
/// patcher.Name = "John";
/// patcher.Age = 30;
/// 
/// var person = new Person();
/// patcher.Patch(person); // Only Name and Age will be updated
/// </code>
/// </example>
[JsonConverter(typeof(PatcherJsonConverterFactory))]
public sealed class Patcher<T>(NonExistingPropertiesMode nonExistingPropertiesMode = NonExistingPropertiesMode.NotAllowed) : DynamicObject
	where T : notnull
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Patcher{T}"/> class with default settings.
	/// Used by JSON deserialization.
	/// </summary>
	[JsonConstructor]
	private Patcher() : this(NonExistingPropertiesMode.NotAllowed) { }
	
	/// <summary>
	/// Internal dictionary storing property names, their metadata, and values that have been set.
	/// </summary>
	internal readonly Dictionary<string, (PropertyInfo? Property, object? Value)> Properties = new();
	
	/// <summary>
	/// Gets or sets the mode that determines how non-existing properties are handled.
	/// </summary>
	public NonExistingPropertiesMode NonExistingPropertiesMode { get; set; } = nonExistingPropertiesMode;
	
	/// <summary>
	/// Applies all properties that have been set in this patcher to the target object.
	/// </summary>
	/// <param name="obj">The target object to patch with the stored property values.</param>
	/// <remarks>
	/// This method iterates through all properties that have been set and updates the corresponding
	/// properties in the target object. Special handling is provided for enumerable types.
	/// </remarks>
	/// <exception cref="ArgumentException">Thrown when a property cannot be set on the target object.</exception>
	public void Patch(T obj)
	{
		foreach (var pro in Properties)
		{
			var property = pro.Value.Property ?? obj.GetType()
				.GetProperty(pro.Key);
			if (property == null) continue;
			var isList = property.PropertyType.GetTypeInfo()
				.IsGenericType && property.PropertyType.IsSubclassOfGenericDefinition(typeof(IEnumerable<>));
			if (isList)
			{
				var listType = typeof(List<>).MakeGenericType(property.PropertyType.GenericTypeArguments[0]);
				var list = Activator.CreateInstance(listType) as IList;
				foreach (var item in pro.Value.Value as IList ?? Array.Empty<object>()) list?.Add(item);
				property.SetValue(obj, list);
			} else
				property.SetValue(obj, CastValue(property.PropertyType, pro.Value.Value));
		}
	}

	/// <summary>
	/// Casts a value to the specified type, handling nullables, enums, and GUIDs.
	/// </summary>
	/// <param name="type">The target type to cast the value to.</param>
	/// <param name="value">The value to cast.</param>
	/// <returns>The value cast to the specified type, or null if the value is null.</returns>
	private static object? CastValue(Type type, object? value)
	{
		var isNullable = type.IsSubclassOfGenericDefinition(typeof(Nullable<>));
		var valueType = isNullable
			? type.GetTypeInfo()
				.GenericTypeArguments.First()
			: type;
		object? res = null;
		if (value != null && valueType.GetTypeInfo()
			.IsEnum)
			res = Enum.Parse(valueType, value?.ToString() ?? "");
		else if (value != null && valueType == typeof(Guid))
			res = Guid.Parse(value?.ToString() ?? "");
		else if (value != null) res = Convert.ChangeType(value, valueType);
		if (value != null && isNullable) res = Activator.CreateInstance(typeof(Nullable<>).MakeGenericType(valueType), res);
		return res;
	}
	
	/// <summary>
	/// Converts this patcher to a patcher of a different type.
	/// </summary>
	/// <typeparam name="R">The target type for the new patcher. Must be a reference type.</typeparam>
	/// <param name="allowNonExistingProperties">If true, properties that don't exist in type R will be included; otherwise, an exception is thrown.</param>
	/// <returns>A new <see cref="Patcher{R}"/> with the same property values as this patcher.</returns>
	/// <exception cref="InvalidCastException">Thrown when a property cannot be transferred to the target type and <paramref name="allowNonExistingProperties"/> is false.</exception>
	public Patcher<R> ToPatcher<R>(bool allowNonExistingProperties = false)
		where R : class
	{
		var res = new Patcher<R>(NonExistingPropertiesMode);
		foreach (var pair in Properties)
		{
			var pro = typeof(R).GetRuntimeProperty(pair.Key);
			if (pro == null && !allowNonExistingProperties) throw new InvalidCastException($"Property '{pair.Key}' cannot be transferred to type '{typeof(R).Name}'");
			res.Properties.Add(pair.Key, (pro, pair.Value.Value));
		}
		return res;
	}
	
	/// <summary>
	/// Determines whether a property with the specified name has been set in this patcher.
	/// </summary>
	/// <param name="memberName">The name of the property to check.</param>
	/// <returns>true if the property has been set; otherwise, false.</returns>
	public bool Has(string memberName) => Properties.ContainsKey(memberName);
	
	/// <summary>
	/// Creates a new patcher by executing a dynamic action that sets properties.
	/// </summary>
	/// <param name="action">An action that receives a dynamic patcher and sets properties on it.</param>
	/// <param name="nonExistingPropertiesMode">The mode for handling non-existing properties. Default is 'NotAllowed'</param>
	/// <returns>A new <see cref="Patcher{T}"/> with the properties set by the action.</returns>
	/// <example>
	/// <code>
	/// var patcher = Patcher&lt;Person&gt;.FromDynamic(p => {
	///     p.Name = "John";
	///     p.Age = 30;
	/// });
	/// </code>
	/// </example>
	public static Patcher<T> FromDynamic(Action<dynamic> action, NonExistingPropertiesMode nonExistingPropertiesMode = NonExistingPropertiesMode.NotAllowed)
	{
		dynamic res = new Patcher<T>(nonExistingPropertiesMode);
		action(res);
		return res;
	}
	
	/// <summary>
	/// Creates a new patcher from an anonymous object or any object instance.
	/// </summary>
	/// <param name="func">A function that returns the object whose properties will be copied to the patcher.</param>
	/// <param name="nonExistingPropertiesMode">The mode for handling non-existing properties.</param>
	/// <returns>A new <see cref="Patcher{T}"/> with properties copied from the source object.</returns>
	/// <exception cref="RuntimeBinderException">Thrown when a property from the source object doesn't exist in type T and <paramref name="nonExistingPropertiesMode"/> is NotAllowed.</exception>
	/// <example>
	/// <code>
	/// var patcher = Patcher&lt;Person&gt;.FromObject(() => new { Name = "John", Age = 30 });
	/// </code>
	/// </example>
	public static Patcher<T> FromObject(Func<object> func, NonExistingPropertiesMode nonExistingPropertiesMode = NonExistingPropertiesMode.NotAllowed)
	{
		var res = new Patcher<T>(nonExistingPropertiesMode);
		var obj = func();
		foreach (var pro in obj.GetType()
			.GetProperties())
		{
			if (typeof(T).GetProperty(pro.Name) is null && nonExistingPropertiesMode == NonExistingPropertiesMode.NotAllowed)
				throw new RuntimeBinderException($"Type '{typeof(T).GetSignature()}' not has a property with name '{pro.Name}'");
			res.Properties.Add(pro.Name, (pro, pro.GetValue(obj)));
		}
		return res;
	}

	#region SET
	/// <summary>
	/// Attempts to set a property dynamically. This method is called by the dynamic language runtime.
	/// </summary>
	/// <param name="binder">Provides information about the dynamic operation.</param>
	/// <param name="value">The value to set.</param>
	/// <returns>true if the operation succeeds; otherwise, false.</returns>
	public override bool TrySetMember(SetMemberBinder binder, object? value)
	{
		var pro = typeof(T).GetRuntimeProperty(binder.Name);
		if (pro != null)
		{
			Properties[pro.Name] = (pro, value);
			return true;
		}
		switch (NonExistingPropertiesMode)
		{
			case NonExistingPropertiesMode.OnlySet:
			case NonExistingPropertiesMode.GetAndSet:
				// NULLABLE - To review
				Properties.Add(binder.Name, (null, value));
				return true;
			case NonExistingPropertiesMode.NotAllowed:
			default: return base.TrySetMember(binder, value);
		}
	}

	/// <summary>
	/// Sets a property value by name using runtime reflection.
	/// </summary>
	/// <param name="propertyName">The name of the property to set.</param>
	/// <param name="value">The value to set.</param>
	public void Set(string propertyName, object value)
	{
		var binder = Binder.SetMember(CSharpBinderFlags.None, propertyName, GetType(), new List<CSharpArgumentInfo>
		{
			CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
			CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
		});
		var callsite = CallSite<Func<CallSite, object, object, object>>.Create(binder);
		callsite.Target(callsite, this, value);
	}
	#endregion

	#region GET
	/// <summary>
	/// Gets the value of a property by name.
	/// </summary>
	/// <param name="propertyName">The name of the property to get.</param>
	/// <returns>The value of the property, or null if it hasn't been set.</returns>
	/// <exception cref="RuntimeBinderException">Thrown when the property doesn't exist and the mode doesn't allow getting non-existing properties.</exception>
	public object? Get(string propertyName)
	{
		if (!Properties.TryGetValue(propertyName, out var value))
			return NonExistingPropertiesMode switch
			{
				NonExistingPropertiesMode.NotAllowed or NonExistingPropertiesMode.OnlySet => throw new RuntimeBinderException($"Type '{GetType().GetSignature()}' not has a property with name '{propertyName}'"),
				_ => null
			};
		if (typeof(T).GetRuntimeProperty(propertyName) == null && NonExistingPropertiesMode is NonExistingPropertiesMode.NotAllowed or NonExistingPropertiesMode.OnlySet)
			throw new RuntimeBinderException($"Type '{typeof(T).GetSignature()}' not has a property with name '{propertyName}'");
		return value.Value;
	}
	
	/// <summary>
	/// Attempts to get a property dynamically. This method is called by the dynamic language runtime.
	/// </summary>
	/// <param name="binder">Provides information about the dynamic operation.</param>
	/// <param name="result">The result of the get operation.</param>
	/// <returns>Always returns true.</returns>
	public override bool TryGetMember(GetMemberBinder binder, out object? result)
	{
		result = Get(binder.Name);
		return true;
	}
	
	/// <summary>
	/// Gets the value of a property by name, cast to the specified type.
	/// </summary>
	/// <typeparam name="TValue">The type to cast the property value to.</typeparam>
	/// <param name="memberName">The name of the property to get.</param>
	/// <returns>The property value cast to <typeparamref name="TValue"/>, or the default value if not set.</returns>
	public TValue? Get<TValue>(string memberName)
	{
		var res = Get(memberName);
		if (res != null) return (TValue?)CastValue(typeof(TValue), res);
		return default!;
	}
	
	/// <summary>
	/// Attempts to get a property value using a strongly-typed expression.
	/// </summary>
	/// <typeparam name="TValue">The type of the property value.</typeparam>
	/// <param name="memberSelector">An expression that selects the property to get.</param>
	/// <param name="value">When this method returns, contains the property value if it exists; otherwise, the default value.</param>
	/// <returns>true if the property has been set; otherwise, false.</returns>
	/// <exception cref="InvalidProgramException">Thrown when the property is tracked but returns null unexpectedly.</exception>
	/// <example>
	/// <code>
	/// if (patcher.TryGet(p => p.Name, out var name))
	/// {
	///     Console.WriteLine($"Name was set to: {name}");
	/// }
	/// </code>
	/// </example>
	public bool TryGet<TValue>(Expression<Func<T, TValue>> memberSelector, [MaybeNullWhen(false)] out TValue value)
	{
		if (Has(memberSelector.GetMemberName()))
		{
			var res = Get<TValue>(memberSelector.GetMemberName());
			value = res ?? throw new InvalidProgramException($"Patchable has '{memberSelector.GetMemberName()}' but Get return null");
			return true;
		}
		value = default;
		return false;
	}
	
	/// <summary>
	/// Attempts to get a property value by name.
	/// </summary>
	/// <typeparam name="TValue">The type of the property value.</typeparam>
	/// <param name="memberName">The name of the property to get.</param>
	/// <param name="value">When this method returns, contains the property value if it exists; otherwise, the default value.</param>
	/// <returns>true if the property has been set; otherwise, false.</returns>
	/// <exception cref="InvalidProgramException">Thrown when the property is tracked but returns null unexpectedly.</exception>
	public bool TryGet<TValue>(string memberName, [MaybeNullWhen(false)] out TValue value)
	{
		if (Has(memberName))
		{
			var res = Get<TValue>(memberName);
			value = res ?? throw new InvalidProgramException($"Patchable has '{memberName}' but Get return null");
			return true;
		}
		value = default;
		return false;
	}
	#endregion
}