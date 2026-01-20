using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using App.Models;

namespace App.Services
{
    public class ExportSettings
    {
        public bool UseOriginalFormat { get; set; } = true;
        public int SampleRate { get; set; } = 44100;
        public int BitsPerSample { get; set; } = 16;
    }

    public class WaveformExporter
    {
        public async Task ExportCutsAsync(
            List<AudioCut> cuts,
            AudioFile sourceFile,
            string outputDirectory,
            IProgress<int>? progress = null,
            ExportSettings? settings = null)
        {
            settings ??= new ExportSettings();

            var selectedCuts = cuts.Where(c => c.IsSelected).ToList();
            if (!selectedCuts.Any())
            {
                throw new InvalidOperationException("No selected cuts to export");
            }

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            await Task.Run(() =>
            {
                for (int i = 0; i < selectedCuts.Count; i++)
                {
                    var cut = selectedCuts[i];
                    string outputPath = Path.Combine(outputDirectory, $"{cut.TrackName}.wav");

                    using var reader = new AudioFileReader(sourceFile.FilePath);

                    if (settings.UseOriginalFormat)
                    {
                        ExportOriginalFormat(reader, cut, outputPath);
                    }
                    else
                    {
                        ExportWithConversion(reader, cut, outputPath, settings);
                    }

                    progress?.Report((i + 1) * 100 / selectedCuts.Count);
                }
            });
        }

        private void ExportOriginalFormat(AudioFileReader reader, AudioCut cut, string outputPath)
        {
            var waveFormat = reader.WaveFormat;
            int blockAlign = waveFormat.BlockAlign;

            reader.CurrentTime = cut.Start;

            long totalBytes = (long)(cut.Duration.TotalSeconds * waveFormat.AverageBytesPerSecond);
            totalBytes = (totalBytes / blockAlign) * blockAlign;

            using var outputStream = File.Create(outputPath);
            using var writer = new WaveFileWriter(outputStream, waveFormat);

            const int chunkSize = 4096;
            byte[] buffer = new byte[chunkSize];
            long bytesRead = 0;

            while (bytesRead < totalBytes)
            {
                int toRead = (int)Math.Min(chunkSize, totalBytes - bytesRead);
                toRead = (toRead / blockAlign) * blockAlign;
                if (toRead == 0) break;

                int read = reader.Read(buffer, 0, toRead);
                if (read == 0) break;

                writer.Write(buffer, 0, read);
                bytesRead += read;
            }
        }

        private void ExportWithConversion(AudioFileReader reader, AudioCut cut, string outputPath, ExportSettings settings)
        {
            reader.CurrentTime = cut.Start;

            // Create resampler if needed
            ISampleProvider sampleProvider = reader;

            if (reader.WaveFormat.SampleRate != settings.SampleRate)
            {
                sampleProvider = new WdlResamplingSampleProvider(sampleProvider, settings.SampleRate);
            }

            // Target format
            var targetFormat = new WaveFormat(settings.SampleRate, settings.BitsPerSample, reader.WaveFormat.Channels);

            // Calculate samples to read
            int totalSamples = (int)(cut.Duration.TotalSeconds * settings.SampleRate * reader.WaveFormat.Channels);

            using var outputStream = File.Create(outputPath);
            using var writer = new WaveFileWriter(outputStream, targetFormat);

            const int bufferSize = 4096;
            float[] floatBuffer = new float[bufferSize];
            int samplesRead = 0;

            while (samplesRead < totalSamples)
            {
                int toRead = Math.Min(bufferSize, totalSamples - samplesRead);
                int read = sampleProvider.Read(floatBuffer, 0, toRead);
                if (read == 0) break;

                // Convert float samples to target bit depth
                WriteSamples(writer, floatBuffer, read, settings.BitsPerSample);
                samplesRead += read;
            }
        }

        private void WriteSamples(WaveFileWriter writer, float[] samples, int count, int bitsPerSample)
        {
            for (int i = 0; i < count; i++)
            {
                float sample = Math.Clamp(samples[i], -1f, 1f);

                switch (bitsPerSample)
                {
                    case 8:
                        // 8-bit is unsigned
                        byte sample8 = (byte)((sample + 1f) * 127.5f);
                        writer.Write(new byte[] { sample8 }, 0, 1);
                        break;

                    case 16:
                        short sample16 = (short)(sample * 32767f);
                        byte[] bytes16 = BitConverter.GetBytes(sample16);
                        writer.Write(bytes16, 0, 2);
                        break;

                    case 24:
                        int sample24 = (int)(sample * 8388607f);
                        byte[] bytes24 = new byte[3];
                        bytes24[0] = (byte)(sample24 & 0xFF);
                        bytes24[1] = (byte)((sample24 >> 8) & 0xFF);
                        bytes24[2] = (byte)((sample24 >> 16) & 0xFF);
                        writer.Write(bytes24, 0, 3);
                        break;

                    case 32:
                        byte[] bytes32 = BitConverter.GetBytes(sample);
                        writer.Write(bytes32, 0, 4);
                        break;
                }
            }
        }
    }
}
