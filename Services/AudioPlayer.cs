using NAudio.Wave;
using App.Models;

namespace App.Services
{
    public class AudioPlayer : IDisposable
    {
        private IWavePlayer? _wavePlayer;
        private AudioFileReader? _audioFileReader;
        private System.Threading.Timer? _positionTimer;
        private bool _disposed = false;

        public event EventHandler<TimeSpan>? PositionChanged;
        public event EventHandler<Models.PlaybackState>? StateChanged;

        public Models.PlaybackState State { get; private set; } = Models.PlaybackState.Stopped;
        public TimeSpan Duration => _audioFileReader?.TotalTime ?? TimeSpan.Zero;

        public TimeSpan Position
        {
            get => _audioFileReader?.CurrentTime ?? TimeSpan.Zero;
            set => Seek(value);
        }

        public async Task<bool> LoadAsync(string filePath)
        {
            try
            {
                await Task.Run(() =>
                {
                    Stop();
                    DisposeAudio();

                    _audioFileReader = new AudioFileReader(filePath);

                    _wavePlayer = new WaveOutEvent();
                    _wavePlayer.Init(_audioFileReader);
                    _wavePlayer.PlaybackStopped += OnPlaybackStopped;
                });

                UpdateState(Models.PlaybackState.Stopped);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Play()
        {
            if (_wavePlayer == null || State == Models.PlaybackState.Playing) return;

            _wavePlayer.Play();
            UpdateState(Models.PlaybackState.Playing);
            StartPositionTimer();
        }

        public void Pause()
        {
            if (_wavePlayer == null || State != Models.PlaybackState.Playing) return;

            _wavePlayer.Pause();
            UpdateState(Models.PlaybackState.Paused);
            StopPositionTimer();
        }

        public void Stop()
        {
            if (_wavePlayer == null) return;

            _wavePlayer.Stop();
            if (_audioFileReader != null)
                _audioFileReader.CurrentTime = TimeSpan.Zero;

            UpdateState(Models.PlaybackState.Stopped);
            StopPositionTimer();
        }

        public void Seek(TimeSpan position)
        {
            if (_audioFileReader == null) return;

            var clampedPosition = TimeSpan.FromMilliseconds(
                Math.Max(0, Math.Min(Duration.TotalMilliseconds, position.TotalMilliseconds)));

            _audioFileReader.CurrentTime = clampedPosition;
            PositionChanged?.Invoke(this, clampedPosition);
        }

        private void StartPositionTimer()
        {
            _positionTimer?.Dispose();
            _positionTimer = new System.Threading.Timer(OnPositionTimer, null, 0, 30);
        }

        private void StopPositionTimer()
        {
            _positionTimer?.Dispose();
            _positionTimer = null;
        }

        private void OnPositionTimer(object? state)
        {
            if (State == Models.PlaybackState.Playing && _audioFileReader != null)
            {
                PositionChanged?.Invoke(this, _audioFileReader.CurrentTime);
            }
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            UpdateState(Models.PlaybackState.Stopped);
            StopPositionTimer();
        }

        private void UpdateState(Models.PlaybackState newState)
        {
            if (State != newState)
            {
                State = newState;
                StateChanged?.Invoke(this, newState);
            }
        }

        public void Close()
        {
            DisposeAudio();
            UpdateState(Models.PlaybackState.Stopped);
        }

        private void DisposeAudio()
        {
            StopPositionTimer();
            _wavePlayer?.Dispose();
            _audioFileReader?.Dispose();
            _wavePlayer = null;
            _audioFileReader = null;
        }

        public void Dispose()
        {
            if (_disposed) return;

            DisposeAudio();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
