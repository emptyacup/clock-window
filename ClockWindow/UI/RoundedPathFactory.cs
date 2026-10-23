using System.Drawing;
using System.Drawing.Drawing2D;

namespace ClockWindow.UI;

internal static class RoundedPathFactory
{
    public static GraphicsPath Create(int width, int height, int diameter)
    {
        diameter = Math.Max(1, Math.Min(diameter, Math.Min(width, height)));
        var path = new GraphicsPath();
        path.AddArc(0, 0, diameter, diameter, 180, 90);
        path.AddArc(width - diameter, 0, diameter, diameter, 270, 90);
        path.AddArc(width - diameter, height - diameter, diameter, diameter, 0, 90);
        path.AddArc(0, height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    // 浮点版本：用于抗锯齿绘制（描边时传入内缩 0.5px 的矩形，线条才不会糊成 2px）
    public static GraphicsPath Create(RectangleF rect, float radius)
    {
        var diameter = Math.Max(1f, Math.Min(radius * 2, Math.Min(rect.Width, rect.Height)));
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
