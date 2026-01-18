using App.Models;
using App.Services;
using App.Controls;
using App.Utilities;

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

            var openItem = new ToolStripMenuItem("&Abrir...", null, OnOpenFileClick);
            openItem.ShortcutKeys = Keys.Control | Keys.O;
            fileMenu.DropDownItems.Add(openItem);

            var saveItem = new ToolStripMenuItem("&Guardar...", null, OnSaveClick);
            saveItem.ShortcutKeys = Keys.Control | Keys.S;
            fileMenu.DropDownItems.Add(saveItem);

            fileMenu.DropDownItems.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("&Salir", null, (s, e) => this.Close());
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

            var aboutItem = new ToolStripMenuItem("&Acerca de...", null, (s, e) =>
            {
                MessageBox.Show("AudioCut v1.0\nEditor de audio con controles nativos Windows Forms",
                    "Acerca de", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            // Columna Tiempo Inicio
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Start",
                HeaderText = "Tiempo Inicio",
                Width = 100,
                ReadOnly = true
            });

            // Columna Duración
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Duration",
                HeaderText = "Duración",
                Width = 100,
                ReadOnly = true
            });

            // Columna Nombre (editable)
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "Nombre",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            // Columna Checkbox
            grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Selected",
                HeaderText = "✓",
                Width = 40
            });

            // Columna Eliminar
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
                Title = "Seleccionar archivo WAV"
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

                MessageBox.Show("Archivo cargado correctamente", "Éxito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show($"Error al cargar archivo: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSaveClick(object? sender, EventArgs e)
        {
            if (_audioService.CurrentAudio == null)
            {
                MessageBox.Show("No hay archivo cargado", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedCuts = _audioCuts.Where(c => c.IsSelected).ToList();
            if (!selectedCuts.Any())
            {
                MessageBox.Show("No hay cortes seleccionados para exportar", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var folderDialog = new FolderBrowserDialog
            {
                Description = "Seleccionar carpeta de destino",
                ShowNewFolderButton = true
            };

            if (folderDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                this.Cursor = Cursors.WaitCursor;

                var exporter = new WaveformExporter();
                exporter.ExportCutsAsync(selectedCuts, _audioService.CurrentAudio!, folderDialog.SelectedPath, null).Wait();

                this.Cursor = Cursors.Default;

                MessageBox.Show($"Exportación completada.\nArchivos guardados en: {folderDialog.SelectedPath}",
                    "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show($"Error durante exportación: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnCutClick(object? sender, EventArgs e)
        {
            var currentPosition = _audioService.Position;

            if (currentPosition <= TimeSpan.Zero || currentPosition >= _audioService.Duration)
            {
                MessageBox.Show("Posición no válida para cortar", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Encontrar corte a dividir
            var cutToSplit = _audioCuts.FirstOrDefault(c =>
                currentPosition >= c.Start && currentPosition < c.EndTime);

            if (cutToSplit == null)
            {
                MessageBox.Show("No se encontró un corte en esta posición", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Crear dos nuevos cortes
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

            // Reemplazar
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
                    MessageBox.Show("No se puede eliminar el último corte", "Información",
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
