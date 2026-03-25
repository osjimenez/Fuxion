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

	extension<T>(ValidationExtensions<T?> me)
	{
		/// <summary>
		///    Validates the wrapped value and converts the result into a <see cref="Response" />.
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
		public Response<List<ValidationResult>> ToResponse(bool nullValueIsValid = false)
		{
			if (me.Value is null)
				return nullValueIsValid
					? Response.Get.SuccessPayload<List<ValidationResult>>([])
					: Response.Get.InvalidData("Value is null").AsPayload<List<ValidationResult>>();

			List<ValidationResult> validation = [];
			Validator.TryValidateObject(me.Value, new(me.Value), validation, true);

			return validation.IsNullOrEmpty()
				? Response.Get.SuccessPayload<List<ValidationResult>>([])
				: Response.Get.InvalidData(
					string.Join("\r\n", validation.Select(v => v.ErrorMessage)),
					validation);
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