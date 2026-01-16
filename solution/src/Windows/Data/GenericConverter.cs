using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using Fuxion.Reflection;

namespace Fuxion.Windows.Data;

/// <summary>
///    Base class for strongly-typed WPF value converters that convert between <typeparamref name="TSource"/>
///    and <typeparamref name="TResult"/> types.
/// </summary>
/// <typeparam name="TSource">The type of the source value in the binding.</typeparam>
/// <typeparam name="TResult">The type of the target value in the binding.</typeparam>
/// <remarks>
///    <para>
///       This abstract class provides a strongly-typed wrapper around <see cref="IValueConverter"/>,
///       eliminating the need for manual type checking and casting in derived converters. It automatically
///       handles type validation, null values, and <see cref="DependencyProperty.UnsetValue"/> scenarios.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Type safety:</strong> Automatic type validation and casting with compile-time type checking
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Null handling:</strong> Configurable null value support for nullable types
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>UnsetValue support:</strong> Optional handling of <see cref="DependencyProperty.UnsetValue"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Bidirectional conversion:</strong> Support for both <see cref="Convert"/> and <see cref="ConvertBack"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Clean implementation:</strong> Derived classes only implement typed conversion logic
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Implementation requirements:</strong>
///    </para>
///    <list type="number">
///       <item>
///          <description>Override <see cref="Convert(TSource, CultureInfo)"/> with conversion logic</description>
///       </item>
///       <item>
///          <description>
///             Optionally override <see cref="ConvertBack(TResult, CultureInfo)"/> for two-way binding
///          </description>
///       </item>
///       <item>
///          <description>No need to handle type checking, null values, or UnsetValue manually</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Simple converter implementation:</strong>
///    <code>
/// public class StringToUpperConverter : GenericConverter&lt;string, string&gt;
/// {
///     public override string Convert(string source, CultureInfo culture)
///     {
///         return source?.ToUpper() ?? string.Empty;
///     }
///     
///     public override string ConvertBack(string result, CultureInfo culture)
///     {
///         return result?.ToLower() ?? string.Empty;
///     }
/// }
/// </code>
///    <strong>Converter with UnsetValue support:</strong>
///    <code>
/// public class IntToStringConverter : GenericConverter&lt;int, string&gt;
/// {
///     public IntToStringConverter()
///     {
///         AllowUnsetValue = true;
///         UnsetValue = "N/A";
///     }
///     
///     public override string Convert(int source, CultureInfo culture)
///     {
///         return source.ToString();
///     }
/// }
/// </code>
/// </example>
public abstract class GenericConverter<TSource, TResult> : IValueConverter
{
	/// <summary>
	///    Initializes a new instance of the <see cref="GenericConverter{TSource,TResult}"/> class.
	/// </summary>
	public GenericConverter() { }

	/// <summary>
	///    Initializes a new instance of the <see cref="GenericConverter{TSource,TResult}"/> class
	///    with configurable nullable value type handling.
	/// </summary>
	/// <param name="includeNullableValueTypes">
	///    <c>true</c> to treat nullable value types (e.g., <c>int?</c>) as nullable;
	///    <c>false</c> to treat them as non-nullable. Default is <c>true</c>.
	/// </param>
	public GenericConverter(bool includeNullableValueTypes) : this() => this.includeNullableValueTypes = includeNullableValueTypes;

	readonly bool includeNullableValueTypes = true;

	/// <summary>
	///    Gets or sets a value indicating whether <see cref="DependencyProperty.UnsetValue"/> is allowed as input.
	/// </summary>
	/// <value>
	///    <c>true</c> to allow <see cref="DependencyProperty.UnsetValue"/> and return <see cref="UnsetValue"/>;
	///    otherwise, <c>false</c> to throw <see cref="NotSupportedException"/>. Default is <c>false</c>.
	/// </value>
	/// <remarks>
	///    When set to <c>true</c>, if the converter receives <see cref="DependencyProperty.UnsetValue"/>,
	///    it will return the value specified in <see cref="UnsetValue"/> instead of throwing an exception.
	/// </remarks>
	public bool AllowUnsetValue { get; set; }

	/// <summary>
	///    Gets or sets the value to return when <see cref="DependencyProperty.UnsetValue"/> is received
	///    and <see cref="AllowUnsetValue"/> is <c>true</c>.
	/// </summary>
	/// <value>
	///    The value to return for <see cref="DependencyProperty.UnsetValue"/>. Default is <c>default(TResult)</c>.
	/// </value>
	public TResult UnsetValue { get; set; } = default!;

	object? IValueConverter.Convert(object? value, Type targetType, object parameter, CultureInfo culture)
	{
		// if allow unset value
		if (AllowUnsetValue && value == DependencyProperty.UnsetValue) return UnsetValue;
		if (value == DependencyProperty.UnsetValue)
			throw new NotSupportedException($"The value is DependencyProperty.UnsetValue. To support unset values use '{nameof(AllowUnsetValue)}' property of the '{GetType().Name}' class");
		// value must be TSource, call Convert
		// value is null and TSource is nullable, call Convert
		if (value is TSource || value == null && typeof(TSource).CanBeNull(includeNullableValueTypes)) return Convert((TSource)value!, culture);
		// value is null and TResult is nullable, return null
		if (value == null && typeof(TResult).CanBeNull(includeNullableValueTypes)) return null;
		// In any other case, value is not supported exception
		throw new NotSupportedException($"The value '{value}' is not supported for '{GetType().Name}.{nameof(Convert)}' method, must be of type '{typeof(TSource).GetSignature()}'");
	}

