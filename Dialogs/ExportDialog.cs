using App.Models;
using App.Services;

namespace App.Dialogs
{
    public partial class ExportDialog : Form
    {
        private readonly List<AudioCut> _audioCuts;
        private readonly AudioFile _audioFile;

        // Controles UI
        private Label _lblMessage = null!;
        private Label _lblOutputPath = null!;
        private TextBox _txtOutputPath = null!;
        private Button _btnBrowse = null!;
        private Label _lblSummary = null!;
        private ProgressBar _progressBar = null!;
        private Button _btnExport = null!;
        private Button _btnCancel = null!;

        public ExportDialog(List<AudioCut> audioCuts, AudioFile audioFile)
        {
            _audioCuts = audioCuts ?? new List<AudioCut>();
            _audioFile = audioFile ?? throw new ArgumentNullException(nameof(audioFile));

            InitializeComponent();
            InitializeForm();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(500, 300);
            this.Text = "Export Audio Tracks";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Main message
            _lblMessage = new Label
            {
                Text = "Selected audio tracks will be exported",
                Location = new Point(20, 20),
                Size = new Size(450, 40),
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            // Output folder label
            _lblOutputPath = new Label
            {
                Text = "Output folder:",
                Location = new Point(20, 70),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 9F)
            };

            // TextBox path
            _txtOutputPath = new TextBox
            {
                Location = new Point(20, 95),
                Size = new Size(370, 25),
                ReadOnly = true,
                BackColor = Color.White
            };

            // Botón Browse
            _btnBrowse = new Button
            {
                Text = "...",
                Location = new Point(400, 93),
                Size = new Size(60, 27)
            };
            _btnBrowse.Click += OnBrowseClick;

            // Label resumen
            _lblSummary = new Label
            {
                Location = new Point(20, 135),
                Size = new Size(450, 60),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            // ProgressBar
            _progressBar = new ProgressBar
            {
                Location = new Point(20, 200),
                Size = new Size(450, 23),
                Visible = false
            };

            // Export Button
            _btnExport = new Button
            {
                Text = "Export",
                Location = new Point(280, 230),
                Size = new Size(90, 30),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnExport.FlatAppearance.BorderSize = 0;
            _btnExport.Click += OnExportClick;

            // Cancel Button
            _btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(380, 230),
                Size = new Size(90, 30),
                Font = new Font("Segoe UI", 9F)
            };
            _btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            // Agregar controles al formulario
            this.Controls.AddRange(new Control[]
            {
                _lblMessage,
                _lblOutputPath,
                _txtOutputPath,
                _btnBrowse,
                _lblSummary,
                _progressBar,
                _btnExport,
                _btnCancel
            });
        }

        private void InitializeForm()
        {
            // Configurar ruta de salida por defecto (subcarpeta "tracks" en el directorio del archivo original)
            if (_audioFile != null)
            {
                var originalDirectory = Path.GetDirectoryName(_audioFile.FilePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var defaultOutputPath = Path.Combine(originalDirectory, "tracks");
                _txtOutputPath.Text = defaultOutputPath;
            }
            else
            {
                _txtOutputPath.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "tracks");
            }

            // Actualizar resumen
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            if (_audioFile == null)
            {
                _lblSummary.Text = "No file loaded";
                return;
            }

            // Calculate selected tracks only
            var selectedTracks = _audioCuts.Where(cut => cut.IsSelected).ToList();

            if (selectedTracks.Count == 0)
            {
                _lblSummary.Text = "⚠ No tracks selected for export";
                _lblSummary.ForeColor = Color.FromArgb(200, 100, 0);
                _btnExport.Enabled = false;
                return;
            }

            _btnExport.Enabled = true;
            _lblSummary.ForeColor = Color.FromArgb(100, 100, 100);

            // Calculate total duration
            var totalDuration = TimeSpan.Zero;
            foreach (var track in selectedTracks)
            {
                totalDuration = totalDuration.Add(track.Duration);
            }

            // Estimate size (WAV stereo 16-bit 44.1kHz ≈ 172KB/second)
            var estimatedSizeBytes = (long)(totalDuration.TotalSeconds * 176400);
            var estimatedSizeMB = estimatedSizeBytes / (1024.0 * 1024.0);

            _lblSummary.Text = $"Selected tracks: {selectedTracks.Count}\n" +
                              $"Total duration: {FormatDuration(totalDuration)}\n" +
                              $"Estimated size: {estimatedSizeMB:F1} MB";
        }

        private string FormatDuration(TimeSpan duration)
        {
            var totalMinutes = (int)duration.TotalMinutes;
            var seconds = duration.Seconds;
            var centiseconds = duration.Milliseconds / 10;
            return $"{totalMinutes:D2}:{seconds:D2}:{centiseconds:D2}";
        }

        private void OnBrowseClick(object? sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog
            {
                Description = "Select destination folder for tracks",
                ShowNewFolderButton = true,
                SelectedPath = _txtOutputPath.Text
            };

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                _txtOutputPath.Text = folderDialog.SelectedPath;
            }
        }

        private async void OnExportClick(object? sender, EventArgs e)
        {
            // Validate selected tracks
            var selectedTracks = _audioCuts.Where(cut => cut.IsSelected).ToList();
            if (selectedTracks.Count == 0)
            {
                MessageBox.Show("No tracks selected for export.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Validate output directory
            if (string.IsNullOrWhiteSpace(_txtOutputPath.Text))
            {
                MessageBox.Show("Please select a valid destination folder.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Crear carpeta si no existe
                Directory.CreateDirectory(_txtOutputPath.Text);

                // Deshabilitar controles durante exportación
                _btnExport.Enabled = false;
                _btnBrowse.Enabled = false;
                _btnCancel.Enabled = false;
                _progressBar.Visible = true;
                _progressBar.Value = 0;

                // Crear progress handler
                var progress = new Progress<int>(p =>
                {
                    if (InvokeRequired)
                    {
                        Invoke(() => _progressBar.Value = p);
                    }
                    else
                    {
                        _progressBar.Value = p;
                    }
                });

                // Exportar
                var exporter = new WaveformExporter();
                await exporter.ExportCutsAsync(selectedTracks, _audioFile, _txtOutputPath.Text, progress);

                MessageBox.Show($"Export completed successfully.\n\n" +
                              $"Files saved to:\n{_txtOutputPath.Text}",
                              "Export Completed",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during export:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Restaurar controles
                _btnExport.Enabled = true;
                _btnBrowse.Enabled = true;
                _btnCancel.Enabled = true;
                _progressBar.Visible = false;
            }
        }
    }
}
