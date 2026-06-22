using System.Runtime.InteropServices;

namespace TrackerApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            var form = new MainForm();
            Application.Run(form);
        }
        catch (Exception ex)
        {
            try
            {
                var folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tracker");
                System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "crash.txt"), ex.ToString());
            }
            catch {}
            MessageBox.Show(ex.ToString(), "Tracker Error");
        }
    }
}