	object? IValueConverter.ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
	{
		// value must be TSource, call ConvertBack
		// value is null and TResult is nullable, call ConvertBack
		if (value is TResult || value == null && typeof(TResult).CanBeNull(includeNullableValueTypes)) return ConvertBack((TResult)value!, culture);
		// value is null and Tsource is nullable, return null
		if (value == null && typeof(TSource).CanBeNull(includeNullableValueTypes)) return null;
		// In any other case, value is not supported exception
		throw new NotSupportedException($"The value '{value}' is not supported for '{GetType().Name}.{nameof(ConvertBack)}' method, must be of type '{typeof(TResult).GetSignature()}'");
	}

	/// <summary>
	///    Converts a value from <typeparamref name="TSource"/> to <typeparamref name="TResult"/>.
	/// </summary>
	/// <param name="source">The source value to convert.</param>
	/// <param name="culture">The culture to use in the converter.</param>
	/// <returns>The converted value of type <typeparamref name="TResult"/>.</returns>
	/// <remarks>
	///    Override this method to implement the conversion logic from source to target type.
	///    Type validation and null handling are performed automatically by the base class.
	/// </remarks>
	public abstract TResult Convert(TSource source, CultureInfo culture);

	/// <summary>
	///    Converts a value from <typeparamref name="TResult"/> back to <typeparamref name="TSource"/>.
	/// </summary>
	/// <param name="result">The target value to convert back.</param>
	/// <param name="culture">The culture to use in the converter.</param>
	/// <returns>The converted value of type <typeparamref name="TSource"/>.</returns>
	/// <exception cref="NotSupportedException">
	///    Thrown by default implementation. Override this method to support two-way binding.
	/// </exception>
	/// <remarks>
	///    Override this method to implement reverse conversion logic for two-way binding scenarios.
	///    The default implementation throws <see cref="NotSupportedException"/>.
	/// </remarks>
	public virtual TSource ConvertBack(TResult result, CultureInfo culture) =>
		throw new NotSupportedException($"'{GetType().GetMethod(nameof(ConvertBack))?.GetSignature(includeReturn: true)}' method, is not supported");
}

/// <summary>
///    Base class for strongly-typed WPF value converters that convert between <typeparamref name="TSource"/>
///    and <typeparamref name="TResult"/> types with a converter parameter of type <typeparamref name="TParameter"/>.
/// </summary>
/// <typeparam name="TSource">The type of the source value in the binding.</typeparam>
/// <typeparam name="TResult">The type of the target value in the binding.</typeparam>
/// <typeparam name="TParameter">The type of the converter parameter.</typeparam>
/// <remarks>
///    <para>
///       This abstract class extends <see cref="GenericConverter{TSource,TResult}"/> by adding support for
///       a strongly-typed converter parameter. This is useful when the conversion logic depends on additional
///       configuration provided via the <c>ConverterParameter</c> in XAML bindings.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Typed parameter:</strong> Automatic parameter type validation and casting
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>All base features:</strong> Inherits null handling, UnsetValue support, and type safety
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Flexible conversion:</strong> Conversion logic can vary based on parameter value
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Implementation requirements:</strong>
///    </para>
///    <list type="number">
///       <item>
///          <description>
///             Override <see cref="Convert(TSource, TParameter, CultureInfo)"/> with parameterized conversion logic
///          </description>
///       </item>
///       <item>
///          <description>
///             Optionally override <see cref="ConvertBack(TResult, TParameter, CultureInfo)"/> for two-way binding
///          </description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Converter with parameter:</strong>
///    <code>
/// public class NumberToStringConverter : GenericConverter&lt;double, string, string&gt;
/// {
///     public override string Convert(double source, string format, CultureInfo culture)
///     {
///         return source.ToString(format, culture);
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <!-- XAML usage -->
/// <TextBlock Text="{Binding Value, 
///                           Converter={StaticResource NumberConverter},
///                           ConverterParameter='N2'}"/>
/// ]]></code>
/// </example>
public abstract class GenericConverter<TSource, TResult, TParameter> : IValueConverter
{
	/// <summary>
	///    Initializes a new instance of the <see cref="GenericConverter{TSource,TResult,TParameter}"/> class.
	/// </summary>
	public GenericConverter() { }

	/// <summary>
	///    Initializes a new instance of the <see cref="GenericConverter{TSource,TResult,TParameter}"/> class
	///    with configurable nullable value type handling.
	/// </summary>
	/// <param name="valueTypesAreNotNullables">
	///    <c>true</c> to treat nullable value types as non-nullable;
	///    <c>false</c> to treat them as nullable. Default is <c>true</c>.
	/// </param>
	public GenericConverter(bool valueTypesAreNotNullables) : this() => this.valueTypesAreNotNullables = valueTypesAreNotNullables;

