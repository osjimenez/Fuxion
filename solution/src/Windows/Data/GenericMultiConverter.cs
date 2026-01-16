using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using Fuxion.Reflection;

namespace Fuxion.Windows.Data;

/// <summary>
///    Base class for strongly-typed WPF multi-value converters that convert multiple <typeparamref name="TSource"/>
///    values to a single <typeparamref name="TResult"/> value.
/// </summary>
/// <typeparam name="TSource">The type of each source value in the binding.</typeparam>
/// <typeparam name="TResult">The type of the target value in the binding.</typeparam>
/// <remarks>
///    <para>
///       This abstract class provides a strongly-typed wrapper around <see cref="IMultiValueConverter"/>,
///       eliminating the need for manual type checking and casting in derived converters. It automatically
///       handles type validation, null values, and <see cref="DependencyProperty.UnsetValue"/> scenarios
///       for multiple input values.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Type safety:</strong> Automatic type validation for all input values with compile-time checking
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>UnsetValue handling:</strong> Configurable handling of <see cref="DependencyProperty.UnsetValue"/> in inputs
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Flexible UnsetValue filtering:</strong> Option to ignore or handle unset values
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Null handling:</strong> Automatic null value support for nullable types
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Multi-binding support:</strong> Works with <see cref="System.Windows.Data.MultiBinding"/> in XAML
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Bidirectional conversion:</strong> Support for both <see cref="Convert"/> and <see cref="ConvertBack"/>
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Implementation requirements:</strong>
///    </para>
///    <list type="number">
///       <item>
///          <description>Override <see cref="Convert(TSource[], CultureInfo)"/> with conversion logic</description>
///       </item>
///       <item>
///          <description>
///             Optionally override <see cref="ConvertBack(TResult, CultureInfo)"/> for two-way binding
///          </description>
///       </item>
///       <item>
///          <description>Configure <see cref="AllowUnsetValues"/> and <see cref="IgnoreUnsetValues"/> as needed</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Simple multi-value converter:</strong>
///    <code>
/// public class MultiStringConcatenator : GenericMultiConverter&lt;string, string&gt;
/// {
///     public string Separator { get; set; } = " ";
///     
///     public override string Convert(string[] source, CultureInfo culture)
///     {
///         return string.Join(Separator, source);
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <!-- XAML usage -->
/// <TextBlock>
///     <TextBlock.Text>
///         <MultiBinding Converter="{StaticResource StringConcatenator}">
///             <Binding Path="FirstName"/>
///             <Binding Path="LastName"/>
///         </MultiBinding>
///     </TextBlock.Text>
/// </TextBlock>
/// ]]></code>
/// </example>
public abstract class GenericMultiConverter<TSource, TResult> : IMultiValueConverter
{
	/// <summary>
	///    Initializes a new instance of the <see cref="GenericMultiConverter{TSource,TResult}"/> class.
	/// </summary>
	public GenericMultiConverter() { }

	/// <summary>
	///    Initializes a new instance of the <see cref="GenericMultiConverter{TSource,TResult}"/> class
	///    with configurable nullable value type handling.
	/// </summary>
	/// <param name="includeNullableValueTypes">
	///    <c>true</c> to treat nullable value types (e.g., <c>int?</c>) as nullable;
	///    <c>false</c> to treat them as non-nullable. Default is <c>true</c>.
	/// </param>
	public GenericMultiConverter(bool includeNullableValueTypes) : this() => this.includeNullableValueTypes = includeNullableValueTypes;

	readonly bool includeNullableValueTypes = true;

	/// <summary>
	///    Gets or sets a value indicating whether <see cref="DependencyProperty.UnsetValue"/> is allowed in the input array.
	/// </summary>
	/// <value>
	///    <c>true</c> to allow <see cref="DependencyProperty.UnsetValue"/> in inputs; otherwise, <c>false</c>
	///    to throw <see cref="NotSupportedException"/>. Default is <c>false</c>.
	/// </value>
	/// <remarks>
	///    When set to <c>true</c>, the behavior depends on <see cref="IgnoreUnsetValues"/>:
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             If <see cref="IgnoreUnsetValues"/> is <c>true</c>, unset values are filtered out
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             If <see cref="IgnoreUnsetValues"/> is <c>false</c>, returns <see cref="UnsetValue"/>
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	public bool AllowUnsetValues { get; set; }

