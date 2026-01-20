namespace App.Models
{
    public class AudioFile
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public int BitDepth { get; set; }

        /// <summary>
        /// Datos de audio re-sampleados para visualización (menor resolución que el original)
        /// </summary>
        public float[] AudioData { get; set; } = Array.Empty<float>();

        /// <summary>
        /// Sample rate efectivo de AudioData (para visualización)
        /// </summary>
        public int WaveformSampleRate { get; set; }
    }
}
