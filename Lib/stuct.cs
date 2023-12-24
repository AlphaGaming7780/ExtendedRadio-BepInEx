using System;
using System.Collections.Generic;
using Game.Audio.Radio;

namespace ExtendedRadio.JsonFormat
{	
	[Serializable]
	public class jsAudioAsset
	{
		public string AudioFileFormat = "OGG";
		public string Title = null;
		public string Album = null;
		public string Artist = null;
		public string Type = null;
		public string Brand = null;
		public string RadioStation = null;
		public string RadioChannel = null;
		public string PSAType = null;
		public string AlertType = null;
		public string NewsType = null;
		public string WeatherType = null;
		// public double durationMs;
		public double loopStart = -1;
		public double loopEnd = -1;
		public double alternativeStart = -1;
		public float fadeoutTime = 1f;
	}
}
 