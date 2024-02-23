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
		public static bool SaveLastRadio = false;
		public static string LastRadio = null;

		internal static void LoadSettings() {
			if(Directory.Exists(GameManager_Awake.PathToMods)) {
				if(File.Exists(GameManager_Awake.PathToMods+"\\settings.json")) {
					JsonToSettings(Decoder.Decode(File.ReadAllText(GameManager_Awake.PathToMods+"\\settings.json")).Make<SettingsJSON>());
				}
			} 
		}

		internal static void SaveSettings() {
			if(!Directory.Exists(GameManager_Awake.PathToMods)) Directory.CreateDirectory(GameManager_Awake.PathToMods);
			File.WriteAllText(GameManager_Awake.PathToMods+"\\settings.json", Encoder.Encode(SettingsToJSON(), EncodeOptions.None));
		}

		private static SettingsJSON SettingsToJSON() {
			SettingsJSON settingsJSON = new()
			{
				customNetworkUI = customNetworkUI,
				DisableAdsOnStartup = DisableAdsOnStartup,
				SaveLastRadio = SaveLastRadio,
				LastRadio = LastRadio
			};
			return settingsJSON;
		}

		private static void JsonToSettings(SettingsJSON settingsJSON) {
			customNetworkUI = settingsJSON.customNetworkUI;
			DisableAdsOnStartup = settingsJSON.DisableAdsOnStartup;
			SaveLastRadio = settingsJSON.SaveLastRadio;
			LastRadio = settingsJSON.LastRadio;
		}
	}
	[Serializable]
	public class SettingsJSON
	{
		public bool customNetworkUI = true;
		public bool DisableAdsOnStartup = false;
		public bool SaveLastRadio = false;
		public string LastRadio = null;
	}

}