using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Markup;
using Fuxion.Reflection;

namespace Fuxion.Windows.Data;

/// <summary>
///    A value converter that chains multiple <see cref="IValueConverter"/> instances in a pipeline,
///    passing the output of each converter as the input to the next.
/// </summary>
/// <remarks>
///    <para>
///       This converter allows you to compose complex value transformations by connecting multiple simple
///       converters in sequence. Each converter's output becomes the next converter's input, creating a
///       data transformation pipeline. It automatically handles type inference between converters using
///       <see cref="ValueConversionAttribute"/> or <see cref="GenericConverter{TSource,TResult}"/> type information.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Converter chaining:</strong> Combine multiple converters into a single conversion pipeline
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Automatic type inference:</strong> Determines intermediate types from converter metadata
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Parameter modes:</strong> Support for shared or individual parameters per converter
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>DoNothing support:</strong> Respects <see cref="Binding.DoNothing"/> to terminate pipeline
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Content property:</strong> Simplified XAML syntax with <see cref="ContentPropertyAttribute"/>
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Important notes:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             Converters must be decorated with <see cref="ValueConversionAttribute"/> or inherit from
///             <see cref="GenericConverter{TSource,TResult}"/> for type inference to work
///          </description>
///       </item>
///       <item>
///          <description>
///             <see cref="ConvertBack"/> is not implemented and will throw <see cref="NotImplementedException"/>
///          </description>
///       </item>
///       <item>
///          <description>
///             Parameter handling depends on <see cref="ParameterMode"/> setting
///          </description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Simple pipeline: Enum → NamedEnumValue → String:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:PipeConverter x:Key="EnumToStringPipe">
///         <data:EnumToNamedEnumValueConverter/>
///         <data:NamedEnumValueToStringConverter/>
///     </data:PipeConverter>
/// </Window.Resources>
/// 
/// <TextBlock Text="{Binding Status, Converter={StaticResource EnumToStringPipe}}"/>
/// ]]></code>
///    <strong>Multi-step conversion with visibility:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:PipeConverter x:Key="NullToVisibilityPipe">
///         <data:NullToBooleanConverter/>
///         <data:BooleanToVisibilityConverter/>
///     </data:PipeConverter>
/// </Window.Resources>
/// 
/// <!-- Shows element when object is not null -->
/// <Border Visibility="{Binding CurrentUser, Converter={StaticResource NullToVisibilityPipe}}"/>
/// ]]></code>
///    <strong>Using individual parameters:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:PipeConverter x:Key="FormattingPipe"
///                         ParameterMode="Individual"
///                         ParameterSeparator="|">
///         <data:NumberToStringConverter/>
///         <data:StringToUpperConverter/>
///     </data:PipeConverter>
/// </Window.Resources>
/// 
/// <!-- Parameter "N2|True" splits to "N2" for first converter, "True" for second -->
/// <TextBlock Text="{Binding Value, 
///                          Converter={StaticResource FormattingPipe},
///                          ConverterParameter='N2|True'}"/>
/// ]]></code>
/// </example>
[ContentProperty(nameof(Converters))]
public class PipeConverter : IValueConverter
{
	/// <summary>
	///    Gets or sets the parameter mode determining how converter parameters are distributed.
	/// </summary>
	/// <value>
	///    A <see cref="PipeConverterParameterMode"/> value. Default is <see cref="PipeConverterParameterMode.AllSame"/>.
	/// </value>
	/// <remarks>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             <see cref="PipeConverterParameterMode.AllSame"/>: All converters receive the same parameter
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <see cref="PipeConverterParameterMode.Individual"/>: Parameter string is split by
	///             <see cref="ParameterSeparator"/> and distributed to converters by index
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	public PipeConverterParameterMode ParameterMode { get; set; }

	/// <summary>
	///    Gets or sets the separator string used to split parameters when <see cref="ParameterMode"/>
	///    is <see cref="PipeConverterParameterMode.Individual"/>.
	/// </summary>
	/// <value>
	///    The separator string. Default is <c>"|"</c>.
	/// </value>
	public string ParameterSeparator { get; set; } = "|";

	/// <summary>
	///    Gets the collection of converters that will be executed in sequence.
	/// </summary>
	/// <value>
	///    An <see cref="ObservableCollection{T}"/> of <see cref="IValueConverter"/> instances.
	/// </value>
	/// <remarks>
	///    Converters are executed in the order they appear in this collection.
	///    This property is the content property for XAML, allowing simplified syntax.
	/// </remarks>
	public ObservableCollection<IValueConverter> Converters { get; } = new();

