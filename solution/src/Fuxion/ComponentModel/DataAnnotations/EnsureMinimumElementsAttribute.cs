using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Fuxion.ComponentModel.DataAnnotations;

/// <summary>
/// Validates that a collection contains at least a specified minimum number of elements.
/// </summary>
/// <remarks>
/// <para>
/// This validation attribute is useful for ensuring that list-based properties meet minimum size requirements.
/// It works with any property that implements <see cref="IList"/>.
/// </para>
/// <para>
/// The validation fails if:
/// </para>
/// <list type="bullet">
/// <item><description>The value is null</description></item>
/// <item><description>The value is not an <see cref="IList"/></description></item>
/// <item><description>The collection contains fewer than the minimum number of elements</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class Order
/// {
///     [EnsureMinimumElements(1, ErrorMessage = "An order must have at least one item")]
///     public List&lt;OrderItem&gt; Items { get; set; } = new();
///     
///     [EnsureMinimumElements(2)]
///     public List&lt;string&gt; Tags { get; set; } = new();
/// }
/// 
/// // Valid
/// var validOrder = new Order
/// {
///     Items = new List&lt;OrderItem&gt; { new OrderItem() }
/// };
/// 
/// // Invalid - will fail validation
/// var invalidOrder = new Order
/// {
///     Items = new List&lt;OrderItem&gt;() // Empty list
/// };
/// 
/// // Using with Validator class
/// var context = new ValidationContext(invalidOrder);
/// var results = new List&lt;ValidationResult&gt;();
/// bool isValid = Validator.TryValidateObject(invalidOrder, context, results, validateAllProperties: true);
/// 
/// if (!isValid)
/// {
///     foreach (var error in results)
///     {
///         Console.WriteLine(error.ErrorMessage);
///     }
/// }
/// 
/// // ASP.NET Core usage
/// public class CreateOrderDto
/// {
///     [EnsureMinimumElements(1, ErrorMessage = "Please add at least one item to the order")]
///     public List&lt;OrderItemDto&gt; Items { get; set; }
/// }
/// 
/// [HttpPost]
/// public IActionResult CreateOrder([FromBody] CreateOrderDto dto)
/// {
///     if (!ModelState.IsValid)
///         return BadRequest(ModelState);
///     
///     // Process order...
///     return Ok();
/// }
/// </code>
/// </example>
public class EnsureMinimumElementsAttribute : ValidationAttribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="EnsureMinimumElementsAttribute"/> class.
	/// </summary>
	/// <param name="minElements">The minimum number of elements required in the collection.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="minElements"/> is negative.</exception>
	public EnsureMinimumElementsAttribute(int minElements) => _minElements = minElements;
	
	readonly int _minElements;
	
	/// <summary>
	/// Formats the error message to display when validation fails.
	/// </summary>
	/// <param name="name">The name of the field being validated.</param>
	/// <returns>A formatted error message string.</returns>
	/// <remarks>
	/// The default error message includes the property name and the minimum number of required elements.
	/// You can customize the message by setting the ErrorMessage property.
	/// Use format placeholders: {0} for the property name and {1} for the minimum element count.
	/// </remarks>
	public override string FormatErrorMessage(string name) => string.Format(ErrorMessageString, name, _minElements);
	
	/// <summary>
	/// Determines whether the specified value is valid.
	/// </summary>
	/// <param name="value">The value to validate.</param>
	/// <returns>
	/// true if the value is an <see cref="IList"/> with at least the minimum number of elements; otherwise, false.
	/// </returns>
	/// <remarks>
	/// <para>The method validates that:</para>
	/// <list type="number">
	/// <item><description>The value is not null</description></item>
	/// <item><description>The value implements <see cref="IList"/></description></item>
	/// <item><description>The list's Count property is greater than or equal to the minimum</description></item>
	/// </list>
	/// <para>
	/// Note: This method returns false for null values. If you want to allow null (optional) collections,
	/// do not apply this attribute or combine it with conditional validation logic.
	/// </para>
	/// </remarks>
	public override bool IsValid(object? value)
	{
		if (value is IList list) return list.Count >= _minElements;
		return false;
	}
}