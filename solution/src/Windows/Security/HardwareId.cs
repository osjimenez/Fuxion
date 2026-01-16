using System;
using System.Collections.Generic;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace Fuxion.Windows.Security;

/// <summary>
///    Specifies hardware components that can be used to generate a unique hardware identifier.
/// </summary>
/// <remarks>
///    This enum uses the <see cref="FlagsAttribute"/> to allow combining multiple hardware components
///    using bitwise operations. Each flag represents a different hardware component that can contribute
///    to creating a unique machine fingerprint.
/// </remarks>
[Flags]
public enum HardwareIdField
{
	/// <summary>
	///    CPU (Central Processing Unit) identifier.
	/// </summary>
	Cpu = 1,

	/// <summary>
	///    BIOS (Basic Input/Output System) information.
	/// </summary>
	Bios = 2,

	/// <summary>
	///    Motherboard (Base Board) identifier.
	/// </summary>
	Motherboard = 4,

	/// <summary>
	///    Disk drive identifier.
	/// </summary>
	Disk = 8,

	/// <summary>
	///    Video controller (graphics card) identifier.
	/// </summary>
	Video = 16,

	/// <summary>
	///    MAC (Media Access Control) address of network adapters.
	/// </summary>
	Mac = 32
}

/// <summary>
///    Provides static methods for generating unique hardware-based identifiers for Windows machines.
/// </summary>
/// <remarks>
///    <para>
///       This class uses Windows Management Instrumentation (WMI) to query hardware information and
///       generates unique <see cref="Guid"/> identifiers based on various hardware components. It's
///       particularly useful for software licensing, machine identification, and hardware-based authentication.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Multiple hardware components:</strong> CPU, BIOS, Motherboard, Disk, Video, and MAC address
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Caching:</strong> Hardware IDs are computed once and cached for performance
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Combinable:</strong> Generate IDs from single or multiple hardware components
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Hash-based:</strong> Uses MD5 hashing to create consistent GUIDs from hardware data
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>OS integration:</strong> Includes Operating System Product ID from Windows Registry
///          </description>
///       </item>
///    </list>
///    <para>
///       <strong>Common use cases:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>Software licensing and activation</description>
///       </item>
///       <item>
///          <description>Machine-specific encryption keys</description>
///       </item>
///       <item>
///          <description>Hardware-based authentication</description>
///       </item>
///       <item>
///          <description>Device fingerprinting for security</description>
///       </item>
///       <item>
///          <description>Trial software machine locking</description>
///       </item>
///    </list>
///    <para>
///       <strong>Important notes:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             Requires administrator privileges for some WMI queries
///          </description>
///       </item>
///       <item>
///          <description>
///             Hardware IDs may change if hardware is replaced or updated
///          </description>
///       </item>
///       <item>
///          <description>
///             Different hardware components have different stability characteristics
///          </description>
///       </item>
///       <item>
///          <description>
///             Virtual machines may not provide reliable hardware identifiers
///          </description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Get CPU-based hardware ID:</strong>
///    <code>
/// Guid cpuId = HardwareId.Cpu;
/// Console.WriteLine($"CPU ID: {cpuId}");
/// </code>
///    <strong>Combine multiple hardware components:</strong>
///    <code>
/// // Generate ID from CPU, BIOS, and Motherboard
/// Guid hardwareId = HardwareId.Get(
///     HardwareIdField.Cpu | 
///     HardwareIdField.Bios | 
///     HardwareIdField.Motherboard
/// );
/// 
/// Console.WriteLine($"Combined Hardware ID: {hardwareId}");
/// </code>
///    <strong>Software licensing example:</strong>
///    <code>
/// public class LicenseManager
/// {
///     private const string LICENSE_KEY = "your-license-key";
///     
///     public bool ValidateLicense(string userLicenseKey)
///     {
///         // Get unique machine ID
///         Guid machineId = HardwareId.Get(
///             HardwareIdField.Cpu | 
///             HardwareIdField.Motherboard |
///             HardwareIdField.Bios
///         );
///         
///         // Generate expected license key for this machine
///         string expectedKey = GenerateLicenseKey(machineId);
///         
///         return userLicenseKey == expectedKey;
///     }
///     
///     private string GenerateLicenseKey(Guid machineId)
///     {
///         // Combine machine ID with license key
///         var combined = machineId.ToString() + LICENSE_KEY;
///         using (var sha256 = SHA256.Create())
///         {
///             var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
///             return Convert.ToBase64String(hash);
///         }
///     }
/// }
/// </code>
///    <strong>Check all hardware IDs:</strong>
///    <code>
/// Console.WriteLine($"CPU ID: {HardwareId.Cpu}");
/// Console.WriteLine($"BIOS ID: {HardwareId.Bios}");
/// Console.WriteLine($"Motherboard ID: {HardwareId.Motherboard}");
/// Console.WriteLine($"Disk ID: {HardwareId.Disk}");
/// Console.WriteLine($"Video ID: {HardwareId.Video}");
/// Console.WriteLine($"MAC ID: {HardwareId.Mac}");
/// Console.WriteLine($"OS Product ID: {HardwareId.OperatingSystemProductId}");
/// </code>
///    <strong>Trial software machine locking:</strong>
///    <code>
/// public class TrialManager
/// {
///     private const string TRIAL_KEY = "TrialMachineId";
///     
///     public bool IsTrialValid()
///     {
///         // Get stable hardware ID
///         Guid currentMachineId = HardwareId.Get(
///             HardwareIdField.Motherboard | 
///             HardwareIdField.Bios
///         );
///         
///         // Check if trial was started on this machine
///         string storedId = Registry.GetValue(
///             @"HKEY_LOCAL_MACHINE\SOFTWARE\YourApp", 
///             TRIAL_KEY, 
///             null
///         )?.ToString();
///         
///         if (storedId == null)
///         {
///             // First run, register this machine
///             Registry.SetValue(
///                 @"HKEY_LOCAL_MACHINE\SOFTWARE\YourApp", 
///                 TRIAL_KEY, 
///                 currentMachineId.ToString()
///             );
///             return true;
///         }
///         
///         // Verify machine ID matches
///         return storedId == currentMachineId.ToString();
///     }
/// }
/// </code>
/// </example>
public static class HardwareId
{
	static Guid _cpu = Guid.Empty;
	static Guid _bios = Guid.Empty;
	static Guid _disk = Guid.Empty;
	static Guid _motherboard = Guid.Empty;
	static Guid _video = Guid.Empty;
	static Guid _mac = Guid.Empty;
	static Guid _operatingSystemProductId = Guid.Empty;

