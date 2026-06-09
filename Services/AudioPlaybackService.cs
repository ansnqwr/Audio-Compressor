using System;
using NAudio.Wave;

namespace AudioCompressor.Services
{
    public class AudioPlaybackService : IDisposable
    {
        private WaveOutEvent _waveOut;
        private AudioFileReader _audioReader;

        public bool IsPlaying => _waveOut != null && _waveOut.PlaybackState == PlaybackState.Playing;
        public event EventHandler PlaybackStopped;

        public long Position
        {
            get => _audioReader?.Position ?? 0;
            set
            {
                if (_audioReader != null)
                    _audioReader.Position = value;
            }
        }

        public long Length => _audioReader?.Length ?? 0;

        public bool IsFinished => Position >= Length && Length > 0;

        public void Load(string filePath)
        {
            StopAndDispose();
            _audioReader = new AudioFileReader(filePath);
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_audioReader);
            _waveOut.PlaybackStopped += (s, a) => PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }

        public void Play()
        {
            if (IsFinished)
            {
                Position = 0;
            }

            if (_waveOut != null && (_waveOut.PlaybackState == PlaybackState.Paused || _waveOut.PlaybackState == PlaybackState.Stopped))
                _waveOut.Play();
        }

        public void Pause() => _waveOut?.Pause();
        public void Stop() => _waveOut?.Stop();

        private void StopAndDispose()
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _waveOut = null;
            _audioReader?.Dispose();
            _audioReader = null;
        }

        public void Dispose() => StopAndDispose();
    }
}
