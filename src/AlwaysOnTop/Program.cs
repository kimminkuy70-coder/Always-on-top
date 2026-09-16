using System;
using System.Threading;
using System.Windows.Forms;

namespace AlwaysOnTop;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Single-instance guard so the hotkey isn't registered twice.
        using var mutex = new Mutex(true, "AlwaysOnTop_SingleInstance_9F1C", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("Always On Top이 이미 실행 중입니다. (시스템 트레이 확인)",
                "Always On Top", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayApplicationContext());

        GC.KeepAlive(mutex);
    }
}
