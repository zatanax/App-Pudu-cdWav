using System.ComponentModel;
using App.Models;
using App.Utilities;

namespace App.Controls
{
    public partial class FullWaveformControl : UserControl
    {
        private float[] _decimatedData = Array.Empty<float>();
        private Color _waveColor = Color.Cyan;
        private Color _backgroundColor = Color.Black;
        private Color _gridColor = Color.DarkGray;
        private Color _positionMarkerColor = Color.Yellow;
        private TimeSpan _currentPosition = TimeSpan.Zero;
        private TimeSpan _duration = TimeSpan.Zero;
        private List<AudioCut> _audioCuts = new List<AudioCut>();
        private bool _showGrid = true;
        private int _maxDisplaySamples = 50000;
        private Rectangle _lastMarkerArea = Rectangle.Empty;

        // Cache de Pens para evitar crear objetos en cada paint
        private readonly Dictionary<Color, Pen> _penCache = new Dictionary<Color, Pen>();
        private Pen? _waveformPen;

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

        public FullWaveformControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            BackColor = Color.Black;
            Name = "FullWaveformControl";
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
            if (_decimatedData == null || _decimatedData.Length == 0)
            {
                DrawEmptyState(e.Graphics);
                return;
            }

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            g.Clear(_backgroundColor);

            if (_showGrid)
            {
                DrawGrid(g);
            }

            DrawWaveform(g);
            DrawAudioCutSeparators(g);
            DrawCutMarkers(g);
            DrawPositionMarker(g);
        }

        private void DrawEmptyState(Graphics g)
        {
            g.Clear(_backgroundColor);

            using var font = new Font("Segoe UI", 12);
            using var brush = new SolidBrush(Color.Gray);
            var text = "No audio cargado";
            var size = g.MeasureString(text, font);
            var x = (Width - size.Width) / 2;
            var y = (Height - size.Height) / 2;
            g.DrawString(text, font, brush, x, y);
        }

