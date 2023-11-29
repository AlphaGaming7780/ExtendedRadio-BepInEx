using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using ExtendedRadio.MonoBehaviours;
using Game.Audio.Radio;
using Game.SceneFlow;
using Game.UI;
using HarmonyLib;
using UnityEngine;
using static Game.Audio.Radio.Radio;

namespace ExtendedRadio.Patches
{
	[HarmonyPatch(typeof(GameManager), "InitializeThumbnails")]
    internal class GameManager_InitializeThumbnails
    {
		static readonly string IconsResourceKey = $"{MyPluginInfo.PLUGIN_NAME.ToLower()}ui";

		public static readonly string COUIBaseLocation = $"coui://{IconsResourceKey}";
        static void Prefix(GameManager __instance)
        {		

			Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomRadio"));

			string resources = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "resources");

			if(!Directory.Exists(resources)) {
				Directory.CreateDirectory(resources);
				File.Move(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DefaultIcon.svg"), Path.Combine(resources , "DefaultIcon.svg"));
			}

			var gameUIResourceHandler = (GameUIResourceHandler)GameManager.instance.userInterface.view.uiSystem.resourceHandler;
            
			if (gameUIResourceHandler == null)
            {
                Debug.LogError("Failed retrieving GameManager's GameUIResourceHandler instance, exiting.");
                return;
            }
			
			gameUIResourceHandler.HostLocationsMap.Add(
                IconsResourceKey,
                new List<string> {
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                }
            );
		}
	}

	[HarmonyPatch(typeof( Radio ), "LoadRadio")]
	class Radio_LoadRadio {

		public static GameObject gameObjectmusicLoader = new( "MusicLoader" );
		public static MusicLoader musicLoader = gameObjectmusicLoader.AddComponent<MusicLoader>( );
		public static string radioDirectory = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "CustomRadio");

		public static List<string> customeRadioChannel = [];
		public static List<string> customeNetwork = [];

		static void Postfix( Radio __instance) {

			Traverse radioTravers = Traverse.Create(__instance);

			Dictionary<string, RadioNetwork> m_Networks = Traverse.Create(__instance).Field("m_Networks").GetValue<Dictionary<string, RadioNetwork>>();
			Dictionary<string, RuntimeRadioChannel> m_RadioChannels = Traverse.Create(__instance).Field("m_RadioChannels").GetValue<Dictionary<string, RuntimeRadioChannel>>();

			int radioNetworkIndex = m_Networks.Count()-1;

			foreach(string radioNetwork in Directory.GetDirectories( radioDirectory )) {
				if(radioNetwork != radioDirectory) {
					if(Directory.GetFiles(radioNetwork, "*.ogg").Count() == 0) {

						RadioNetwork network = new()
						{
							name = new DirectoryInfo(radioNetwork).Name,
							nameId = new DirectoryInfo(radioNetwork).Name,
							description = "A custom Network",
							descriptionId = "A custom Network",
							icon = File.Exists(Path.Combine(radioNetwork, "icon.svg")) ? $"{GameManager_InitializeThumbnails.COUIBaseLocation}/CustomRadio/{new DirectoryInfo(radioNetwork).Name}/icon.svg" : $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/DefaultIcon.svg",
							uiPriority = radioNetworkIndex++,
							allowAds = true
						};
						
						if(!m_Networks.ContainsKey(network.name)) {
							customeNetwork.Add(network.name);
							m_Networks.Add(network.name, network);
						}
						
						foreach(string radioStation in Directory.GetDirectories( radioNetwork )) {
							if(radioStation != radioNetwork) {
								RadioChannel radioChannel = createRadioStation(radioStation, network.name, m_RadioChannels, radioTravers);
								customeRadioChannel.Add(radioChannel.name);
								m_RadioChannels.Add(radioChannel.name, radioChannel.CreateRuntime(radioStation));
							}
						}
					} else {
						RadioChannel radioChannel = createRadioStation(radioNetwork, "Public Citizen Radio", m_RadioChannels, radioTravers);
						customeRadioChannel.Add(radioChannel.name);
						m_RadioChannels.Add(radioChannel.name, radioChannel.CreateRuntime(radioNetwork));
					}
				}
			}

			radioTravers.Field("m_Networks").SetValue(m_Networks);
			radioTravers.Field("m_RadioChannels").SetValue(m_RadioChannels);
			radioTravers.Field("m_CachedRadioChannelDescriptors").SetValue(null);
		}

		private static RadioChannel createRadioStation( string path, string radioNetwork, Dictionary<string, RuntimeRadioChannel> m_RadioChannels, Traverse radioTravers) {
			
			string radioName = new DirectoryInfo(path).Name;
			while (m_RadioChannels.ContainsKey(radioName))
			{
				radioName = radioName + "_" + radioTravers.Method("MakeUniqueRandomName", radioName, 4).GetValue<string>();
			}
			
			AudioAsset[] audioAssets = musicLoader.LoadAllAudioClips(path, radioName , radioNetwork);

            Segment segment = new()
            {
                type = SegmentType.Playlist,
                clipsCap = audioAssets.Length,
                clips = audioAssets,
                tags = ["type:Music", "radio channel:" + radioName]
            };

            Program program = new()
            {
                name = "My Custom Program",
                description = "My Custom Program",
                icon = "coui://UIResources/Media/Radio/TheVibe.svg",
                startTime = "00:00",
                endTime = "00:00",
                loopProgram = true,
                segments = [segment]
            };

			string iconPath = $"{GameManager_InitializeThumbnails.COUIBaseLocation}/CustomRadio{(customeNetwork.Contains(radioNetwork) ? $"/{radioNetwork}" : "" )}/{radioName}/icon.svg";

            RadioChannel radioChannel = new()
            {
                network = radioNetwork,
                name = radioName,
				nameId = radioName,
                description = "A cutome Radio",
                icon = File.Exists(Path.Combine(path, "icon.svg")) ? iconPath : $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/DefaultIcon.svg", //"Media/Radio/Stations/TheVibe.svg";
                uiPriority = 1,
                programs = [program]
            };

			musicLoader.AddToDataBase(radioChannel);

            return radioChannel;

		}
	}

	[HarmonyPatch( typeof( Radio ), "GetPlaylistClips" )]
	class Radio_GetPlaylistClips
	{
		static bool Prefix( Radio __instance, RuntimeSegment segment)
		{		
			if(Radio_LoadRadio.customeRadioChannel.Contains(__instance.currentChannel.name)) {

				IEnumerable<AudioAsset> assets = Radio_LoadRadio.musicLoader.GetAudiAssets(__instance, segment.type);
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
