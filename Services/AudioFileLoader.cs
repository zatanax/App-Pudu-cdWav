using NAudio.Wave;
using App.Models;

namespace App.Services
{
    public static class AudioFileLoader
    {
        public static async Task<AudioFile> LoadWavFileAsync(
            string filePath,
            IProgress<int>? progress = null)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"El archivo no existe: {filePath}");

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (extension != ".wav")
                throw new NotSupportedException("Solo se admiten archivos WAV");

            return await Task.Run(() =>
            {
                progress?.Report(0);

                using var reader = new AudioFileReader(filePath);

                // Crear objeto AudioFile con metadata
                var audioFile = new AudioFile
                {
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    Duration = reader.TotalTime,
                    SampleRate = reader.WaveFormat.SampleRate,
                    Channels = reader.WaveFormat.Channels,
                    BitDepth = reader.WaveFormat.BitsPerSample
                };

                progress?.Report(20);

                // Leer samples (chunk-based para archivos grandes)
                long totalSamples = reader.Length / sizeof(float);
                float[] samples = new float[totalSamples];

                const int bufferSize = 8192;
                float[] buffer = new float[bufferSize];
                int totalRead = 0;

                while (totalRead < totalSamples)
                {
                    int toRead = Math.Min(bufferSize, (int)(totalSamples - totalRead));
                    int samplesRead = reader.Read(buffer, 0, toRead);
                    if (samplesRead == 0) break;

                    Array.Copy(buffer, 0, samples, totalRead, samplesRead);
                    totalRead += samplesRead;

                    // Reportar progreso (20-100%)
                    int percentage = 20 + (int)((long)totalRead * 80 / totalSamples);
                    progress?.Report(percentage);
                }

                audioFile.AudioData = samples;
                progress?.Report(100);

                return audioFile;
            });
        }

        public static string GetImportFileFilter()
        {
            return "Archivos WAV|*.wav|Todos los archivos|*.*";
        }
    }
}