	/// <summary>
	///    Converts a value by passing it through each converter in the pipeline sequentially.
	/// </summary>
	/// <param name="value">The initial value to convert.</param>
	/// <param name="targetType">The final target type (used for the last converter).</param>
	/// <param name="parameter">
	///    The converter parameter(s). Interpretation depends on <see cref="ParameterMode"/>.
	/// </param>
	/// <param name="culture">The culture to use in the conversion.</param>
	/// <returns>
	///    The result of the final converter in the pipeline, or <see cref="Binding.DoNothing"/>
	///    if any converter returns that value.
	/// </returns>
	/// <remarks>
	///    <para>
	///       The conversion process:
	///    </para>
	///    <list type="number">
	///       <item>
	///          <description>Starts with the input value</description>
	///       </item>
	///       <item>
	///          <description>For each converter, determines the target type for that converter</description>
	///       </item>
	///       <item>
	///          <description>Calls the converter with appropriate parameters</description>
	///       </item>
	///       <item>
	///          <description>Uses the output as input for the next converter</description>
	///       </item>
	///       <item>
	///          <description>Returns the final output or <see cref="Binding.DoNothing"/> if pipeline terminates</description>
	///       </item>
	///    </list>
	/// </remarks>
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var output = value;
		for (var i = 0; i < Converters.Count; ++i)
		{
			var target = i == Converters.Count - 1 ? targetType : GetConverterTypes(Converters[i + 1]).SourceType;
			output = Converters[i].Convert(output, target, GetConverterParameter(Converters[i], parameter), culture);

			// If the converter returns 'DoNothing' then the binding operation should terminate.
			if (output == Binding.DoNothing) break;
		}
		return output;
	}

	/// <summary>
	///    Not implemented. This converter does not support backward conversion.
	/// </summary>
	/// <exception cref="NotImplementedException">Always thrown.</exception>
	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();

	/// <summary>
	///    Gets the source, target, and parameter types for a converter by inspecting its metadata.
	/// </summary>
	/// <param name="converter">The converter to inspect.</param>
	/// <returns>
	///    A tuple containing the source type, target type, and parameter type.
	///    Any component may be <c>null</c> if type information cannot be determined.
	/// </returns>
	/// <remarks>
	///    <para>
	///       Type information is obtained in the following order of precedence:
	///    </para>
	///    <list type="number">
	///       <item>
	///          <description>
	///             <see cref="ValueConversionAttribute"/> if present on the converter type
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             Generic type arguments from <see cref="GenericConverter{TSource,TResult}"/>
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             Generic type arguments from <see cref="GenericConverter{TSource,TResult,TParameter}"/>
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             Returns <c>(null, null, null)</c> if no type information is available
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	public (Type? SourceType, Type? TargetType, Type? ParameterType) GetConverterTypes(IValueConverter converter)
	{
		var att = converter.GetType().GetCustomAttribute<ValueConversionAttribute>(true, false);
		if (att != null) return (att.SourceType, att.TargetType, att.ParameterType);
		if (converter.GetType().IsSubclassOfGenericDefinition(typeof(GenericConverter<,>)))
		{
			var args = converter.GetType().GetSubclassOfGenericDefinition(typeof(GenericConverter<,>))!.GetGenericArguments();
			return (args[0], args[1], null);
		}
		if (converter.GetType().IsSubclassOfGenericDefinition(typeof(GenericConverter<,,>)))
		{
			var args = converter.GetType().GetSubclassOfGenericDefinition(typeof(GenericConverter<,,>))!.GetGenericArguments();
			return (args[0], args[1], args[2]);
		}
		return (null, null, null);
	}

	/// <summary>
	///    Gets the parameter to pass to a specific converter based on <see cref="ParameterMode"/>.
	/// </summary>
	/// <param name="converter">The converter that will receive the parameter.</param>
	/// <param name="parameter">The original parameter value from the binding.</param>
	/// <returns>
	///    The parameter to pass to the converter, which may be the original parameter,
	///    a split portion of it, or a type-converted value.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">
	///    Thrown when <see cref="ParameterMode"/> is <see cref="PipeConverterParameterMode.Individual"/>
	///    and the parameter string doesn't contain enough separated values for all converters.
	/// </exception>
	/// <remarks>
	///    <para>
	///       Parameter handling logic:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             If <see cref="ParameterMode"/> is <see cref="PipeConverterParameterMode.AllSame"/> or parameter is null,
	///             returns the parameter unchanged
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             If <see cref="ParameterMode"/> is <see cref="PipeConverterParameterMode.Individual"/>,
	///             splits the parameter string and selects the portion for this converter by index
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             If the converter has a parameter type, attempts to convert the string value to that type
	///             using <see cref="TypeDescriptor"/>
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	public object? GetConverterParameter(IValueConverter converter, object? parameter)
	{
		if (ParameterMode == PipeConverterParameterMode.AllSame || parameter == null) return parameter;
		var pars = parameter?.ToString()?.Split(new[] {
			ParameterSeparator
		}, StringSplitOptions.None) ?? new string[]
			{ };
		var index = Converters.IndexOf(converter);
		if (pars.Length <= index) throw new ArgumentOutOfRangeException("PipeConverter parameter was not define properly, has less parameters than converters");
		var val = pars[index];
		var parType = GetConverterTypes(converter).ParameterType;
		if (parType == null) return val;
		return TypeDescriptor.GetConverter(parType).ConvertFrom(val);
	}
}

