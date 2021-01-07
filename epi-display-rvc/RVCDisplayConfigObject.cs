using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;

namespace RVCDisplay
{
	/// <summary>
	/// Plugin device configuration object
	/// </summary>
	[ConfigSnippet("\"properties\":{\"control\":{}")]
	public class RVCDisplayConfig
	{
		/// <summary>
		/// JSON control object
		/// </summary>
		/// <remarks>
		/// Required for CCD Transports: ISerialComport, ICecDevice, IIr.
		/// </remarks>
		/// <example>
		/// <code>
		/// "control": {
        ///		"method": "com",
		///		"controlPortDevKey": "processor",
		///		"controlPortNumber": 1,
		///		"comParams": {
		///			"baudRate": 9600,
		///			"dataBits": 8,
		///			"stopBits": 1,
		///			"parity": "None",
		///			"protocol": "RS232",
		///			"hardwareHandshake": "None",
		///			"softwareHandshake": "None"
		///		}
		///	}
		/// </code>
		/// </example>
		[JsonProperty("control", Required=Required.Default)]
		public EssentialsControlPropertiesConfig Control { get; set; }

		/// <summary>
		/// Constuctor
		/// </summary>
		/// <remarks>
		/// If using a collection you must instantiate the collection in the constructor
		/// to avoid exceptions when reading the configuration file 
		/// </remarks>
		public RVCDisplayConfig()
		{
		}
	}
}