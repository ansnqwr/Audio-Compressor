using System;

namespace AudioCompressor
{
    public class CompressionProgress
    {
        public double Percentage { get; set; }          // 0-100
        public long BytesProcessed { get; set; }
        public long TotalBytes { get; set; }
        public double SpeedMBps { get; set; }          
        public double SpeedMBPerSec
        {
            get => SpeedMBps;
            set => SpeedMBps = value;
        }

        public TimeSpan TimeRemaining { get; set; } = TimeSpan.FromSeconds(0);
        public double CurrentCompressionRatio { get; set; } 
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan EstimatedRemaining { get; set; }
        public string ErrorMessage { get; set; }

    }
}
