using NAudio.Wave;
using App.Models;

namespace App.Services
{
    public class WaveformExporter
    {
        public async Task ExportCutsAsync(
            List<AudioCut> cuts,
            AudioFile sourceFile,
            string outputDirectory,
            IProgress<int>? progress = null)
        {
            var selectedCuts = cuts.Where(c => c.IsSelected).ToList();
            if (!selectedCuts.Any())
            {
                throw new InvalidOperationException("No hay cortes seleccionados para exportar");
            }

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            await Task.Run(() =>
            {
                using var reader = new AudioFileReader(sourceFile.FilePath);
                var waveFormat = reader.WaveFormat;
                int blockAlign = waveFormat.BlockAlign;

                for (int i = 0; i < selectedCuts.Count; i++)
                {
                    var cut = selectedCuts[i];
                    string outputPath = Path.Combine(outputDirectory, $"{cut.TrackName}.wav");

                    // Posicionar en inicio del corte
                    reader.CurrentTime = cut.Start;

                    // Calcular bytes a leer (alineado a blockAlign)
                    long totalBytes = (long)(cut.Duration.TotalSeconds * waveFormat.AverageBytesPerSecond);
                    totalBytes = (totalBytes / blockAlign) * blockAlign;

                    // Escribir archivo chunk-based
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

                    progress?.Report((i + 1) * 100 / selectedCuts.Count);
                }
            });
        }
    }
}
