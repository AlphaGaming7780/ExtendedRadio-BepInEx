using System.IO;
using System.Collections.Generic;
using Colossal.Json;
using Colossal.IO.AssetDatabase;
using static Colossal.IO.AssetDatabase.AudioAsset;
using Game.Audio.Radio;
using static Game.Audio.Radio.Radio;
using ExtendedRadio.Patches;
using ExtendedRadio.JsonFormat;
using ATL;
using HarmonyLib;
namespace ExtendedRadio
{
	public class ExtendedRadio
	{   
		public delegate void OnRadioLoad();
		public static event OnRadioLoad CallOnRadioLoad;

		private static readonly List<string> radioDirectories = [Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "CustomRadio")];

		// private static readonly string radioDirectory = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "CustomRadio");

		private static readonly List<RadioChannel> customRadioChannels = [];
		private static readonly List<RadioNetwork> customRadioNetworks = [];
		internal static List<string> customeRadioChannelsName = [];
		private static readonly List<string> customeNetworksName = [];
		private static readonly Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<SegmentType, List<AudioAsset>>>>> audioDataBase = [];
		private static Dictionary<string, RadioNetwork> m_Networks = [];
		private static Dictionary<string, RuntimeRadioChannel> m_RadioChannels = [];

		private static Traverse radioTravers;

		private static int radioNetworkIndex;
		static internal void OnLoadRadio(Radio __instance) {

			customRadioChannels.Clear();
			customRadioNetworks.Clear();
			audioDataBase.Clear();

			radioTravers = Traverse.Create(__instance);
			m_Networks = radioTravers.Field("m_Networks").GetValue<Dictionary<string, RadioNetwork>>();
			m_RadioChannels = radioTravers.Field("m_RadioChannels").GetValue<Dictionary<string, RuntimeRadioChannel>>();

			radioNetworkIndex = m_Networks.Count;

			foreach(string radioDirectory in radioDirectories) {
				foreach(string radioNetwork in Directory.GetDirectories( radioDirectory )) {
					if(radioNetwork != radioDirectory) {
						if(Directory.GetFiles(radioNetwork, "*.ogg").Length == 0) {
							
							RadioNetwork network = new();

							if(Directory.GetFiles(radioNetwork, "RadioNetwork.json").Length > 0) {
								network = JsonToRadioNetwork(radioNetwork);
							} else {
								network.nameId = new DirectoryInfo(radioNetwork).Name;
								network.description = "A custom Network";
								network.descriptionId = "A custom Network";
								network.icon = File.Exists(Path.Combine(radioNetwork, "icon.svg")) ? $"{GameManager_InitializeThumbnails.COUIBaseLocation}/CustomRadio/{new DirectoryInfo(radioNetwork).Name}/icon.svg" : $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/DefaultIcon.svg";
								network.allowAds = true;
							}
							network.name = new DirectoryInfo(radioNetwork).Name;
							network.uiPriority = radioNetworkIndex++;
							
							if(!m_Networks.ContainsKey(network.name)) {
								customeNetworksName.Add(network.name);
								m_Networks.Add(network.name, network);
							}
							
							foreach(string radioStation in Directory.GetDirectories( radioNetwork )) {
								if(radioStation != radioNetwork) {
									RadioChannel radioChannel;

									if(Directory.GetFiles(radioStation, "RadioChannel.json").Length > 0) {
										radioChannel = JsonToRadio(radioStation, network.name);
									} else {
										radioChannel = CreateRadioFromPath(radioStation, network.name);
									}
									AddAudioToDataBase(radioChannel);
									customeRadioChannelsName.Add(radioChannel.name);
									m_RadioChannels.Add(radioChannel.name, radioChannel.CreateRuntime(radioStation));
								}
							}
						} else {

							RadioChannel radioChannel;

							if(Directory.GetFiles(radioNetwork, "RadioChannel.json").Length > 0) {
								radioChannel = JsonToRadio(radioNetwork, "Public Citizen Radio");
							} else {
								radioChannel = CreateRadioFromPath(radioNetwork, "Public Citizen Radio");
							}
							AddAudioToDataBase(radioChannel);
							customeRadioChannelsName.Add(radioChannel.name);
							m_RadioChannels.Add(radioChannel.name, radioChannel.CreateRuntime(radioNetwork));
						}
					}
				}
			}

			try {
				CallOnRadioLoad();
			} catch {}

			radioTravers.Field("m_Networks").SetValue(m_Networks);
			radioTravers.Field("m_RadioChannels").SetValue(m_RadioChannels);
			radioTravers.Field("m_CachedRadioChannelDescriptors").SetValue(null);
		}

