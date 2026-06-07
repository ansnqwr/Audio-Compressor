using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Lame;
using AudioCompressor.Models;
using AudioCompressor;

namespace AudioCompressor.Services
{
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

            return await Task.Run(async () =>
            {
                try
                {
                    using (var reader = new AudioFileReader(inputPath))
                    {
                        WaveStream conversionStream = null;
                        WaveFormat targetFormat = null;
                        bool conversionFailed = false;

                        try
                        {
                            var outFormat = new WaveFormat(settings.SampleRate, settings.BitDepth, settings.Channels);
                            conversionStream = new WaveFormatConversionStream(outFormat, reader);
                            targetFormat = outFormat;
                        }
                        catch (Exception ex)
                        {
                            conversionFailed = true;
                            progress?.Report(new CompressionProgress
                            {
                                Percentage = 0,
                                ErrorMessage = $"تحذير: تعذر تطبيق الإعدادات ({ex.Message}). سيتم استخدام التنسيق الأصلي."
                            });
                            conversionStream = reader;
                            targetFormat = reader.WaveFormat;
                        }

                        using (conversionStream)
                        {
                            long originalLength = conversionStream.Length;
                            var startTime = DateTime.Now;

                            if (settings.Algorithm == CompressionAlgorithm.MP3)
                            {
                                using (var mp3Writer = new LameMP3FileWriter(outputPath, targetFormat, settings.BitRate))
                                {
                                    return await CompressStream(conversionStream, mp3Writer, outputPath, originalLength, startTime, progress, token);
                                }
                            }
                            else
                            {
                                WaveFormat algoFormat;
                                switch (settings.Algorithm)
                                {
                                    case CompressionAlgorithm.ADPCM:
                                        algoFormat = new WaveFormat(settings.SampleRate, 4, settings.Channels);
                                        break;
                                    case CompressionAlgorithm.DPCM:
                                        algoFormat = new WaveFormat(settings.SampleRate, 8, settings.Channels);
                                        break;
                                    case CompressionAlgorithm.MuLaw:
                                        algoFormat = WaveFormat.CreateMuLawFormat(settings.SampleRate, settings.Channels);
                                        break;
                                    default:
                                        algoFormat = targetFormat;
                                        break;
                                }

                                if (conversionFailed)
                                    algoFormat = targetFormat;

                                using (var writer = new WaveFileWriter(outputPath, algoFormat))
                                {
                                    return await CompressStream(conversionStream, writer, outputPath, originalLength, startTime, progress, token);
                                }
                            }
                        }
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

        private async Task<CompressionResult> CompressStream(WaveStream source, Stream writer, string outputPath, long originalLength, DateTime startTime, IProgress<CompressionProgress> progress, CancellationToken token)
        {
            byte[] buffer = new byte[source.WaveFormat.AverageBytesPerSecond];
            long totalBytesWritten = 0;
            int bytesRead;

            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                token.ThrowIfCancellationRequested();
                writer.Write(buffer, 0, bytesRead);
                totalBytesWritten += bytesRead;

                double percentage = (double)source.Position / originalLength * 100;
                var elapsed = DateTime.Now - startTime;
                double speed = totalBytesWritten / 1024.0 / 1024.0 / elapsed.TotalSeconds;
                double ratio = (double)totalBytesWritten / source.Position;
                TimeSpan remaining = elapsed.TotalSeconds > 0
                    ? TimeSpan.FromSeconds((originalLength - source.Position) / (source.Position / elapsed.TotalSeconds))
                    : TimeSpan.Zero;

                progress?.Report(new CompressionProgress
                {
                    Percentage = percentage,
                    SpeedMBPerSec = speed,
                    CurrentCompressionRatio = ratio,
                    EstimatedRemaining = remaining
                });
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
                            long totalBytesWritten = 0;
                            int bytesRead;
                            var startTime = DateTime.Now;

                            while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                writer.Write(buffer, 0, bytesRead);
                                totalBytesWritten += bytesRead;
                                double percentage = (double)reader.Position / originalLength * 100;
                                var elapsed = DateTime.Now - startTime;
                                double speed = totalBytesWritten / 1024.0 / 1024.0 / elapsed.TotalSeconds;
                                progress?.Report(new CompressionProgress
                                {
                                    Percentage = percentage,
                                    SpeedMBPerSec = speed,
                                    EstimatedRemaining = elapsed.TotalSeconds > 0
                                        ? TimeSpan.FromSeconds((originalLength - reader.Position) / (reader.Position / elapsed.TotalSeconds))
                                        : TimeSpan.Zero
                                });
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