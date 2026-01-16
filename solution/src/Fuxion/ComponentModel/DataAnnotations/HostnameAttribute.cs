using System.ComponentModel.DataAnnotations;

namespace Fuxion.ComponentModel.DataAnnotations;

/// <summary>
/// Validates that a string value is a valid hostname according to RFC 1123.
/// </summary>
/// <remarks>
/// <para>
/// This attribute validates hostnames using a regular expression that enforces RFC 1123 naming conventions:
/// </para>
/// <list type="bullet">
/// <item><description>Can contain alphanumeric characters (a-z, A-Z, 0-9) and hyphens (-)</description></item>
/// <item><description>Must start and end with an alphanumeric character (not a hyphen)</description></item>
/// <item><description>Each label (segment between dots) can be 1 to 63 characters long</description></item>
/// <item><description>Labels can be separated by dots (.) to form a fully qualified domain name</description></item>
/// <item><description>Cannot start or end with a hyphen</description></item>
/// </list>
/// <para>
/// Valid hostnames include simple hostnames (e.g., "server1") and fully qualified domain names (e.g., "www.example.com").
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ServerConfiguration
/// {
///     [Hostname(ErrorMessage = "Please enter a valid hostname")]
///     public string ServerName { get; set; }
///     
///     [Hostname]
///     public string DatabaseHost { get; set; }
/// }
/// 
/// // Valid hostnames
/// var config1 = new ServerConfiguration { ServerName = "server1" }; // Valid
/// var config2 = new ServerConfiguration { ServerName = "my-server" }; // Valid
/// var config3 = new ServerConfiguration { ServerName = "web.example.com" }; // Valid
/// var config4 = new ServerConfiguration { ServerName = "api-v2.prod.company.com" }; // Valid
/// 
/// // Invalid hostnames
/// var invalid1 = new ServerConfiguration { ServerName = "-server" }; // Invalid: starts with hyphen
/// var invalid2 = new ServerConfiguration { ServerName = "server-" }; // Invalid: ends with hyphen
/// var invalid3 = new ServerConfiguration { ServerName = "server_name" }; // Invalid: contains underscore
/// var invalid4 = new ServerConfiguration { ServerName = "server..com" }; // Invalid: consecutive dots
/// 
/// // Validation example
/// var context = new ValidationContext(config1);
/// var results = new List&lt;ValidationResult&gt;();
/// bool isValid = Validator.TryValidateObject(config1, context, results, validateAllProperties: true);
/// 
/// // ASP.NET Core usage
/// public class NetworkSettingsDto
/// {
///     [Required]
///     [Hostname(ErrorMessage = "The {0} must be a valid hostname")]
///     public string MailServer { get; set; }
///     
///     [Hostname]
///     public string ProxyServer { get; set; }
/// }
/// 
/// [HttpPost]
/// public IActionResult UpdateSettings([FromBody] NetworkSettingsDto dto)
/// {
///     if (!ModelState.IsValid)
///         return BadRequest(ModelState);
///     
///     // Save settings...
///     return Ok();
/// }
/// </code>
/// </example>
public class HostnameAttribute : RegularExpressionAttribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="HostnameAttribute"/> class.
	/// </summary>
	/// <remarks>
	/// The validation uses the RFC 1123 hostname pattern:
	/// <code>
	/// ^([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])(\.([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]{0,61}[a-zA-Z0-9]))*$
	/// </code>
	/// This pattern ensures each label starts and ends with an alphanumeric character and can contain hyphens in between.
	/// </remarks>
	public HostnameAttribute() : base(@"^([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])(\.([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]{0,61}[a-zA-Z0-9]))*$") { }
}