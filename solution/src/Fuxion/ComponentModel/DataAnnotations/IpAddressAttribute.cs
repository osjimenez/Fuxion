using System.ComponentModel.DataAnnotations;

namespace Fuxion.ComponentModel.DataAnnotations;

/// <summary>
/// Validates that a string value is a valid IPv4 address.
/// </summary>
/// <remarks>
/// <para>
/// This attribute validates IPv4 addresses using a regular expression that ensures each octet (segment)
/// is a valid number between 0 and 255.
/// </para>
/// <para>
/// The validation enforces that:
/// </para>
/// <list type="bullet">
/// <item><description>The address consists of exactly 4 octets separated by dots (.)</description></item>
/// <item><description>Each octet is a number between 0 and 255</description></item>
/// <item><description>Leading zeros are allowed (e.g., "192.001.002.003" is valid)</description></item>
/// <item><description>No spaces or other characters are allowed</description></item>
/// </list>
/// <para>
/// Note: This validator only supports IPv4 addresses. IPv6 addresses are not supported.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class NetworkSettings
/// {
///     [Required]
///     [IpAddress(ErrorMessage = "Please enter a valid IP address")]
///     public string ServerIp { get; set; }
///     
///     [IpAddress]
///     public string GatewayIp { get; set; }
///     
///     [IpAddress(ErrorMessage = "The DNS server address is invalid")]
///     public string DnsServer { get; set; }
/// }
/// 
/// // Valid IP addresses
/// var config1 = new NetworkSettings { ServerIp = "192.168.1.1" }; // Valid
/// var config2 = new NetworkSettings { ServerIp = "10.0.0.1" }; // Valid
/// var config3 = new NetworkSettings { ServerIp = "255.255.255.255" }; // Valid (broadcast)
/// var config4 = new NetworkSettings { ServerIp = "0.0.0.0" }; // Valid (any address)
/// var config5 = new NetworkSettings { ServerIp = "127.0.0.1" }; // Valid (localhost)
/// var config6 = new NetworkSettings { ServerIp = "192.001.002.003" }; // Valid (with leading zeros)
/// 
/// // Invalid IP addresses
/// var invalid1 = new NetworkSettings { ServerIp = "256.1.1.1" }; // Invalid: 256 > 255
/// var invalid2 = new NetworkSettings { ServerIp = "192.168.1" }; // Invalid: only 3 octets
/// var invalid3 = new NetworkSettings { ServerIp = "192.168.1.1.1" }; // Invalid: 5 octets
/// var invalid4 = new NetworkSettings { ServerIp = "192.168.-1.1" }; // Invalid: negative number
/// var invalid5 = new NetworkSettings { ServerIp = "192.168.1.a" }; // Invalid: contains letter
/// var invalid6 = new NetworkSettings { ServerIp = "192.168. 1.1" }; // Invalid: contains space
/// 
/// // Validation example
/// var context = new ValidationContext(config1);
/// var results = new List&lt;ValidationResult&gt;();
/// bool isValid = Validator.TryValidateObject(config1, context, results, validateAllProperties: true);
/// 
/// // ASP.NET Core usage
/// public class ServerConfigDto
/// {
///     [Required(ErrorMessage = "Server IP is required")]
///     [IpAddress(ErrorMessage = "Invalid IP address format")]
///     public string IpAddress { get; set; }
///     
///     [IpAddress]
///     public string SubnetMask { get; set; }
/// }
/// 
/// [HttpPost("configure")]
/// public IActionResult ConfigureServer([FromBody] ServerConfigDto dto)
/// {
///     if (!ModelState.IsValid)
///         return BadRequest(ModelState);
///     
///     // Configure server with IP...
///     return Ok(new { message = "Server configured successfully" });
/// }
/// 
/// // Real-world example: Network configuration form
/// public class NetworkConfiguration
/// {
///     [Required]
///     [IpAddress(ErrorMessage = "Invalid IP address")]
///     public string LocalIpAddress { get; set; }
///     
///     [Required]
///     [IpAddress(ErrorMessage = "Invalid subnet mask")]
///     public string SubnetMask { get; set; }
///     
///     [Required]
///     [IpAddress(ErrorMessage = "Invalid gateway address")]
///     public string DefaultGateway { get; set; }
///     
///     [IpAddress(ErrorMessage = "Invalid DNS server address")]
///     public string PrimaryDns { get; set; }
///     
///     [IpAddress(ErrorMessage = "Invalid DNS server address")]
///     public string SecondaryDns { get; set; }
/// }
/// </code>
/// </example>
public class IpAddressAttribute : RegularExpressionAttribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="IpAddressAttribute"/> class.
	/// </summary>
	/// <remarks>
	/// The validation uses a regular expression that matches IPv4 addresses:
	/// <code>
	/// ^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$
	/// </code>
	/// <para>Each octet can be:</para>
	/// <list type="bullet">
	/// <item><description>[0-9]: Single digit (0-9)</description></item>
	/// <item><description>[1-9][0-9]: Two digits (10-99)</description></item>
	/// <item><description>1[0-9]{2}: Three digits starting with 1 (100-199)</description></item>
	/// <item><description>2[0-4][0-9]: Three digits starting with 2 and second digit 0-4 (200-249)</description></item>
	/// <item><description>25[0-5]: Three digits starting with 25 and last digit 0-5 (250-255)</description></item>
	/// </list>
	/// </remarks>
	public IpAddressAttribute() : base(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$") { }
}