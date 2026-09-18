using System;
using System.Drawing;
using System.Windows.Forms;

namespace AlwaysOnTop;

/// <summary>
/// Read-only "how to use" window, presented as a minimalist bento grid of
/// rounded cards in the navy + lime theme (matching the settings dialog).
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
        ClientSize = new Size(600, 680);

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

        // Quick start (full width)
        var quick = MakeCard(content, M, 8, availW, 158, "빠른 시작");
        var steps = new[]
        {
            "항상 위에 두고 싶은 창을 클릭해 활성화합니다.",
            $"단축키 [{_hotkey}] 를 누르면 그 창이 최상위로 고정됩니다.",
            "같은 단축키를 다시 누르면 고정이 해제됩니다."
        };
        int sy = 50;
        for (int i = 0; i < steps.Length; i++)
        {
            AddStep(quick, i + 1, steps[i], sy);
            sy += 34;
        }

        // Tray menu (left) + hotkey change (right)
        var tray = MakeCard(content, M, 182, colW, 176, "트레이 아이콘 메뉴");
        AddBullets(tray, new[]
        {
            "지금 창 고정/해제 — 활성 창을 즉시 토글",
            "설정 — 단축키와 옵션 변경",
            "사용법 보기 — 이 창 다시 열기",
            "종료 — 프로그램 끝내기(모든 고정 해제)",
            "※ 아이콘 더블 클릭도 토글로 동작"
        });

        var change = MakeCard(content, rightX, 182, colW, 176, "단축키 변경");
        AddBullets(change, new[]
        {
            "설정 창을 엽니다.",
            "Win / Ctrl / Alt / Shift 조합을 고릅니다.",
            "키 입력칸을 클릭한 뒤 원하는 키를 누릅니다.",
            "저장하면 즉시 새 단축키가 적용됩니다."
        });

        // Excluded (left) + notes (right)
        var excluded = MakeCard(content, M, 374, colW, 150, "제외할 창");
        AddBullets(excluded, new[]
        {
            "특정 창을 고정 대상에서 빼려면 설정의",
            "'제외할 창' 목록에 창 제목의 일부를",
            "한 줄에 하나씩 입력하세요."
        });

        var notes = MakeCard(content, rightX, 374, colW, 150, "참고");
        AddBullets(notes, new[]
        {
            "시스템 트레이에서 백그라운드로 상주합니다.",
            "관리자 창을 고정하려면 이 앱도 관리자로 실행.",
            "설정은 %APPDATA%\\AlwaysOnTop 에 저장됩니다."
        });
    }

    private void BuildFooter()
    {
        // Faint, watermark-like developer credit.
        var credit = new Label
        {
            Text = "제작 · 김민규", AutoSize = true,
            Left = 22, Top = ClientSize.Height - 40,
            ForeColor = Color.FromArgb(0xC2, 0xC6, 0xBD), BackColor = Theme.Canvas,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic)
        };
        Controls.Add(credit);

        var close = new PillButton
        {
            Text = "닫기", Width = 110, Height = 40,
            Left = ClientSize.Width - 20 - 110, Top = ClientSize.Height - 52,
            FillColor = Theme.Navy, HoverColor = Theme.NavySoft, ForeColor = Theme.OnNavy,
            DialogResult = DialogResult.OK
        };
        Controls.Add(close);
        AcceptButton = close;
    }

    // ---- Card helpers -----------------------------------------------------
    private static CardPanel MakeCard(Control host, int x, int y, int w, int h, string title)
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
        host.Controls.Add(card);
        return card;
    }

    private static void AddStep(CardPanel card, int number, string text, int y)
    {
        var num = new Label
        {
            Text = number.ToString(), Left = 16, Top = y, Width = 22, Height = 22,
            TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.Navy, BackColor = Theme.Lime,
            Font = Theme.Body(9.5f, FontStyle.Bold)
        };
        var body = new Label
        {
            Text = text, Left = 48, Top = y + 2, Width = card.Width - 64, AutoSize = false, Height = 22,
            ForeColor = Theme.Ink, BackColor = Theme.Card, Font = Theme.Body(9.75f)
        };
        card.Controls.Add(num);
        card.Controls.Add(body);
    }

    private static void AddBullets(CardPanel card, string[] lines)
    {
        int y = 48;
        foreach (string line in lines)
        {
            var dot = new Panel { Left = 18, Top = y + 7, Width = 5, Height = 5, BackColor = Theme.LimeDeep };
            var body = new Label
            {
                Text = line, Left = 32, Top = y, Width = card.Width - 48, Height = 24, AutoSize = false,
                ForeColor = Theme.Ink, BackColor = Theme.Card, Font = Theme.Body(9.25f)
            };
            card.Controls.Add(dot);
            card.Controls.Add(body);
            y += 26;
        }
    }
}
