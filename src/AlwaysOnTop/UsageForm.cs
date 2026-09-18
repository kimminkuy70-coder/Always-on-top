using System;
using System.Drawing;
using System.Windows.Forms;

namespace AlwaysOnTop;

/// <summary>
/// Read-only "how to use" window, presented as a minimalist bento grid of
/// rounded cards in the navy + lime theme (matching the settings dialog).
/// Card heights and text wrap to their content so nothing is clipped.
/// </summary>
public sealed class UsageForm : Form
{
    private readonly string _hotkey;

    public UsageForm(string hotkey)
    {
        _hotkey = string.IsNullOrWhiteSpace(hotkey) ? "Win + Ctrl + T" : hotkey;

        Text = "Always On Top - 사용법";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Theme.Canvas;
        Font = Theme.Body();
        ClientSize = new Size(640, 740);

        BuildHeader();
        BuildFooter();
        BuildContent();
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
            Text = "사용법", AutoSize = true, Left = 22, Top = 38,
            ForeColor = Theme.OnNavy, BackColor = Theme.Navy, Font = Theme.Display(20f)
        };

        // Current hotkey shown as a lime pill on the right.
        var pill = new CardPanel
        {
            BackColor = Theme.Lime, BorderWidth = 0, Radius = 10, Padding = new Padding(3),
            Height = 34, Top = 31, Width = Math.Max(120, TextRenderer.MeasureText(_hotkey, Theme.Body(10f, FontStyle.Bold)).Width + 34)
        };
        pill.Left = ClientSize.Width - pill.Width - 24;
        var pillText = new Label
        {
            Text = _hotkey, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Theme.Navy, BackColor = Theme.Lime, Font = Theme.Body(10f, FontStyle.Bold)
        };
        pill.Controls.Add(pillText);

