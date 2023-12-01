using System.Collections.Generic;
using System.IO;
using Colossal.IO.AssetDatabase;
using Game.Audio.Radio;
using HarmonyLib;
using UnityEngine;
using static Game.Audio.Radio.Radio;
using Colossal.Json;
using ExtendedRadio.Patches;
using ATL;
using static Colossal.IO.AssetDatabase.AudioAsset;
using System.Diagnostics;

namespace ExtendedRadio
{
	public class ExtendedRadio
	{   
		public delegate void OnRadioLoad();
		public static event OnRadioLoad LoadRadio;

		private static readonly string radioDirectory = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "CustomRadio");

		private static readonly List<RadioChannel> customRadioChannels = [];
		private static readonly List<RadioNetwork> customRadioNetworks = [];
		internal static List<string> customeRadioChannelsName = [];
		internal static List<string> customeNetworksName = [];
		private static readonly Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<SegmentType, List<AudioAsset>>>>> audioDataBase = [];
		private static Dictionary<string, RadioNetwork> m_Networks = [];
		private static Dictionary<string, RuntimeRadioChannel> m_RadioChannels = [];

		private static Traverse radioTravers;

		static internal void OnLoadRadio(Radio __instance) {

			customRadioChannels.Clear();
			customRadioNetworks.Clear();
			audioDataBase.Clear();

			radioTravers = Traverse.Create(__instance);
			m_Networks = radioTravers.Field("m_Networks").GetValue<Dictionary<string, RadioNetwork>>();
			m_RadioChannels = radioTravers.Field("m_RadioChannels").GetValue<Dictionary<string, RuntimeRadioChannel>>();

			int radioNetworkIndex = m_Networks.Count -1;

			foreach(string radioNetwork in Directory.GetDirectories( radioDirectory )) {
				if(radioNetwork != radioDirectory) {
					if(Directory.GetFiles(radioNetwork, "*.ogg").Length == 0) {
						
						RadioNetwork network = new();

						if(Directory.GetFiles(radioNetwork, "RadioNetwork.json").Length > 0) {
							network = CreateRadioNetworkFromJson(radioNetwork);
						} else {
							network.name = new DirectoryInfo(radioNetwork).Name;
							network.nameId = new DirectoryInfo(radioNetwork).Name;
							network.description = "A custom Network";
							network.descriptionId = "A custom Network";
							network.icon = File.Exists(Path.Combine(radioNetwork, "icon.svg")) ? $"{GameManager_InitializeThumbnails.COUIBaseLocation}/CustomRadio/{new DirectoryInfo(radioNetwork).Name}/icon.svg" : $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/DefaultIcon.svg";
							network.uiPriority = radioNetworkIndex++;
							network.allowAds = true;
						}
						
						if(!m_Networks.ContainsKey(network.name)) {
							customeNetworksName.Add(network.name);
							m_Networks.Add(network.name, network);
						}
						
						foreach(string radioStation in Directory.GetDirectories( radioNetwork )) {
							if(radioStation != radioNetwork) {
								RadioChannel radioChannel;

								if(Directory.GetFiles(radioStation, "Radio.json").Length > 0) {
									Stopwatch timer = new();
									timer.Start();
									radioChannel = CreateRadioFromJson(radioStation);
									timer.Stop();
									UnityEngine.Debug.Log($"RadioFromJson = {timer.ElapsedMilliseconds}ms");
								} else if(Directory.GetFiles(radioStation, "RadioChannel.json").Length > 0) {
									Stopwatch timer = new();
									timer.Start();
									radioChannel = CreateRadioFromPathAndJson(radioStation);
									timer.Stop();
									UnityEngine.Debug.Log($"RadioFromPathAndJson = {timer.ElapsedMilliseconds}ms");
								} else {
									Stopwatch timer = new();
									timer.Start();
									radioChannel = CreateRadioFromPath(radioStation, network.name);
									timer.Stop();
									UnityEngine.Debug.Log($"RadioFromPath = {timer.ElapsedMilliseconds}ms");
								}
								AddAudioToDataBase(radioChannel);
								customeRadioChannelsName.Add(radioChannel.name);
								m_RadioChannels.Add(radioChannel.name, radioChannel.CreateRuntime(radioStation));
							}
						}
					} else {

						RadioChannel radioChannel;

						if(Directory.GetFiles(radioNetwork, "RadioChannel.json").Length > 0) {
							radioChannel = CreateRadioFromPathAndJson(radioNetwork);
						} else if(Directory.GetFiles(radioNetwork, "Radio.json").Length > 0) {
							radioChannel = CreateRadioFromJson(radioNetwork);
						} else {
							radioChannel = CreateRadioFromPath(radioNetwork, "Public Citizen Radio");
						}
						AddAudioToDataBase(radioChannel);
						customeRadioChannelsName.Add(radioChannel.name);
						m_RadioChannels.Add(radioChannel.name, radioChannel.CreateRuntime(radioNetwork));
					}
				}
			}

			radioTravers.Field("m_Networks").SetValue(m_Networks);
			radioTravers.Field("m_RadioChannels").SetValue(m_RadioChannels);
			radioTravers.Field("m_CachedRadioChannelDescriptors").SetValue(null);

			try {
				LoadRadio();
			} catch {}
		}