	/// <summary>
	///    Gets a unique identifier based on the CPU (Central Processing Unit).
	/// </summary>
	/// <value>
	///    A <see cref="Guid"/> representing the CPU's unique identifier, or <see cref="Guid.Empty"/> if unable to retrieve.
	/// </value>
	/// <remarks>
	///    <para>
	///       This property attempts to retrieve the CPU identifier in order of preference:
	///    </para>
	///    <list type="number">
	///       <item>
	///          <description>UniqueId property (most reliable)</description>
	///       </item>
	///       <item>
	///          <description>ProcessorId property (fallback)</description>
	///       </item>
	///    </list>
	///    <para>
	///       The value is computed once on first access and cached for subsequent calls.
	///       Uses WMI Win32_Processor class to query hardware information.
	///    </para>
	///    <para>
	///       <strong>Stability:</strong> High - CPUs are rarely replaced in typical usage.
	///    </para>
	///    <para>
	///       <strong>Note:</strong> Virtual machines may not provide reliable CPU identifiers.
	///    </para>
	/// </remarks>
	/// <example>
	///    <code>
	/// Guid cpuId = HardwareId.Cpu;
	/// if (cpuId != Guid.Empty)
	/// {
	///     Console.WriteLine($"CPU Identifier: {cpuId}");
	/// }
	/// else
	/// {
	///     Console.WriteLine("Unable to retrieve CPU identifier");
	/// }
	/// </code>
	/// </example>
	public static Guid Cpu
	{
		get
		{
			if (_cpu == Guid.Empty)
			{
				// https://www.codeproject.com/Articles/28678/Generating-Unique-Key-Finger-Print-for-a-Computer
				//_cpu = CombineHash(_cpu, GetHash("Win32_Processor", "UniqueId"));
				//_cpu = CombineHash(_cpu, GetHash("Win32_Processor", "ProcessorId"));
				//_cpu = CombineHash(_cpu, GetHash("Win32_Processor", "Name"));
				//_cpu = CombineHash(_cpu, GetHash("Win32_Processor", "Manufacturer"));
				//_cpu = CombineHash(_cpu, GetHash("Win32_Processor", "MaxClockSpeed"));

				//Uses first CPU identifier available in order of preference
				//Don't get all identifiers, as it is very time consuming
				_cpu = GetHash("Win32_Processor", "UniqueId");
				if (_cpu == Guid.Empty) //If no UniqueID, use ProcessorID
					_cpu = GetHash("Win32_Processor", "ProcessorId");
				//if (_cpu == Guid.Empty) //If no ProcessorId, use Name
				//{
				//	_cpu = GetHash("Win32_Processor", "Name");
				//	if (_cpu == Guid.Empty) //If no Name, use Manufacturer
				//	{
				//		_cpu = GetHash("Win32_Processor", "Manufacturer");
				//	}
				//	//Add clock speed for extra security
				//	_cpu = CombineHash(_cpu, GetHash("Win32_Processor", "MaxClockSpeed"));
				//}
			}
			return _cpu;
		}
	}

