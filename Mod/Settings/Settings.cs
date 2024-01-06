using System;
using System.IO;
using Colossal.Json;
using ExtendedRadio.Patches;

namespace ExtendedRadio
{	
	public class Settings
	{
		public static bool customNetworkUI = true;
		public static bool DisableAdsOnStartup = false;

		internal static void LoadSettings() {
			if(Directory.Exists(GameManager_InitializeThumbnails.PathToMods)) {
				if(File.Exists(GameManager_InitializeThumbnails.PathToMods+"\\settings.json")) {
					JsonToSettings(Decoder.Decode(File.ReadAllText(GameManager_InitializeThumbnails.PathToMods+"\\settings.json")).Make<SettingsJSON>());
				}
			} 
		}

		internal static void SaveSettings() {
			if(!Directory.Exists(GameManager_InitializeThumbnails.PathToMods)) Directory.CreateDirectory(GameManager_InitializeThumbnails.PathToMods);
			File.WriteAllText(GameManager_InitializeThumbnails.PathToMods+"\\settings.json", Encoder.Encode(SettingsToJSON(), EncodeOptions.None));
		}

		private static SettingsJSON SettingsToJSON() {
			SettingsJSON settingsJSON = new()
			{
				customNetworkUI = customNetworkUI,
				DisableAdsOnStartup = DisableAdsOnStartup
			};
			return settingsJSON;
		}

		private static void JsonToSettings(SettingsJSON settingsJSON) {
			customNetworkUI = settingsJSON.customNetworkUI;
			DisableAdsOnStartup = settingsJSON.DisableAdsOnStartup;
		}
	}
	[Serializable]
	public class SettingsJSON
	{
		public bool customNetworkUI = true;
		public bool DisableAdsOnStartup = false;
	}

}