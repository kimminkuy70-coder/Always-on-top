using System;
using System.Windows.Forms;

namespace AlwaysOnTop;

/// <summary>
/// Registers a single system-wide hotkey and raises <see cref="HotKeyPressed"/>
/// whenever it is triggered. Uses a hidden message window (NativeWindow) so it
/// works without a visible form.
/// </summary>
public sealed class HotKeyManager : NativeWindow, IDisposable
{
    private const int HotKeyId = 0xA0B1;

    private bool _registered;

    public event EventHandler? HotKeyPressed;

    public HotKeyManager()
    {
        // Create a message-only window handle to receive WM_HOTKEY.
        CreateHandle(new CreateParams());
    }

    /// <summary>
    /// (Re)registers the hotkey. Returns false when the combination is already
    /// taken by another application.
    /// </summary>
    public bool Register(uint modifiers, uint key)
    {
        Unregister();
        _registered = NativeMethods.RegisterHotKey(Handle, HotKeyId, modifiers, key);
        return _registered;
    }

    public void Unregister()
    {
        if (_registered)
        {
            NativeMethods.UnregisterHotKey(Handle, HotKeyId);
            _registered = false;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HotKeyId)
        {
            HotKeyPressed?.Invoke(this, EventArgs.Empty);
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        Unregister();
        DestroyHandle();
    }
}