	/// <summary>
	///    Gets or sets a unique identifier based on the BIOS (Basic Input/Output System).
	/// </summary>
	/// <value>
	///    A <see cref="Guid"/> representing the BIOS unique identifier.
	/// </value>
	/// <remarks>
	///    <para>
	///       This property combines hashes from multiple BIOS properties:
	///    </para>
	///    <list type="bullet">
	///       <item><description>Manufacturer</description></item>
	///       <item><description>SMBIOS BIOS Version</description></item>
	///       <item><description>Identification Code</description></item>
	///       <item><description>Serial Number</description></item>
	///       <item><description>Release Date</description></item>
	///       <item><description>Version</description></item>
	///    </list>
	///    <para>
	///       The value is computed once on first access and cached. Uses WMI Win32_BIOS class.
	///    </para>
	///    <para>
	///       <strong>Stability:</strong> Very High - BIOS information rarely changes unless updated or motherboard replaced.
	///    </para>
	/// </remarks>
	public static Guid Bios
	{
		get
		{
			if (_bios == Guid.Empty)
			{
				_bios = CombineHash(_bios, GetHash("Win32_BIOS", "Manufacturer"));
				_bios = CombineHash(_bios, GetHash("Win32_BIOS", "SMBIOSBIOSVersion"));
				_bios = CombineHash(_bios, GetHash("Win32_BIOS", "IdentificationCode"));
				_bios = CombineHash(_bios, GetHash("Win32_BIOS", "SerialNumber"));
				_bios = CombineHash(_bios, GetHash("Win32_BIOS", "ReleaseDate"));
				_bios = CombineHash(_bios, GetHash("Win32_BIOS", "Version"));
			}
			return _bios;
		}
		set => _bios = value;
	}

