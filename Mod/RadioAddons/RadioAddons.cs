
using System.Collections.Generic;
using System.IO;
using Colossal.Json;
using UnityEngine;

namespace ExtendedRadio
{
	class RadioAddons
	{
		private static readonly List<string> addonsDirectories = [];
		internal static void LoadRadioAddons() {
			foreach(string radioAddonsFolder in addonsDirectories ) {
				foreach(string folder in Directory.GetDirectories(radioAddonsFolder)) {
					if(File.Exists(folder+"\\RadioAddon.json")) {
						JsonRadioAddons jsonRadioAddons = Decoder.Decode(File.ReadAllText(folder+"\\RadioAddon.json")).Make<JsonRadioAddons>();
						foreach(string audioFileFolder in Directory.GetDirectories(folder)) {
							foreach(string audioAssetFile in Directory.GetFiles(audioFileFolder, "*.ogg")) {
								ExtendedRadio.audioDataBase[jsonRadioAddons.RadioNetwork][jsonRadioAddons.RadioChannel][jsonRadioAddons.Program][CustomRadios.StringToSegmentType(jsonRadioAddons.SegmentType)].Add(MusicLoader.LoadAudioFile(audioAssetFile, CustomRadios.StringToSegmentType(jsonRadioAddons.SegmentType), jsonRadioAddons.RadioNetwork, jsonRadioAddons.RadioChannel));

							}
						}
					}
				}
			}
		}
		public static void RegisterRadioAddonsDirectory(string path) {
			addonsDirectories.Add(path);
		}
	}
}