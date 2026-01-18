namespace App.Utilities
{
    public static class WaveformRenderer
    {
        /// <summary>
        /// Algoritmo PeakDownsample que preserva picos y valles
        /// Extraído de: CdWavCochayuyo_funciona_bien_los_campos_de_waveform/Cochayuyo/Audio.cs líneas 476-506
        /// </summary>
        public static float[] PeakDownsample(float[] source, int targetPoints)
        {
            if (source.Length <= targetPoints) return source;

            int step = source.Length / targetPoints;
            float[] result = new float[targetPoints];

            for (int i = 0; i < targetPoints; i++)
            {
                int start = i * step;
                int end = Math.Min(start + step, source.Length);

                if (end <= start) continue;

                // Find both min and max to preserve both peaks and valleys
                float min = float.MaxValue;
                float max = float.MinValue;

                for (int j = start; j < end; j++)
                {
                    float val = source[j];
                    if (val < min) min = val;
                    if (val > max) max = val;
                }

                // Use the value with largest absolute magnitude
                result[i] = Math.Abs(min) > Math.Abs(max) ? min : max;
            }

            return result;
        }

        /// <summary>
        /// Obtiene min/max para un pixel específico
        /// </summary>
        public static (float min, float max) GetMinMaxForPixel(
            float[] data,
            int pixelX,
            int totalWidth)
        {
            if (data == null || data.Length == 0 || totalWidth <= 0)
                return (0, 0);

            float samplesPerPixel = (float)data.Length / totalWidth;
            int startSample = (int)(pixelX * samplesPerPixel);
            int endSample = Math.Min((int)((pixelX + 1) * samplesPerPixel), data.Length);

            float min = 0, max = 0;
            for (int i = startSample; i < endSample; i++)
            {
                min = Math.Min(min, data[i]);
                max = Math.Max(max, data[i]);
            }

            return (min, max);
        }
    }
}
