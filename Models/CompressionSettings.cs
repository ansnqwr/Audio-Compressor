namespace AudioCompressor.Models
{
   
    public class CompressionSettings
    {
        public int SampleRate { get; set; } = 44100;
        public int BitDepth { get; set; } = 16;
        public int Channels { get; set; } = 2;
        public CompressionAlgorithm Algorithm { get; set; } = CompressionAlgorithm.MP3;
        public int BitRate { get; set; } = 128;
    }
}