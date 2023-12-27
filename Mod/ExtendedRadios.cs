using System;
using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
using Game.Audio.Radio;
using HarmonyLib;
using UnityEngine;
using static Game.Audio.Radio.Radio;

namespace ExtendedRadio
{
	public class ExtendedRadio
	{
		public delegate void OnRadioLoad();
		public static event OnRadioLoad CallOnRadioLoad;
		internal static readonly Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<SegmentType, List<AudioAsset>>>>> audioDataBase = [];
		public static Traverse radioTravers = null;
		public static Radio radioObject = null;
		static internal void OnLoadRadio(Radio __instance) { 

			audioDataBase.Clear();

			radioObject = __instance;
			radioTravers = Traverse.Create(__instance);

			CustomRadios.LoadCustomRadios();
			RadioAddons.LoadRadioAddons();

			try {
				CallOnRadioLoad();
			} catch {}

		}

		static internal void AddAudioToDataBase(RadioChannel radioChannel) {
			foreach(Program program in radioChannel.programs) {
				foreach(Segment segment in program.segments) {
					if(audioDataBase.ContainsKey(radioChannel.network)){
						if(audioDataBase[radioChannel.network].ContainsKey(radioChannel.name)) {
							if(audioDataBase[radioChannel.network][radioChannel.name].ContainsKey(program.name)) {
								if(audioDataBase[radioChannel.network][radioChannel.name][program.name].ContainsKey(segment.type)) {
                                    audioDataBase[radioChannel.network][radioChannel.name][program.name][segment.type].AddRange([..segment.clips]);
								} else {
									audioDataBase[radioChannel.network][radioChannel.name][program.name].Add(segment.type, [..segment.clips]);
								}
							} else {
								Dictionary<SegmentType, List<AudioAsset>> dict1 = [];
								dict1.Add(segment.type, [..segment.clips]);

								audioDataBase[radioChannel.network][radioChannel.name].Add(program.name, dict1);
							}
						} else {
							Dictionary<SegmentType, List<AudioAsset>> dict1 = [];
							dict1.Add(segment.type, [..segment.clips]);

							Dictionary<string, Dictionary<SegmentType, List<AudioAsset>>> dict2 = [];
							dict2.Add(program.name, dict1);

							audioDataBase[radioChannel.network].Add(radioChannel.name, dict2);
						}	
					} else {

						Dictionary<SegmentType, List<AudioAsset>> dict1 = [];
						dict1.Add(segment.type, [..segment.clips]);

						Dictionary<string, Dictionary<SegmentType, List<AudioAsset>>> dict2 = [];
						dict2.Add(program.name, dict1);

						Dictionary<string, Dictionary<string, Dictionary<SegmentType, List<AudioAsset>>>> dict3 = [];
						dict3.Add(radioChannel.name, dict2);
						audioDataBase.Add(radioChannel.network, dict3);
					}
				}
			}
		}

		static internal void AddAudioToDataBase(string network, string radioChannel, string program, SegmentType segmentType, List<AudioAsset> audioAssets) {
			audioDataBase[network][radioChannel][program][segmentType].AddRange(audioAssets);
		}

		static internal void AddAudioToDataBase(string network, string radioChannel, string program, SegmentType segmentType, AudioAsset audioAssets) {
			audioDataBase[network][radioChannel][program][segmentType].Add(audioAssets);
		}

		static internal List<AudioAsset> GetAudioAssetsFromAudioDataBase(Radio radio, SegmentType type) {

			return audioDataBase[radio.currentChannel.network][radio.currentChannel.name][radio.currentChannel.currentProgram.name][type];
		}
		/// <summary>This methode add you folder that contains your radio to the list of radio to load.</summary>
		/// <param name="path">The global path to the folder that contains your custom radio</param>
		[Obsolete("Please, use ExtendedRadio.CustomRadios.RegisterCustomRadioDirectory(path).")]
		public static void RegisterCustomRadioDirectory(string path) {
			Debug.LogWarning("ExtendedRadio.ExtendedRadio.RegisterCustomRadioDirectory(string path) is Obsolete, use ExtendedRadio.CustomRadios.RegisterCustomRadioDirectory(string path) instead.");
			CustomRadios.RegisterCustomRadioDirectory(path);
		}

	}
}