	/// <summary>
	///    Gets or sets a unique identifier based on the disk drive.
	/// </summary>
	/// <value>
	///    A <see cref="Guid"/> representing the disk drive's unique identifier.
	/// </value>
	/// <remarks>
	///    <para>
	///       This property combines hashes from multiple disk properties:
	///    </para>
	///    <list type="bullet">
	///       <item><description>Model</description></item>
	///       <item><description>Manufacturer</description></item>
	///       <item><description>Signature</description></item>
	///       <item><description>Total Heads</description></item>
	///    </list>
	///    <para>
	///       The value is computed once on first access and cached. Uses WMI Win32_DiskDrive class.
	///    </para>
	///    <para>
	///       <strong>Stability:</strong> Medium - Disk drives can be replaced or added/removed.
	///    </para>
	///    <para>
	///       <strong>Warning:</strong> Not recommended as sole identifier for licensing as disks are commonly replaced.
	///    </para>
	/// </remarks>
	public static Guid Disk
	{
		get
		{
			if (_disk == Guid.Empty)
			{
				_disk = CombineHash(_disk, GetHash("Win32_DiskDrive", "Model"));
				_disk = CombineHash(_disk, GetHash("Win32_DiskDrive", "Manufacturer"));
				_disk = CombineHash(_disk, GetHash("Win32_DiskDrive", "Signature"));
				_disk = CombineHash(_disk, GetHash("Win32_DiskDrive", "TotalHeads"));
			}
			return _disk;
		}
		set => _disk = value;
	}

	/// <summary>
	///    Gets or sets a unique identifier based on the motherboard (base board).
	/// </summary>
	/// <value>
	///    A <see cref="Guid"/> representing the motherboard's unique identifier.
	/// </value>
	/// <remarks>
	///    <para>
	///       This property combines hashes from multiple motherboard properties:
	///    </para>
	///    <list type="bullet">
	///       <item><description>Model</description></item>
	///       <item><description>Manufacturer</description></item>
	///       <item><description>Name</description></item>
	///       <item><description>Serial Number</description></item>
	///    </list>
	///    <para>
	///       The value is computed once on first access and cached. Uses WMI Win32_BaseBoard class.
	///    </para>
	///    <para>
	///       <strong>Stability:</strong> Very High - Motherboards are rarely replaced in typical usage.
	///    </para>
	///    <para>
	///       <strong>Recommendation:</strong> Excellent choice for software licensing as it's stable and unique.
	///    </para>
	/// </remarks>
	public static Guid Motherboard
	{
		get
		{
			if (_motherboard == Guid.Empty)
			{
				_motherboard = CombineHash(_motherboard, GetHash("Win32_BaseBoard", "Model"));
				_motherboard = CombineHash(_motherboard, GetHash("Win32_BaseBoard", "Manufacturer"));
				_motherboard = CombineHash(_motherboard, GetHash("Win32_BaseBoard", "Name"));
				_motherboard = CombineHash(_motherboard, GetHash("Win32_BaseBoard", "SerialNumber"));
			}
			return _motherboard;
		}
		set => _motherboard = value;
	}

	/// <summary>
	///    Gets or sets a unique identifier based on the video controller (graphics card).
	/// </summary>
	/// <value>
	///    A <see cref="Guid"/> representing the video controller's unique identifier.
	/// </value>
	/// <remarks>
	///    <para>
	///       This property combines hashes from video controller properties:
	///    </para>
	///    <list type="bullet">
	///       <item><description>Driver Version</description></item>
	///       <item><description>Name</description></item>
	///    </list>
	///    <para>
	///       The value is computed once on first access and cached. Uses WMI Win32_VideoController class.
	///    </para>
	///    <para>
	///       <strong>Stability:</strong> Low - Video cards can be upgraded and drivers change frequently.
	///    </para>
	///    <para>
	///       <strong>Warning:</strong> Not recommended for licensing as driver updates will change this ID.
	///    </para>
	/// </remarks>
	public static Guid Video
	{
		get
		{
			if (_video == Guid.Empty)
			{
				_video = CombineHash(_video, GetHash("Win32_VideoController", "DriverVersion"));
				_video = CombineHash(_video, GetHash("Win32_VideoController", "Name"));
			}
			return _video;
		}
		set => _video = value;
	}

