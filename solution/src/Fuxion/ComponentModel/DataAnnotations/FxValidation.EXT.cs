using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using Fuxion.Collections.Generic;

namespace Fuxion.ComponentModel.DataAnnotations;

/// <summary>
///    Provides extension methods related to component metadata and data annotation validation.
/// </summary>
public static class DataAnnotationsExtensions
{
	extension(PropertyDescriptor me)
	{
		/// <summary>
		///    Gets the display name for a property, with priority given to the <see cref="DisplayAttribute" /> if present.
		/// </summary>
		/// <returns>
		///    The display name from the <see cref="DisplayAttribute" /> if present;
		///    otherwise, the value from <see cref="PropertyDescriptor" />.DisplayName.
		/// </returns>
		/// <remarks>
		///    <para>
		///       This method follows a fallback strategy for determining the display name:
		///    </para>
		///    <list type="number">
		///       <item>
		///          <description>First, checks for a <see cref="DisplayAttribute" /> and uses its Name property</description>
		///       </item>
		///       <item>
		///          <description>
		///             If no <see cref="DisplayAttribute" /> is found, returns the default
		///             PropertyDescriptor.DisplayName
		///          </description>
		///       </item>
		///    </list>
		///    <para>
		///       This is particularly useful when working with data-driven UI scenarios where property metadata
		///       is used for labels, headers, or other display purposes, and you want to support both
		///       <see cref="DisplayAttribute" /> (from System.ComponentModel.DataAnnotations) and
		///       <see cref="DisplayNameAttribute" /> (from System.ComponentModel).
		///    </para>
		/// </remarks>
		/// <example>
		///    <code>
		/// public class Person
		/// {
		///     [Display(Name = "Full Name")]
		///     public string Name { get; set; }
		///     
		///     [DisplayName("Years Old")]
		///     public int Age { get; set; }
		///     
		///     public string Email { get; set; }
		/// }
		/// 
		/// // Using PropertyDescriptor
		/// var properties = TypeDescriptor.GetProperties(typeof(Person));
		/// 
		/// var nameDescriptor = properties["Name"];
		/// var displayName1 = nameDescriptor.GetDisplayName(); // Returns: "Full Name" (from DisplayAttribute)
		/// 
		/// var ageDescriptor = properties["Age"];
		/// var displayName2 = ageDescriptor.GetDisplayName(); // Returns: "Years Old" (from DisplayNameAttribute)
		/// 
		/// var emailDescriptor = properties["Email"];
		/// var displayName3 = emailDescriptor.GetDisplayName(); // Returns: "Email" (default PropertyDescriptor.DisplayName)
		/// 
		/// // Real-world usage: generating UI labels
		/// foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(typeof(Person)))
		/// {
		///     var label = prop.GetDisplayName();
		///     Console.WriteLine($"Label for {prop.Name}: {label}");
		/// }
		/// </code>
		/// </example>
		public string GetDisplayName()
		{
			var att = me.Attributes.OfType<DisplayAttribute>().FirstOrDefault();
			return att?.GetName() ?? me.DisplayName;
		}
	}

