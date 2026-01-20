namespace App.Dialogs
{
    public class ExportOptions
    {
        public bool UseOriginalFormat { get; set; } = true;
        public int SampleRate { get; set; } = 44100;
        public int BitsPerSample { get; set; } = 16;
    }

    public partial class ExportOptionsDialog : Form
    {
        public ExportOptions Options { get; private set; } = new ExportOptions();

        private RadioButton _rbOriginal = null!;
        private RadioButton _rbCustom = null!;
        private ComboBox _cmbSampleRate = null!;
        private ComboBox _cmbBitsPerSample = null!;
        private Label _lblSampleRate = null!;
        private Label _lblBitsPerSample = null!;
        private Button _btnOk = null!;
        private Button _btnCancel = null!;
        private GroupBox _groupFormat = null!;

        private readonly int _originalSampleRate;
        private readonly int _originalBitsPerSample;

        public ExportOptionsDialog(int originalSampleRate, int originalBitsPerSample)
        {
            _originalSampleRate = originalSampleRate;
            _originalBitsPerSample = originalBitsPerSample;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Export Options";
            this.Size = new Size(350, 280);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Group box for format options
            _groupFormat = new GroupBox
            {
                Text = "Audio Format",
                Location = new Point(15, 15),
                Size = new Size(305, 170)
            };

            // Original format radio button
            _rbOriginal = new RadioButton
            {
                Text = $"Keep original format ({_originalSampleRate} Hz, {_originalBitsPerSample}-bit)",
                Location = new Point(15, 25),
                Size = new Size(280, 25),
                Checked = true
            };
            _rbOriginal.CheckedChanged += OnFormatSelectionChanged;

            // Custom format radio button
            _rbCustom = new RadioButton
            {
                Text = "Custom format:",
                Location = new Point(15, 55),
                Size = new Size(280, 25)
            };
            _rbCustom.CheckedChanged += OnFormatSelectionChanged;

            // Sample Rate label and combo
            _lblSampleRate = new Label
            {
                Text = "Sample Rate:",
                Location = new Point(35, 90),
                Size = new Size(80, 20),
                Enabled = false
            };

            _cmbSampleRate = new ComboBox
            {
                Location = new Point(120, 87),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            _cmbSampleRate.Items.AddRange(new object[] { "8000 Hz", "11025 Hz", "22050 Hz", "44100 Hz", "48000 Hz", "96000 Hz" });
            _cmbSampleRate.SelectedIndex = 3; // 44100 Hz

            // Bits per sample label and combo
            _lblBitsPerSample = new Label
            {
                Text = "Bit Depth:",
                Location = new Point(35, 125),
                Size = new Size(80, 20),
                Enabled = false
            };

            _cmbBitsPerSample = new ComboBox
            {
                Location = new Point(120, 122),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            _cmbBitsPerSample.Items.AddRange(new object[] { "8-bit", "16-bit", "24-bit", "32-bit" });
            _cmbBitsPerSample.SelectedIndex = 1; // 16-bit

            _groupFormat.Controls.AddRange(new Control[] {
                _rbOriginal, _rbCustom,
                _lblSampleRate, _cmbSampleRate,
                _lblBitsPerSample, _cmbBitsPerSample
            });

            // OK Button
            _btnOk = new Button
            {
                Text = "Export",
                Location = new Point(145, 200),
                Size = new Size(80, 30),
                DialogResult = DialogResult.OK
            };
            _btnOk.Click += OnOkClick;

            // Cancel Button
            _btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(235, 200),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[] { _groupFormat, _btnOk, _btnCancel });
            this.AcceptButton = _btnOk;
            this.CancelButton = _btnCancel;
        }

        private void OnFormatSelectionChanged(object? sender, EventArgs e)
        {
            bool customEnabled = _rbCustom.Checked;
            _lblSampleRate.Enabled = customEnabled;
            _cmbSampleRate.Enabled = customEnabled;
            _lblBitsPerSample.Enabled = customEnabled;
            _cmbBitsPerSample.Enabled = customEnabled;
        }

        private void OnOkClick(object? sender, EventArgs e)
        {
            Options = new ExportOptions
            {
                UseOriginalFormat = _rbOriginal.Checked,
                SampleRate = ParseSampleRate(_cmbSampleRate.SelectedItem?.ToString() ?? "44100 Hz"),
                BitsPerSample = ParseBitsPerSample(_cmbBitsPerSample.SelectedItem?.ToString() ?? "16-bit")
            };
        }

        private int ParseSampleRate(string value)
        {
            // Extract number from "44100 Hz"
            var num = value.Replace(" Hz", "").Trim();
            return int.TryParse(num, out int result) ? result : 44100;
        }

        private int ParseBitsPerSample(string value)
        {
            // Extract number from "16-bit"
            var num = value.Replace("-bit", "").Trim();
            return int.TryParse(num, out int result) ? result : 16;
        }
    }
}