	/// <summary>
	///    Gets or sets a value indicating whether <see cref="DependencyProperty.UnsetValue"/> items
	///    should be filtered out from the input array.
	/// </summary>
	/// <value>
	///    <c>true</c> to remove unset values from the array before conversion; <c>false</c> to return
	///    <see cref="UnsetValue"/> when any input is unset. Default is <c>true</c>.
	/// </value>
	/// <remarks>
	///    This property only has an effect when <see cref="AllowUnsetValues"/> is <c>true</c>.
	/// </remarks>
	public bool IgnoreUnsetValues { get; set; } = true;

	/// <summary>
	///    Gets or sets the value to return when <see cref="DependencyProperty.UnsetValue"/> is present
	///    in inputs and <see cref="IgnoreUnsetValues"/> is <c>false</c>.
	/// </summary>
	/// <value>
	///    The value to return for unset inputs. Default is <c>default(TResult)</c>.
	/// </value>
	public TResult UnsetValue { get; set; } = default!;

	object? IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		// if allow any unset value
		if (AllowUnsetValues)
		{
			// if unset values must be ignored, remove it from values
			if (IgnoreUnsetValues)
				values = values.Where(v => v != DependencyProperty.UnsetValue).ToArray();
			else if (values.Any(v => v == DependencyProperty.UnsetValue)) return UnsetValue;
		} else
		{
			if (values.Any(v => v == DependencyProperty.UnsetValue))
				throw new NotSupportedException($"Some values are DependencyProperty.UnsetValue. To support unset values use '{nameof(AllowUnsetValues)}' property of the '{GetType().Name}' class");
		}
		// value must be TSource, call Convert
		// value is null and TSource is nullable, call Convert
		if (values.All(value => value is TSource || value == null && typeof(TSource).CanBeNull(includeNullableValueTypes))) return Convert(values.Cast<TSource>().ToArray(), culture);
		// In any other case, value is not supported exception
		throw new NotSupportedException(
			$"The values '{values.Aggregate("", (a, c) => a + ", " + c, a => a.Trim(',', ' '))}' are not supported for '{GetType().Name}.{nameof(Convert)}' method, all must be of type '{typeof(TSource).Name}'");
	}

	object[]? IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		// value must be TSource, call ConvertBack
		// value is null and TResult is nullable, call ConvertBack
		if (value is TResult || value == null && typeof(TResult).CanBeNull(includeNullableValueTypes)) return ConvertBack((TResult)value!, culture).Cast<object>().ToArray();
		// value is null and Tsource is nullable, return null
		if (value == null && typeof(TSource).CanBeNull(includeNullableValueTypes)) return null;
		// In any other case, value is not supported exception
		throw new NotSupportedException($"The value '{value}' is not supported for '{GetType().Name}.{nameof(ConvertBack)}' method, must be of type '{typeof(TResult).Name}'");
	}

	/// <summary>
	///    Converts an array of <typeparamref name="TSource"/> values to a single <typeparamref name="TResult"/> value.
	/// </summary>
	/// <param name="source">The array of source values to convert.</param>
	/// <param name="culture">The culture to use in the converter.</param>
	/// <returns>The converted value of type <typeparamref name="TResult"/>.</returns>
	/// <remarks>
	///    Override this method to implement the multi-value conversion logic. Type validation and
	///    UnsetValue handling are performed automatically by the base class before this method is called.
	/// </remarks>
	public abstract TResult Convert(TSource[] source, CultureInfo culture);

	/// <summary>
	///    Converts a single <typeparamref name="TResult"/> value back to an array of <typeparamref name="TSource"/> values.
	/// </summary>
	/// <param name="result">The target value to convert back.</param>
	/// <param name="culture">The culture to use in the converter.</param>
	/// <returns>An array of <typeparamref name="TSource"/> values.</returns>
	/// <exception cref="NotSupportedException">
	///    Thrown by default implementation. Override this method to support two-way binding.
	/// </exception>
	/// <remarks>
	///    Override this method to implement reverse conversion logic for two-way binding scenarios.
	///    The default implementation throws <see cref="NotSupportedException"/>.
	/// </remarks>
	public virtual TSource[] ConvertBack(TResult result, CultureInfo culture) =>
		throw new NotSupportedException($"'{GetType().GetMethod(nameof(ConvertBack))?.GetSignature(includeReturn: true)}' method, is not supported");
}