	/// <summary>
	///    Gets or sets a unique identifier based on the MAC (Media Access Control) address of enabled network adapters.
	/// </summary>
	/// <value>
	///    A <see cref="Guid"/> representing the MAC address unique identifier.
	/// </value>
	/// <remarks>
	///    <para>
	///       This property retrieves MAC addresses from network adapters where IPEnabled is true.
	///       Uses WMI Win32_NetworkAdapterConfiguration class.
	///    </para>
	///    <para>
	///       <strong>Stability:</strong> Medium - Network adapters can be added/removed/replaced.
	///    </para>
	///    <para>
	///       <strong>Note:</strong> Virtual network adapters and VPNs may affect this value.
	///    </para>
	/// </remarks>
	public static Guid Mac
	{
		get
		{
			if (_mac == Guid.Empty) _mac = CombineHash(_mac, GetHash("Win32_NetworkAdapterConfiguration", "MACAddress", "IPEnabled"));
			return _mac;
		}
		set => _mac = value;
	}

	/// <summary>
	///    Gets or sets the Windows Operating System Product ID from the registry.
	/// </summary>
	/// <value>
	///    A <see cref="Guid"/> representing the OS Product ID.
	/// </value>
	/// <remarks>
	///    <para>
	///       This property reads the ProductId from the Windows Registry at:
	///       <c>HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion</c>
	///    </para>
	///    <para>
	///       Uses the appropriate registry view (32-bit or 64-bit) based on the operating system architecture.
	///    </para>
	///    <para>
	///       <strong>Stability:</strong> High - Tied to Windows installation but may change on OS reinstall.
	///    </para>
	/// </remarks>
	/// <exception cref="Exception">
	///    Thrown when unable to retrieve the ProductId from the registry.
	/// </exception>
	public static Guid OperatingSystemProductId
	{
		get
		{
			if (_operatingSystemProductId == Guid.Empty)
			{
				string? value64 = null;
				var localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
				localKey = localKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
				if (localKey is not null) value64 = localKey.GetValue("ProductId")?.ToString();
				Console.WriteLine("RegisteredOrganization [value64]: {0}", value64);

				//var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion");
				//var val = key.GetValue("ProductId");
				_operatingSystemProductId = new(ComputeHash(value64 ?? throw new Exception("Value cannot be retrieved from registry")));
			}
			return _operatingSystemProductId;
		}
		set => _operatingSystemProductId = value;
	}

	/// <summary>
	///    Generates a combined hardware identifier from specified hardware components.
	/// </summary>
	/// <param name="fields">
	///    A bitwise combination of <see cref="HardwareIdField"/> values specifying which hardware components to include.
	/// </param>
	/// <returns>
	///    A <see cref="Guid"/> representing the combined hash of all specified hardware components.
	/// </returns>
	/// <remarks>
	///    <para>
	///       This method combines multiple hardware identifiers using XOR operations to create a single unique ID.
	///       The order of combination doesn't affect the final result due to the XOR operation properties.
	///    </para>
	///    <para>
	///       <strong>Recommended combinations for licensing:</strong>
	///    </para>
	///    <list type="bullet">
	///       <item>
	///          <description>
	///             <c>Cpu | Bios | Motherboard</c> - Most stable combination
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <c>Motherboard | Bios</c> - Simplest stable combination
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             Avoid <c>Disk</c> and <c>Video</c> for licensing (too volatile)
	///          </description>
	///       </item>
	///    </list>
	/// </remarks>
	/// <example>
	///    <code>
	/// // Stable combination for licensing
	/// Guid licenseId = HardwareId.Get(
	///     HardwareIdField.Cpu | 
	///     HardwareIdField.Bios | 
	///     HardwareIdField.Motherboard
	/// );
	/// 
	/// // Include all components
	/// Guid fullId = HardwareId.Get(
	///     HardwareIdField.Cpu | 
	///     HardwareIdField.Bios | 
	///     HardwareIdField.Motherboard |
	///     HardwareIdField.Disk |
	///     HardwareIdField.Video |
	///     HardwareIdField.Mac
	/// );
	/// 
	/// // Single component
	/// Guid cpuOnly = HardwareId.Get(HardwareIdField.Cpu);
	/// </code>
	/// </example>
	public static Guid Get(HardwareIdField fields)
	{
		var res = Guid.Empty;
		if (fields.HasFlag(HardwareIdField.Bios)) res = CombineHash(res, Bios);
		if (fields.HasFlag(HardwareIdField.Cpu)) res = CombineHash(res, Cpu);
		if (fields.HasFlag(HardwareIdField.Disk)) res = CombineHash(res, Disk);
		if (fields.HasFlag(HardwareIdField.Mac)) res = CombineHash(res, Mac);
		if (fields.HasFlag(HardwareIdField.Motherboard)) res = CombineHash(res, Motherboard);
		if (fields.HasFlag(HardwareIdField.Video)) res = CombineHash(res, Video);
		return res;
	}

