using System.Collections.Generic;
using System.IO;
using Colossal.IO.AssetDatabase;
using HarmonyLib;
using UnityEngine;
using static Colossal.IO.AssetDatabase.AudioAsset;

namespace ExtendedRadio.MonoBehaviours
{
	/// <summary>
	/// A custom .ogg music loader
	/// </summary>
	/// <remarks>
	/// (Looks in {assemblyPath}\music for .ogg files, loads async.)
	/// </remarks>
	/// 

	public class MusicLoader : MonoBehaviour
	{   

		// private int inCoroutine = 0;
		private List<AudioAsset> audioAssets = [];

		public Dictionary<string, List<AudioAsset>> radioDatabase = [];

		public Dictionary<Metatag, string> m_Metatags;

		public AudioAsset[] LoadAllAudioClips( string path, string radioStation, string radioNetwork )
		{   
			m_Metatags = [];
			audioAssets = [];
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
					AudioAsset audioAsset = new();
					Traverse audioAssetTravers = Traverse.Create(audioAsset);
					audioAssetTravers.Field("m_Instance").SetValue(clip);

					TagLib.File tagFile = TagLib.File.Create(filePath);
					m_Metatags[Metatag.Title] = tagFile.Tag.Title;
					// m_Metatags[Metatag.Title] = new DirectoryInfo(filePath).Name;
					m_Metatags[Metatag.Album] = tagFile.Tag.Album; 
					m_Metatags[Metatag.Artist] = tagFile.Tag.FirstPerformer;
					m_Metatags[Metatag.Type] = "Music";
					m_Metatags[Metatag.Brand] = "Brand";
					m_Metatags[Metatag.RadioStation] = radioNetwork;
					m_Metatags[Metatag.RadioChannel] = radioChannel;
					m_Metatags[Metatag.PSAType] = "";
					m_Metatags[Metatag.AlertType] = "";
					m_Metatags[Metatag.NewsType] = "";
					m_Metatags[Metatag.WeatherType] = "";
					audioAssetTravers.Field("m_Metatags").SetValue(m_Metatags);

					audioAssetTravers.Field("durationMs").SetValue((double)tagFile.Properties.Duration.Milliseconds);

					// if (GetTimeTag(track, "LOOPSTART", out double time))
					// {
					// 	audioAssetTravers.Field("loopStart").SetValue(time);
					// }

					// if (GetTimeTag(track, "LOOPEND", out time))
					// {
					// 	audioAssetTravers.Field("loopEnd").SetValue(time);
					// }

					// if (GetTimeTag(track, "ALTERNATIVESTART", out time))
					// {
					// 	audioAssetTravers.Field("alternativeStart").SetValue(time);
					// }

					// if (GetTimeTag(track, "FADEOUTTIME", out float time2))
					// {
					// 	audioAssetTravers.Field("fadeoutTime").SetValue(time2);
					// }

					// // audioAssetTravers.Method("UpdateMetaTags");
					// try {
					// 	foreach((Metatag metatag, string value) in audioAssetTravers.Field("m_Metatags").GetValue<Dictionary<Metatag, string>>()) {
					// 		Debug.Log(metatag + " | " + value);
					// 	}
					// } catch {
					// 	Debug.LogWarning("Failed to print the audio metayag.");
					// }

					
					audioAssets.Add(audioAsset);

					// Debug.Log( "Loaded audio clip: " + audioAsset.GetMetaTag(Metatag.Title)  );
				}
				else
				{
					Debug.LogError( "Failed to load AudioClip from: " + filePath );
				}
			}
		}

		// private static bool GetTimeTag(Track trackMeta, string tag, out double time)
		// {
		// 	if (trackMeta.AdditionalFields.TryGetValue(tag, out var value) && double.TryParse(value, out time))
		// 	{
		// 		return true;
		// 	}

		// 	time = -1.0;
		// 	return false;
		// }

		// private static bool GetTimeTag(Track trackMeta, string tag, out float time)
		// {
		// 	if (trackMeta.AdditionalFields.TryGetValue(tag, out var value) && float.TryParse(value, out time))
		// 	{
		// 		return true;
		// 	}

		// 	time = -1f;
		// 	return false;
		// }

		// public AudioClip GetRandomClip( )
		// {
		//     if ( audioClips.Count > 0 )
		//     {
		//         var randomIndex = Random.Range( 0, audioClips.Count );
		//         return audioClips[randomIndex];
		//     }

		//     return null;
		// }
	}
}
