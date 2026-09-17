using System;
using System.Collections.Generic;
using System.Drawing;
using System.Media;
using System.Text;
using System.Windows.Forms;

namespace AlwaysOnTop;

/// <summary>
/// Core of the utility: pins / unpins windows (top-most toggle), keeps track of
/// pinned windows, manages their border overlays and plays feedback sounds.
/// </summary>
public sealed class WindowManager : IDisposable
{
    /// <summary>Tracks a pinned window plus the identity we use to detect it closing.</summary>
    private sealed class PinnedWindow
    {
        public BorderOverlay? Border;
        public uint ProcessId;
    }

    private readonly Dictionary<IntPtr, PinnedWindow> _pinned = new();
    private readonly System.Windows.Forms.Timer _timer;
    private Config _config;

    public WindowManager(Config config)
    {
        _config = config;
        _timer = new System.Windows.Forms.Timer { Interval = 30 };
        _timer.Tick += (_, _) => Refresh();
    }

    public int PinnedCount => _pinned.Count;

    public void UpdateConfig(Config config) => _config = config;

    /// <summary>Toggle the current foreground window. Returns a result for UI feedback.</summary>
    public ToggleResult ToggleForeground()
    {
        IntPtr hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return new ToggleResult(ToggleAction.NoWindow, string.Empty);

        string title = GetTitle(hwnd);

        if (IsExcluded(title))
            return new ToggleResult(ToggleAction.Excluded, title);

        bool nowPinned = Toggle(hwnd);
        PlayFeedback(nowPinned);
        return new ToggleResult(nowPinned ? ToggleAction.Pinned : ToggleAction.Unpinned, title);
    }

    private bool Toggle(IntPtr hwnd)
    {
        if (_pinned.ContainsKey(hwnd))
        {
            Unpin(hwnd);
            return false;
        }
        Pin(hwnd);
        return true;
    }

    private void Pin(IntPtr hwnd)
    {
        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);

        BorderOverlay? border = null;
        if (_config.ShowBorder)
        {
            border = new BorderOverlay(hwnd, ParseColor(_config.BorderColor), _config.BorderThickness);
            border.Show();
            border.UpdatePosition();
        }

        // Remember the owning process so a recycled handle (a new window reusing
        // this HWND after the pinned one closes) is not mistaken for the same
        // window - otherwise a stray border could linger.
        NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);

        _pinned[hwnd] = new PinnedWindow { Border = border, ProcessId = pid };

        if (!_timer.Enabled)
            _timer.Start(); // needed to follow moves and detect closed windows
    }

    private void Unpin(IntPtr hwnd)
    {
        if (NativeMethods.IsWindow(hwnd))
        {
            NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_NOTOPMOST, 0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
        }

        if (_pinned.TryGetValue(hwnd, out PinnedWindow? entry) && entry.Border != null)
        {
            entry.Border.Close();
            entry.Border.Dispose();
        }

        _pinned.Remove(hwnd);

        if (_pinned.Count == 0)
            _timer.Stop();
    }

    /// <summary>Remove top-most from every tracked window (used on exit).</summary>
    public void UnpinAll()
    {
        foreach (IntPtr hwnd in new List<IntPtr>(_pinned.Keys))
            Unpin(hwnd);
    }

    /// <summary>Keep borders aligned and drop windows that have been closed.</summary>
    private void Refresh()
    {
        List<IntPtr>? closed = null;
        foreach (KeyValuePair<IntPtr, PinnedWindow> kvp in _pinned)
        {
            if (IsGone(kvp.Key, kvp.Value.ProcessId))
            {
                (closed ??= new List<IntPtr>()).Add(kvp.Key);
                continue;
            }
            kvp.Value.Border?.UpdatePosition();
        }

        if (closed != null)
            foreach (IntPtr hwnd in closed)
                Unpin(hwnd);
    }

    /// <summary>
    /// True when the pinned window no longer exists. Besides the plain
    /// <c>IsWindow</c> check this also catches the case where the process died
    /// abruptly and Windows handed the same HWND to a different process - which
    /// otherwise left the border overlay hanging around on screen.
    /// </summary>
    private static bool IsGone(IntPtr hwnd, uint originalPid)
    {
        if (!NativeMethods.IsWindow(hwnd))
            return true;

        uint currentPid;
        uint threadId = NativeMethods.GetWindowThreadProcessId(hwnd, out currentPid);
        if (threadId == 0 || currentPid == 0)
            return true;

        // The handle now belongs to a different process => the pinned window is gone.
        return originalPid != 0 && currentPid != originalPid;
    }

    private bool IsExcluded(string title)
    {
        if (_config.ExcludedApps == null || string.IsNullOrEmpty(title))
            return false;

        foreach (string app in _config.ExcludedApps)
        {
            if (!string.IsNullOrWhiteSpace(app) &&
                title.Contains(app.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private void PlayFeedback(bool pinned)
    {
        if (!_config.PlaySound)
            return;

        if (pinned) SystemSounds.Asterisk.Play();
        else SystemSounds.Hand.Play();
    }

    private static string GetTitle(IntPtr hwnd)
    {
        int len = NativeMethods.GetWindowTextLength(hwnd);
        if (len <= 0)
            return string.Empty;

        var sb = new StringBuilder(len + 1);
        NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static Color ParseColor(string hex)
    {
        try { return ColorTranslator.FromHtml(hex); }
        catch { return Color.FromArgb(10, 132, 255); } // fall back to blue
    }

    public void Dispose()
    {
        UnpinAll();
        _timer.Dispose();
    }
}

public enum ToggleAction
{
    Pinned,
    Unpinned,
    Excluded,
    NoWindow
}

public readonly record struct ToggleResult(ToggleAction Action, string WindowTitle);