	readonly bool valueTypesAreNotNullables = true;

	/// <summary>
	///    Gets or sets a value indicating whether <see cref="DependencyProperty.UnsetValue"/> is allowed as input.
	/// </summary>
	/// <value>
	///    <c>true</c> to allow <see cref="DependencyProperty.UnsetValue"/> and return <see cref="UnsetValue"/>;
	///    otherwise, <c>false</c> to throw <see cref="NotSupportedException"/>. Default is <c>false</c>.
	/// </value>
	public bool AllowUnsetValue { get; set; }

	/// <summary>
	///    Gets or sets the value to return when <see cref="DependencyProperty.UnsetValue"/> is received
	///    and <see cref="AllowUnsetValue"/> is <c>true</c>.
	/// </summary>
	/// <value>
	///    The value to return for <see cref="DependencyProperty.UnsetValue"/>. Default is <c>default(TResult)</c>.
	/// </value>
	public TResult UnsetValue { get; set; } = default!;

	object? IValueConverter.Convert(object? value, Type targetType, object parameter, CultureInfo culture)
	{
		if (typeof(TParameter) != typeof(object) && !(parameter is TParameter)) throw new NotSupportedException($"The parameter must be of type '{typeof(TParameter).GetSignature()}'");
		// if allow unset value
		if (AllowUnsetValue && value == DependencyProperty.UnsetValue) return UnsetValue;
		if (value == DependencyProperty.UnsetValue)
			throw new NotSupportedException($"The value is DependencyProperty.UnsetValue. To support unset values use '{nameof(AllowUnsetValue)}' property of the '{GetType().Name}' class");
		// value must be TSource, call Convert
		// value is null and TSource is nullable, call Convert
		if (value is TSource || value == null && typeof(TSource).CanBeNull(valueTypesAreNotNullables)) return Convert((TSource)value!, (TParameter)parameter, culture);
		// value is null and TResult is nullable, return null
		if (value == null && typeof(TResult).CanBeNull(valueTypesAreNotNullables)) return null;
		// In any other case, value is not supported exception
		throw new NotSupportedException($"The value '{value}' is not supported for '{GetType().Name}.{nameof(Convert)}' method, must be of type '{typeof(TSource).GetSignature()}'");
	}

	object? IValueConverter.ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
	{
		if (typeof(TParameter) != typeof(object) && !(parameter is TParameter)) throw new NotSupportedException($"The parameter must be of type '{typeof(TParameter).GetSignature()}'");
		// value must be TSource, call ConvertBack
		// value is null and TResult is nullable, call ConvertBack
		if (value is TResult || value == null && typeof(TResult).CanBeNull(valueTypesAreNotNullables)) return ConvertBack((TResult)value!, (TParameter)parameter, culture);
		// value is null and Tsource is nullable, return null
		if (value == null && typeof(TSource).CanBeNull(valueTypesAreNotNullables)) return null;
		// In any other case, value is not supported exception
		throw new NotSupportedException($"The value '{value}' is not supported for '{GetType().Name}.{nameof(ConvertBack)}' method, must be of type '{typeof(TResult).GetSignature()}'");
	}

	/// <summary>
	///    Converts a value from <typeparamref name="TSource"/> to <typeparamref name="TResult"/>
	///    using the specified parameter.
	/// </summary>
	/// <param name="source">The source value to convert.</param>
	/// <param name="parameter">The converter parameter of type <typeparamref name="TParameter"/>.</param>
	/// <param name="culture">The culture to use in the converter.</param>
	/// <returns>The converted value of type <typeparamref name="TResult"/>.</returns>
	/// <remarks>
	///    Override this method to implement the parameterized conversion logic from source to target type.
	///    Type validation for both value and parameter, along with null handling, are performed automatically.
	/// </remarks>
	public abstract TResult Convert(TSource source, TParameter parameter, CultureInfo culture);

	/// <summary>
	///    Converts a value from <typeparamref name="TResult"/> back to <typeparamref name="TSource"/>
	///    using the specified parameter.
	/// </summary>
	/// <param name="result">The target value to convert back.</param>
	/// <param name="parameter">The converter parameter of type <typeparamref name="TParameter"/>.</param>
	/// <param name="culture">The culture to use in the converter.</param>
	/// <returns>The converted value of type <typeparamref name="TSource"/>.</returns>
	/// <exception cref="NotSupportedException">
	///    Thrown by default implementation. Override this method to support two-way binding.
	/// </exception>
	/// <remarks>
	///    Override this method to implement reverse conversion logic for two-way binding scenarios.
	///    The default implementation throws <see cref="NotSupportedException"/>.
	/// </remarks>
	public virtual TSource ConvertBack(TResult result, TParameter parameter, CultureInfo culture) =>
		throw new NotSupportedException($"'{GetType().GetMethod(nameof(ConvertBack))?.GetSignature(includeReturn: true)}' method, is not supported");
}