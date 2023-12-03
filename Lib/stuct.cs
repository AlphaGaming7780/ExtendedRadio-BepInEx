using System;
using System.Collections.Generic;
using Game.Audio.Radio;

namespace ExtendedRadio.JsonFormat
{	
	// [Serializable]
	// public class RadioChannel
	// {
	// 	public string name;
	// 	public string nameId;

	// 	public string description;

	// 	public string icon;

	// 	public string network;

	// 	public List<Program> programs = [];
	// }
	// [Serializable]
	// public class Program
	// {
    //     public string name;

    //     public string description;

    //     public string icon;

    //     public string startTime;

    //     public string endTime;

    //     public bool loopProgram;

    //     public bool pairIntroOutro;

    //     public List<Segment> segments = [];
	// }
	[Serializable]
	public class jsSegment
	{
        public Radio.SegmentType type;

        public List<jsAudioAsset> clips = [];

        public List<string> tags = [];

        public int clipsCap;
    }
	[Serializable]
	public class jsAudioAsset
	{
		public string PathToSong = null;
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
 