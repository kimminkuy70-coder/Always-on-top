using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AlwaysOnTop;

/// <summary>
/// Settings dialog: lets the user assign the activation hotkey and configure the
/// border, sound, notification and excluded-apps options. Returns an updated
/// <see cref="Config"/> via <see cref="ResultConfig"/> when accepted.
/// </summary>
public sealed class SettingsForm : Form
{
    private readonly CheckBox _chkWin = new() { Text = "Win", AutoSize = true };
    private readonly CheckBox _chkCtrl = new() { Text = "Ctrl", AutoSize = true };
    private readonly CheckBox _chkAlt = new() { Text = "Alt", AutoSize = true };
    private readonly CheckBox _chkShift = new() { Text = "Shift", AutoSize = true };

    private readonly TextBox _txtKey = new() { ReadOnly = true, Width = 80, TextAlign = HorizontalAlignment.Center };
    private string _key;

    private readonly CheckBox _chkBorder = new() { Text = "고정된 창에 테두리 표시", AutoSize = true };
    private readonly TextBox _txtColor = new() { Width = 90 };
    private readonly Button _btnColor = new() { Text = "...", Width = 30 };
    private readonly NumericUpDown _numThickness = new() { Minimum = 1, Maximum = 20, Width = 60 };

    private readonly CheckBox _chkSound = new() { Text = "고정/해제 시 소리 재생", AutoSize = true };
    private readonly CheckBox _chkNotify = new() { Text = "고정/해제 시 알림 표시", AutoSize = true };

    private readonly TextBox _txtExcluded = new()
    {
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        Width = 360,
        Height = 80
    };

    public Config ResultConfig { get; private set; }

    public SettingsForm(Config current)
    {
        ResultConfig = current.Clone();
        _key = string.IsNullOrWhiteSpace(current.Key) ? "T" : current.Key;

        Text = "Always On Top - 설정";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);
        ClientSize = new Size(420, 470);