		public static void RegisterCustomRadioDirectory(string path) {
			radioDirectories.Add(path);
		}

		public static bool AddRadioNetworkToTheGame(RadioNetwork radioNetwork) {

			if(m_Networks.ContainsKey(radioNetwork.name)) return false;

			radioNetwork.uiPriority = radioNetworkIndex++;
			customeNetworksName.Add(radioNetwork.name);
			m_Networks.Add(radioNetwork.name, radioNetwork);

			return true;
		}

		public static bool AddRadioChannelToTheGame( RadioChannel radioChannel, string path = "") {

			if (customeRadioChannelsName.Contains(radioChannel.name)) return false;

			AddAudioToDataBase(radioChannel);
			customeRadioChannelsName.Add(radioChannel.name);
			m_RadioChannels.Add(radioChannel.name, radioChannel.CreateRuntime(path));

			return true;
		}

		static internal void AddAudioToDataBase(RadioChannel radioChannel) {
			foreach(Program program in radioChannel.programs) {
				foreach(Segment segment in program.segments) {
					Dictionary<SegmentType, List<AudioAsset>> dict1 = [];
					dict1.Add(segment.type, [..segment.clips]);

					Dictionary<string, Dictionary<SegmentType, List<AudioAsset>>> dict2 = [];
					dict2.Add(program.name, dict1);

					if(audioDataBase.ContainsKey(radioChannel.network)){
						audioDataBase[radioChannel.network].Add(radioChannel.name, dict2);
					} else {
						Dictionary<string, Dictionary<string, Dictionary<SegmentType, List<AudioAsset>>>> dict3 = [];
						dict3.Add(radioChannel.name, dict2);
						audioDataBase.Add(radioChannel.network, dict3);
					}
				}
			}
		}
		public static RadioNetwork JsonToRadioNetwork(string path) {
			return Decoder.Decode(File.ReadAllText(path+"\\RadioNetwork.json")).Make<RadioNetwork>();
		}

		public static string RadioNetworkToJson( RadioNetwork radioNetwork) {
			return Encoder.Encode(radioNetwork, EncodeOptions.None);
		}

		public static RadioChannel JsonToRadio(string path, string radioNetwork = null) {
			
			RadioChannel radioChannel = Decoder.Decode(File.ReadAllText(path+"\\RadioChannel.json")).Make<RadioChannel>();
			
			if(radioNetwork != null) {
				radioChannel.network = radioNetwork;
			}

			foreach(string programDirectory in Directory.GetDirectories( path )) {
				Program program = Decoder.Decode(File.ReadAllText(programDirectory+"\\Program.json")).Make<Program>();

				foreach(string segmentDirectory in Directory.GetDirectories( programDirectory )) {
					Segment segment = JsonToSegment(segmentDirectory+"\\Segment.json", radioChannel.network, radioChannel.name);
					
					program.segments = program.segments.AddToArray(segment);
				}

				radioChannel.programs = radioChannel.programs.AddToArray(program);
			}

			return radioChannel;

		}

		static private RadioChannel CreateRadioFromPath(string path, string radioNetwork = null) {

			string radioName = new DirectoryInfo(path).Name;
			while (m_RadioChannels.ContainsKey(radioName))
			{
				radioName = radioName + "_" + radioTravers.Method("MakeUniqueRandomName", radioName, 4).GetValue<string>();
			}
			
			AudioAsset[] audioAssets = MusicLoader.LoadAllAudioClips(path, radioName, radioNetwork);

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

			string iconPath = $"{GameManager_InitializeThumbnails.COUIBaseLocation}/CustomRadio{(customeNetworksName.Contains(radioNetwork) ? $"/{radioNetwork}" : "" )}/{radioName}/icon.svg";

			RadioChannel radioChannel = new()
			{
				network = radioNetwork,
				name = radioName,
				nameId = radioName,
				description = "A cutome Radio",
				icon = File.Exists(Path.Combine(path, "icon.svg")) ? iconPath : $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/DefaultIcon.svg",
				uiPriority = 1,
				programs = [program]
			};

			return radioChannel;

		}

