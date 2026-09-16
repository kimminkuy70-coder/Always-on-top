using System;
using System.Drawing;
using System.Windows.Forms;

namespace AlwaysOnTop;

/// <summary>Read-only window that explains how to use the utility.</summary>
public sealed class UsageForm : Form
{
    public UsageForm(string hotkey)
    {
        Text = "Always On Top - 사용법";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9.5F);
        ClientSize = new Size(500, 400);

        var text = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            BackColor = SystemColors.Window,
            Dock = DockStyle.Top,
            Height = 348,
            Font = new Font("Segoe UI", 10F),
            Text = BuildText(hotkey)
        };
        text.Select(0, 0);

        var ok = new Button
        {
            Text = "닫기",
            Width = 90,
            Height = 30,
            Left = ClientSize.Width - 100,
            Top = 358,
            DialogResult = DialogResult.OK,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };

        Controls.Add(text);
        Controls.Add(ok);
        AcceptButton = ok;
    }

    private static string BuildText(string hotkey)
    {
        return
            "Always On Top (PowerToys Always On Top 클론)\r\n" +
            "──────────────────────────────────────────\r\n\r\n" +
            "■ 기본 사용법\r\n" +
            $"   1. 항상 위에 두고 싶은 창을 클릭해 활성화합니다.\r\n" +
            $"   2. 단축키 [{hotkey}] 를 누르면 그 창이 항상 최상위로 고정됩니다.\r\n" +
            "   3. 같은 단축키를 다시 누르면 고정이 해제됩니다.\r\n\r\n" +
            "■ 고정 표시\r\n" +
            "   - 고정된 창 주위에 색상 테두리가 표시됩니다.\r\n" +
            "   - 고정/해제 시 소리와 알림으로 상태를 알려줍니다.\r\n" +
            "   (테두리 · 소리 · 알림은 설정에서 끌 수 있습니다.)\r\n\r\n" +
            "■ 트레이 아이콘 메뉴 (오른쪽 클릭)\r\n" +
            "   - 지금 창 고정/해제 : 활성 창을 즉시 토글합니다.\r\n" +
            "   - 설정 : 단축키와 옵션을 변경합니다.\r\n" +
            "   - 사용법 보기 : 이 창을 다시 엽니다.\r\n" +
            "   - 종료 : 프로그램을 끝냅니다(모든 고정 해제).\r\n" +
            "   ※ 트레이 아이콘을 더블 클릭해도 활성 창을 토글할 수 있습니다.\r\n\r\n" +
            "■ 단축키 변경\r\n" +
            "   설정 창에서 Win/Ctrl/Alt/Shift 조합을 고르고, 키 입력칸을\r\n" +
            "   클릭한 뒤 원하는 키를 누르면 됩니다.\r\n\r\n" +
            "■ 제외할 창\r\n" +
            "   특정 창을 고정 대상에서 빼려면 설정의 '제외할 창' 목록에\r\n" +
            "   창 제목의 일부를 한 줄씩 입력하세요.\r\n\r\n" +
            "■ 참고\r\n" +
            "   - 백그라운드(시스템 트레이)에서 상주 실행됩니다.\r\n" +
            "   - 관리자 권한으로 실행되는 창을 고정하려면 이 프로그램도\r\n" +
            "     관리자 권한으로 실행해야 합니다.\r\n" +
            "   - 설정은 %APPDATA%\\AlwaysOnTop\\config.json 에 저장됩니다.\r\n";
    }
}
