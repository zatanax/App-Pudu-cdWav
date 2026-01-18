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
            this.Text = "Exportar Pistas de Audio";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Mensaje principal
            _lblMessage = new Label
            {
                Text = "Se exportarán las pistas de audio seleccionadas",
                Location = new Point(20, 20),
                Size = new Size(450, 40),
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            // Label carpeta de destino
            _lblOutputPath = new Label
            {
                Text = "Carpeta de destino:",
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

            // Botón Exportar
            _btnExport = new Button
            {
                Text = "Exportar",
                Location = new Point(280, 230),
                Size = new Size(90, 30),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnExport.FlatAppearance.BorderSize = 0;
            _btnExport.Click += OnExportClick;

            // Botón Cancelar
            _btnCancel = new Button
            {
                Text = "Cancelar",
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
                _lblSummary.Text = "No hay archivo cargado";
                return;
            }

            // Calcular solo pistas seleccionadas
            var selectedTracks = _audioCuts.Where(cut => cut.IsSelected).ToList();

            if (selectedTracks.Count == 0)
            {
                _lblSummary.Text = "⚠ No hay pistas seleccionadas para exportar";
                _lblSummary.ForeColor = Color.FromArgb(200, 100, 0);
                _btnExport.Enabled = false;
                return;
            }

            _btnExport.Enabled = true;
            _lblSummary.ForeColor = Color.FromArgb(100, 100, 100);

            // Calcular duración total
            var totalDuration = TimeSpan.Zero;
            foreach (var track in selectedTracks)
            {
                totalDuration = totalDuration.Add(track.Duration);
            }

            // Estimar tamaño (WAV estéreo 16-bit 44.1kHz ≈ 172KB/segundo)
            var estimatedSizeBytes = (long)(totalDuration.TotalSeconds * 176400);
            var estimatedSizeMB = estimatedSizeBytes / (1024.0 * 1024.0);

            _lblSummary.Text = $"Pistas seleccionadas: {selectedTracks.Count}\n" +
                              $"Duración total: {FormatDuration(totalDuration)}\n" +
                              $"Espacio estimado: {estimatedSizeMB:F1} MB";
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
                Description = "Seleccionar carpeta de destino para las pistas",
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
            // Validar que hay pistas seleccionadas
            var selectedTracks = _audioCuts.Where(cut => cut.IsSelected).ToList();
            if (selectedTracks.Count == 0)
            {
                MessageBox.Show("No hay pistas seleccionadas para exportar.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Validar directorio de salida
            if (string.IsNullOrWhiteSpace(_txtOutputPath.Text))
            {
                MessageBox.Show("Seleccione una carpeta de destino válida.", "Error",
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

                MessageBox.Show($"Exportación completada con éxito.\n\n" +
                              $"Archivos guardados en:\n{_txtOutputPath.Text}",
                              "Exportación Completada",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante la exportación:\n{ex.Message}", "Error",
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