	/// <summary>
	///    Combines two GUIDs using XOR hash operation.
	/// </summary>
	/// <param name="guid1">First GUID to combine.</param>
	/// <param name="guid2">Second GUID to combine.</param>
	/// <returns>A new GUID representing the XOR combination of both input GUIDs.</returns>
	static Guid CombineHash(Guid guid1, Guid guid2)
	{
		var res = guid1.ToByteArray();
		res.CombineHash(guid2.ToByteArray());
		return new(res);
	}

	/// <summary>
	///    Combines two byte arrays using XOR operation in place.
	/// </summary>
	/// <param name="me">The byte array to modify (this parameter).</param>
	/// <param name="byteArray">The byte array to combine with.</param>
	/// <exception cref="ArgumentException">
	///    Thrown when the arrays have different lengths.
	/// </exception>
	static void CombineHash(this byte[] me, byte[] byteArray)
	{
		if (me.Length != byteArray.Length) throw new ArgumentException("Los arrays a combinar deben tener el mismo tamaño");
		var res = new byte[me.Length];
		for (var i = 0; i < me.Length; i++) me[i] = (byte)(me[i] ^ byteArray[i]);
	}

	/// <summary>
	///    Computes an MD5 hash of a string.
	/// </summary>
	/// <param name="me">The string to hash.</param>
	/// <returns>A byte array containing the MD5 hash.</returns>
	static byte[] ComputeHash(this string me) => MD5.Create().ComputeHash(new ASCIIEncoding().GetBytes(me));

	/// <summary>
	///    Retrieves a GUID hash from a WMI class property.
	/// </summary>
	/// <param name="wmiClass">The WMI class name (e.g., "Win32_Processor").</param>
	/// <param name="wmiProperty">The property name to retrieve (e.g., "ProcessorId").</param>
	/// <param name="wmiMustBeTrue">Optional property that must equal "True" to include this instance.</param>
	/// <returns>
	///    A GUID representing the combined hash of all matching property values, or <see cref="Guid.Empty"/>
	///    if no values found.
	/// </returns>
	/// <exception cref="InvalidProgramException">
	///    Thrown when unable to read a WMI property value.
	/// </exception>
	/// <remarks>
	///    This method queries all instances of the specified WMI class and combines the hashes of the
	///    specified property from all instances that match the optional filter.
	/// </remarks>
	static Guid GetHash(string wmiClass, string wmiProperty, string? wmiMustBeTrue = null)
	{
		var list = new List<byte[]>();
		var res = new byte[16];
		var mc = new ManagementClass(wmiClass);
		var moc = mc.GetInstances();
		foreach (ManagementObject mo in moc)
			if ((wmiMustBeTrue == null || mo[wmiMustBeTrue].ToString() == "True") && mo[wmiProperty] != null)
			{
				var pro = mo[wmiProperty].ToString();
				if (pro == null) throw new InvalidProgramException("Error reading WMI property");
				list.Add(pro.ComputeHash());
			}
		if (list.Count == 0) return Guid.Empty;
		foreach (var g in list) res.CombineHash(g);
		return new(res);
	}
}