/// <summary>
///    Specifies how parameters are distributed to converters in a <see cref="PipeConverter"/>.
/// </summary>
public enum PipeConverterParameterMode
{
	/// <summary>
	///    All converters receive the same parameter value.
	/// </summary>
	AllSame,

	/// <summary>
	///    The parameter string is split and distributed to converters individually by index.
	/// </summary>
	Individual
}

/// <summary>
///    A value converter which contains a list of <see cref="IValueConverter"/> instances and invokes their
///    <see cref="IValueConverter.Convert"/> or <see cref="IValueConverter.ConvertBack"/> methods in sequence,
///    creating a modular conversion pipeline.
/// </summary>
/// <remarks>
///    <para>
///       This converter allows you to compose complex value transformations by chaining multiple simple
///       converters together. The output of one converter is piped into the next converter, allowing for
///       modular and reusable conversion logic. When <see cref="IValueConverter.ConvertBack"/> is invoked,
///       the converters are executed in reverse order (highest to lowest index).
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Bidirectional conversion:</strong> Supports both <see cref="IValueConverter.Convert"/> and
///             <see cref="IValueConverter.ConvertBack"/> in forward and reverse order
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Automatic type inference:</strong> Uses <see cref="ValueConversionAttribute"/> to determine
///             intermediate types between converters
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>DoNothing support:</strong> Respects <see cref="Binding.DoNothing"/> to terminate pipeline
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Dynamic collection:</strong> Validates converters as they are added to the collection
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Content property:</strong> Simplified XAML syntax with <see cref="ContentPropertyAttribute"/>
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Important requirements:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             All converters MUST be decorated with <see cref="ValueConversionAttribute"/> exactly once
///          </description>
///       </item>
///       <item>
///          <description>
///             No elements in the <see cref="Converters"/> collection can be <c>null</c>
///          </description>
///       </item>
///       <item>
///          <description>
///             The attribute is validated when converters are added to the collection
///          </description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Basic two-step conversion:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:ValueConverterGroup x:Key="EnumToStringPipe">
///         <data:EnumToNamedEnumValueConverter/>
///         <data:NamedEnumValueToStringConverter/>
///     </data:ValueConverterGroup>
/// </Window.Resources>
/// 
/// <TextBlock Text="{Binding Status, Converter={StaticResource EnumToStringPipe}}"/>
/// ]]></code>
///    <strong>Three-step conversion with two-way binding:</strong>
///    <code><![CDATA[
/// <Window.Resources>
///     <data:ValueConverterGroup x:Key="ComplexPipe">
///         <data:StringToIntConverter/>
///         <data:IntToDoubleConverter/>
///         <data:DoubleToStringConverter/>
///     </data:ValueConverterGroup>
/// </Window.Resources>
/// 
/// <!-- Supports two-way binding if all converters support ConvertBack -->
/// <TextBox Text="{Binding Value, 
///                        Converter={StaticResource ComplexPipe}, 
///                        Mode=TwoWay}"/>
/// ]]></code>
/// </example>
[ContentProperty("Converters")]
public class ValueConverterGroup : IValueConverter
{
	/// <summary>
	///    Initializes a new instance of the <see cref="ValueConverterGroup"/> class.
	/// </summary>
	public ValueConverterGroup() => Converters.CollectionChanged += OnConvertersCollectionChanged;

	readonly Dictionary<IValueConverter, ValueConversionAttribute> cachedAttributes = new();

	/// <summary>
	///    Gets the collection of converters that will be executed in sequence.
	/// </summary>
	/// <value>
	///    An <see cref="ObservableCollection{T}"/> of <see cref="IValueConverter"/> instances.
	/// </value>
	/// <remarks>
	///    <para>
	///       Converters are executed in forward order during <see cref="IValueConverter.Convert"/>
	///       and in reverse order during <see cref="IValueConverter.ConvertBack"/>.
	///    </para>
	///    <para>
	///       When converters are added to this collection, they are validated to ensure they are
	///       decorated with <see cref="ValueConversionAttribute"/>. An <see cref="InvalidOperationException"/>
	///       is thrown if validation fails.
	///    </para>
	/// </remarks>
	public ObservableCollection<IValueConverter> Converters { get; } = new();

