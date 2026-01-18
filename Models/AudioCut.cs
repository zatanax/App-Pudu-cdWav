namespace App.Models
{
    public class AudioCut
    {
        public string TrackName { get; set; } = string.Empty;
        public TimeSpan Start { get; set; }
        public TimeSpan Duration { get; set; }
        public Color CutColor { get; set; }
        public bool IsSelected { get; set; } = true;

        // Propiedades calculadas
        public TimeSpan EndTime => Start + Duration;

        public string StartFormatted
        {
            get
            {
                int totalMinutes = (int)Start.TotalMinutes;
                int seconds = Start.Seconds;
                int centiseconds = Start.Milliseconds / 10;
                return $"{totalMinutes:D2}:{seconds:D2}:{centiseconds:D2}";
            }
        }

        public string DurationFormatted
        {
            get
            {
                int totalMinutes = (int)Duration.TotalMinutes;
                int seconds = Duration.Seconds;
                int centiseconds = Duration.Milliseconds / 10;
                return $"{totalMinutes:D2}:{seconds:D2}:{centiseconds:D2}";
            }
        }
    }
}
