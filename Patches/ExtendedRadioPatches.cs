using System.Collections.Generic;
using System.IO;
using System.Linq;
using Colossal.IO.AssetDatabase;
using ExtendedRadio.MonoBehaviours;
using Game;
using Game.Audio;
using Game.Audio.Radio;
using HarmonyLib;
using UnityEngine;
using static Game.Audio.Radio.Radio;

namespace ExtendedRadio.Patches
{

	/// <summary>
	/// An example patch
	/// </summary>
	/// <remarks>
	/// (So far the best way I've found to determine when the game AND map is fully loaded.)
	/// </remarks>
	[HarmonyPatch( typeof( AudioManager ), "OnGameLoadingComplete" )]
	class AudioManager_OnGameLoadingCompletePatch
	{
		static void Postfix( AudioManager __instance, Colossal.Serialization.Entities.Purpose purpose, GameMode mode )
		{
			if ( !mode.IsGameOrEditor( ) )
				return;

			UnityEngine.Debug.Log( "Game loaded!" );

			//__instance.World.GetOrCreateSystem<ExtendedRadioSystem>( );
		}
	}

	/// <summary>
	/// Used to load songs before the map is loaded.
	/// </summary>
	/// <remarks>
	/// (Could be more robust, may encounter errors as is
	/// if loading is too slow.)
	/// </remarks>
	// [HarmonyPatch( typeof( AudioManager ), "OnGameLoaded" )]
	// class AudioManager_OnGameLoadedPatch
	// {
	//     static bool Prefix(AudioManager __instance, Colossal.Serialization.Entities.Context serializationContext )
	//     {
	//         Debug.Log("Prefix");
	//         var musicLoader = new GameObject( "MusicLoader" );
	//         musicLoader.AddComponent<RadioLoader>( ); // Our custom music loader
	//         RadioLoader radioLoader = musicLoader.GetComponent<RadioLoader>();
	//         radioLoader.Setup(__instance.radio);
	//         return true;
	//     }
	// }

	[HarmonyPatch(typeof( Radio ), "LoadRadio")]
	class Radio_LoadRadio {

		// private static bool isGameObjectAlreadycreated = false;
		public static GameObject musicLoader = new( "MusicLoader" );
		public static string radioDirectory = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "CustomRadio");

		public static List<string> customeRadioChannel = [];
		public static List<string> customeNetwork = [];

		static void Postfix( Radio __instance) {

			Traverse radioTravers = Traverse.Create(__instance);
			MusicLoader loader;

			Debug.Log("Loading Radio");

			loader = musicLoader.AddComponent<MusicLoader>( ); // Our custom music loader
			musicLoader.tag = "MusicLoader";
			// musicLoader.GetComponent<MusicLoader>().Setup();

			
			
			Directory.CreateDirectory(radioDirectory);

			Dictionary<string, RadioNetwork> m_Networks = Traverse.Create(__instance).Field("m_Networks").GetValue<Dictionary<string, RadioNetwork>>();
			Dictionary<string, RuntimeRadioChannel> m_RadioChannels = Traverse.Create(__instance).Field("m_RadioChannels").GetValue<Dictionary<string, RuntimeRadioChannel>>();

			int radioNetworkIndex = m_Networks.Count()-1;

			foreach(string radioNetwork in Directory.GetDirectories( radioDirectory )) {
				if(radioNetwork != radioDirectory) {
					if(Directory.GetFiles(radioNetwork, "*.ogg").Count() == 0) {
						Debug.Log("Creating Network : " + new DirectoryInfo(radioNetwork).Name);
						RadioNetwork network = new()
						{
							name = new DirectoryInfo(radioNetwork).Name,
							nameId = new DirectoryInfo(radioNetwork).Name,
							description = "A custom radio",
							descriptionId = "A custom radio",
							icon = "Media/Radio/Networks/Commercial.svg",
							uiPriority = radioNetworkIndex++,
							allowAds = true
						};
						customeNetwork.Add(network.name);
						m_Networks.Add(network.name, network);
						
						foreach(string radioStation in Directory.GetDirectories( radioNetwork )) {
							if(radioStation != radioNetwork) {
								Debug.Log("Creating Radio : " + new DirectoryInfo(radioStation).Name);
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
						Debug.Log("Creating Radio : " + new DirectoryInfo(radioNetwork).Name);
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

			// Debug.Log("Now Printing the value of m_Networks");
			// try {
			// 	foreach((string name, RadioNetwork radioNetwork) in radioTravers.Field("m_Networks").GetValue<Dictionary<string, RadioNetwork>>()) {
			// 		Debug.Log(name);
			// 	}
			// } catch {
			// 	Debug.LogWarning("Failled to print the value");
			// }
			// Debug.Log("Now Printing the value of m_RadioChannels");
			// try {
			// 	foreach((string name, RuntimeRadioChannel runtimeRadioChannel) in radioTravers.Field("m_RadioChannels").GetValue<Dictionary<string, RuntimeRadioChannel>>()) {
			// 		Debug.Log(name);
			// 	}
			// } catch {
			// 	Debug.LogWarning("Failled to print the value");
			// }

		}

		private static RadioChannel createRadioStation( string path, string radioNetwork) {
			
			AudioAsset[] audioAsset = musicLoader.GetComponent<MusicLoader>().LoadAllAudioClips(path, new DirectoryInfo(path).Name , radioNetwork); // , radioNetwork, new DirectoryInfo(path).Name

			Segment segment = new();
			segment.type = SegmentType.Playlist;
			segment.clipsCap = 2;
			segment.clips = audioAsset;
			segment.tags = ["type:Music","radio channel:" + new DirectoryInfo(path).Name];

			Program program = new();
			program.name = "test";
			program.description = "test";
			program.icon = "coui://UIResources/Media/Radio/TheVibe.svg";
			program.startTime = "00:00";
			program.endTime = "00:00";
			program.loopProgram = true;
			program.segments = [segment];

			RadioChannel radioChannel = new();
			radioChannel.network = radioNetwork;
			radioChannel.name = new DirectoryInfo(path).Name;
			radioChannel.description = "A cutome Radio";
			radioChannel.icon = "Media/Radio/Stations/TheVibe.svg";
			radioChannel.uiPriority = 1;
			radioChannel.programs = [program];

			return radioChannel;

		}
	}

	[HarmonyPatch( typeof( Radio ), "GetPlaylistClips" )]
	class Radio_GetPlaylistClips
	{
		static bool Prefix( Radio __instance, RuntimeSegment segment)
		{	
			Debug.Log("Radio Network : " + __instance.currentChannel.network + " | Radio Channel : " + __instance.currentChannel.name);
		
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