/// <summary>
///    Base class for strongly-typed WPF multi-value converters that convert multiple <typeparamref name="TSource"/>
///    values to a single <typeparamref name="TResult"/> value with a converter parameter of type <typeparamref name="TParameter"/>.
/// </summary>
/// <typeparam name="TSource">The type of each source value in the binding.</typeparam>
/// <typeparam name="TResult">The type of the target value in the binding.</typeparam>
/// <typeparam name="TParameter">The type of the converter parameter.</typeparam>
/// <remarks>
///    <para>
///       This abstract class extends <see cref="GenericMultiConverter{TSource,TResult}"/> by adding support for
///       a strongly-typed converter parameter. This is useful when the multi-value conversion logic depends on
///       additional configuration provided via the <c>ConverterParameter</c> in XAML multi-bindings.
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
///             <strong>All base features:</strong> Inherits UnsetValue handling, null support, and type safety
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
///             Override <see cref="Convert(TSource[], TParameter, CultureInfo)"/> with parameterized conversion logic
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
///    <strong>Multi-value converter with parameter:</strong>
///    <code>
/// public class MultiNumberFormatter : GenericMultiConverter&lt;double, string, string&gt;
/// {
///     public override string Convert(double[] source, string format, CultureInfo culture)
///     {
///         return string.Join(", ", source.Select(n => n.ToString(format, culture)));
///     }
/// }
/// </code>
///    <code><![CDATA[
/// <!-- XAML usage -->
/// <TextBlock>
///     <TextBlock.Text>
///         <MultiBinding Converter="{StaticResource MultiNumberFormatter}"
///                       ConverterParameter="N2">
///             <Binding Path="Value1"/>
///             <Binding Path="Value2"/>
///             <Binding Path="Value3"/>
///         </MultiBinding>
///     </TextBlock.Text>
/// </TextBlock>
/// ]]></code>
/// </example>
public abstract class GenericMultiConverter<TSource, TResult, TParameter> : IMultiValueConverter
{
	/// <summary>
	///    Initializes a new instance of the <see cref="GenericMultiConverter{TSource,TResult,TParameter}"/> class.
	/// </summary>
	public GenericMultiConverter() { }

	/// <summary>
	///    Initializes a new instance of the <see cref="GenericMultiConverter{TSource,TResult,TParameter}"/> class
	///    with configurable nullable value type handling.
	/// </summary>
	/// <param name="valueTypesAreNotNullables">
	///    <c>true</c> to treat nullable value types as non-nullable;
	///    <c>false</c> to treat them as nullable. Default is <c>true</c>.
	/// </param>
	public GenericMultiConverter(bool valueTypesAreNotNullables) : this() => this.valueTypesAreNotNullables = valueTypesAreNotNullables;

	readonly bool valueTypesAreNotNullables = true;

	/// <summary>
	///    Gets or sets a value indicating whether <see cref="DependencyProperty.UnsetValue"/> is allowed in the input array.
	/// </summary>
	/// <value>
	///    <c>true</c> to allow <see cref="DependencyProperty.UnsetValue"/> in inputs; otherwise, <c>false</c>
	///    to throw <see cref="NotSupportedException"/>. Default is <c>false</c>.
	/// </value>
	public bool AllowUnsetValues { get; set; }

	/// <summary>
	///    Gets or sets a value indicating whether <see cref="DependencyProperty.UnsetValue"/> items
	///    should be filtered out from the input array.
	/// </summary>
	/// <value>
	///    <c>true</c> to remove unset values from the array before conversion; <c>false</c> to return
	///    <see cref="UnsetValue"/> when any input is unset. Default is <c>true</c>.
	/// </value>
	public bool IgnoreUnsetValues { get; set; } = true;

	/// <summary>
	///    Gets or sets the value to return when <see cref="DependencyProperty.UnsetValue"/> is present
	///    in inputs and <see cref="IgnoreUnsetValues"/> is <c>false</c>.
	/// </summary>
	/// <value>
	///    The value to return for unset inputs. Default is <c>default(TResult)</c>.
	/// </value>
	public TResult UnsetValue { get; set; } = default!;

