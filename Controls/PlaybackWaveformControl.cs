using System.ComponentModel;
using App.Models;

namespace App.Controls
{
    public partial class PlaybackWaveformControl : UserControl
    {
        private float[] _fullAudioData = Array.Empty<float>();
        private Color _waveColor = Color.Lime;
        private Color _backgroundColor = Color.Black;
        private Color _gridColor = Color.DarkGray;
        private Color _positionMarkerColor = Color.White;
        private TimeSpan _currentPosition = TimeSpan.Zero;
        private TimeSpan _duration = TimeSpan.Zero;
        private List<AudioCut> _audioCuts = new List<AudioCut>();
        private bool _showGrid = true;

        private readonly TimeSpan _windowDuration = TimeSpan.FromSeconds(20);
        private float[] _windowData = Array.Empty<float>();
        private int _samplesPerSecond = 44100;
        private TimeSpan _currentWindowStart = TimeSpan.Zero;
        private bool _needsWindowUpdate = true;

        private System.Windows.Forms.Timer _refreshTimer;
        private bool _isPlaying = false;
        private Rectangle _lastCursorArea = Rectangle.Empty;

        public event EventHandler<TimeSpan>? PositionClicked;

        [Category("Appearance")]
        [Description("Color de la onda")]
        public Color WaveColor
        {
            get => _waveColor;
            set { _waveColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        [Description("Color de fondo")]
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set { _backgroundColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        [Description("Mostrar grilla")]
        public bool ShowGrid
        {
            get => _showGrid;
            set { _showGrid = value; Invalidate(); }
        }

        public PlaybackWaveformControl()
        {
            InitializeComponent();

            _refreshTimer = new System.Windows.Forms.Timer();
            _refreshTimer.Interval = 30; // 30ms = ~33 FPS
            _refreshTimer.Tick += OnRefreshTimer;
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            BackColor = Color.Black;
            Name = "PlaybackWaveformControl";
            MouseClick += OnMouseClick;
            MouseWheel += OnMouseWheel;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            ResumeLayout(false);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            g.Clear(_backgroundColor);

            if (_windowData == null || _windowData.Length == 0)
            {
                DrawEmptyState(g);
                return;
            }

            if (_showGrid)
            {
                DrawGrid(g);
            }

            DrawWaveform(g);
            DrawCutMarkersInWindow(g);
            DrawPositionMarker(g);
        }

        private void DrawEmptyState(Graphics g)
        {
            using var font = new Font("Segoe UI", 12);
            using var brush = new SolidBrush(Color.Gray);
            var text = "Vista en tiempo real (20s)";
            var size = g.MeasureString(text, font);
            var x = (Width - size.Width) / 2;
            var y = (Height - size.Height) / 2;
            g.DrawString(text, font, brush, x, y);
        }

        private void DrawGrid(Graphics g)
        {
            using var pen = new Pen(_gridColor, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };

            int timeLines = 4;
            for (int i = 1; i < timeLines; i++)
            {
                int x = Width * i / timeLines;
                g.DrawLine(pen, x, 0, x, Height);
            }

            int ampLines = 5;
            for (int i = 1; i < ampLines; i++)
            {
                int y = Height * i / ampLines;
                g.DrawLine(pen, 0, y, Width, y);
            }

            using var centerPen = new Pen(_gridColor, 2);
            int centerY = Height / 2;
            g.DrawLine(centerPen, 0, centerY, Width, centerY);
        }

        private void DrawWaveform(Graphics g)
        {
            int width = Width;
            int height = Height;
            int centerY = height / 2;

            float samplesPerPixel = (float)_windowData.Length / width;

            for (int x = 0; x < width; x++)
            {
                using var pen = new Pen(_waveColor, 1);

                int startSample = (int)(x * samplesPerPixel);
                int endSample = Math.Min((int)((x + 1) * samplesPerPixel), _windowData.Length);

                if (startSample >= _windowData.Length) break;

                float min = 0, max = 0;

                for (int i = startSample; i < endSample; i++)
                {
                    min = Math.Min(min, _windowData[i]);
                    max = Math.Max(max, _windowData[i]);
                }

                int y1 = centerY - (int)(max * centerY * 0.9f);
                int y2 = centerY - (int)(min * centerY * 0.9f);

                y1 = Math.Max(0, Math.Min(height - 1, y1));
                y2 = Math.Max(0, Math.Min(height - 1, y2));

                if (y1 == y2)
                {
                    g.DrawLine(pen, x, y1, x, y1 + 1);
                }
                else
                {
                    g.DrawLine(pen, x, y1, x, y2);
                }
            }
        }

        private void DrawCutMarkersInWindow(Graphics g)
        {
            if (_audioCuts == null || _audioCuts.Count == 0 || _duration.TotalSeconds == 0) return;

            var windowEnd = _currentWindowStart.Add(_windowDuration);

            using var pen = new Pen(Color.Red, 1);

            foreach (var cut in _audioCuts)
            {
                var cutStart = cut.Start;
                var cutEnd = cut.Start + cut.Duration;

                if (cutStart >= _currentWindowStart && cutStart <= windowEnd)
                {
                    float position = (float)((cutStart - _currentWindowStart).TotalSeconds / _windowDuration.TotalSeconds);
                    int x = (int)(position * Width);
                    g.DrawLine(pen, x, 0, x, Height);
                }

                if (cutEnd >= _currentWindowStart && cutEnd <= windowEnd && cutEnd < _duration)
                {
                    float position = (float)((cutEnd - _currentWindowStart).TotalSeconds / _windowDuration.TotalSeconds);
                    int x = (int)(position * Width);
                    g.DrawLine(pen, x, 0, x, Height);
                }
            }
        }

        private void DrawPositionMarker(Graphics g)
        {
            if (_duration.TotalSeconds == 0) return;

            var relativePosition = _currentPosition - _currentWindowStart;

            if (relativePosition < TimeSpan.Zero || relativePosition > _windowDuration)
                return;

            float position = (float)(relativePosition.TotalSeconds / _windowDuration.TotalSeconds);
            int x = (int)(position * Width);

            using var pen = new Pen(_positionMarkerColor, 1);
            g.DrawLine(pen, x, 0, x, Height);
        }

        public void UpdateCursorPosition(TimeSpan position)
        {
            var oldPosition = _currentPosition;
            _currentPosition = position;

            bool needsNewBlock = CheckIfNeedsNewBlock(oldPosition, position);

            if (needsNewBlock)
            {
                _needsWindowUpdate = true;
                UpdateWindowIfNeeded();
                Invalidate();
            }
            else
            {
                UpdateWindowIfNeeded();
                InvalidateCursorArea();
            }
        }

        private void InvalidateCursorArea()
        {
            var relativePosition = _currentPosition - _currentWindowStart;

            if (relativePosition >= TimeSpan.Zero && relativePosition <= _windowDuration)
            {
                float posRatio = (float)(relativePosition.TotalSeconds / _windowDuration.TotalSeconds);
                int x = (int)(posRatio * Width);

                Rectangle newCursorArea = new Rectangle(Math.Max(0, x - 3), 0, 6, Height);

                if (!_lastCursorArea.IsEmpty)
                {
                    int minX = Math.Max(0, Math.Min(_lastCursorArea.X, newCursorArea.X));
                    int maxX = Math.Min(Width - 1, Math.Max(_lastCursorArea.Right, newCursorArea.Right));
                    Rectangle combinedArea = new Rectangle(minX, 0, maxX - minX, Height);

                    Invalidate(combinedArea);
                }
                else
                {
                    Invalidate(newCursorArea);
                }

                _lastCursorArea = newCursorArea;
            }
        }

        private void UpdateWindowIfNeeded()
        {
            var windowEnd = _currentWindowStart.Add(_windowDuration);

            if (_currentPosition < _currentWindowStart || _currentPosition >= windowEnd || _needsWindowUpdate)
            {
                if (_needsWindowUpdate)
                {
                    _currentWindowStart = _currentPosition;
                }
                else
                {
                    var blockSize = _windowDuration.TotalSeconds;
                    var blockNumber = (int)(_currentPosition.TotalSeconds / blockSize);
                    _currentWindowStart = TimeSpan.FromSeconds(blockNumber * blockSize);
                }

                if (_currentWindowStart < TimeSpan.Zero)
                    _currentWindowStart = TimeSpan.Zero;

                var maxStart = _duration - _windowDuration;
                if (maxStart < TimeSpan.Zero)
                    maxStart = TimeSpan.Zero;

                if (_currentWindowStart > maxStart)
                    _currentWindowStart = maxStart;

                UpdateWindowData();
                _needsWindowUpdate = false;
            }
        }

        private bool CheckIfNeedsNewBlock(TimeSpan oldPosition, TimeSpan newPosition)
        {
            if (_duration.TotalSeconds == 0) return false;

            int oldBlock = (int)(oldPosition.TotalSeconds / _windowDuration.TotalSeconds);
            int newBlock = (int)(newPosition.TotalSeconds / _windowDuration.TotalSeconds);

            return oldBlock != newBlock;
        }

        private void UpdateWindowData()
        {
            if (_fullAudioData == null || _fullAudioData.Length == 0)
            {
                _windowData = Array.Empty<float>();
                return;
            }

            var samplesPerSecond = _fullAudioData.Length / _duration.TotalSeconds;

            int startSample = (int)(_currentWindowStart.TotalSeconds * samplesPerSecond);
            int windowSamples = (int)(_windowDuration.TotalSeconds * samplesPerSecond);
            int endSample = Math.Min(startSample + windowSamples, _fullAudioData.Length);

            if (startSample >= _fullAudioData.Length)
            {
                _windowData = Array.Empty<float>();
                return;
            }

            int actualSamples = endSample - startSample;
            _windowData = new float[actualSamples];

            Array.Copy(_fullAudioData, startSample, _windowData, 0, actualSamples);
        }

        private void OnMouseClick(object? sender, MouseEventArgs e)
        {
            if (_duration.TotalSeconds == 0) return;

            float position = (float)e.X / Width;
            TimeSpan clickedTime = _currentWindowStart.Add(TimeSpan.FromSeconds(position * _windowDuration.TotalSeconds));

            PositionClicked?.Invoke(this, clickedTime);
        }

        private void OnMouseWheel(object? sender, MouseEventArgs e)
        {
            if (_duration.TotalSeconds == 0) return;

            double deltaTimeSeconds = 1.0;
            double currentTimeSeconds = _currentPosition.TotalSeconds;

            if (e.Delta > 0)
                currentTimeSeconds -= deltaTimeSeconds;
            else
                currentTimeSeconds += deltaTimeSeconds;

            currentTimeSeconds = Math.Max(0, Math.Min(currentTimeSeconds, _duration.TotalSeconds));

            PositionClicked?.Invoke(this, TimeSpan.FromSeconds(currentTimeSeconds));
        }

        public void SetAudioData(float[] audioData, TimeSpan duration, int sampleRate = 44100)
        {
            _fullAudioData = audioData ?? Array.Empty<float>();
            _duration = duration;
            _samplesPerSecond = sampleRate;
            _needsWindowUpdate = true;

            UpdateWindowIfNeeded();
            Invalidate();
        }

        public void SetAudioCuts(List<AudioCut> audioCuts)
        {
            _audioCuts = audioCuts ?? new List<AudioCut>();
            Invalidate();
        }

        public void StartPlayback()
        {
            _isPlaying = true;
            _refreshTimer.Start();
        }

        public void StopPlayback()
        {
            _isPlaying = false;
            _refreshTimer.Stop();
        }

        public void ClearWaveform()
        {
            _fullAudioData = Array.Empty<float>();
            _windowData = Array.Empty<float>();
            _duration = TimeSpan.Zero;
            _currentPosition = TimeSpan.Zero;
            _currentWindowStart = TimeSpan.Zero;
            _needsWindowUpdate = true;
            _audioCuts.Clear();
            _isPlaying = false;
            _refreshTimer.Stop();
            Invalidate();
        }

        private void OnRefreshTimer(object? sender, EventArgs e)
        {
            if (_isPlaying && _duration.TotalSeconds > 0)
            {
                InvalidateCursorArea();
            }
        }
    }
}
