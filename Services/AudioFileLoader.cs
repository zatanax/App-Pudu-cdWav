using NAudio.Wave;
using App.Models;

namespace App.Services
{
    public static class AudioFileLoader
    {
        // Sample rate para visualización del waveform (4000 Hz = ~91% reducción de RAM)
        private const int WaveformTargetSampleRate = 4000;

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

                int originalSampleRate = reader.WaveFormat.SampleRate;
                int channels = reader.WaveFormat.Channels;

                // Crear objeto AudioFile con metadata
                var audioFile = new AudioFile
                {
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    Duration = reader.TotalTime,
                    SampleRate = originalSampleRate,
                    Channels = channels,
                    BitDepth = reader.WaveFormat.BitsPerSample,
                    WaveformSampleRate = WaveformTargetSampleRate
                };

                progress?.Report(10);

                // Calcular factor de reducción
                // Samples por segundo en el archivo original (considerando canales)
                int samplesPerSecondOriginal = originalSampleRate * channels;

                // Cuántos samples del original por cada sample de salida
                int downsampleFactor = samplesPerSecondOriginal / WaveformTargetSampleRate;
                if (downsampleFactor < 1) downsampleFactor = 1;

                // Tamaño del resultado
                long totalOriginalSamples = reader.Length / sizeof(float);
                int totalOutputSamples = (int)(totalOriginalSamples / downsampleFactor) + 1;

                float[] resampledData = new float[totalOutputSamples * 2]; // *2 para min y max (peak detection)
                int outputIndex = 0;

                const int bufferSize = 8192;
                float[] buffer = new float[bufferSize];
                long totalRead = 0;

                float blockMin = 0, blockMax = 0;
                int samplesInBlock = 0;

                while (true)
                {
                    int samplesRead = reader.Read(buffer, 0, bufferSize);
                    if (samplesRead == 0) break;

                    for (int i = 0; i < samplesRead; i++)
                    {
                        float sample = buffer[i];
                        blockMin = Math.Min(blockMin, sample);
                        blockMax = Math.Max(blockMax, sample);
                        samplesInBlock++;

                        // Cuando completamos un bloque, guardamos min y max
                        if (samplesInBlock >= downsampleFactor)
                        {
                            if (outputIndex < resampledData.Length - 1)
                            {
                                resampledData[outputIndex++] = blockMax;
                                resampledData[outputIndex++] = blockMin;
                            }
                            blockMin = 0;
                            blockMax = 0;
                            samplesInBlock = 0;
                        }
                    }

                    totalRead += samplesRead;

                    // Reportar progreso (10-100%)
                    int percentage = 10 + (int)(totalRead * 90 / totalOriginalSamples);
                    progress?.Report(Math.Min(percentage, 99));
                }

                // Guardar último bloque si quedó incompleto
                if (samplesInBlock > 0 && outputIndex < resampledData.Length - 1)
                {
                    resampledData[outputIndex++] = blockMax;
                    resampledData[outputIndex++] = blockMin;
                }

                // Ajustar tamaño del array al tamaño real usado
                if (outputIndex < resampledData.Length)
                {
                    Array.Resize(ref resampledData, outputIndex);
                }

                audioFile.AudioData = resampledData;
                progress?.Report(100);

                return audioFile;
            });
        }

        public static string GetImportFileFilter()
        {
            return "WAV Files|*.wav|All Files|*.*";
        }
    }
}
