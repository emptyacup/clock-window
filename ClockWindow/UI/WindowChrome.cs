using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClockWindow.UI;

/// <summary>
/// 无边框窗口的外观辅助：圆角、阴影、描边、应用图标。
/// </summary>
internal static class WindowChrome
{
    public const int CS_DROPSHADOW = 0x00020000;

    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    // Win11（build 22000+）的 DWM 能画抗锯齿圆角和 1px 系统描边；Win10 没有这个能力
    public static bool SupportsNativeRoundedCorners => Environment.OSVersion.Version.Build >= 22000;

    /// <summary>
    /// Win11 交给 DWM 画圆角；Win10 退回 Region 裁剪（边缘会有轻微锯齿，再用 <see cref="DrawBorder"/> 描一圈边弱化）。
    /// 需要在窗口句柄创建后调用。
    /// </summary>
    public static void ApplyRoundedCorners(Form form, int radius)
    {
        if (SupportsNativeRoundedCorners)
        {
            int preference = DWMWCP_ROUND;
            DwmSetWindowAttribute(form.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
            return;
        }

        void ApplyRegion()
        {
            if (form.Width <= 0 || form.Height <= 0) return;
            var old = form.Region;
            using var path = RoundedPathFactory.Create(form.Width, form.Height, radius * 2);
            form.Region = new Region(path);
            old?.Dispose();
        }

        form.Resize += (_, _) => ApplyRegion();
        ApplyRegion();
    }

    public static void DrawBorder(Graphics g, Size size, int radius)
    {
        // Win11 下 DWM 已经画了系统描边，再画一圈会和它叠在一起
        if (SupportsNativeRoundedCorners) return;

        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedPathFactory.Create(new RectangleF(0.5f, 0.5f, size.Width - 1f, size.Height - 1f), radius);
        using var pen = new Pen(Theme.BorderStrong, 1f);
        g.DrawPath(pen, path);
    }

    /// <summary>应用图标：渐变圆角方块 + 白色时钟指针，和托盘图标同一套语言。</summary>
    public static void DrawAppIcon(Graphics g, Rectangle rect)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var path = RoundedPathFactory.Create(rect, rect.Width * 0.3f))
        using (var brush = new LinearGradientBrush(rect, Theme.AccentStart, Theme.AccentEnd, 45f))
        {
            g.FillPath(brush, path);
        }

        float cx = rect.X + rect.Width / 2f;
        float cy = rect.Y + rect.Height / 2f;
        float r = rect.Width * 0.3f;
        using var pen = new Pen(Color.White, Math.Max(1.5f, rect.Width / 12f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
        g.DrawLine(pen, cx, cy, cx, cy - r * 0.6f);
        g.DrawLine(pen, cx, cy, cx + r * 0.45f, cy + r * 0.25f);
    }
}