	extension<T>(FuxionExtensions<T?> me)
	{
		/// <summary>
		///    Provides validation extension operations for this value.
		/// </summary>
		public ValidationExtensions<T?> Validation
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me.Value);
		}
	}
	const string ValidationResultsKey = "validation-results";

	extension(ResponseExtensionsDictionary me)
	{
		/// <summary>
      /// Gets or sets the validation results stored in the response extensions dictionary.
		/// </summary>
      /// <value>
		/// A <see cref="Undefinable{T}"/> containing the list of <see cref="ValidationResult"/> items associated with the response,
		/// or <see cref="Undefinable{T}.Undefined"/> when no validation results are present.
		/// </value>
		/// <remarks>
		/// This property provides typed access to the extension entry identified by <c>"validation-results"</c>.
		/// Setting the value to <see cref="Undefinable{T}.Undefined"/> removes the entry from the dictionary.
		/// </remarks>
		/// <example>
		/// <code>
		/// var extensions = new ResponseExtensionsDictionary();
		/// extensions.ValidationResults = new List&lt;ValidationResult&gt;
		/// {
		///     new("Email is required", new[] { "Email" })
		/// };
		/// 
		/// if (!extensions.ValidationResults.IsUndefined)
		/// {
		///     foreach (var error in extensions.ValidationResults.Value)
		///         Console.WriteLine(error.ErrorMessage);
		/// }
		/// </code>
		/// </example>
		public Undefinable<List<ValidationResult>> ValidationResults
		{
			get
				=> me.TryGetValue(ValidationResultsKey, out var val)
					? val switch
					{
						Undefinable<List<ValidationResult>> und => und,
						List<ValidationResult> res => res,
						_ => Undefinable<List<ValidationResult>>.Undefined
					}
					: Undefinable<List<ValidationResult>>.Undefined;
			set
			{
				if (value.IsUndefined)
					me.Remove(ValidationResultsKey);
				else
					me[ValidationResultsKey] = value;
			}
		}
	}

	extension<T>(ValidationExtensions<T?> me)
	{
		/// <summary>
		///    Validates the wrapped value and converts the result into a <see cref="IResponse" />.
		/// </summary>
		/// <param name="nullValueIsValid">
		///    When <see langword="true" />, a <see langword="null" /> value is considered valid and produces a success response.
		///    When <see langword="false" />, a <see langword="null" /> value produces an <see cref="ErrorType.InvalidData" />
		///    response.
		/// </param>
		/// <returns>
		///    A success response when validation passes; otherwise, an invalid-data response containing the aggregated
		///    validation error messages and the collected <see cref="ValidationResult" /> items.
		/// </returns>
		/// <remarks>
		///    This method uses
		///    <see
		///       cref="Validator.TryValidateObject(object, ValidationContext, System.Collections.Generic.ICollection{System.ComponentModel.DataAnnotations.ValidationResult}, bool)" />
		///    with <c>validateAllProperties</c> set to <see langword="true" />.
		/// </remarks>
		/// <example>
		///    <code>
		/// var person = new Person { Name = null };
		/// var response = person.Fx.Validation.ToResponse();
		/// 
		/// if (response.IsError)
		///     Console.WriteLine(response.Message);
		/// </code>
		/// </example>
		public IResponse ToResponse(bool nullValueIsValid = false)
		{
			if (me.Value is null)
				return nullValueIsValid
					? ResponseExt.Get.Success()
					: ResponseExt.Get.InvalidData(
						"Value is null",
						extensions: new ResponseExtensionsDictionary()
						{
							ValidationResults = new List<ValidationResult>([new("Value is null")])
						}.ToEnumerable());

			List<ValidationResult> validation = [];
			Validator.TryValidateObject(me.Value, new(me.Value), validation, true);

			return validation.IsNullOrEmpty()
				? ResponseExt.Get.Success()
				: ResponseExt.Get.InvalidData(
					string.Join("\r\n", validation.Select(v => v.ErrorMessage)),
					extensions: new ResponseExtensionsDictionary()
					{
						ValidationResults = validation
					}.ToEnumerable());
		}
	}
}

/// <summary>
///    Wraps a value to expose validation operations through the Fuxion fluent API.
/// </summary>
/// <typeparam name="T">The type of the wrapped value.</typeparam>
/// <param name="me">The value to wrap for validation operations.</param>
/// <remarks>
///    This wrapper enables the fluent syntax <c>value.Fx.Validation</c>, from which validation-oriented
///    extension methods such as <c>ToResponse()</c> can be invoked.
/// </remarks>
/// <example>
///    <code>
/// var person = new Person();
/// var response = person.Fx.Validation.ToResponse();
/// </code>
/// </example>
public class ValidationExtensions<T>(T me) : Extensions<T>(me);