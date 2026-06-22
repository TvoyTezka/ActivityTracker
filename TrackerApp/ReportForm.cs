using System.Data;

namespace TrackerApp;

public partial class ReportForm : Form
{
    public ReportForm(string title, List<(string process, string category, string? customName, long totalSeconds)> data)
    {
        Text = title;
        Size = new Size(500, 400);
        StartPosition = FormStartPosition.CenterScreen;

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(212, 212, 212),
            GridColor = Color.FromArgb(60, 60, 60),
            BorderStyle = BorderStyle.None,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.FromArgb(74, 144, 217),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                SelectionBackColor = Color.FromArgb(45, 45, 45),
                SelectionForeColor = Color.FromArgb(74, 144, 217),
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(212, 212, 212),
                SelectionBackColor = Color.FromArgb(38, 79, 120),
                SelectionForeColor = Color.White,
            }
        };

        var dt = new DataTable();
        dt.Columns.Add("Time", typeof(string));
        dt.Columns.Add("Category", typeof(string));
        dt.Columns.Add("Application", typeof(string));

        long total = 0;
        foreach (var (proc, cat, customName, dur) in data)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(proc);
            var appDisplayName = string.IsNullOrEmpty(customName) ? name : $"{customName} ({name})";
            dt.Rows.Add(FormatDuration(dur), cat, appDisplayName);
            total += dur;
        }
        dt.Rows.Add(FormatDuration(total), "", "TOTAL");

        grid.DataSource = dt;
        Controls.Add(grid);
    }

    private static string FormatDuration(long s)
    {
        var h = s / 3600;
        var m = (s % 3600) / 60;
        var sec = s % 60;
        if (h > 0) return $"{h}h {m}m";
        if (m > 0) return $"{m}m {sec}s";
        return $"{sec}s";
    }
}
