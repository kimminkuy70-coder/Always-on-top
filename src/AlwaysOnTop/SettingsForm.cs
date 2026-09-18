using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AlwaysOnTop;

/// <summary>
/// Settings dialog. Lets the user assign the activation hotkey and configure the
/// border, sound, notification and excluded-apps options, laid out as a
/// minimalist "bento grid" of rounded cards in the app's navy + lime theme.
/// Returns an updated <see cref="Config"/> via <see cref="ResultConfig"/>.
/// </summary>
public sealed class SettingsForm : Form
{
    private readonly CheckBox _chkWin   = new() { Text = "Win" };
    private readonly CheckBox _chkCtrl  = new() { Text = "Ctrl" };
    private readonly CheckBox _chkAlt   = new() { Text = "Alt" };
    private readonly CheckBox _chkShift = new() { Text = "Shift" };

    private readonly TextBox _txtKey = new()
    {
        ReadOnly = true, Width = 84, TextAlign = HorizontalAlignment.Center,
        BorderStyle = BorderStyle.FixedSingle
    };
    private string _key;

    private readonly CheckBox _chkBorder = new() { Text = "고정된 창에 테두리 표시" };
    private readonly Panel    _swatch    = new() { Width = 26, Height = 24 };
    private readonly TextBox  _txtColor  = new() { Width = 96, BorderStyle = BorderStyle.FixedSingle };
    private readonly PillButton _btnColor = new()
    {
        Text = "선택", Width = 60, Height = 26,
        FillColor = Theme.Navy, HoverColor = Theme.NavySoft, Radius = 8,
        Font = new Font("Segoe UI", 9f, FontStyle.Bold)
    };
    private readonly NumericUpDown _numThickness = new()
    {
        Minimum = 1, Maximum = 20, Width = 58, BorderStyle = BorderStyle.FixedSingle
    };

    private readonly CheckBox _chkSound  = new() { Text = "고정 / 해제 시 소리 재생" };
    private readonly CheckBox _chkNotify = new() { Text = "고정 / 해제 시 알림 표시" };

    private readonly TextBox _txtExcluded = new()
    {
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        BorderStyle = BorderStyle.FixedSingle
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
        BackColor = Theme.Canvas;
        Font = Theme.Body();
        ClientSize = new Size(660, 684);

        BuildUi();
        LoadFrom(current);
    }

    // ---- Layout -----------------------------------------------------------
    private void BuildUi()
    {
        BuildHeader();

        const int M = 20, gap = 16;
        int fullW = ClientSize.Width - 2 * M;         // 620
        int colW = (fullW - gap) / 2;                 // 302
        int rightX = M + colW + gap;

        // Card 1 - Hotkey (full width)
        CardPanel hotkey = MakeCard(M, 114, fullW, 132, "활성화 단축키");
        BuildHotkeyCard(hotkey);

        // Card 2 - Border (left)
        CardPanel border = MakeCard(M, 262, colW, 152, "테두리");
        BuildBorderCard(border);

        // Card 3 - Feedback (right)
        CardPanel feedback = MakeCard(rightX, 262, colW, 152, "피드백");
        BuildFeedbackCard(feedback);

        // Card 4 - Excluded apps (full width)
        CardPanel excluded = MakeCard(M, 430, fullW, 172, "제외할 창");
        BuildExcludedCard(excluded);

        BuildFooter(M, gap);
    }

