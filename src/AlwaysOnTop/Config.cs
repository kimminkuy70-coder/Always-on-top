using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace AlwaysOnTop;

/// <summary>
/// User configuration, persisted to %APPDATA%\AlwaysOnTop\config.json.
/// Mirrors the configurable options of the PowerToys "Always On Top" utility.
/// </summary>
public class Config
{
    /// <summary>Hotkey modifiers, e.g. ["Win", "Control"].</summary>
    public List<string> Modifiers { get; set; } = new() { "Win", "Control" };

    /// <summary>Main (non-modifier) key, e.g. "T".</summary>
    public string Key { get; set; } = "T";

    /// <summary>Draw a colored border around pinned windows.</summary>
    public bool ShowBorder { get; set; } = true;

    /// <summary>Border color as an HTML hex string (e.g. "#0A84FF").</summary>
    public string BorderColor { get; set; } = "#0A84FF";

    /// <summary>Border thickness in pixels.</summary>
    public int BorderThickness { get; set; } = 4;

    /// <summary>Play a sound when pinning / unpinning.</summary>
    public bool PlaySound { get; set; } = true;

    /// <summary>Window titles (or parts of them) to exclude, one per entry.</summary>
    public List<string> ExcludedApps { get; set; } = new();

    /// <summary>Show a tray notification balloon on pin / unpin.</summary>
    public bool ShowNotification { get; set; } = true;

    [JsonIgnore]
    public static string ConfigDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AlwaysOnTop");

    [JsonIgnore]
    public static string ConfigPath => Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static Config Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                Config? cfg = JsonSerializer.Deserialize<Config>(json, JsonOptions);
                if (cfg != null)
                {
                    cfg.Normalize();
                    return cfg;
                }
            }
        }
        catch
        {
            // Corrupt / unreadable config -> fall back to defaults.
        }
        return new Config();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            string json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(ConfigPath, json);
        }
        catch
        {
            // Non-fatal: settings simply won't persist across restarts.
        }
    }

    public Config Clone()
    {
        return new Config
        {
            Modifiers = new List<string>(Modifiers),
            Key = Key,
            ShowBorder = ShowBorder,
            BorderColor = BorderColor,
            BorderThickness = BorderThickness,
            PlaySound = PlaySound,
            ExcludedApps = new List<string>(ExcludedApps),
            ShowNotification = ShowNotification
        };
    }

    private void Normalize()
    {
        Modifiers ??= new List<string>();
        ExcludedApps ??= new List<string>();
        if (string.IsNullOrWhiteSpace(Key)) Key = "T";
        if (BorderThickness < 1) BorderThickness = 1;
        if (BorderThickness > 20) BorderThickness = 20;
    }

    /// <summary>Combined modifier flags for RegisterHotKey (with MOD_NOREPEAT).</summary>
    public uint GetModifierFlags()
    {
        uint flags = NativeMethods.MOD_NOREPEAT;
        foreach (string m in Modifiers)
        {
            switch (m.Trim().ToLowerInvariant())
            {
                case "win":
                case "windows": flags |= NativeMethods.MOD_WIN; break;
                case "ctrl":
                case "control": flags |= NativeMethods.MOD_CONTROL; break;
                case "alt": flags |= NativeMethods.MOD_ALT; break;
                case "shift": flags |= NativeMethods.MOD_SHIFT; break;
            }
        }
        return flags;
    }

    /// <summary>Virtual-key code for the main key.</summary>
    public uint GetVirtualKey()
    {
        return Enum.TryParse<Keys>(Key, true, out Keys k) ? (uint)k : (uint)Keys.T;
    }

    /// <summary>Human-readable hotkey text, e.g. "Win + Ctrl + T".</summary>
    public string HotkeyDisplay()
    {
        var parts = new List<string>();
        foreach (string m in Modifiers)
        {
            string s = m.Trim().ToLowerInvariant() switch
            {
                "win" or "windows" => "Win",
                "ctrl" or "control" => "Ctrl",
                "alt" => "Alt",
                "shift" => "Shift",
                _ => m
            };
            parts.Add(s);
        }
        parts.Add(Key);
        return string.Join(" + ", parts);
    }
}