	object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var output = value;
		for (var i = 0; i < Converters.Count; ++i)
		{
			var converter = Converters[i];
			var currentTargetType = GetTargetType(i, targetType, true);
			output = converter.Convert(output, currentTargetType, parameter, culture);

			// If the converter returns 'DoNothing' then the binding operation should terminate.
			if (output == Binding.DoNothing) break;
		}
		return output;
	}

	object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var output = value;
		for (var i = Converters.Count - 1; i > -1; --i)
		{
			var converter = Converters[i];
			var currentTargetType = GetTargetType(i, targetType, false);
			output = converter.ConvertBack(output, currentTargetType, parameter, culture);

			// When a converter returns 'DoNothing' the binding operation should terminate.
			if (output == Binding.DoNothing) break;
		}
		return output;
	}

	/// <summary>
	///    Gets the target type to pass to a converter at a specific index in the pipeline.
	/// </summary>
	/// <param name="converterIndex">The zero-based index of the converter.</param>
	/// <param name="finalTargetType">The final target type of the entire conversion.</param>
	/// <param name="convert">
	///    <c>true</c> if getting the type for <see cref="IValueConverter.Convert"/>;
	///    <c>false</c> for <see cref="IValueConverter.ConvertBack"/>.
	/// </param>
	/// <returns>
	///    The target type for the converter at the specified index, or <paramref name="finalTargetType"/>
	///    if this is the last/first converter.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	///    Thrown if the <see cref="Converters"/> collection contains a <c>null</c> reference.
	/// </exception>
	/// <remarks>
	///    <para>
	///       Type determination logic:
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             For <see cref="IValueConverter.Convert"/>: Returns the <see cref="ValueConversionAttribute.SourceType"/>
	///             of the next converter, or <paramref name="finalTargetType"/> if this is the last converter
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             For <see cref="IValueConverter.ConvertBack"/>: Returns the <see cref="ValueConversionAttribute.TargetType"/>
	///             of the previous converter, or <paramref name="finalTargetType"/> if this is the first converter
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	protected virtual Type GetTargetType(int converterIndex, Type finalTargetType, bool convert)
	{
		// If the current converter is not the last/first in the list, 
		// get a reference to the next/previous converter.
		IValueConverter? nextConverter = null;
		if (convert)
		{
			if (converterIndex < Converters.Count - 1)
			{
				nextConverter = Converters[converterIndex + 1];
				if (nextConverter == null) throw new InvalidOperationException("The Converters collection of the ValueConverterGroup contains a null reference at index: " + (converterIndex + 1));
			}
		} else
		{
			if (converterIndex > 0)
			{
				nextConverter = Converters[converterIndex - 1];
				if (nextConverter == null) throw new InvalidOperationException("The Converters collection of the ValueConverterGroup contains a null reference at index: " + (converterIndex - 1));
			}
		}
		if (nextConverter != null)
		{
			var conversionAttribute = cachedAttributes[nextConverter];

			// If the Convert method is going to be called, we need to use the SourceType of the next 
			// converter in the list.  If ConvertBack is called, use the TargetType.
			return convert ? conversionAttribute.SourceType : conversionAttribute.TargetType;
		}

		// If the current converter is the last one to be executed return the target type passed into the conversion method.
		return finalTargetType;
	}

	void OnConvertersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		// The 'Converters' collection has been modified, so validate that each value converter it now
		// contains is decorated with ValueConversionAttribute and then cache the attribute value.
		IList convertersToProcess = new List<object>();
		if (e.NewItems is not null && (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace))
			convertersToProcess = e.NewItems;
		else if (e.OldItems is not null && e.Action == NotifyCollectionChangedAction.Remove)
			foreach (IValueConverter? converter in e.OldItems)
			{
				if (converter != null) cachedAttributes.Remove(converter);
			}
		else if (e.Action == NotifyCollectionChangedAction.Reset)
		{
			cachedAttributes.Clear();
			convertersToProcess = Converters;
		}
		foreach (IValueConverter? converter in convertersToProcess)
			if (converter != null)
			{
				var attributes = converter.GetType().GetCustomAttributes<ValueConversionAttribute>(false).ToList();
				if (attributes.Count != 1)
					throw new InvalidOperationException("All value converters added to a ValueConverterGroup must be decorated with the ValueConversionAttribute attribute exactly once.");
				cachedAttributes.Add(converter, attributes[0]);
			}
	}
}