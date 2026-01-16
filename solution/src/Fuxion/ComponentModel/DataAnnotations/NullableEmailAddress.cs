using System.ComponentModel.DataAnnotations;

namespace Fuxion.ComponentModel.DataAnnotations;

/// <summary>
/// Validates that a string value is either null, empty, or a valid email address.
/// </summary>
/// <remarks>
/// <para>
/// This attribute extends the standard <see cref="EmailAddressAttribute"/> behavior by allowing null or empty values.
/// It's useful when you want to validate email format only when a value is provided, making the field optional.
/// </para>
/// <para>
/// The validation passes when:
/// </para>
/// <list type="bullet">
/// <item><description>The value is null</description></item>
/// <item><description>The value is an empty string</description></item>
/// <item><description>The value is a valid email address according to <see cref="EmailAddressAttribute"/></description></item>
/// </list>
/// <para>
/// Note: If you want to make the email required (not nullable), use the standard <see cref="EmailAddressAttribute"/> 
/// combined with <see cref="RequiredAttribute"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class UserProfile
/// {
///     [Required]
///     public string Username { get; set; }
///     
///     // Email is optional but must be valid if provided
///     [NullableEmailAddress(ErrorMessage = "Please enter a valid email address")]
///     public string Email { get; set; }
///     
///     // Alternative email is also optional
///     [NullableEmailAddress]
///     public string AlternativeEmail { get; set; }
/// }
/// 
/// // Valid scenarios
/// var profile1 = new UserProfile 
/// { 
///     Username = "john", 
///     Email = "john@example.com" 
/// }; // Valid
/// 
/// var profile2 = new UserProfile 
/// { 
///     Username = "jane", 
///     Email = null 
/// }; // Valid: null is allowed
/// 
/// var profile3 = new UserProfile 
/// { 
///     Username = "bob", 
///     Email = "" 
/// }; // Valid: empty string is allowed
/// 
/// // Invalid scenario
/// var profile4 = new UserProfile 
/// { 
///     Username = "alice", 
///     Email = "not-an-email" 
/// }; // Invalid: malformed email
/// 
/// // Validation example
/// var context = new ValidationContext(profile1);
/// var results = new List&lt;ValidationResult&gt;();
/// bool isValid = Validator.TryValidateObject(profile1, context, results, validateAllProperties: true);
/// 
/// // ASP.NET Core usage
/// public class ContactFormDto
/// {
///     [Required(ErrorMessage = "Name is required")]
///     public string Name { get; set; }
///     
///     [Required(ErrorMessage = "Message is required")]
///     public string Message { get; set; }
///     
///     // Email is optional but validated if provided
///     [NullableEmailAddress(ErrorMessage = "Invalid email format")]
///     public string ReplyToEmail { get; set; }
/// }
/// 
/// [HttpPost("contact")]
/// public IActionResult SubmitContactForm([FromBody] ContactFormDto dto)
/// {
///     if (!ModelState.IsValid)
///         return BadRequest(ModelState);
///     
///     // Process contact form...
///     // If ReplyToEmail is provided, send response to that email
///     return Ok(new { message = "Thank you for contacting us!" });
/// }
/// 
/// // Real-world example: User registration with optional notifications
/// public class RegistrationDto
/// {
///     [Required(ErrorMessage = "Username is required")]
///     public string Username { get; set; }
///     
///     [Required]
///     [EmailAddress(ErrorMessage = "Invalid primary email")]
///     public string PrimaryEmail { get; set; } // Required and must be valid
///     
///     [NullableEmailAddress(ErrorMessage = "Invalid recovery email format")]
///     public string RecoveryEmail { get; set; } // Optional but validated
///     
///     [NullableEmailAddress]
///     public string NotificationEmail { get; set; } // Optional notification preference
/// }
/// </code>
/// </example>
public class NullableEmailAddress : DataTypeAttribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="NullableEmailAddress"/> class.
	/// </summary>
	/// <remarks>
	/// Sets the data type to <see cref="DataType.EmailAddress"/> to provide client-side HTML5 validation hints
	/// and proper UI rendering in forms.
	/// </remarks>
	public NullableEmailAddress() : base(DataType.EmailAddress) { }
	
	/// <summary>
	/// Determines whether the specified value is valid.
	/// </summary>
	/// <param name="value">The value to validate.</param>
	/// <returns>
	/// true if the value is null, empty, or a valid email address; otherwise, false.
	/// </returns>
	/// <remarks>
	/// <para>The validation logic:</para>
	/// <list type="number">
	/// <item><description>If value is null ? returns true (valid)</description></item>
	/// <item><description>If value is not a string ? returns false (invalid)</description></item>
	/// <item><description>If value is an empty string ? returns true (valid)</description></item>
	/// <item><description>Otherwise, validates using <see cref="EmailAddressAttribute"/></description></item>
	/// </list>
	/// </remarks>
	public override bool IsValid(object? value) => value == null || value is string input && (string.IsNullOrEmpty(input) || new EmailAddressAttribute().IsValid(input));
}