    private void BuildHeader()
    {
        var header = new Panel { Left = 0, Top = 0, Width = ClientSize.Width, Height = 96, BackColor = Theme.Navy };

        var eyebrow = new Label
        {
            Text = "A L W A Y S   O N   T O P", AutoSize = true, Left = 24, Top = 22,
            ForeColor = Theme.Lime, BackColor = Theme.Navy,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold)
        };
        var title = new Label
        {
            Text = "설정", AutoSize = true, Left = 22, Top = 38,
            ForeColor = Theme.OnNavy, BackColor = Theme.Navy,
            Font = Theme.Display(20f)
        };
        var subtitle = new Label
        {
            Text = "단축키 · 테두리 · 피드백 · 제외 창", AutoSize = true, Top = 55,
            ForeColor = Theme.OnNavyMuted, BackColor = Theme.Navy,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular)
        };
        // right-align the subtitle
        header.Controls.Add(subtitle);
        subtitle.Left = ClientSize.Width - subtitle.PreferredWidth - 24;

        header.Controls.Add(eyebrow);
        header.Controls.Add(title);
        Controls.Add(header);
    }

    /// <summary>Create a bento card with a lime marker and a bold navy title.</summary>
    private CardPanel MakeCard(int x, int y, int w, int h, string title)
    {
        var card = new CardPanel { Left = x, Top = y, Width = w, Height = h };

        var marker = new Panel { Left = 16, Top = 19, Width = 4, Height = 15, BackColor = Theme.Lime };
        var lbl = new Label
        {
            Text = title, AutoSize = true, Left = 28, Top = 15,
            Font = Theme.Body(10.5f, FontStyle.Bold), ForeColor = Theme.Navy, BackColor = Theme.Card
        };

        card.Controls.Add(marker);
        card.Controls.Add(lbl);
        Controls.Add(card);
        return card;
    }

    private void BuildHotkeyCard(CardPanel card)
    {
        StyleChip(_chkWin);
        StyleChip(_chkCtrl);
        StyleChip(_chkAlt);
        StyleChip(_chkShift);

        var chips = new FlowLayoutPanel
        {
            Left = 16, Top = 50, Width = card.Width - 32, Height = 40,
            BackColor = Theme.Card, WrapContents = false
        };
        _chkWin.Margin = _chkCtrl.Margin = _chkAlt.Margin = _chkShift.Margin = new Padding(0, 0, 8, 0);
        chips.Controls.AddRange(new Control[] { _chkWin, _chkCtrl, _chkAlt, _chkShift });
        card.Controls.Add(chips);

        var keyLbl = new Label
        {
            Text = "메인 키", AutoSize = true, Left = 16, Top = 102,
            ForeColor = Theme.Muted, BackColor = Theme.Card, Font = Theme.Body(9f)
        };
        _txtKey.Left = 74; _txtKey.Top = 98; _txtKey.Height = 26;
        _txtKey.BackColor = Color.White; _txtKey.ForeColor = Theme.Ink;
        _txtKey.Font = Theme.Body(10f, FontStyle.Bold);
        _txtKey.KeyDown += TxtKey_KeyDown;

        var hint = new Label
        {
            Text = "칸을 클릭한 뒤 원하는 키를 누르세요", AutoSize = true, Left = 170, Top = 102,
            ForeColor = Theme.Muted, BackColor = Theme.Card, Font = Theme.Body(9f)
        };

        card.Controls.Add(keyLbl);
        card.Controls.Add(_txtKey);
        card.Controls.Add(hint);
    }

    private void BuildBorderCard(CardPanel card)
    {
        StyleCheck(_chkBorder);
        _chkBorder.Left = 16; _chkBorder.Top = 48; _chkBorder.Width = card.Width - 32;
        _chkBorder.CheckedChanged += (_, _) => UpdateBorderEnabled();
        card.Controls.Add(_chkBorder);

        var colorLbl = new Label
        {
            Text = "색상", AutoSize = true, Left = 16, Top = 86,
            ForeColor = Theme.Muted, BackColor = Theme.Card, Font = Theme.Body(9f)
        };
        _swatch.Left = 56; _swatch.Top = 82;
        _swatch.BorderStyle = BorderStyle.FixedSingle; _swatch.BackColor = Theme.Navy;
        _txtColor.Left = 90; _txtColor.Top = 82; _txtColor.Height = 24;
        _txtColor.BackColor = Color.White; _txtColor.ForeColor = Theme.Ink;
        _txtColor.TextChanged += (_, _) => UpdateSwatch();
        _btnColor.Left = 194; _btnColor.Top = 82; _btnColor.ForeColor = Theme.OnNavy;
        _btnColor.Click += BtnColor_Click;

        var thickLbl = new Label
        {
            Text = "두께", AutoSize = true, Left = 16, Top = 120,
            ForeColor = Theme.Muted, BackColor = Theme.Card, Font = Theme.Body(9f)
        };
        _numThickness.Left = 56; _numThickness.Top = 116;
        _numThickness.BackColor = Color.White; _numThickness.ForeColor = Theme.Ink;
        var pxLbl = new Label
        {
            Text = "px", AutoSize = true, Left = 120, Top = 120,
            ForeColor = Theme.Muted, BackColor = Theme.Card, Font = Theme.Body(9f)
        };

        card.Controls.Add(colorLbl);
        card.Controls.Add(_swatch);
        card.Controls.Add(_txtColor);
        card.Controls.Add(_btnColor);
        card.Controls.Add(thickLbl);
        card.Controls.Add(_numThickness);
        card.Controls.Add(pxLbl);
    }

    private void BuildFeedbackCard(CardPanel card)
    {
        StyleCheck(_chkSound);
        StyleCheck(_chkNotify);
        _chkSound.Left = 16; _chkSound.Top = 56; _chkSound.Width = card.Width - 32;
        _chkNotify.Left = 16; _chkNotify.Top = 92; _chkNotify.Width = card.Width - 32;

        var note = new Label
        {
            Text = "고정 상태를 소리와 트레이 알림으로 알려줍니다.",
            Left = 16, Top = 122, Width = card.Width - 32, Height = 24,
            ForeColor = Theme.Muted, BackColor = Theme.Card, Font = Theme.Body(8.5f)
        };

        card.Controls.Add(_chkSound);
        card.Controls.Add(_chkNotify);
        card.Controls.Add(note);
    }

    private void BuildExcludedCard(CardPanel card)
    {
        var hint1 = new Label
        {
            Text = "단축키를 눌러도 여기 등록한 창은 고정되지 않도록 막아 주는 기능입니다.",
            AutoSize = true, Left = 16, Top = 42,
            ForeColor = Theme.Ink, BackColor = Theme.Card, Font = Theme.Body(9f)
        };
        var hint2 = new Label
        {
            Text = "제외할 창 제목의 일부를 한 줄에 하나씩 입력하세요.  예) YouTube, 카카오톡",
            AutoSize = true, Left = 16, Top = 62,
            ForeColor = Theme.Muted, BackColor = Theme.Card, Font = Theme.Body(8.5f)
        };
        _txtExcluded.Left = 16; _txtExcluded.Top = 88;
        _txtExcluded.Width = card.Width - 32; _txtExcluded.Height = card.Height - 102;
        _txtExcluded.BackColor = Color.White; _txtExcluded.ForeColor = Theme.Ink;
        _txtExcluded.Font = Theme.Body();

        card.Controls.Add(hint1);
        card.Controls.Add(hint2);
        card.Controls.Add(_txtExcluded);
    }

    private void BuildFooter(int m, int gap)
    {
        int y = 430 + 172 + gap; // below the excluded card
        var save = new PillButton
        {
            Text = "저장", Width = 118, Height = 40, Left = ClientSize.Width - m - 118, Top = y,
            FillColor = Theme.Lime, HoverColor = Theme.LimeDeep, ForeColor = Theme.Navy,
            DialogResult = DialogResult.OK
        };
        save.Click += BtnOk_Click;
        var cancel = new PillButton
        {
            Text = "취소", Width = 96, Height = 40, Left = save.Left - gap - 96, Top = y,
            FillColor = Theme.Card, HoverColor = Color.FromArgb(0xEE, 0xEF, 0xEA),
            OutlineColor = Theme.FieldBorder, ForeColor = Theme.Navy,
            DialogResult = DialogResult.Cancel
        };

        Controls.Add(save);
        Controls.Add(cancel);
        AcceptButton = save;
        CancelButton = cancel;
    }

    // ---- Control styling helpers -----------------------------------------
    /// <summary>Modifier keys shown as lime toggle "chips".</summary>
    private static void StyleChip(CheckBox c)
    {
        c.Appearance = Appearance.Button;
        c.FlatStyle = FlatStyle.Flat;
        c.AutoSize = false;
        c.Size = new Size(66, 34);
        c.TextAlign = ContentAlignment.MiddleCenter;
        c.Font = Theme.Body(9.5f, FontStyle.Bold);
        c.BackColor = Theme.Card;
        c.Cursor = Cursors.Hand;
        c.FlatAppearance.BorderSize = 1;
        c.FlatAppearance.CheckedBackColor = Theme.Lime;
        c.FlatAppearance.MouseOverBackColor = Color.FromArgb(0xF2, 0xF5, 0xE2);

        void Update()
        {
            c.ForeColor = c.Checked ? Theme.Navy : Theme.Muted;
            c.FlatAppearance.BorderColor = c.Checked ? Theme.LimeDeep : Theme.FieldBorder;
        }
        c.CheckedChanged += (_, _) => Update();
        Update();
    }

    /// <summary>Plain option checkbox with themed text.</summary>
    private static void StyleCheck(CheckBox c)
    {
        c.AutoSize = false;
        c.Height = 26;
        c.FlatStyle = FlatStyle.Standard;
        c.BackColor = Theme.Card;
        c.ForeColor = Theme.Ink;
        c.Font = Theme.Body(9.75f);
        c.Cursor = Cursors.Hand;
    }

    // ---- Data <-> UI ------------------------------------------------------
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

        UpdateSwatch();
        UpdateBorderEnabled();
    }

    private void UpdateBorderEnabled()
    {
        bool on = _chkBorder.Checked;
        _swatch.Enabled = on;
        _txtColor.Enabled = on;
        _btnColor.Enabled = on;
        _numThickness.Enabled = on;
    }

    private void UpdateSwatch()
    {
        try { _swatch.BackColor = ColorTranslator.FromHtml(_txtColor.Text.Trim()); }
        catch { /* keep previous swatch color while the user is typing */ }
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
        ResultConfig.BorderColor = string.IsNullOrWhiteSpace(_txtColor.Text) ? "#0A84FF" : _txtColor.Text.Trim();
        ResultConfig.BorderThickness = (int)_numThickness.Value;
        ResultConfig.PlaySound = _chkSound.Checked;
        ResultConfig.ShowNotification = _chkNotify.Checked;
        ResultConfig.ExcludedApps = _txtExcluded.Lines
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();
    }
}
