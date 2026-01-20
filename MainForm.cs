using App.Models;
using App.Services;
using App.Controls;
using App.Utilities;
using App.Dialogs;

namespace App
{
    public partial class MainForm : Form
    {
        // Controles UI
        private MenuStrip _menuStrip = null!;
        private FullWaveformControl _fullWaveform = null!;
        private Panel _controlsPanel = null!;
        private PlaybackWaveformControl _playbackWaveform = null!;
        private DataGridView _cutsGrid = null!;

        // Botones de control
        private Button _btnPlay = null!;
        private Button _btnPause = null!;
        private Button _btnStop = null!;
        private Button _btnCut = null!;
        private Label _lblTime = null!;

        // Servicios y datos
        private AudioService _audioService = null!;
        private List<AudioCut> _audioCuts = new List<AudioCut>();

        public MainForm()
        {
            InitializeComponent();
            InitializeLayout();
            InitializeServices();
            InitializeEvents();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(1200, 800);
            this.Text = "AudioCut - Windows Forms Native";
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void InitializeLayout()
        {
            // ELEMENTO 1: MENÚ SUPERIOR
            _menuStrip = new MenuStrip();
            _menuStrip.Items.Add(CreateFileMenu());
            _menuStrip.Items.Add(CreateEditMenu());
            _menuStrip.Items.Add(CreateHelpMenu());
            this.MainMenuStrip = _menuStrip;
            this.Controls.Add(_menuStrip);

            // TableLayoutPanel para layout vertical
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(5)
            };

            // Configurar porcentajes de altura
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));  // Panel 1: 25%
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F)); // Controles: 60px
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));  // Panel 4: 40%
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));  // Lista: 35%

            // ELEMENTO 2: WAVEFORM COMPLETO
            _fullWaveform = new FullWaveformControl
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.Black,
                WaveColor = Color.Cyan
            };
            mainLayout.Controls.Add(_fullWaveform, 0, 0);

            // ELEMENTO 3: BARRA DE CONTROLES
            _controlsPanel = CreateControlsPanel();
            mainLayout.Controls.Add(_controlsPanel, 0, 1);

            // ELEMENTO 4: WAVEFORM REPRODUCCIÓN
            _playbackWaveform = new PlaybackWaveformControl
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.Black,
                WaveColor = Color.Lime
            };
            mainLayout.Controls.Add(_playbackWaveform, 0, 2);

            // ELEMENTO 5: LISTA DE CORTES
            _cutsGrid = CreateCutsDataGridView();
            mainLayout.Controls.Add(_cutsGrid, 0, 3);

            this.Controls.Add(mainLayout);
        }

        private ToolStripMenuItem CreateFileMenu()
        {
            var fileMenu = new ToolStripMenuItem("&File");

            // Open Audio
            var openAudioItem = new ToolStripMenuItem("&Open Audio...", null, OnOpenFileClick);
            openAudioItem.ShortcutKeys = Keys.Control | Keys.O;
            fileMenu.DropDownItems.Add(openAudioItem);

            // Close
            var closeItem = new ToolStripMenuItem("&Close", null, OnCloseFileClick);
            closeItem.ShortcutKeys = Keys.Control | Keys.W;
            fileMenu.DropDownItems.Add(closeItem);

            fileMenu.DropDownItems.Add(new ToolStripSeparator());

            // Open Cue
            var openCueItem = new ToolStripMenuItem("Open &Cue...", null, OnOpenCueClick);
            openCueItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.O;
            fileMenu.DropDownItems.Add(openCueItem);

            // Save Cue
            var saveCueItem = new ToolStripMenuItem("&Save Cue...", null, OnSaveCueClick);
            saveCueItem.ShortcutKeys = Keys.Control | Keys.S;
            fileMenu.DropDownItems.Add(saveCueItem);

            fileMenu.DropDownItems.Add(new ToolStripSeparator());

            // Export Audio (submenu)
            var exportMenu = new ToolStripMenuItem("&Export Audio");

            var exportSelectedItem = new ToolStripMenuItem("Export &Selected...", null, OnExportSelectedClick);
            exportSelectedItem.ShortcutKeys = Keys.Control | Keys.E;
            exportMenu.DropDownItems.Add(exportSelectedItem);

            var exportAllItem = new ToolStripMenuItem("Export &All...", null, OnExportAllClick);
            exportAllItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.E;
            exportMenu.DropDownItems.Add(exportAllItem);

            fileMenu.DropDownItems.Add(exportMenu);

            fileMenu.DropDownItems.Add(new ToolStripSeparator());

            // Exit
            var exitItem = new ToolStripMenuItem("E&xit", null, OnExitClick);
            exitItem.ShortcutKeys = Keys.Alt | Keys.F4;
            fileMenu.DropDownItems.Add(exitItem);

            return fileMenu;
        }

        private ToolStripMenuItem CreateEditMenu()
        {
            var editMenu = new ToolStripMenuItem("&Edit");
            // Futuras opciones de edición
            return editMenu;
        }

        private ToolStripMenuItem CreateHelpMenu()
        {
            var helpMenu = new ToolStripMenuItem("&Help");

            var aboutItem = new ToolStripMenuItem("&About...", null, (s, e) =>
            {
                MessageBox.Show("AudioCut v1.0\nAudio editor with native Windows Forms controls",
                    "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            helpMenu.DropDownItems.Add(aboutItem);

            return helpMenu;
        }

        private Panel CreateControlsPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.DarkGray
            };

            // Botones
            _btnPlay = new Button
            {
                Text = "▶ Play",
                Width = 80,
                Height = 30,
                Location = new Point(10, 15)
            };

            _btnPause = new Button
            {
                Text = "⏸ Pause",
                Width = 80,
                Height = 30,
                Location = new Point(100, 15)
            };

            _btnStop = new Button
            {
                Text = "⏹ Stop",
                Width = 80,
                Height = 30,
                Location = new Point(190, 15)
            };

            _btnCut = new Button
            {
                Text = "✂ Cut",
                Width = 80,
                Height = 30,
                Location = new Point(280, 15)
            };

            // Visor de tiempo
            _lblTime = new Label
            {
                Text = "00:00.00 / 00:00.00",
                AutoSize = true,
                Location = new Point(380, 20),
                Font = new Font("Consolas", 12F),
                ForeColor = Color.Black
            };

            panel.Controls.AddRange(new Control[] { _btnPlay, _btnPause, _btnStop, _btnCut, _lblTime });
            return panel;
        }

        private DataGridView CreateCutsDataGridView()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                MultiSelect = false
            };

            // Columna #
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "#",
                Width = 50,
                ReadOnly = true
            });

            // Start Time Column
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Start",
                HeaderText = "Start Time",
                Width = 100,
                ReadOnly = true
            });

            // Duration Column
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Duration",
                HeaderText = "Duration",
                Width = 100,
                ReadOnly = true
            });

            // Name Column (editable)
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "Name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            // Checkbox Column
            grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Selected",
                HeaderText = "✓",
                Width = 40
            });

            // Delete Column
            grid.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "Delete",
                HeaderText = "",
                Text = "🗑️",
                UseColumnTextForButtonValue = true,
                Width = 50
            });

            return grid;
        }

        private void InitializeServices()
        {
            _audioService = new AudioService();
        }

        private void InitializeEvents()
        {
            // AudioService events
            _audioService.PositionChanged += OnPositionChanged;
            _audioService.StateChanged += OnStateChanged;
            _audioService.AudioLoaded += OnAudioLoaded;

            // Waveform events
            _fullWaveform.PositionClicked += OnWaveformPositionClicked;
            _playbackWaveform.PositionClicked += OnWaveformPositionClicked;

            // Button events
            _btnPlay.Click += (s, e) => _audioService.Play();
            _btnPause.Click += (s, e) => _audioService.Pause();
            _btnStop.Click += (s, e) => _audioService.Stop();
            _btnCut.Click += OnCutClick;

            // Grid events
            _cutsGrid.CellClick += OnGridCellClick;
            _cutsGrid.CellValueChanged += OnGridCellValueChanged;
        }

        private async void OnOpenFileClick(object? sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = AudioFileLoader.GetImportFileFilter(),
                Title = "Select WAV file"
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                this.Cursor = Cursors.WaitCursor;

                var audioFile = await _audioService.LoadWavFileAsync(openFileDialog.FileName);

                // Inicializar waveforms
                _fullWaveform.SetAudioData(audioFile.AudioData, audioFile.Duration);
                _playbackWaveform.SetAudioData(audioFile.AudioData, audioFile.Duration, audioFile.SampleRate);

                // Crear corte inicial (archivo completo)
                _audioCuts.Clear();
                _audioCuts.Add(new AudioCut
                {
                    TrackName = $"{Path.GetFileNameWithoutExtension(audioFile.FileName)}_001",
                    Start = TimeSpan.Zero,
                    Duration = audioFile.Duration,
                    CutColor = ColorGenerator.GetColorForIndex(0),
                    IsSelected = true
                });

                SortAndUpdateCuts();

                this.Cursor = Cursors.Default;

                MessageBox.Show("File loaded successfully", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show($"Error loading file: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnCloseFileClick(object? sender, EventArgs e)
        {
            if (_audioService.CurrentAudio == null)
            {
                return;
            }

            // Stop playback
            _audioService.Stop();

            // Clear audio service
            _audioService.Close();

            // Clear waveforms
            _fullWaveform.ClearWaveform();
            _playbackWaveform.ClearWaveform();

            // Clear cuts
            _audioCuts.Clear();
            _cutsGrid.Rows.Clear();

            // Reset time label
            _lblTime.Text = "00:00.00 / 00:00.00";

            // Reset window title
            this.Text = "AudioCut";
        }

        private void OnOpenCueClick(object? sender, EventArgs e)
        {
            if (_audioService.CurrentAudio == null)
            {
                MessageBox.Show("Please load an audio file first", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var openFileDialog = new OpenFileDialog
            {
                Filter = "Cue Files|*.cue|All Files|*.*",
                Title = "Open Cue File"
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                var lines = File.ReadAllLines(openFileDialog.FileName);
                var trackTimes = new List<TimeSpan>();

                // Parse CUE format: INDEX 01 MM:SS:FF (FF = frames, 75 per second)
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("INDEX 01"))
                    {
                        var timePart = trimmed.Substring(9).Trim();
                        var time = ParseCueTime(timePart);
                        if (time.HasValue)
                        {
                            trackTimes.Add(time.Value);
                        }
                    }
                }

                if (trackTimes.Count > 0)
                {
                    // Create cuts from track times
                    var newCuts = new List<AudioCut>();
                    var duration = _audioService.Duration;

                    for (int i = 0; i < trackTimes.Count; i++)
                    {
                        var start = trackTimes[i];
                        var end = (i < trackTimes.Count - 1) ? trackTimes[i + 1] : duration;

                        newCuts.Add(new AudioCut
                        {
                            Start = start,
                            Duration = end - start,
                            IsSelected = true
                        });
                    }

                    _audioCuts = newCuts;
                    SortAndUpdateCuts();
                    MessageBox.Show($"Loaded {newCuts.Count} tracks", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Cue: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private TimeSpan? ParseCueTime(string timeStr)
        {
            // Format: MM:SS:FF where FF = frames (75 frames per second)
            var parts = timeStr.Split(':');
            if (parts.Length == 3 &&
                int.TryParse(parts[0], out int minutes) &&
                int.TryParse(parts[1], out int seconds) &&
                int.TryParse(parts[2], out int frames))
            {
                double totalSeconds = minutes * 60 + seconds + frames / 75.0;
                return TimeSpan.FromSeconds(totalSeconds);
            }
            return null;
        }

        private string ToCueTime(TimeSpan time)
        {
            // Format: MM:SS:FF where FF = frames (75 frames per second)
            int totalMinutes = (int)time.TotalMinutes;
            int seconds = time.Seconds;
            int frames = (int)(time.Milliseconds / 1000.0 * 75);
            return $"{totalMinutes:D2}:{seconds:D2}:{frames:D2}";
        }

        private void OnSaveCueClick(object? sender, EventArgs e)
        {
            if (_audioService.CurrentAudio == null || _audioCuts.Count == 0)
            {
                MessageBox.Show("No cuts to save", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var saveFileDialog = new SaveFileDialog
            {
                Filter = "Cue Files|*.cue",
                Title = "Save Cue File",
                FileName = Path.GetFileNameWithoutExtension(_audioService.CurrentAudio.FileName) + ".cue"
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"FILE \"{_audioService.CurrentAudio.FilePath}\" WAVE");

                for (int i = 0; i < _audioCuts.Count; i++)
                {
                    var cut = _audioCuts[i];
                    sb.AppendLine($"  TRACK {(i + 1):D2} AUDIO");
                    sb.AppendLine($"    INDEX 01 {ToCueTime(cut.Start)}");
                }

                File.WriteAllText(saveFileDialog.FileName, sb.ToString());

                MessageBox.Show("Cue file saved successfully", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving Cue: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void OnExportSelectedClick(object? sender, EventArgs e)
        {
            await ExportCutsAsync(_audioCuts.Where(c => c.IsSelected).ToList(), "selected");
        }

        private async void OnExportAllClick(object? sender, EventArgs e)
        {
            await ExportCutsAsync(_audioCuts, "");
        }

        private async Task ExportCutsAsync(List<AudioCut> cuts, string description)
        {
            if (_audioService.CurrentAudio == null)
            {
                MessageBox.Show("No file loaded", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!cuts.Any())
            {
                var msg = string.IsNullOrEmpty(description) ? "No cuts to export" : $"No {description} cuts to export";
                MessageBox.Show(msg, "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Show export options dialog
            using var optionsDialog = new ExportOptionsDialog(
                _audioService.CurrentAudio.SampleRate,
                _audioService.CurrentAudio.BitDepth);

            if (optionsDialog.ShowDialog(this) != DialogResult.OK)
                return;

            var exportOptions = optionsDialog.Options;

            // Select output folder
            using var folderDialog = new FolderBrowserDialog
            {
                Description = "Select destination folder",
                ShowNewFolderButton = true
            };

            if (folderDialog.ShowDialog() != DialogResult.OK)
                return;

            // Create progress form
            var progressForm = new Form
            {
                Text = "Exporting...",
                Size = new Size(350, 120),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false
            };

            var progressLabel = new Label
            {
                Text = "Exporting audio files, please wait...",
                Location = new Point(20, 15),
                Size = new Size(300, 20)
            };

            var progressBar = new ProgressBar
            {
                Location = new Point(20, 45),
                Size = new Size(295, 25),
                Minimum = 0,
                Maximum = 100
            };

            progressForm.Controls.Add(progressLabel);
            progressForm.Controls.Add(progressBar);

            try
            {
                progressForm.Show(this);
                this.Enabled = false;

                var progress = new Progress<int>(p =>
                {
                    progressBar.Value = p;
                    progressLabel.Text = $"Exporting... {p}%";
                });

                var exportSettings = new ExportSettings
                {
                    UseOriginalFormat = exportOptions.UseOriginalFormat,
                    SampleRate = exportOptions.SampleRate,
                    BitsPerSample = exportOptions.BitsPerSample
                };

                var exporter = new WaveformExporter();
                await exporter.ExportCutsAsync(cuts, _audioService.CurrentAudio!, folderDialog.SelectedPath, progress, exportSettings);

                MessageBox.Show($"Export completed.\n{cuts.Count} files saved to: {folderDialog.SelectedPath}",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during export: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Enabled = true;
                progressForm.Close();
                progressForm.Dispose();
                this.Cursor = Cursors.Default;
            }
        }

        private void OnCutClick(object? sender, EventArgs e)
        {
            var currentPosition = _audioService.Position;

            if (currentPosition <= TimeSpan.Zero || currentPosition >= _audioService.Duration)
            {
                MessageBox.Show("Invalid position for cutting", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Find cut to split
            var cutToSplit = _audioCuts.FirstOrDefault(c =>
                currentPosition >= c.Start && currentPosition < c.EndTime);

            if (cutToSplit == null)
            {
                MessageBox.Show("No cut found at this position", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Create two new cuts
            var firstCut = new AudioCut
            {
                Start = cutToSplit.Start,
                Duration = currentPosition - cutToSplit.Start,
                IsSelected = true
            };

            var secondCut = new AudioCut
            {
                Start = currentPosition,
                Duration = cutToSplit.EndTime - currentPosition,
                IsSelected = true
            };

            // Replace
            _audioCuts.Remove(cutToSplit);
            _audioCuts.Add(firstCut);
            _audioCuts.Add(secondCut);

            // Reordenar y actualizar
            SortAndUpdateCuts();
        }

        private void SortAndUpdateCuts()
        {
            // Ordenar por tiempo
            _audioCuts.Sort((a, b) => a.Start.CompareTo(b.Start));

            // Renumerar y reasignar colores
            string baseName = Path.GetFileNameWithoutExtension(_audioService.CurrentAudio?.FileName ?? "audio");
            for (int i = 0; i < _audioCuts.Count; i++)
            {
                _audioCuts[i].TrackName = $"{baseName}_{(i + 1):D3}";
                _audioCuts[i].CutColor = ColorGenerator.GetColorForIndex(i);
            }

            // Actualizar DataGridView
            RefreshGrid();

            // Actualizar waveforms
            _fullWaveform.SetAudioCuts(_audioCuts);
            _playbackWaveform.SetAudioCuts(_audioCuts);
        }

        private void RefreshGrid()
        {
            _cutsGrid.Rows.Clear();
            for (int i = 0; i < _audioCuts.Count; i++)
            {
                var cut = _audioCuts[i];
                _cutsGrid.Rows.Add(
                    i + 1,                  // #
                    cut.StartFormatted,     // Tiempo Inicio
                    cut.DurationFormatted,  // Duración
                    cut.TrackName,          // Nombre
                    cut.IsSelected,         // Checkbox
                    "🗑️"                    // Botón
                );
            }
        }

        private void OnGridCellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Click en columna Delete
            if (e.ColumnIndex == _cutsGrid.Columns["Delete"]!.Index)
            {
                if (_audioCuts.Count <= 1)
                {
                    MessageBox.Show("Cannot delete the last cut", "Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _audioCuts.RemoveAt(e.RowIndex);
                SortAndUpdateCuts();
                return;
            }
        }

        private void OnGridCellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _audioCuts.Count) return;

            // Actualizar nombre del corte
            if (e.ColumnIndex == _cutsGrid.Columns["Name"]!.Index)
            {
                var newName = _cutsGrid.Rows[e.RowIndex].Cells["Name"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    _audioCuts[e.RowIndex].TrackName = newName;
                }
            }

            // Actualizar checkbox de selección
            if (e.ColumnIndex == _cutsGrid.Columns["Selected"]!.Index)
            {
                var isSelected = (bool?)_cutsGrid.Rows[e.RowIndex].Cells["Selected"].Value ?? false;
                _audioCuts[e.RowIndex].IsSelected = isSelected;
            }
        }

        private void OnPositionChanged(object? sender, TimeSpan position)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnPositionChanged(sender, position));
                return;
            }

            // Actualizar waveforms (invalidación parcial)
            _fullWaveform.UpdateCursorPosition(position);
            _playbackWaveform.UpdateCursorPosition(position);

            // Actualizar visor de tiempo
            _lblTime.Text = $"{FormatTime(position)} / {FormatTime(_audioService.Duration)}";
        }

        private void OnStateChanged(object? sender, PlaybackState state)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnStateChanged(sender, state));
                return;
            }

            // Actualizar estado de playback en control de tiempo real
            if (state == PlaybackState.Playing)
            {
                _playbackWaveform.StartPlayback();
            }
            else
            {
                _playbackWaveform.StopPlayback();
            }
        }

        private void OnAudioLoaded(object? sender, AudioFile audioFile)
        {
            // Evento cuando se carga un archivo
        }

        private void OnWaveformPositionClicked(object? sender, TimeSpan position)
        {
            _audioService.Seek(position);
        }

        private string FormatTime(TimeSpan time)
        {
            return $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}.{time.Milliseconds / 10:D2}";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _audioService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