        BuildUi();
        LoadFrom(current);
    }

    private void BuildUi()
    {
        int y = 12;

        // --- Hotkey ---------------------------------------------------------
        Controls.Add(new Label { Text = "활성화 단축키", Left = 12, Top = y, AutoSize = true, Font = new Font(Font, FontStyle.Bold) });
        y += 24;

        var modPanel = new FlowLayoutPanel { Left = 12, Top = y, Width = 396, Height = 28, FlowDirection = FlowDirection.LeftToRight };
        modPanel.Controls.AddRange(new Control[] { _chkWin, _chkCtrl, _chkAlt, _chkShift });
        Controls.Add(modPanel);
        y += 32;

        Controls.Add(new Label { Text = "키:", Left = 12, Top = y + 4, AutoSize = true });
        _txtKey.Left = 40; _txtKey.Top = y;
        _txtKey.KeyDown += TxtKey_KeyDown;
        Controls.Add(_txtKey);
        Controls.Add(new Label
        {
            Text = "(칸을 클릭 후 원하는 키를 누르세요)",
            Left = 130, Top = y + 4, AutoSize = true, ForeColor = Color.Gray
        });
        y += 40;

        // --- Border ---------------------------------------------------------
        Controls.Add(new Label { Text = "테두리", Left = 12, Top = y, AutoSize = true, Font = new Font(Font, FontStyle.Bold) });
        y += 24;
        _chkBorder.Left = 12; _chkBorder.Top = y;
        _chkBorder.CheckedChanged += (_, _) => UpdateBorderEnabled();
        Controls.Add(_chkBorder);
        y += 28;

        Controls.Add(new Label { Text = "색상:", Left = 24, Top = y + 4, AutoSize = true });
        _txtColor.Left = 70; _txtColor.Top = y;
        Controls.Add(_txtColor);
        _btnColor.Left = 165; _btnColor.Top = y - 1;
        _btnColor.Click += BtnColor_Click;
        Controls.Add(_btnColor);

        Controls.Add(new Label { Text = "두께:", Left = 220, Top = y + 4, AutoSize = true });
        _numThickness.Left = 260; _numThickness.Top = y;
        Controls.Add(_numThickness);
        Controls.Add(new Label { Text = "px", Left = 324, Top = y + 4, AutoSize = true });
        y += 40;

        // --- Feedback -------------------------------------------------------
        Controls.Add(new Label { Text = "피드백", Left = 12, Top = y, AutoSize = true, Font = new Font(Font, FontStyle.Bold) });
        y += 24;
        _chkSound.Left = 12; _chkSound.Top = y; Controls.Add(_chkSound);
        y += 26;
        _chkNotify.Left = 12; _chkNotify.Top = y; Controls.Add(_chkNotify);
        y += 34;

        // --- Excluded apps --------------------------------------------------
        Controls.Add(new Label { Text = "제외할 창 (제목의 일부, 한 줄에 하나)", Left = 12, Top = y, AutoSize = true, Font = new Font(Font, FontStyle.Bold) });
        y += 24;
        _txtExcluded.Left = 12; _txtExcluded.Top = y;
        Controls.Add(_txtExcluded);
        y += _txtExcluded.Height + 16;

        // --- Buttons --------------------------------------------------------
        var btnOk = new Button { Text = "확인", Width = 80, Left = 240, Top = y, DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "취소", Width = 80, Left = 328, Top = y, DialogResult = DialogResult.Cancel };
        btnOk.Click += BtnOk_Click;
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    private void LoadFrom(Config c)
    {
        var mods = new HashSet<string>(c.Modifiers.Select(m => m.Trim().ToLowerInvariant()));
        _chkWin.Checked = mods.Contains("win") || mods.Contains("windows");
        _chkCtrl.Checked = mods.Contains("ctrl") || mods.Contains("control");
        _chkAlt.Checked = mods.Contains("alt");
        _chkShift.Checked = mods.Contains("shift");

        _key = c.Key;
        _txtKey.Text = _key;

        _chkBorder.Checked = c.ShowBorder;
        _txtColor.Text = c.BorderColor;
        _numThickness.Value = Math.Clamp(c.BorderThickness, 1, 20);
        _chkSound.Checked = c.PlaySound;
        _chkNotify.Checked = c.ShowNotification;
        _txtExcluded.Text = string.Join(Environment.NewLine, c.ExcludedApps);

        UpdateBorderEnabled();
    }

    private void UpdateBorderEnabled()
    {
        bool on = _chkBorder.Checked;
        _txtColor.Enabled = on;
        _btnColor.Enabled = on;
        _numThickness.Enabled = on;
    }

    private void TxtKey_KeyDown(object? sender, KeyEventArgs e)
    {
        e.SuppressKeyPress = true;

        // Ignore lone modifier presses - we only capture the "main" key.
        switch (e.KeyCode)
        {
            case Keys.ControlKey:
            case Keys.ShiftKey:
            case Keys.Menu:
            case Keys.LWin:
            case Keys.RWin:
                return;
        }

        _key = e.KeyCode.ToString();
        _txtKey.Text = _key;
    }

    private void BtnColor_Click(object? sender, EventArgs e)
    {
        using var dlg = new ColorDialog { FullOpen = true };
        try { dlg.Color = ColorTranslator.FromHtml(_txtColor.Text); } catch { /* keep default */ }
        if (dlg.ShowDialog(this) == DialogResult.OK)
            _txtColor.Text = ColorTranslator.ToHtml(dlg.Color);
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        var mods = new List<string>();
        if (_chkWin.Checked) mods.Add("Win");
        if (_chkCtrl.Checked) mods.Add("Control");
        if (_chkAlt.Checked) mods.Add("Alt");
        if (_chkShift.Checked) mods.Add("Shift");

        if (mods.Count == 0)
        {
            MessageBox.Show(this, "적어도 하나의 조합 키(Win/Ctrl/Alt/Shift)를 선택하세요.",
                "Always On Top", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }
        if (string.IsNullOrWhiteSpace(_key))
        {
            MessageBox.Show(this, "메인 키를 지정하세요.",
                "Always On Top", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        ResultConfig.Modifiers = mods;
        ResultConfig.Key = _key;
        ResultConfig.ShowBorder = _chkBorder.Checked;
        ResultConfig.BorderColor = string.IsNullOrWhiteSpace(_txtColor.Text) ? "#FF8C00" : _txtColor.Text.Trim();
        ResultConfig.BorderThickness = (int)_numThickness.Value;
        ResultConfig.PlaySound = _chkSound.Checked;
        ResultConfig.ShowNotification = _chkNotify.Checked;
        ResultConfig.ExcludedApps = _txtExcluded.Lines
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();
    }
}