		static internal List<AudioAsset> GetAudiAssetsFromAudioDataBase(Radio radio, SegmentType type) {

			return audioDataBase[radio.currentChannel.network][radio.currentChannel.name][radio.currentChannel.currentProgram.name][type];
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
		public static RadioNetwork CreateRadioNetworkFromJson(string path) {
			return Decoder.Decode(File.ReadAllText(path+"\\RadioNetwork.json")).Make<RadioNetwork>();
		}

		public static string RadioNetworkToJson( RadioNetwork radioNetwork) {
			return Encoder.Encode(radioNetwork, EncodeOptions.None);
		}

		public static RadioChannel CreateRadioFromJson(string path) {
			JsonFormat.RadioChannel jsRadioChannel = Decoder.Decode(File.ReadAllText(path+"\\Radio.json")).Make<JsonFormat.RadioChannel>();

			RadioChannel radioChannel = new()
			{
				network = jsRadioChannel.network,
				name = jsRadioChannel.name,
				nameId = jsRadioChannel.nameId,
				description = jsRadioChannel.description,
				icon = jsRadioChannel.icon,
				uiPriority = -1,
				programs = [],
			};

			foreach(JsonFormat.Program jsProgram in jsRadioChannel.programs) {

				Program program = new() {
					name = jsProgram.name,
					description = jsProgram.description,
					icon = jsProgram.icon,
					startTime = jsProgram.startTime,
					endTime = jsProgram.endTime,
					loopProgram = jsProgram.loopProgram,
					pairIntroOutro = jsProgram.pairIntroOutro,
					segments = [],
				};

				radioChannel.programs = radioChannel.programs.AddToArray(program);

				foreach(JsonFormat.Segment jsSegment in jsProgram.segments) {
					Segment segment = new() {
						type = jsSegment.type,
						clipsCap = jsSegment.clipsCap,
						tags = [..jsSegment.tags],
						clips = [],
					};

					program.segments = program.segments.AddToArray(segment);

					foreach(JsonFormat.AudioAsset jsAudioAsset in jsSegment.clips) {
						AudioAsset audioAsset = new();
						audioAsset.AddTag($"AudioFilePath={path}\\{jsAudioAsset.PathToSong}");

						Dictionary<Metatag, string> m_Metatags = [];
						Traverse audioAssetTravers = Traverse.Create(audioAsset);

						Track track = new(path+jsAudioAsset.PathToSong, true);
						// m_Metatags[Metatag.Title] = jsAudioAsset.Title ?? track.Title;
						// m_Metatags[Metatag.Album] = jsAudioAsset.Album ?? track.Album;
						// m_Metatags[Metatag.Artist] = jsAudioAsset.Artist ?? track.Artist;
						// m_Metatags[Metatag.Type] = jsAudioAsset.Type ?? "Music";
						// m_Metatags[Metatag.Brand] = jsAudioAsset.Brand ?? "Brand";
						// m_Metatags[Metatag.RadioStation] = jsAudioAsset.RadioStation ?? radioChannel.network;
						// m_Metatags[Metatag.RadioChannel] = jsAudioAsset.RadioChannel ?? radioChannel.name;
						// m_Metatags[Metatag.PSAType] = jsAudioAsset.PSAType ?? "";
						// m_Metatags[Metatag.AlertType] = jsAudioAsset.AlertType ?? "";
						// m_Metatags[Metatag.NewsType] = jsAudioAsset.NewsType ?? "";
						// m_Metatags[Metatag.WeatherType] = jsAudioAsset.WeatherType ?? "";

						MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.Title, jsAudioAsset.Title ?? track.Title);
						MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.Album, jsAudioAsset.Album ?? track.Album);
						MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.Artist, track.Artist);
						MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.Type, track, "TYPE", jsAudioAsset.Type ?? "Music");
						MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.Brand, track, "BRAND", jsAudioAsset.Brand ?? "Brand");
						MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.RadioStation, track, "RADIO STATION", jsAudioAsset.RadioStation ?? radioChannel.network);
						MusicLoader.AddMetaTag(audioAsset, m_Metatags, Metatag.RadioChannel, track, "RADIO CHANNEL", jsAudioAsset.RadioChannel ?? radioChannel.name);
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
				}
			}

			return radioChannel;
		}

		static public RadioChannel CreateRadioFromPathAndJson(string path) {
			
			RadioChannel radioChannel = CreateRadioChannelFromJson(path+"\\RadioChannel.json");

			foreach(string programDirectory in Directory.GetDirectories( path )) {
				Program program = CreateProgramFromJson(programDirectory+"\\Program.json");

				foreach(string segmentDirectory in Directory.GetDirectories( programDirectory )) {
					Segment segment = CreatePSegmentFromJson(segmentDirectory+"\\Segment.json", radioChannel.network, radioChannel.name);
					
					program.segments = program.segments.AddToArray(segment);
				}

				radioChannel.programs = radioChannel.programs.AddToArray(program);
			}

			return radioChannel;

		}

		static public RadioChannel CreateRadioFromPath(string path, string radioNetwork = null) {

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

		public static RadioChannel CreateRadioChannelFromJson(string path) {

			JsonFormat.RadioChannel jsRadioChannel = Decoder.Decode(File.ReadAllText(path)).Make<JsonFormat.RadioChannel>();

			RadioChannel radioChannel = new()
			{
				network = jsRadioChannel.network,
				name = jsRadioChannel.name,
				nameId = jsRadioChannel.nameId,
				description = jsRadioChannel.description,
				icon = jsRadioChannel.icon,
				uiPriority = -1,
				programs = [],
			};

			return radioChannel;
		}

		public static Program CreateProgramFromJson(string path) {

			JsonFormat.Program jsProgram = Decoder.Decode(File.ReadAllText(path)).Make<JsonFormat.Program>();

			Program program = new() {
				name = jsProgram.name,
				description = jsProgram.description,
				icon = jsProgram.icon,
				startTime = jsProgram.startTime,
				endTime = jsProgram.endTime,
				loopProgram = jsProgram.loopProgram,
				pairIntroOutro = jsProgram.pairIntroOutro,
				segments = [],
			};

			return program;
		}

		public static Segment CreatePSegmentFromJson(string path, string radioNetwork, string radioChannel) {

			JsonFormat.Segment jsSegment = Decoder.Decode(File.ReadAllText(path)).Make<JsonFormat.Segment>();

			Segment segment = new() {
				type = jsSegment.type,
				clipsCap = jsSegment.clipsCap,
				tags = [..jsSegment.tags],
				clips = [],
			};

			foreach(JsonFormat.AudioAsset jsAudioAsset in jsSegment.clips) {
				AudioAsset audioAsset = new();
				audioAsset.AddTag($"AudioFilePath={path[..^"\\Segment.json".Length]}\\{jsAudioAsset.PathToSong}");

				Dictionary<Metatag, string> m_Metatags = [];
				Traverse audioAssetTravers = Traverse.Create(audioAsset);

				Track track = new(path[..^"\\Segment.json".Length]+"\\"+jsAudioAsset.PathToSong, true);
				// m_Metatags[Metatag.Title] = jsAudioAsset.Title ?? track.Title;
				// m_Metatags[Metatag.Album] = jsAudioAsset.Album ?? track.Album;
				// m_Metatags[Metatag.Artist] = jsAudioAsset.Artist ?? track.Artist;
				// m_Metatags[Metatag.Type] = jsAudioAsset.Type ?? "Music";
				// m_Metatags[Metatag.Brand] = jsAudioAsset.Brand ?? "Brand";
				// m_Metatags[Metatag.RadioStation] = jsAudioAsset.RadioStation ?? radioChannel.network;
				// m_Metatags[Metatag.RadioChannel] = jsAudioAsset.RadioChannel ?? radioChannel.name;
				// m_Metatags[Metatag.PSAType] = jsAudioAsset.PSAType ?? "";
				// m_Metatags[Metatag.AlertType] = jsAudioAsset.AlertType ?? "";
				// m_Metatags[Metatag.NewsType] = jsAudioAsset.NewsType ?? "";
				// m_Metatags[Metatag.WeatherType] = jsAudioAsset.WeatherType ?? "";

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

		public static string RadioToJson( RadioChannel radioChannel) {


			JsonFormat.RadioChannel jsRadioChannel = new()
			{	
				network = radioChannel.network,
				name = radioChannel.name,
				nameId = radioChannel.nameId,
				description = radioChannel.description,
				icon = radioChannel.icon,
				programs = [],
			};

			foreach(Program program in radioChannel.programs) {

				JsonFormat.Program jsProgram = new()
				{
					name = program.name,
					description = program.description,
					icon = program.icon,
					startTime = program.startTime,
					endTime = program.endTime,
					loopProgram = program.loopProgram,
					pairIntroOutro = program.pairIntroOutro,
					segments = [],
				};

				jsRadioChannel.programs.Add(jsProgram);

				foreach(Segment segment in program.segments) {

					JsonFormat.Segment jsSegment = new() {
						type = segment.type,
						clipsCap = segment.clipsCap,
						tags = [..segment.tags],
						clips = [],
					};

					jsProgram.segments.Add(jsSegment);

					foreach(AudioAsset audioAsset in segment.clips) {

						JsonFormat.AudioAsset jsAudioAsset = new() {
							PathToSong = GetClipPathFromAudiAsset(audioAsset),
							Title = audioAsset.GetMetaTag(Metatag.Title),
							Album = audioAsset.GetMetaTag(Metatag.Album),
							Artist = audioAsset.GetMetaTag(Metatag.Artist),
							Type = audioAsset.GetMetaTag(Metatag.Type),
							Brand = audioAsset.GetMetaTag(Metatag.Brand),
							RadioStation = audioAsset.GetMetaTag(Metatag.RadioStation),
							RadioChannel = audioAsset.GetMetaTag(Metatag.RadioChannel),
							PSAType = audioAsset.GetMetaTag(Metatag.PSAType),
							AlertType = audioAsset.GetMetaTag(Metatag.AlertType),
							NewsType = audioAsset.GetMetaTag(Metatag.NewsType),
							WeatherType = audioAsset.GetMetaTag(Metatag.WeatherType),
							// durationMs = audioAsset.durationMs,
							loopStart = audioAsset.loopStart,
							loopEnd = audioAsset.loopEnd,
							alternativeStart = audioAsset.alternativeStart,
							fadeoutTime = audioAsset.fadeoutTime,
						};

						jsSegment.clips.Add(jsAudioAsset);
					}
				}
			}

			return Encoder.Encode(jsRadioChannel, EncodeOptions.None);
		}

		public static string GetClipPathFromAudiAsset(AudioAsset audioAsset) {

			foreach(string s in audioAsset.tags) {
				if(s.Contains("AudioFilePath=")) {
					return s["AudioFilePath=".Length..];
				}
			}
			return "";
		}
	}
}
