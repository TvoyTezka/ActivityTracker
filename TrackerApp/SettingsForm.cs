namespace TrackerApp;

public class SettingsForm : Form
{
    private readonly TrackService _tracker;
    private readonly NumericUpDown _intervalInput;
    private readonly NumericUpDown _idleInput;

    public SettingsForm(TrackService tracker)
    {
        _tracker = tracker;
        var cfg = Config.Load();

        Text = "Settings";
        Size = new Size(380, 220);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = true
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

        panel.Controls.Add(new Label
        {
            Text = "Poll interval (seconds):",
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10)
        }, 0, 0);

        _intervalInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 30,
            Value = cfg.PollIntervalMs / 1000,
            Font = new Font("Segoe UI", 10),
            Width = 80
        };
        panel.Controls.Add(_intervalInput, 1, 0);

        panel.Controls.Add(new Label
        {
            Text = "Idle threshold (seconds):",
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10)
        }, 0, 1);

        _idleInput = new NumericUpDown
        {
            Minimum = 10,
            Maximum = 600,
            Value = cfg.IdleThresholdS,
            Font = new Font("Segoe UI", 10),
            Width = 80
        };
        panel.Controls.Add(_idleInput, 1, 1);

        var btnPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Padding = new Padding(12)
        };

        var saveBtn = new Button { Text = "Save", Width = 80, Height = 30 };
        saveBtn.Click += (_, _) =>
        {
            var newCfg = new Config
            {
                PollIntervalMs = (int)_intervalInput.Value * 1000,
                IdleThresholdS = (int)_idleInput.Value
            };
            _tracker.ApplySettings(newCfg);
            Close();
        };

        var cancelBtn = new Button { Text = "Cancel", Width = 80, Height = 30 };
        cancelBtn.Click += (_, _) => Close();

        btnPanel.Controls.Add(saveBtn);
        btnPanel.Controls.Add(cancelBtn);

        Controls.Add(panel);
        Controls.Add(btnPanel);
    }
}