		public static RadioChannel JsonToRadioChannel(string path) {
			return Decoder.Decode(File.ReadAllText(path+"\\RadioChannel.json")).Make<RadioChannel>();
		}

		public static Program JsonToProgram(string path) {
			return Decoder.Decode(File.ReadAllText(path+"\\Program.json")).Make<Program>();
		}

		public static Segment JsonToSegment(string path, string radioNetwork = null, string radioChannel =  null) {

			jsSegment jsSegment = Decoder.Decode(File.ReadAllText(path)).Make<jsSegment>();

			Segment segment = new() {
				type = jsSegment.type,
				clipsCap = jsSegment.clipsCap,
				tags = [..jsSegment.tags],
				clips = [],
			};

			foreach(jsAudioAsset jsAudioAsset in jsSegment.clips) {
				AudioAsset audioAsset = new();
				audioAsset.AddTag($"AudioFilePath={path[..^"\\Segment.json".Length]}\\{jsAudioAsset.PathToSong}");

				Dictionary<Metatag, string> m_Metatags = [];
				Traverse audioAssetTravers = Traverse.Create(audioAsset);

				Track track = new(path[..^"\\Segment.json".Length]+"\\"+jsAudioAsset.PathToSong, true);

				MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.Title, jsAudioAsset.Title ?? track.Title);
				MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.Album, jsAudioAsset.Album ?? track.Album);
				MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.Artist, track.Artist);
				MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.Type, track, "TYPE", jsAudioAsset.Type ?? "Music");
				MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.Brand, track, "BRAND", jsAudioAsset.Brand ?? "Brand");
				MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.RadioStation, track, "RADIO STATION", jsAudioAsset.RadioStation ?? radioNetwork);
				MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.RadioChannel, track, "RADIO CHANNEL", jsAudioAsset.RadioChannel ?? radioChannel);
				MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.PSAType, track, "PSA TYPE", jsAudioAsset.PSAType);
				MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.AlertType, track, "ALERT TYPE", jsAudioAsset.AlertType);
				MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.NewsType, track, "NEWS TYPE", jsAudioAsset.NewsType);
				MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.WeatherType, track, "WEATHER TYPE", jsAudioAsset.WeatherType);

				audioAssetTravers.Field("m_Metatags").SetValue(m_Metatags);
				audioAssetTravers.Field("durationMs").SetValue(track.DurationMs);
				audioAssetTravers.Field("m_Instance").SetValue(null);

				if (jsAudioAsset.loopStart == -1 && MusicLoader.GetTimeTag(track, "LOOPSTART", out double time))
				{
					audioAssetTravers.Field("loopStart").SetValue(time);
				} else {
					audioAssetTravers.Field("loopStart").SetValue(jsAudioAsset.loopStart);
				}

				if (jsAudioAsset.loopEnd == -1 && MusicLoader.GetTimeTag(track, "LOOPEND", out time))
				{
					audioAssetTravers.Field("loopEnd").SetValue(time);
				} else {
					audioAssetTravers.Field("loopEnd").SetValue(jsAudioAsset.loopEnd);
				}

				if (jsAudioAsset.alternativeStart == -1 && MusicLoader.GetTimeTag(track, "ALTERNATIVESTART", out time))
				{
					audioAssetTravers.Field("alternativeStart").SetValue(time);
				} else {
					audioAssetTravers.Field("alternativeStart").SetValue(jsAudioAsset.alternativeStart);
				}

				if (jsAudioAsset.fadeoutTime == -1 && MusicLoader.GetTimeTag(track, "FADEOUTTIME", out float time2))
				{
					audioAssetTravers.Field("fadeoutTime").SetValue(time2);
				} else {
					audioAssetTravers.Field("fadeoutTime").SetValue(jsAudioAsset.fadeoutTime);
				}

				segment.clips = segment.clips.AddToArray(audioAsset);
			}

			return segment;

		}

		internal static string GetClipPathFromAudiAsset(AudioAsset audioAsset) {

			foreach(string s in audioAsset.tags) {
				if(s.Contains("AudioFilePath=")) {
					return s["AudioFilePath=".Length..];
				}
			}
			return "";
		}
		static internal List<AudioAsset> GetAudiAssetsFromAudioDataBase(Radio radio, SegmentType type) {

			return audioDataBase[radio.currentChannel.network][radio.currentChannel.name][radio.currentChannel.currentProgram.name][type];
		}
	}
}