        header.Controls.Add(eyebrow);
        header.Controls.Add(title);
        header.Controls.Add(pill);
        Controls.Add(header);
    }

    private void BuildContent()
    {
        var content = new Panel
        {
            Left = 0, Top = 96, Width = ClientSize.Width,
            Height = ClientSize.Height - 96 - 64, BackColor = Theme.Canvas, AutoScroll = true
        };
        Controls.Add(content);

        const int M = 20, gap = 16;
        int availW = ClientSize.Width - 2 * M - 8; // leave slack for a scrollbar
        int colW = (availW - gap) / 2;
        int rightX = M + colW + gap;
        int y = 8;

        // Quick start (full width)
        var quick = MakeCard(content, M, y, availW, "빠른 시작");
        int qb = AddSteps(quick, new[]
        {
            "항상 위에 두고 싶은 창을 클릭해 활성화합니다.",
            $"단축키 [{_hotkey}] 를 누르면 그 창이 최상위로 고정됩니다.",
            "같은 단축키를 다시 누르면 고정이 해제됩니다."
        }, 48);
        quick.Height = qb + 10;
        y = quick.Bottom + gap;

        // Tray menu (left) + hotkey change (right)
        var tray = MakeCard(content, M, y, colW, "트레이 아이콘 메뉴");
        int tb = AddBullets(tray, new[]
        {
            "지금 창 고정/해제 — 활성 창을 즉시 토글",
            "설정 — 단축키와 옵션 변경",
            "사용법 보기 — 이 창 다시 열기",
            "종료 — 프로그램 끝내기 (모든 고정 해제)",
            "※ 아이콘 더블 클릭도 토글로 동작합니다."
        }, 46);

        var change = MakeCard(content, rightX, y, colW, "단축키 변경");
        int cb = AddBullets(change, new[]
        {
            "설정 창을 엽니다.",
            "Win / Ctrl / Alt / Shift 조합을 고릅니다.",
            "키 입력칸을 클릭한 뒤 원하는 키를 누릅니다.",
            "저장하면 즉시 새 단축키가 적용됩니다."
        }, 46);

        int row2 = Math.Max(tb, cb) + 10;
        tray.Height = row2; change.Height = row2;
        y += row2 + gap;

        // Excluded windows (left) + notes (right)
        var excluded = MakeCard(content, M, y, colW, "제외할 창");
        int eb = AddBullets(excluded, new[]
        {
            "단축키를 눌러도 특정 창은 고정되지 않게 막아 주는 기능입니다.",
            "설정의 '제외할 창' 칸에 제외하고 싶은 창 제목의 일부를 한 줄에 하나씩 적으세요.",
            "예: 'YouTube' 를 넣으면 제목에 YouTube 가 들어간 창은 고정되지 않습니다."
        }, 46);

        var notes = MakeCard(content, rightX, y, colW, "참고");
        int nb = AddBullets(notes, new[]
        {
            "시스템 트레이에서 백그라운드로 상주합니다.",
            "관리자 창을 고정하려면 이 앱도 관리자 권한으로 실행하세요.",
            "설정은 %APPDATA%\\AlwaysOnTop 에 저장됩니다."
        }, 46);

        int row3 = Math.Max(eb, nb) + 10;
        excluded.Height = row3; notes.Height = row3;
    }

    private void BuildFooter()
    {
        // Faint, watermark-like developer credit.
        var credit = new Label
        {
            Text = "made by 김민규", AutoSize = true,
            Left = 22, Top = ClientSize.Height - 40,
            ForeColor = Color.FromArgb(0xC2, 0xC6, 0xBD), BackColor = Theme.Canvas,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic)
        };
        Controls.Add(credit);

        var close = new PillButton
        {
            Text = "닫기", Width = 110, Height = 40,
            Left = ClientSize.Width - 20 - 110, Top = ClientSize.Height - 52,
            FillColor = Theme.Navy, HoverColor = Theme.NavySoft, ForeColor = Theme.OnNavy
        };
        // This window is shown modeless (Show, not ShowDialog), so a DialogResult
        // does not close it - close explicitly.
        close.Click += (_, _) => Close();
        Controls.Add(close);
        AcceptButton = close;
        CancelButton = close;
    }

    // ---- Card helpers -----------------------------------------------------
    private static CardPanel MakeCard(Control host, int x, int y, int w, string title)
    {
        var card = new CardPanel { Left = x, Top = y, Width = w, Height = 120 };
        var marker = new Panel { Left = 16, Top = 19, Width = 4, Height = 15, BackColor = Theme.Lime };
        var lbl = new Label
        {
            Text = title, AutoSize = true, Left = 28, Top = 15,
            Font = Theme.Body(10.5f, FontStyle.Bold), ForeColor = Theme.Navy, BackColor = Theme.Card
        };
        card.Controls.Add(marker);
        card.Controls.Add(lbl);
        host.Controls.Add(card);
        return card;
    }

    /// <summary>Numbered steps that wrap to their width. Returns the bottom Y.</summary>
    private static int AddSteps(CardPanel card, string[] steps, int startY)
    {
        int y = startY;
        int textW = card.Width - 64;
        for (int i = 0; i < steps.Length; i++)
        {
            var body = new Label
            {
                Text = steps[i], Left = 48, Top = y + 1, AutoSize = true,
                MaximumSize = new Size(textW, 0),
                ForeColor = Theme.Ink, BackColor = Theme.Card, Font = Theme.Body(9.75f)
            };
            card.Controls.Add(body);
            var num = new Label
            {
                Text = (i + 1).ToString(), Left = 16, Top = y, Width = 22, Height = 22,
                TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.Navy, BackColor = Theme.Lime,
                Font = Theme.Body(9.5f, FontStyle.Bold)
            };
            card.Controls.Add(num);
            y = Math.Max(body.Bottom, y + 22) + 10;
        }
        return y;
    }

    /// <summary>Bulleted lines that wrap to their width. Returns the bottom Y.</summary>
    private static int AddBullets(CardPanel card, string[] lines, int startY)
    {
        int y = startY;
        int textW = card.Width - 44;
        foreach (string line in lines)
        {
            var body = new Label
            {
                Text = line, Left = 32, Top = y, AutoSize = true,
                MaximumSize = new Size(textW, 0),
                ForeColor = Theme.Ink, BackColor = Theme.Card, Font = Theme.Body(9.25f)
            };
            card.Controls.Add(body);
            var dot = new Panel { Left = 18, Top = y + 7, Width = 5, Height = 5, BackColor = Theme.LimeDeep };
            card.Controls.Add(dot);
            y = body.Bottom + 8;
        }
        return y;
    }
}
