﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Game.Audio.Radio;
using Game.SceneFlow;
using Game.UI;
using HarmonyLib;
using UnityEngine;
using static Game.Audio.Radio.Radio;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Colossal.Randomization;
using Unity.Jobs;
using Unity.Collections;
using Game.City;
using Unity.Entities;
using Game.Prefabs;

namespace ExtendedRadio.Patches
{
	[HarmonyPatch(typeof(GameManager), "InitializeThumbnails")]
	internal class GameManager_InitializeThumbnails
	{	
		static readonly string IconsResourceKey = $"{MyPluginInfo.PLUGIN_NAME.ToLower()}";

		public static readonly string COUIBaseLocation = $"coui://{IconsResourceKey}";

		static readonly string resources = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "resources");
		static readonly string CustomRadioPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomRadio");
		public static readonly string CustomRadiosPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomRadios");
		static readonly string PathToParent = Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName;
		public static readonly string PathToMods = Path.Combine(PathToParent,"ExtendedRadio_mods");
		public static readonly string CustomRadioFolderPlugins = Path.Combine(PathToMods,"CustomRadios");

		static void Prefix(GameManager __instance)
		{		

			List<string> pathToIconToLoad = [Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)];

			if(Directory.Exists(CustomRadioPath)) {
				Directory.Move(CustomRadioPath, CustomRadiosPath);
			}

			Directory.CreateDirectory(CustomRadiosPath);

			if(!Directory.Exists(resources)) {
				Directory.CreateDirectory(resources);
				File.Move(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DefaultIcon.svg"), Path.Combine(resources , "DefaultIcon.svg"));
			}

			if(Directory.Exists(CustomRadioFolderPlugins)) {
				ExtendedRadio.RegisterCustomRadioDirectory(CustomRadioFolderPlugins);
				pathToIconToLoad.Add(PathToMods);
			}

			var gameUIResourceHandler = (GameUIResourceHandler)GameManager.instance.userInterface.view.uiSystem.resourceHandler;
			
			if (gameUIResourceHandler == null)
			{
				Debug.LogError("Failed retrieving GameManager's GameUIResourceHandler instance, exiting.");
				return;
			}
			
			gameUIResourceHandler.HostLocationsMap.Add(
				IconsResourceKey, pathToIconToLoad

			);
		}
	}

	[HarmonyPatch(typeof( Radio ), "LoadRadio")]
	class Radio_LoadRadio {

		static void Postfix( Radio __instance) {

			ExtendedRadio.OnLoadRadio(__instance);

		}
	}

	[HarmonyPatch(typeof(AudioAsset), "LoadAsync")]
	internal class AudioAssetLoadAsyncPatch
	{
		static bool Prefix(AudioAsset __instance, ref Task<AudioClip> __result)
		{	
			if(!ExtendedRadio.customeRadioChannelsName.Contains(__instance.GetMetaTag(AudioAsset.Metatag.RadioChannel))) return true;
			
			__result = LoadAudioFile(__instance);
			return false;
		}

		private static async Task<AudioClip> LoadAudioFile(AudioAsset audioAsset)
		{
			Traverse audioAssetTravers = Traverse.Create(audioAsset);

			if(audioAssetTravers.Field("m_Instance").GetValue() == null)
			{
				string sPath = ExtendedRadio.GetClipPathFromAudiAsset(audioAsset);
				using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + sPath, ExtendedRadio.GetClipFormatFromAudiAsset(audioAsset));
				((DownloadHandlerAudioClip) www.downloadHandler).streamAudio = true;
				await www.SendWebRequest();
				AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
				www.Dispose();

				clip.name = sPath;
				clip.hideFlags = HideFlags.DontSave;

				audioAssetTravers.Field("m_Instance").SetValue(clip);
			}

			return (AudioClip) audioAssetTravers.Field("m_Instance").GetValue();
		}
	}


	[HarmonyPatch( typeof( Radio ), "GetPlaylistClips" )]
	class Radio_GetPlaylistClips
	{
		static bool Prefix( Radio __instance, RuntimeSegment segment)
		{		
			if(ExtendedRadio.customeRadioChannelsName.Contains(__instance.currentChannel.name)) {

				IEnumerable<AudioAsset> assets = ExtendedRadio.GetAudiAssetsFromAudioDataBase(__instance, segment.type);
				List<AudioAsset> list = [.. assets];
				System.Random rnd = new();
				List<int> list2 = (from x in Enumerable.Range(0, list.Count)
								orderby rnd.Next()
								select x).Take(segment.clipsCap).ToList();
				AudioAsset[] array = new AudioAsset[segment.clipsCap];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = list[list2[i]];
				}

				segment.clips = array;

				return false;
			}
			return true;
		}
	}

	[HarmonyPatch( typeof( Radio ), "GetCommercialClips" )]
	class Radio_GetCommercialClips
	{
        static bool Prefix( Radio __instance, RuntimeSegment segment)
		{
			if(ExtendedRadio.customeRadioChannelsName.Contains(__instance.currentChannel.name)) {


				Dictionary<string, RadioNetwork> m_Networks = Traverse.Create(__instance).Field("m_Networks").GetValue<Dictionary<string, RadioNetwork>>();

				if (!m_Networks.TryGetValue(__instance.currentChannel.network, out var value) || !value.allowAds)
				{	
					return false;
				}

				IEnumerable<AudioAsset> assets = ExtendedRadio.GetAudiAssetsFromAudioDataBase(__instance, segment.type);
				List<AudioAsset> list = [.. assets];
				System.Random rnd = new();
				List<int> list2 = (from x in Enumerable.Range(0, list.Count)
								orderby rnd.Next()
								select x).Take(segment.clipsCap).ToList();
				AudioAsset[] array = new AudioAsset[segment.clipsCap];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = list[list2[i]];
				}

				segment.clips = array;

				return false;
			}
			return true;
		}
	}
}
