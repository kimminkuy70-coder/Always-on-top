using System;
using System.Drawing;
using System.Windows.Forms;

namespace AlwaysOnTop;

/// <summary>
/// Owns the tray icon, the global hotkey and the window manager, and wires them
/// together. This is the application's runtime "shell" - it has no main window
/// and lives entirely in the notification area.
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly HotKeyManager _hotkeys;
    private readonly WindowManager _windows;
    private Config _config;
    private UsageForm? _usageForm;
    private SettingsForm? _settingsForm;

    public TrayApplicationContext()
    {
        _config = Config.Load();
        _windows = new WindowManager(_config);
        _hotkeys = new HotKeyManager();
        _hotkeys.HotKeyPressed += (_, _) => OnToggle();

        _tray = new NotifyIcon
        {
            Icon = CreateIcon(),
            Text = "Always On Top",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };
        _tray.DoubleClick += (_, _) => OnToggle();

        RegisterHotkey(initial: true);
    }

    // ---- Menu -------------------------------------------------------------
    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add($"지금 창 고정/해제  ({_config.HotkeyDisplay()})", null, (_, _) => OnToggle());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("설정 (단축키 지정)...", null, (_, _) => OpenSettings());
        menu.Items.Add("사용법 보기...", null, (_, _) => ShowUsage());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("종료", null, (_, _) => ExitApp());
        return menu;
    }

    private void RefreshMenu()
    {
        _tray.ContextMenuStrip?.Dispose();
        _tray.ContextMenuStrip = BuildMenu();
    }

    // ---- Actions ----------------------------------------------------------
    private void OnToggle()
    {
        ToggleResult result = _windows.ToggleForeground();
        switch (result.Action)
        {
            case ToggleAction.Pinned:
                Notify("창 고정됨", Title(result.WindowTitle) + " 창이 항상 위에 표시됩니다.");
                break;
            case ToggleAction.Unpinned:
                Notify("고정 해제됨", Title(result.WindowTitle) + " 창의 고정을 해제했습니다.");
                break;
            case ToggleAction.Excluded:
                Notify("제외된 창", "이 창은 제외 목록에 있어 고정되지 않습니다.");
                break;
            case ToggleAction.NoWindow:
                break;
        }
        _tray.Text = _windows.PinnedCount > 0
            ? $"Always On Top - 고정된 창 {_windows.PinnedCount}개"
            : "Always On Top";
    }

    private void OpenSettings()
    {
        if (_settingsForm != null) { _settingsForm.Activate(); return; }

        using var form = new SettingsForm(_config);
        _settingsForm = form;
        try
        {
            if (form.ShowDialog() == DialogResult.OK)
            {
                _config = form.ResultConfig;
                _config.Save();
                _windows.UpdateConfig(_config);
                RegisterHotkey(initial: false);
                RefreshMenu();
            }
        }
        finally
        {
            _settingsForm = null;
        }
    }

    private void ShowUsage()
    {
        if (_usageForm != null && !_usageForm.IsDisposed)
        {
            _usageForm.Activate();
            return;
        }
        _usageForm = new UsageForm(_config.HotkeyDisplay());
        _usageForm.FormClosed += (_, _) => _usageForm = null;
        _usageForm.Show();
    }

    private void RegisterHotkey(bool initial)
    {
        bool ok = _hotkeys.Register(_config.GetModifierFlags(), _config.GetVirtualKey());
        if (!ok)
        {
            _tray.ShowBalloonTip(4000, "Always On Top",
                $"단축키 [{_config.HotkeyDisplay()}] 를 등록하지 못했습니다.\n" +
                "다른 프로그램이 사용 중일 수 있습니다. 설정에서 변경하세요.",
                ToolTipIcon.Warning);
        }
        else if (initial)
        {
            _tray.ShowBalloonTip(3000, "Always On Top",
                $"실행되었습니다. [{_config.HotkeyDisplay()}] 로 창을 고정하세요.",
                ToolTipIcon.Info);
        }
    }

    private void ExitApp()
    {
        _windows.UnpinAll();
        _tray.Visible = false;
        _tray.Dispose();
        _hotkeys.Dispose();
        _windows.Dispose();
        _usageForm?.Close();
        ExitThread();
    }

    // ---- Helpers ----------------------------------------------------------
    private void Notify(string title, string message)
    {
        if (!_config.ShowNotification)
            return;
        _tray.ShowBalloonTip(1500, title, message, ToolTipIcon.None);
    }

    private static string Title(string title)
        => string.IsNullOrWhiteSpace(title) ? "선택한" : $"'{Truncate(title, 40)}'";

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s.Substring(0, max - 1) + "…";

    /// <summary>
    /// Generate the brand tray icon at runtime (no external .ico needed): a navy
    /// rounded tile with a lime "T", matching the app's navy + lime theme.
    /// </summary>
    private static Icon CreateIcon()
    {
        Color navy = Color.FromArgb(0x1B, 0x2A, 0x4A);
        Color lime = Color.FromArgb(0xCE, 0xE6, 0x4E);

        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            var tile = new Rectangle(2, 2, 28, 28);
            using (var path = RoundedTile(tile, 8))
            using (var fill = new SolidBrush(navy))
                g.FillPath(fill, path);

            using var font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Pixel);
            using var text = new SolidBrush(lime);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("T", font, text, new RectangleF(1, 1, 32, 32), sf);
        }

        IntPtr hIcon = bmp.GetHicon();
        // Clone so the icon survives after the temporary HICON is destroyed.
        using var tmp = Icon.FromHandle(hIcon);
        return (Icon)tmp.Clone();
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedTile(Rectangle b, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        int d = radius * 2;
        path.AddArc(b.X, b.Y, d, d, 180, 90);
        path.AddArc(b.Right - d, b.Y, d, d, 270, 90);
        path.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90);
        path.AddArc(b.X, b.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
