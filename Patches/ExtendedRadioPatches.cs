using System.Collections.Generic;
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

namespace ExtendedRadio.Patches
{
	[HarmonyPatch(typeof(GameManager), "InitializeThumbnails")]
	internal class GameManager_InitializeThumbnails
	{	
		static readonly string IconsResourceKey = $"{MyPluginInfo.PLUGIN_NAME.ToLower()}";

		public static readonly string COUIBaseLocation = $"coui://{IconsResourceKey}";

		static void Prefix(GameManager __instance)
		{		

			Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomRadio"));

			string resources = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "resources");

			if(!Directory.Exists(resources)) {
				Directory.CreateDirectory(resources);
				File.Move(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DefaultIcon.svg"), Path.Combine(resources , "DefaultIcon.svg"));
			}

			string CustomRadioFolderPlugins = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location).Name,"CustomRadio");

			if(Directory.Exists(CustomRadioFolderPlugins)) {
				ExtendedRadio.RegisterCustomRadioDirectory(CustomRadioFolderPlugins);
			}

			var gameUIResourceHandler = (GameUIResourceHandler)GameManager.instance.userInterface.view.uiSystem.resourceHandler;
			
			if (gameUIResourceHandler == null)
			{
				Debug.LogError("Failed retrieving GameManager's GameUIResourceHandler instance, exiting.");
				return;
			}
			
			gameUIResourceHandler.HostLocationsMap.Add(
				IconsResourceKey,
				[
					Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
				]
			);
		}
	}

	[HarmonyPatch(typeof( Radio ), "LoadRadio")]
	class Radio_LoadRadio {

		static void Postfix( Radio __instance) {

			ExtendedRadio.OnLoadRadio(__instance);

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
				using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + sPath, AudioType.OGGVORBIS);
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
}
