using System.ComponentModel.DataAnnotations;

namespace Fuxion.ComponentModel.DataAnnotations;

public class IpAddressAttribute : RegularExpressionAttribute
{
	public IpAddressAttribute() : base(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$") { }
}