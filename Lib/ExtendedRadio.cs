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
using System.Linq;
using UnityEngine;

namespace ExtendedRadio
{
	public class ExtendedRadio
	{   
		public delegate void OnRadioLoad();
		public static event OnRadioLoad CallOnRadioLoad;

		private static readonly List<string> radioDirectories = [GameManager_InitializeThumbnails.CustomRadiosPath];

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
								network.icon = File.Exists(Path.Combine(radioNetwork, "icon.svg")) ? $"{GameManager_InitializeThumbnails.COUIBaseLocation}/CustomRadios/{new DirectoryInfo(radioNetwork).Name}/icon.svg" : $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/DefaultIcon.svg";
								network.allowAds = true;
							}
							network.name = new DirectoryInfo(radioNetwork).Name;
							network.uiPriority = radioNetworkIndex++;
							
							if(!m_Networks.ContainsKey(network.name)) {
								customeNetworksName.Add(network.name);
								m_Networks.Add(network.name, network);
							}
							
							foreach(string radioStation in Directory.GetDirectories( radioNetwork )) {

								RadioChannel radioChannel;

								if(!File.Exists(radioStation+"//RadioChannel.json")) {
									radioChannel =CreateRadioFromPath(radioStation, network.name);
								} else {
									radioChannel = JsonToRadio(radioStation, network.name);
								}
										
								AddAudioToDataBase(radioChannel);
								customeRadioChannelsName.Add(radioChannel.name);
								m_RadioChannels.Add(radioChannel.name, radioChannel.CreateRuntime(radioStation));
							}
						} else {
							RadioChannel radioChannel;

							if(!File.Exists(radioNetwork+"//RadioChannel.json")) {
								radioChannel = CreateRadioFromPath(radioNetwork, "Public Citizen Radio");
							} else {
								radioChannel = JsonToRadio(radioNetwork, "Public Citizen Radio");
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
					if(audioDataBase.ContainsKey(radioChannel.network)){
						if(audioDataBase[radioChannel.network].ContainsKey(radioChannel.name)) {
							if(audioDataBase[radioChannel.network][radioChannel.name].ContainsKey(program.name)) {
								audioDataBase[radioChannel.network][radioChannel.name][program.name].Add(segment.type, [..segment.clips]);
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
		public static RadioNetwork JsonToRadioNetwork(string path) {
			return Decoder.Decode(File.ReadAllText(path+"\\RadioNetwork.json")).Make<RadioNetwork>();
		}

		public static string RadioNetworkToJson( RadioNetwork radioNetwork) {
			return Encoder.Encode(radioNetwork, EncodeOptions.None);
		}

		public static RadioChannel JsonToRadio(string path, string radioNetwork = null) {
			
			RadioChannel radioChannel = Decoder.Decode(File.ReadAllText(path+"\\RadioChannel.json")).Make<RadioChannel>();
			
			while (m_RadioChannels.ContainsKey(radioChannel.name))
			{
				radioChannel.name = radioChannel.name + "_" + radioTravers.Method("MakeUniqueRandomName", radioChannel.name, 4).GetValue<string>();
			}

			if(radioNetwork != null) {
				radioChannel.network = radioNetwork;
			}

			if(Directory.GetFiles(Directory.GetDirectories( path )[0], "*.ogg").Length == 0 ) {
				foreach(string programDirectory in Directory.GetDirectories( path )) {

					Program program;

					if(File.Exists(programDirectory+"\\Program.json")) {
						program = Decoder.Decode(File.ReadAllText(programDirectory+"\\Program.json")).Make<Program>();
					} else {
						program = new() {
							name = new DirectoryInfo(radioNetwork).Name,
							description = new DirectoryInfo(radioNetwork).Name,
							icon = $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/DefaultIcon.svg",
							startTime = "00:00",
							endTime = "00:00",
							loopProgram = true,
							pairIntroOutro = false
						};
					}

					foreach(string segmentDirectory in Directory.GetDirectories( programDirectory )) {

						Segment segment;

						if(File.Exists(segmentDirectory+"\\Segment.json")) {
							segment = Decoder.Decode(File.ReadAllText(segmentDirectory+"\\Segment.json")).Make<Segment>();
						} else {
							segment = new() {
								type = StringToSegmentType(new DirectoryInfo(radioNetwork).Name),
								tags = [],
								clipsCap = 0,
							};
						}
						
						foreach(string audioAssetDirectory in Directory.GetDirectories( segmentDirectory )) {
							foreach(string audioAssetFile in Directory.GetFiles(audioAssetDirectory, "*.ogg")) {
								
								string jsAudioAsset = audioAssetFile[..^".ogg".Count()]+".json";

								if(File.Exists(jsAudioAsset)) {
									segment.clips = segment.clips.AddToArray(JsonToAudioAsset(jsAudioAsset, radioChannel.network, radioChannel.name));
								} else {
									AudioAsset audioAsset = MusicLoader.LoadAudioData(audioAssetFile, radioChannel.name, radioChannel.network);
									segment.clips = segment.clips.AddToArray(audioAsset);
								}
							}
						}

						if(!File.Exists(segmentDirectory+"\\Segment.json")) segment.clipsCap = segment.clips.Length;

						program.segments = program.segments.AddToArray(segment);
					}

					radioChannel.programs = radioChannel.programs.AddToArray(program);
				}
			} else {
				radioChannel = CreateRadioFromPath(path, radioChannel.network, radioChannel);
			}

			return radioChannel;

		}


		static private RadioChannel CreateRadioFromPath(string path, string radioNetwork = null, RadioChannel radioChannel = null) {

			Debug.Log("RadioChannel");

			if(radioChannel == null) {

				string radioName = new DirectoryInfo(path).Name;

				while (m_RadioChannels.ContainsKey(radioName))
				{
					radioName = radioName + "_" + radioTravers.Method("MakeUniqueRandomName", radioName, 4).GetValue<string>();
				}

				radioChannel = new() {
					network = radioNetwork,
					name = radioName,
					nameId = radioName,
					description = radioName,
					icon = $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/DefaultIcon.svg",
				};
			}

			Debug.Log("Segment");

			Segment segment = new()
			{
				type = SegmentType.Playlist,
				clipsCap = 0,
				clips = [],
				tags = ["type:Music", "radio channel:" + radioChannel.name]
			};

			foreach(string audioAssetDirectory in Directory.GetDirectories( path )) {
				foreach(string audioAssetFile in Directory.GetFiles(audioAssetDirectory, "*.ogg")) {
					
					string jsAudioAsset = audioAssetFile[..^".ogg".Count()]+".json";

					if(File.Exists(jsAudioAsset)) {
						segment.clips = segment.clips.AddToArray(JsonToAudioAsset(jsAudioAsset, radioChannel.network, radioChannel.name));
					} else {
						segment.clips = segment.clips.AddToArray(MusicLoader.LoadAudioData(audioAssetFile, radioChannel.name, radioChannel.network));
					}
				}
			}

			segment.clipsCap = segment.clips.Length;

			Debug.Log("prgram");

			Program program = new()
			{
				name = "My Custom Program",
				description = "My Custom Program",
				icon = $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/DefaultIcon.svg",
				startTime = "00:00",
				endTime = "00:00",
				loopProgram = true,
				segments = [segment]
			};

			radioChannel.programs = radioChannel.programs.AddToArray(program);



			return radioChannel;

		}

		public static RadioChannel JsonToRadioChannel(string path) {
			return Decoder.Decode(File.ReadAllText(path+"\\RadioChannel.json")).Make<RadioChannel>();
		}

		public static Program JsonToProgram(string path) {
			return Decoder.Decode(File.ReadAllText(path+"\\Program.json")).Make<Program>();
		}

		public static Segment JsonToSegment(string path) {

			Segment segment = Decoder.Decode(File.ReadAllText(path+"\\Segment.json")).Make<Segment>();

			return segment;

		}

		public static AudioAsset JsonToAudioAsset(string audioAssetFile, string networkName = null, string radioChannelName = null) {

			jsAudioAsset jsAudioAsset = Decoder.Decode(File.ReadAllText(audioAssetFile)).Make<jsAudioAsset>();

			AudioAsset audioAsset = new();
			audioAsset.AddTag($"AudioFilePath={audioAssetFile[..^".json".Count()]+".ogg"}");

			Dictionary<Metatag, string> m_Metatags = [];
			Traverse audioAssetTravers = Traverse.Create(audioAsset);

			Track track = new(audioAssetFile[..^".json".Count()]+".ogg", true);

			MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.Title, jsAudioAsset.Title ?? track.Title);
			MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.Album, jsAudioAsset.Album ?? track.Album);
			MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.Artist, jsAudioAsset.Artist ?? track.Artist);
			MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.Type, track, "TYPE", jsAudioAsset.Type ?? "Music");
			MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.Brand, track, "BRAND", jsAudioAsset.Brand ?? "Brand");
			MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.RadioStation, track, "RADIO STATION", networkName ?? jsAudioAsset.RadioStation );
			MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.RadioChannel, track, "RADIO CHANNEL", radioChannelName ?? jsAudioAsset.RadioChannel );
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

			return audioAsset;
		}

		public static SegmentType StringToSegmentType(string s) {
            return s switch
            {
                "Playlist" => SegmentType.Playlist,
                "Talkshow" => SegmentType.Talkshow,
                "PSA" => SegmentType.PSA,
                "Weather" => SegmentType.Weather,
                "News" => SegmentType.News,
                "Commercial" => SegmentType.Commercial,
                "Emergency" => SegmentType.Emergency,
                _ => SegmentType.Playlist,
            };
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
