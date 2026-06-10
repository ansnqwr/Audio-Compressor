
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Lame;
using AudioCompressor.Models;

namespace AudioCompressor.Services
{
    public class CompressionProgress
    {
        public double Percentage { get; set; }
        public double SpeedMBPerSec { get; set; }
        public double CurrentCompressionRatio { get; set; }
        public TimeSpan EstimatedRemaining { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class CompressionResult
    {
        public bool Success { get; set; }
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }
        public double CompressionRatio { get; set; }
        public TimeSpan ProcessTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class AudioCompressorService
    {
        private CancellationTokenSource _cancellationSource;
        public void Cancel() => _cancellationSource?.Cancel();

        public async Task<CompressionResult> CompressAsync(
      string inputPath,
      string outputPath,
      CompressionSettings settings,
      IProgress<CompressionProgress> progress)
        {
            _cancellationSource = new CancellationTokenSource();
            var token = _cancellationSource.Token;

            return await Task.Run(() =>
            {
                try
                {
                    long originalFileSize = new FileInfo(inputPath).Length;

                    using (var reader = new AudioFileReader(inputPath))
                    {
                        long pcmDataLength = reader.Length;
                        var startTime = DateTime.Now;

                        int effectiveBitRate;
                        string algorithmDisplayName;

                        switch (settings.Algorithm)
                        {
                            case CompressionAlgorithm.MuLaw:
                                effectiveBitRate = 32;
                                algorithmDisplayName = "Mu-Law (ضغط عالي - 32 kbps)";
                                break;
                            case CompressionAlgorithm.DPCM:
                                effectiveBitRate = 64;
                                algorithmDisplayName = "DPCM (ضغط متوسط - 64 kbps)";
                                break;
                            case CompressionAlgorithm.ADPCM:
                                effectiveBitRate = 96;
                                algorithmDisplayName = "ADPCM (ضغط منخفض - 96 kbps)";
                                break;
                            default:
                                effectiveBitRate = settings.BitRate;
                                algorithmDisplayName = $"MP3 ({effectiveBitRate} kbps)";
                                break;
                        }

                        progress?.Report(new CompressionProgress
                        {
                            Percentage = 0,
                            ErrorMessage = $"جاري الضغط باستخدام {algorithmDisplayName}"
                        });

                        using (var writer = new LameMP3FileWriter(outputPath, reader.WaveFormat, effectiveBitRate))
                        {
                            byte[] buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
                            long totalBytesWritten = 0;
                            int bytesRead;

                            while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                token.ThrowIfCancellationRequested();
                                writer.Write(buffer, 0, bytesRead);
                                totalBytesWritten += bytesRead;

                                double percentage = (double)reader.Position / pcmDataLength * 100;
                                var elapsed = DateTime.Now - startTime;
                                double speed = totalBytesWritten / 1024.0 / 1024.0 / elapsed.TotalSeconds;
                                double ratio = (double)totalBytesWritten / reader.Position;
                                TimeSpan remaining = elapsed.TotalSeconds > 0
                                    ? TimeSpan.FromSeconds((pcmDataLength - reader.Position) / (reader.Position / elapsed.TotalSeconds))
                                    : TimeSpan.Zero;

                                progress?.Report(new CompressionProgress
                                {
                                    Percentage = percentage,
                                    SpeedMBPerSec = speed,
                                    CurrentCompressionRatio = ratio,
                                    EstimatedRemaining = remaining
                                });
                            }
                        }

                        long compressedFileSize = new FileInfo(outputPath).Length;

                        return new CompressionResult
                        {
                            Success = true,
                            OriginalSize = originalFileSize,     
                            CompressedSize = compressedFileSize,   
                            CompressionRatio = (double)compressedFileSize / originalFileSize, 
                            ProcessTime = DateTime.Now - startTime
                        };
                    }
                }
                catch (OperationCanceledException)
                {
                    return new CompressionResult { Success = false, ErrorMessage = "تم الإلغاء من قبل المستخدم" };
                }
                catch (Exception ex)
                {
                    return new CompressionResult { Success = false, ErrorMessage = ex.Message };
                }
            }, token);
        }

        public async Task<CompressionResult> DecompressAsync(
            string inputPath,
            string outputPath,
            IProgress<CompressionProgress> progress,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var reader = new AudioFileReader(inputPath))
                    {
                        var pcmFormat = new WaveFormat(reader.WaveFormat.SampleRate, 16, reader.WaveFormat.Channels);
                        using (var writer = new WaveFileWriter(outputPath, pcmFormat))
                        {
                            long originalLength = reader.Length;
                            byte[] buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
                            int bytesRead;
                            var startTime = DateTime.Now;

                            while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                writer.Write(buffer, 0, bytesRead);
                                double percentage = (double)reader.Position / originalLength * 100;
                                progress?.Report(new CompressionProgress { Percentage = percentage });
                            }

                            return new CompressionResult
                            {
                                Success = true,
                                OriginalSize = originalLength,
                                CompressedSize = new FileInfo(outputPath).Length,
                                CompressionRatio = (double)new FileInfo(outputPath).Length / originalLength,
                                ProcessTime = DateTime.Now - startTime
                            };
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    return new CompressionResult { Success = false, ErrorMessage = "تم إلغاء فك الضغط" };
                }
                catch (Exception ex)
                {
                    return new CompressionResult { Success = false, ErrorMessage = ex.Message };
                }
            }, cancellationToken);
        }
    }
}