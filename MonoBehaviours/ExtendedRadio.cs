using System.Collections.Generic;
using System.IO;
using ATL;
using Colossal.IO.AssetDatabase;
using HarmonyLib;
using UnityEngine;
using static Colossal.IO.AssetDatabase.AudioAsset;
using Game.Audio.Radio;
using static Game.Audio.Radio.Radio;

namespace ExtendedRadio.MonoBehaviours
{
	public class MusicLoader : MonoBehaviour
	{   
		private List<AudioAsset> audioAssets = [];

		public Dictionary<string, List<AudioAsset>> radioDatabase = [];

		private Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<SegmentType, List<AudioAsset>>>>> dataBase = [];

		public AudioAsset[] LoadAllAudioClips( string path, string radioStation, string radioNetwork )
		{   
			audioAssets.Clear();
			var oggFiles = Directory.GetFiles( path, "*.ogg" );
			foreach ( var oggFile in oggFiles )
			{	
				LoadAudioClip( oggFile, radioStation, radioNetwork );
			}

			return [..audioAssets];
		}

		private void LoadAudioClip( string filePath, string radioChannel, string radioNetwork )  /*IEnumerator*/
		{
			var url = "file://" + filePath;
			using ( var www = new WWW( url ) )
			{
				// yield return www;
				AudioClip clip = www.GetAudioClip( false, true, AudioType.OGGVORBIS );
				if ( clip != null )
				{   
					
					Dictionary<Metatag, string> m_Metatags = [];

					AudioAsset audioAsset = new();
					Traverse audioAssetTravers = Traverse.Create(audioAsset);

					Track track = new(filePath, true);
					m_Metatags[Metatag.Title] = track.Title;
					m_Metatags[Metatag.Album] = track.Album; 
					m_Metatags[Metatag.Artist] = track.Artist;
					m_Metatags[Metatag.Type] = "Music";
					m_Metatags[Metatag.Brand] = "Brand";
					m_Metatags[Metatag.RadioStation] = radioNetwork;
					m_Metatags[Metatag.RadioChannel] = radioChannel;
					m_Metatags[Metatag.PSAType] = "";
					m_Metatags[Metatag.AlertType] = "";
					m_Metatags[Metatag.NewsType] = "";
					m_Metatags[Metatag.WeatherType] = "";

					audioAssetTravers.Field("m_Metatags").SetValue(m_Metatags);
					audioAssetTravers.Field("durationMs").SetValue(track.DurationMs);
					audioAssetTravers.Field("m_Instance").SetValue(clip);

					if (GetTimeTag(track, "LOOPSTART", out double time))
					{
						audioAssetTravers.Field("loopStart").SetValue(time);
					}

					if (GetTimeTag(track, "LOOPEND", out time))
					{
						audioAssetTravers.Field("loopEnd").SetValue(time);
					}

					if (GetTimeTag(track, "ALTERNATIVESTART", out time))
					{
						audioAssetTravers.Field("alternativeStart").SetValue(time);
					}

					if (GetTimeTag(track, "FADEOUTTIME", out float time2))
					{
						audioAssetTravers.Field("fadeoutTime").SetValue(time2);
					}

					audioAssets.Add(audioAsset);
				}
				else
				{
					Debug.LogError( "Failed to load AudioClip from: " + filePath );
				}
			}
		}

		private static bool GetTimeTag(Track trackMeta, string tag, out double time)
		{
			if (trackMeta.AdditionalFields.TryGetValue(tag, out var value) && double.TryParse(value, out time))
			{
				return true;
			}

			time = -1.0;
			return false;
		}

		private static bool GetTimeTag(Track trackMeta, string tag, out float time)
		{
			if (trackMeta.AdditionalFields.TryGetValue(tag, out var value) && float.TryParse(value, out time))
			{
				return true;
			}

			time = -1f;
			return false;
		}

		public List<AudioAsset> GetAudiAssets(Radio radio, SegmentType type) {

			return dataBase[radio.currentChannel.network][radio.currentChannel.name][radio.currentChannel.currentProgram.name][type];
		}

		public void AddToDataBase(RadioChannel radioChannel) {
			foreach(Program program in radioChannel.programs) {
				foreach(Segment segment in program.segments) {

					Dictionary<SegmentType, List<AudioAsset>> dict1 = [];
					dict1.Add(segment.type, [..segment.clips]);

					Dictionary<string, Dictionary<SegmentType, List<AudioAsset>>> dict2 = [];
					dict2.Add(program.name, dict1);

					Dictionary<string, Dictionary<string, Dictionary<SegmentType, List<AudioAsset>>>> dict3 = [];
					dict3.Add(radioChannel.name, dict2);

					dataBase.Add(radioChannel.network, dict3);
				}
			}
		}
    }
}
