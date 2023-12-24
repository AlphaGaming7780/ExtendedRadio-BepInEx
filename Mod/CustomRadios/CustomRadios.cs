using System.IO;
using System.Collections.Generic;
using Colossal.Json;
using Game.Audio.Radio;
using static Game.Audio.Radio.Radio;
using ExtendedRadio.Patches;
using HarmonyLib;

namespace ExtendedRadio
{
	public class CustomRadios
	{
		private static readonly List<string> radioDirectories = [];
		internal static List<string> customeRadioChannelsName = [];
		private static readonly List<string> customeNetworksName = [];
		private static Dictionary<string, RadioNetwork> m_Networks = [];
		private static Dictionary<string, RuntimeRadioChannel> m_RadioChannels = [];
		private static int radioNetworkIndex;
		static internal void LoadCustomRadios() {

			m_Networks = ExtendedRadio.radioTravers.Field("m_Networks").GetValue<Dictionary<string, RadioNetwork>>();
			m_RadioChannels = ExtendedRadio.radioTravers.Field("m_RadioChannels").GetValue<Dictionary<string, RuntimeRadioChannel>>();

			radioNetworkIndex = m_Networks.Count;

			foreach(string radioDirectory in radioDirectories) {
				foreach(string radioNetwork in Directory.GetDirectories( radioDirectory )) {
					if(radioNetwork != radioDirectory) {
						// if(Directory.GetFiles(radioNetwork, "*.ogg").Length == 0) {
							
						RadioNetwork network = new();

						if(File.Exists(radioNetwork + "//RadioNetwork.json")) {
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

							if(!File.Exists(radioStation + "//RadioChannel.json")) {
								radioChannel =CreateRadioFromPath(radioStation, network.name);
							} else {
								radioChannel = JsonToRadio(radioStation, network.name);
							}
									
							ExtendedRadio.AddAudioToDataBase(radioChannel);
							customeRadioChannelsName.Add(radioChannel.name);
							m_RadioChannels.Add(radioChannel.name, radioChannel.CreateRuntime(radioStation));
						}
						// } else {
						// 	RadioChannel radioChannel;

						// 	if(!File.Exists(radioNetwork+"//RadioChannel.json")) {
						// 		radioChannel = CreateRadioFromPath(radioNetwork, "Public Citizen Radio");
						// 	} else {
						// 		radioChannel = JsonToRadio(radioNetwork, "Public Citizen Radio");
						// 	}
							
						// 	AddAudioToDataBase(radioChannel);
						// 	customeRadioChannelsName.Add(radioChannel.name);
						// 	m_RadioChannels.Add(radioChannel.name, radioChannel.CreateRuntime(radioNetwork));
						// }
					}
				}
			}

			ExtendedRadio.radioTravers.Field("m_Networks").SetValue(m_Networks);
			ExtendedRadio.radioTravers.Field("m_RadioChannels").SetValue(m_RadioChannels);
			ExtendedRadio.radioTravers.Field("m_CachedRadioChannelDescriptors").SetValue(null);
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

			ExtendedRadio.AddAudioToDataBase(radioChannel);
			customeRadioChannelsName.Add(radioChannel.name);
			m_RadioChannels.Add(radioChannel.name, radioChannel.CreateRuntime(path));

			return true;
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
				radioChannel.name = radioChannel.name + "_" + ExtendedRadio.radioTravers.Method("MakeUniqueRandomName", radioChannel.name, 4).GetValue<string>();
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

						if(segment.tags.Length <= 0) {
							segment.tags = [(segment.type.ToString() == "Playlist" ? "Music" : segment.type.ToString()), radioChannel.name, radioChannel.network];
						}
						
						foreach(string audioAssetDirectory in Directory.GetDirectories( segmentDirectory )) {
							foreach(string audioAssetFile in Directory.GetFiles(audioAssetDirectory, "*.ogg")) {
								
								segment.clips = segment.clips.AddToArray(MusicLoader.LoadAudioFile(audioAssetFile, segment.type, radioChannel.network, radioChannel.name));

								// string jsAudioAsset = audioAssetFile[..^".ogg".Count()]+".json";

								// if(File.Exists(jsAudioAsset)) {
								// 	segment.clips = segment.clips.AddToArray(MusicLoader.JsonToAudioAsset(jsAudioAsset, segment.type, radioChannel.network, radioChannel.name));
								// } else {
								// 	segment.clips = segment.clips.AddToArray(MusicLoader.LoadAudioData(audioAssetFile, radioChannel.name, radioChannel.network, segment.type));
								// }
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

			if(radioChannel == null) {

				string radioName = new DirectoryInfo(path).Name;

				string iconPath = $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/DefaultIcon.svg";

				if(File.Exists(path+"\\icon.svg")) {
					iconPath = $"{GameManager_InitializeThumbnails.COUIBaseLocation}/CustomRadios/{radioNetwork}/{radioName}/icon.svg";
				}

				while (m_RadioChannels.ContainsKey(radioName))
				{
					radioName = radioName + "_" + ExtendedRadio.radioTravers.Method("MakeUniqueRandomName", radioName, 4).GetValue<string>();
				}

				radioChannel = new() {
					network = radioNetwork,
					name = radioName,
					nameId = radioName,
					description = radioName,
					icon = iconPath,
				};
			}

			Segment segment = new()
			{
				type = SegmentType.Playlist,
				clipsCap = 0,
				clips = [],
				tags = ["Music", radioChannel.name]
			};

			foreach(string audioAssetDirectory in Directory.GetDirectories( path )) {
				foreach(string audioAssetFile in Directory.GetFiles(audioAssetDirectory, "*.ogg")) {

					segment.clips = segment.clips.AddToArray(MusicLoader.LoadAudioFile(audioAssetFile, segment.type, radioChannel.network, radioChannel.name));
					
					// string jsAudioAsset = audioAssetFile[..^".ogg".Count()]+".json";

					// if(File.Exists(jsAudioAsset)) {
					// 	segment.clips = segment.clips.AddToArray(MusicLoader.JsonToAudioAsset(jsAudioAsset, segment.type, radioChannel.network, radioChannel.name));
					// } else {
					// 	segment.clips = segment.clips.AddToArray(MusicLoader.LoadAudioData(audioAssetFile, radioChannel.name, radioChannel.network, segment.type));
					// }
				}
			}

			segment.clipsCap = segment.clips.Length;

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
	}
}
