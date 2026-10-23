using System.Threading;
using System.Windows.Forms;

namespace ClockWindow;

internal static class Program
{
    private const string MutexName = "Local\\ClockWindow_SingleInstance_9F2E4B7A";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out var isNewInstance);
        if (!isNewInstance)
        {
            MessageBox.Show("ClockWindow 已在运行中，请在系统托盘查看图标。", "ClockWindow",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}
