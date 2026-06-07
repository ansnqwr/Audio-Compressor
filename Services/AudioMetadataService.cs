using System;
using System.IO;
using NAudio.Wave;
using AudioCompressor.Models;

namespace AudioCompressor.Services
{
    public class AudioMetadataService
    {
        public AudioFileInfo GetMetadata(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("الملف غير موجود", filePath);

            var info = new AudioFileInfo
            {
                FilePath = filePath,
                FileSizeBytes = new FileInfo(filePath).Length
            };

            using (var reader = new AudioFileReader(filePath))
            {
                info.Duration = reader.TotalTime;
                info.SampleRate = reader.WaveFormat.SampleRate;
                info.Channels = reader.WaveFormat.Channels;
                info.BitDepth = reader.WaveFormat.BitsPerSample;
                if (info.Duration.TotalSeconds > 0)
                {
                    long fileBits = info.FileSizeBytes * 8;
                    info.BitRate = (int)(fileBits / info.Duration.TotalSeconds / 1000);
                }
            }

            string ext = Path.GetExtension(filePath).ToLower();
            switch(ext)
            {
                case ".mp3":
                    info.Codec = "MP3";
                    break;
                case ".wav":
                    info.Codec = "PCM WAV";
                    break;
                case ".m4a":
                case ".mp4":
                    info.Codec = "AAC";
                    break;
                case ".flac":
                    info.Codec = "FLAC";
                    break;
                case ".aac":
                    info.Codec = "AAC";
                    break;
                case ".ogg":
                    info.Codec = "OGG";
                    break;
                default:
                    info.Codec = "Unknown";
                    break;
            }
           
            return info;
        }
    }
}