        private void DrawGrid(Graphics g)
        {
            using var pen = new Pen(_gridColor, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };

            int timeLines = 10;
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

            float samplesPerPixel = (float)_decimatedData.Length / width;

            for (int x = 0; x < width; x++)
            {
                Color pixelColor = GetColorForPixelPosition(x, width);
                Pen pen = GetCachedPen(pixelColor);

                int startSample = (int)(x * samplesPerPixel);
                int endSample = Math.Min((int)((x + 1) * samplesPerPixel), _decimatedData.Length);

                if (startSample >= _decimatedData.Length) break;

                float min = 0, max = 0;

                for (int i = startSample; i < endSample; i++)
                {
                    min = Math.Min(min, _decimatedData[i]);
                    max = Math.Max(max, _decimatedData[i]);
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

        private Pen GetCachedPen(Color color)
        {
            if (!_penCache.TryGetValue(color, out var pen))
            {
                pen = new Pen(color, 1);
                _penCache[color] = pen;
            }
            return pen;
        }

        private Color GetColorForPixelPosition(int pixelX, int totalWidth)
        {
            if (_audioCuts == null || _audioCuts.Count == 0 || _duration.TotalSeconds == 0)
            {
                return _waveColor;
            }

            float timeRatio = (float)pixelX / totalWidth;
            double timeInSeconds = timeRatio * _duration.TotalSeconds;
            TimeSpan currentTime = TimeSpan.FromSeconds(timeInSeconds);

            foreach (var cut in _audioCuts)
            {
                TimeSpan cutStart = cut.Start;
                TimeSpan cutEnd = cut.Start + cut.Duration;

                if (currentTime >= cutStart && currentTime < cutEnd)
                {
                    return cut.CutColor;
                }
            }

            return _waveColor;
        }

        private void DrawAudioCutSeparators(Graphics g)
        {
            if (_audioCuts == null || _audioCuts.Count == 0 || _duration.TotalSeconds == 0) return;

            foreach (var cut in _audioCuts)
            {
                float endPos = (float)((cut.Start + cut.Duration).TotalSeconds / _duration.TotalSeconds);
                int endX = (int)(endPos * Width);

                if (endPos >= 1.0f) continue;

                using var pen = new Pen(Color.FromArgb(180, Color.White), 2);
                g.DrawLine(pen, endX, 0, endX, Height);

                using var brush = new SolidBrush(Color.FromArgb(200, Color.White));

                Point[] triangleTop = {
                    new Point(endX, 0),
                    new Point(endX - 3, 6),
                    new Point(endX + 3, 6)
                };
                g.FillPolygon(brush, triangleTop);

                Point[] triangleBottom = {
                    new Point(endX, Height),
                    new Point(endX - 3, Height - 6),
                    new Point(endX + 3, Height - 6)
                };
                g.FillPolygon(brush, triangleBottom);
            }
        }

        private void DrawCutMarkers(Graphics g)
        {
            if (_audioCuts == null || _audioCuts.Count == 0 || _duration.TotalSeconds == 0) return;

            using var pen = new Pen(Color.Red, 1);

            foreach (var cut in _audioCuts)
            {
                float startPos = (float)(cut.Start.TotalSeconds / _duration.TotalSeconds);
                int startX = (int)(startPos * Width);
                g.DrawLine(pen, startX, 0, startX, Height);

                float endPos = (float)((cut.Start + cut.Duration).TotalSeconds / _duration.TotalSeconds);
                if (endPos < 1.0f)
                {
                    int endX = (int)(endPos * Width);
                    g.DrawLine(pen, endX, 0, endX, Height);
                }
            }
        }

        private void DrawPositionMarker(Graphics g)
        {
            if (_duration.TotalSeconds == 0) return;

            float position = (float)(_currentPosition.TotalSeconds / _duration.TotalSeconds);
            int x = (int)(position * Width);

            using var pen = new Pen(_positionMarkerColor, 3);
            g.DrawLine(pen, x, 0, x, Height);

            using var brush = new SolidBrush(_positionMarkerColor);
            Point[] triangle = {
                new Point(x, 0),
                new Point(x - 5, 10),
                new Point(x + 5, 10)
            };
            g.FillPolygon(brush, triangle);
        }

        public void UpdateCursorPosition(TimeSpan position)
        {
            if (_duration.TotalSeconds == 0 || _currentPosition == position) return;

            if (_lastMarkerArea != Rectangle.Empty)
            {
                Invalidate(_lastMarkerArea);
            }

            _currentPosition = position;

            float positionRatio = (float)(_currentPosition.TotalSeconds / _duration.TotalSeconds);
            int x = (int)(positionRatio * Width);
            int markerWidth = 12;

            _lastMarkerArea = new Rectangle(Math.Max(0, x - markerWidth/2), 0, markerWidth, Height);

            Invalidate(_lastMarkerArea);
        }

        private void OnMouseClick(object? sender, MouseEventArgs e)
        {
            if (_duration.TotalSeconds == 0) return;

            float position = (float)e.X / Width;
            TimeSpan clickedTime = TimeSpan.FromSeconds(position * _duration.TotalSeconds);

            PositionClicked?.Invoke(this, clickedTime);
        }

        private void OnMouseWheel(object? sender, MouseEventArgs e)
        {
            if (_duration.TotalSeconds == 0) return;

            double deltaTimeSeconds = 0.5;
            double currentTimeSeconds = _currentPosition.TotalSeconds;

            if (e.Delta > 0)
                currentTimeSeconds -= deltaTimeSeconds;
            else
                currentTimeSeconds += deltaTimeSeconds;

            currentTimeSeconds = Math.Max(0, Math.Min(currentTimeSeconds, _duration.TotalSeconds));

            PositionClicked?.Invoke(this, TimeSpan.FromSeconds(currentTimeSeconds));
        }

        public void SetAudioData(float[] audioData, TimeSpan duration)
        {
            _duration = duration;

            // Decimate directamente sin guardar copia de audioData
            if (audioData == null || audioData.Length == 0)
            {
                _decimatedData = Array.Empty<float>();
            }
            else
            {
                int targetPoints = Math.Min(audioData.Length, Math.Max(_maxDisplaySamples, this.Width * 2));

                if (audioData.Length <= targetPoints)
                {
                    _decimatedData = new float[audioData.Length];
                    Array.Copy(audioData, _decimatedData, audioData.Length);
                }
                else
                {
                    _decimatedData = WaveformRenderer.PeakDownsample(audioData, targetPoints);
                }
            }

            Invalidate();
        }

        public void SetAudioCuts(List<AudioCut> audioCuts)
        {
            _audioCuts = audioCuts ?? new List<AudioCut>();
            Invalidate();
        }

        public void ClearWaveform()
        {
            _decimatedData = Array.Empty<float>();
            _duration = TimeSpan.Zero;
            _currentPosition = TimeSpan.Zero;
            _audioCuts.Clear();
            _lastMarkerArea = Rectangle.Empty;
            ClearPenCache();
            Invalidate();
        }

        private void ClearPenCache()
        {
            foreach (var pen in _penCache.Values)
            {
                pen.Dispose();
            }
            _penCache.Clear();
            _waveformPen?.Dispose();
            _waveformPen = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ClearPenCache();
            }
            base.Dispose(disposing);
        }
    }
}
