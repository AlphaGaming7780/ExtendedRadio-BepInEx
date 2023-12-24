using System.Collections.Generic;
using ATL;
using Colossal.IO.AssetDatabase;
using HarmonyLib;
using static Colossal.IO.AssetDatabase.AudioAsset;
using static Game.Audio.Radio.Radio;

namespace ExtendedRadio
{
	internal class MusicLoader
	{   

		internal static AudioAsset LoadAudioData( string filePath, string radioChannel, string radioNetwork, SegmentType segmentType )  /*IEnumerator*/
		{
			Dictionary<Metatag, string> m_Metatags = [];

			AudioAsset audioAsset = new();
			Traverse audioAssetTravers = Traverse.Create(audioAsset);
			audioAsset.AddTag($"AudioFilePath={filePath}");

			Track track = new(filePath, true);

			AddMetaTag(audioAsset, m_Metatags, Metatag.Title, track.Title);
			AddMetaTag(audioAsset, m_Metatags, Metatag.Album, track.Album);
			AddMetaTag(audioAsset, m_Metatags, Metatag.Artist, track.Artist);
			AddMetaTag(audioAsset, m_Metatags, Metatag.Type, track, "TYPE", segmentType.ToString() == "Playlist" ? "Music" : segmentType.ToString());
			AddMetaTag(audioAsset, m_Metatags, Metatag.Brand, track, "BRAND", segmentType.ToString() == "Commercial" ? track.Title : null); 
			AddMetaTag(audioAsset, m_Metatags, Metatag.RadioStation, radioNetwork);
			AddMetaTag(audioAsset, m_Metatags, Metatag.RadioChannel, radioChannel);
			AddMetaTag(audioAsset, m_Metatags, Metatag.PSAType, track, "PSA TYPE");
			AddMetaTag(audioAsset, m_Metatags, Metatag.AlertType, track, "ALERT TYPE");
			AddMetaTag(audioAsset, m_Metatags, Metatag.NewsType, track, "NEWS TYPE");
			AddMetaTag(audioAsset, m_Metatags, Metatag.WeatherType, track, "WEATHER TYPE");

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

			return audioAsset;
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
