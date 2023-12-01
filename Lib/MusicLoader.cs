using System.Collections.Generic;
using System.IO;
using ATL;
using Colossal.IO.AssetDatabase;
using HarmonyLib;
using UnityEngine;
using static Colossal.IO.AssetDatabase.AudioAsset;

namespace ExtendedRadio
{
	public class MusicLoader
	{   
		private static List<AudioAsset> audioAssets = [];

		internal static AudioAsset[] LoadAllAudioClips( string path, string radioStation, string radioNetwork )
		{   
			audioAssets.Clear();
			var oggFiles = Directory.GetFiles( path, "*.ogg" );
			foreach ( var oggFile in oggFiles )
			{	
				LoadAudioClip( oggFile, radioStation, radioNetwork );
			}

			return [..audioAssets];
		}

		private static void LoadAudioClip( string filePath, string radioChannel, string radioNetwork )  /*IEnumerator*/
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
					audioAsset.AddTag($"AudioFilePath={filePath}");

					Track track = new(filePath, true);

					AddMetaTag(audioAsset, m_Metatags, Metatag.Title, track.Title);
					AddMetaTag(audioAsset, m_Metatags, Metatag.Album, track.Album);
					AddMetaTag(audioAsset, m_Metatags, Metatag.Artist, track.Artist);
					AddMetaTag(audioAsset, m_Metatags, Metatag.Type, track, "TYPE");
					AddMetaTag(audioAsset, m_Metatags, Metatag.Brand, track, "BRAND");
					AddMetaTag(audioAsset, m_Metatags, Metatag.RadioStation, radioNetwork);
					AddMetaTag(audioAsset, m_Metatags, Metatag.RadioChannel, radioChannel);
					AddMetaTag(audioAsset, m_Metatags, Metatag.PSAType, track, "PSA TYPE");
					AddMetaTag(audioAsset, m_Metatags, Metatag.AlertType, track, "ALERT TYPE");
					AddMetaTag(audioAsset, m_Metatags, Metatag.NewsType, track, "NEWS TYPE");
					AddMetaTag(audioAsset, m_Metatags, Metatag.WeatherType, track, "WEATHER TYPE");

					// m_Metatags[Metatag.Title] = track.Title;
					// m_Metatags[Metatag.Album] = track.Album; 
					// m_Metatags[Metatag.Artist] = track.Artist;
					// m_Metatags[Metatag.Type] = "Music";
					// m_Metatags[Metatag.Brand] = "Brand";
					// m_Metatags[Metatag.RadioStation] = radioNetwork;
					// m_Metatags[Metatag.RadioChannel] = radioChannel;
					// m_Metatags[Metatag.PSAType] = "";
					// m_Metatags[Metatag.AlertType] = "";
					// m_Metatags[Metatag.NewsType] = "";
					// m_Metatags[Metatag.WeatherType] = "";

					audioAssetTravers.Field("m_Metatags").SetValue(m_Metatags);
					audioAssetTravers.Field("durationMs").SetValue(track.DurationMs);
					audioAssetTravers.Field("m_Instance").SetValue(null);

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

		internal static void AddMetaTag(AudioAsset audioAsset, Dictionary<Metatag, string> m_Metatags, Metatag tag, string value)
		{
			audioAsset.AddTag(value);
			m_Metatags[tag] = value;
		}

		internal static void AddMetaTag(AudioAsset audioAsset, Dictionary<Metatag, string> m_Metatags, Metatag tag, Track trackMeta, string oggTag, string value = null)
		{
			string extendedTag = value ?? GetExtendedTag(trackMeta, oggTag);
			if (!string.IsNullOrEmpty(extendedTag))
			{
				audioAsset.AddTag(oggTag.ToLower() + ":" + extendedTag);
				AddMetaTag(audioAsset, m_Metatags, tag, extendedTag);
			}
		}

		private static string GetExtendedTag(Track trackMeta, string tag)
		{
			if (trackMeta.AdditionalFields.TryGetValue(tag, out var value))
			{
				return value;
			}

			return null;
		}

		internal static bool GetTimeTag(Track trackMeta, string tag, out double time)
		{
			if (trackMeta.AdditionalFields.TryGetValue(tag, out var value) && double.TryParse(value, out time))
			{
				return true;
			}

			time = -1.0;
			return false;
		}

		internal static bool GetTimeTag(Track trackMeta, string tag, out float time)
		{
			if (trackMeta.AdditionalFields.TryGetValue(tag, out var value) && float.TryParse(value, out time))
			{
				return true;
			}

			time = -1f;
			return false;
		}
    }
}