	object? IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (typeof(TParameter) != typeof(object) && !(parameter is TParameter)) throw new NotSupportedException($"The parameter must be of type '{typeof(TParameter).Name}'");
		// if allow any unset value
		if (AllowUnsetValues)
		{
			// if unset values must be ignored, remove it from values
			if (IgnoreUnsetValues)
				values = values.Where(v => v != DependencyProperty.UnsetValue).ToArray();
			else if (values.Any(v => v == DependencyProperty.UnsetValue)) return UnsetValue;
		} else
		{
			if (values.Any(v => v == DependencyProperty.UnsetValue))
				throw new NotSupportedException($"Some values are DependencyProperty.UnsetValue. To support unset values use '{nameof(AllowUnsetValues)}' property of the '{GetType().Name}' class");
		}
		// value must be TSource, call Convert
		// value is null and TSource is nullable, call Convert
		if (values.All(value => value is TSource || value == null && typeof(TSource).CanBeNull(valueTypesAreNotNullables)))
			return Convert(values.Cast<TSource>().ToArray(), (TParameter)parameter, culture);
		// In any other case, value is not supported exception
		throw new NotSupportedException(
			$"The values '{values.Aggregate("", (a, c) => a + ", " + c, a => a.Trim(',', ' '))}' are not supported for '{GetType().Name}.{nameof(Convert)}' method, all must be of type '{typeof(TSource).Name}'");
	}

	object[]? IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		if (typeof(TParameter) != typeof(object) && !(parameter is TParameter)) throw new NotSupportedException($"The parameter must be of type '{typeof(TParameter).Name}'");
		// value must be TSource, call ConvertBack
		// value is null and TResult is nullable, call ConvertBack
		if (value is TResult || value == null && typeof(TResult).CanBeNull(valueTypesAreNotNullables)) return ConvertBack((TResult)value!, (TParameter)parameter, culture).Cast<object>().ToArray();
		// value is null and Tsource is nullable, return null
		if (value == null && typeof(TSource).CanBeNull(valueTypesAreNotNullables)) return null;
		// In any other case, value is not supported exception
		throw new NotSupportedException($"The value '{value}' is not supported for '{GetType().Name}.{nameof(ConvertBack)}' method, must be of type '{typeof(TResult).Name}'");
	}

	/// <summary>
	///    Converts an array of <typeparamref name="TSource"/> values to a single <typeparamref name="TResult"/> value
	///    using the specified parameter.
	/// </summary>
	/// <param name="source">The array of source values to convert.</param>
	/// <param name="parameter">The converter parameter of type <typeparamref name="TParameter"/>.</param>
	/// <param name="culture">The culture to use in the converter.</param>
	/// <returns>The converted value of type <typeparamref name="TResult"/>.</returns>
	/// <remarks>
	///    Override this method to implement the parameterized multi-value conversion logic.
	///    Type validation for both values and parameter, along with UnsetValue handling,
	///    are performed automatically.
	/// </remarks>
	public abstract TResult Convert(TSource[] source, TParameter parameter, CultureInfo culture);

	/// <summary>
	///    Converts a single <typeparamref name="TResult"/> value back to an array of <typeparamref name="TSource"/> values
	///    using the specified parameter.
	/// </summary>
	/// <param name="result">The target value to convert back.</param>
	/// <param name="parameter">The converter parameter of type <typeparamref name="TParameter"/>.</param>
	/// <param name="culture">The culture to use in the converter.</param>
	/// <returns>An array of <typeparamref name="TSource"/> values.</returns>
	/// <exception cref="NotSupportedException">
	///    Thrown by default implementation. Override this method to support two-way binding.
	/// </exception>
	/// <remarks>
	///    Override this method to implement reverse conversion logic for two-way binding scenarios.
	///    The default implementation throws <see cref="NotSupportedException"/>.
	/// </remarks>
	public virtual TSource[] ConvertBack(TResult result, TParameter parameter, CultureInfo culture) =>
		throw new NotSupportedException($"'{GetType().GetMethod(nameof(ConvertBack))?.GetSignature(includeReturn: true)}' method, is not supported");
}