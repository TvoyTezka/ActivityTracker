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
            MessageBox.Show(ex.ToString(), "Tracker Error");
        }
    }
}
