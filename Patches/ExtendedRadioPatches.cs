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
using UnityEngine.Assertions.Must;
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
			var gameUIResourceHandler = (GameUIResourceHandler)GameManager.instance.userInterface.view.uiSystem.resourceHandler;
            
			if (gameUIResourceHandler == null)
            {
                Debug.LogError("Failed retrieving GameManager's GameUIResourceHandler instance, exiting.");
                return;
            }
            // Debug.Log("Retrieved GameManager's GameUIResourceHandler instance.");
			
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

		// private static bool isGameObjectAlreadycreated = false;
		public static GameObject musicLoader = new( "MusicLoader" );
		public static string radioDirectory = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "CustomRadio");

		public static List<string> customeRadioChannel = [];
		public static List<string> customeNetwork = [];

		static void Postfix( Radio __instance) {

			Traverse radioTravers = Traverse.Create(__instance);

			// Debug.Log("Loading Radio");

			musicLoader.AddComponent<MusicLoader>( ); // Our custom music loader
			// musicLoader.GetComponent<MusicLoader>().Setup();

			
			
			// Directory.CreateDirectory(radioDirectory);

			Dictionary<string, RadioNetwork> m_Networks = Traverse.Create(__instance).Field("m_Networks").GetValue<Dictionary<string, RadioNetwork>>();
			Dictionary<string, RuntimeRadioChannel> m_RadioChannels = Traverse.Create(__instance).Field("m_RadioChannels").GetValue<Dictionary<string, RuntimeRadioChannel>>();

			int radioNetworkIndex = m_Networks.Count()-1;

			foreach(string radioNetwork in Directory.GetDirectories( radioDirectory )) {
				if(radioNetwork != radioDirectory) {
					if(Directory.GetFiles(radioNetwork, "*.ogg").Count() == 0) {
						// Debug.Log("Creating Network : " + new DirectoryInfo(radioNetwork).Name);

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
								// Debug.Log("Creating Radio : " + new DirectoryInfo(radioStation).Name);
								RadioChannel radioChannel = createRadioStation(radioStation, network.name);
								string text = radioChannel.name;
								while (m_RadioChannels.ContainsKey(text))
								{
									text = text + "_" + radioTravers.Method("MakeUniqueRandomName", text, 4).GetValue<string>();
								}
								customeRadioChannel.Add(radioChannel.name);
								m_RadioChannels.Add(text, radioChannel.CreateRuntime(radioStation));
							}
						}
					} else {
						// Debug.Log("Creating Radio : " + new DirectoryInfo(radioNetwork).Name);
						RadioChannel radioChannel = createRadioStation(radioNetwork, "Public Citizen Radio");
						string text = radioChannel.name;
						while (m_RadioChannels.ContainsKey(text))
						{
							text = text + "_" + radioTravers.Method("MakeUniqueRandomName", text, 4).GetValue<string>();
						}
						customeRadioChannel.Add(radioChannel.name);
						m_RadioChannels.Add(text, radioChannel.CreateRuntime(radioNetwork));
					}
				}
			}

			radioTravers.Field("m_Networks").SetValue(m_Networks);
			radioTravers.Field("m_RadioChannels").SetValue(m_RadioChannels);
			radioTravers.Field("m_CachedRadioChannelDescriptors").SetValue(null);
		}

		private static RadioChannel createRadioStation( string path, string radioNetwork) {
			
			AudioAsset[] audioAsset = musicLoader.GetComponent<MusicLoader>().LoadAllAudioClips(path, new DirectoryInfo(path).Name , radioNetwork); // , radioNetwork, new DirectoryInfo(path).Name

            Segment segment = new()
            {
                type = SegmentType.Playlist,
                clipsCap = 2,
                clips = audioAsset,
                tags = ["type:Music", "radio channel:" + new DirectoryInfo(path).Name]
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

			string iconPath = $"{GameManager_InitializeThumbnails.COUIBaseLocation}/CustomRadio{(customeNetwork.Contains(radioNetwork) ? $"/{radioNetwork}" : "" )}/{new DirectoryInfo(path).Name}/icon.svg";

            RadioChannel radioChannel = new()
            {
                network = radioNetwork,
                name = new DirectoryInfo(path).Name,
                description = "A cutome Radio",
                icon = File.Exists(Path.Combine(path, "icon.svg")) ? iconPath : $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/DefaultIcon.svg", //"Media/Radio/Stations/TheVibe.svg";
                uiPriority = 1,
                programs = [program]
            };

            return radioChannel;

		}
	}

	[HarmonyPatch( typeof( Radio ), "GetPlaylistClips" )]
	class Radio_GetPlaylistClips
	{
		static bool Prefix( Radio __instance, RuntimeSegment segment)
		{	
			// Debug.Log("Radio Network : " + __instance.currentChannel.network + " | Radio Channel : " + __instance.currentChannel.name);
		
			if(Radio_LoadRadio.customeRadioChannel.Contains(__instance.currentChannel.name)) {
				// string path = Radio_LoadRadio.radioDirectory;
				// path = Radio_LoadRadio.customeNetwork.Contains(__instance.currentChannel.network) ? Path.Combine(path, __instance.currentChannel.network) : path;
				// path = Path.Combine(path, __instance.currentChannel.name);

				// segment.clips = Radio_LoadRadio.musicLoader.GetComponent<MusicLoader>().LoadAllAudioClips(path, __instance.currentChannel.name, __instance.currentChannel.network);
				return false;
			}
			return true;
		}
	}
}
