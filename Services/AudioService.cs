using App.Models;

namespace App.Services
{
    public class AudioService : IDisposable
    {
        private readonly AudioPlayer _player;
        private AudioFile? _currentAudio;
        private bool _disposed = false;

        public event EventHandler<AudioFile>? AudioLoaded;
        public event EventHandler<TimeSpan>? PositionChanged;
        public event EventHandler<PlaybackState>? StateChanged;

        public AudioFile? CurrentAudio => _currentAudio;
        public PlaybackState State => _player.State;
        public TimeSpan Position => _player.Position;
        public TimeSpan Duration => _player.Duration;

        public AudioService()
        {
            _player = new AudioPlayer();
            _player.PositionChanged += (s, e) => PositionChanged?.Invoke(this, e);
            _player.StateChanged += (s, e) => StateChanged?.Invoke(this, e);
        }

        public async Task<AudioFile> LoadWavFileAsync(string filePath, IProgress<int>? progress = null)
        {
            var audioFile = await AudioFileLoader.LoadWavFileAsync(filePath, progress);
            _currentAudio = audioFile;

            await _player.LoadAsync(filePath);

            AudioLoaded?.Invoke(this, audioFile);
            return audioFile;
        }

        public void Play()
        {
            _player.Play();
        }

        public void Pause()
        {
            _player.Pause();
        }

        public void Stop()
        {
            _player.Stop();
        }

        public void Seek(TimeSpan position)
        {
            _player.Seek(position);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _player?.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
