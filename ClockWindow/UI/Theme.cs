using System.Drawing;

namespace ClockWindow.UI;

/// <summary>
/// Linear / Raycast 风格的设计 token：中性灰底 + 白色卡片 + 细描边 + 靛蓝→紫渐变强调色。
/// </summary>
internal static class Theme
{
    // 背景与表面
    public static readonly Color WindowBackground = Color.FromArgb(247, 247, 248);
    public static readonly Color Surface = Color.White;
    public static readonly Color SurfaceHover = Color.FromArgb(250, 250, 251);
    public static readonly Color FooterBackground = Color.FromArgb(250, 250, 250);

    // 描边
    public static readonly Color Border = Color.FromArgb(228, 228, 231);
    public static readonly Color BorderStrong = Color.FromArgb(212, 212, 216);

    // 文字
    public static readonly Color TextPrimary = Color.FromArgb(24, 24, 27);
    public static readonly Color TextSecondary = Color.FromArgb(82, 82, 91);
    public static readonly Color TextMuted = Color.FromArgb(161, 161, 170);

    // 强调色（主按钮/开关/选中态用渐变，两端颜色）
    public static readonly Color AccentStart = Color.FromArgb(99, 102, 241);
    public static readonly Color AccentEnd = Color.FromArgb(139, 92, 246);
    public static readonly Color AccentSoft = Color.FromArgb(245, 245, 255);
    public static readonly Color AccentBorder = Color.FromArgb(165, 180, 252);
    public static readonly Color AccentRing = Color.FromArgb(224, 231, 255);

    // 幽灵按钮 / 标题栏按钮的悬停底色
    public static readonly Color GhostHover = Color.FromArgb(237, 237, 240);
    public static readonly Color GhostPressed = Color.FromArgb(228, 228, 231);
    public static readonly Color DangerSoft = Color.FromArgb(254, 226, 226);
    public static readonly Color Danger = Color.FromArgb(220, 38, 38);

    public const int RadiusSmall = 6;
    public const int RadiusMedium = 8;
    public const int RadiusLarge = 12;

    private const string FontName = "Microsoft YaHei UI";
    private const string NumberFontName = "Segoe UI Semibold";

    public static Font Font(float size, FontStyle style = FontStyle.Regular) => new(FontName, size, style);

    public static Font NumberFont(float size) => new(NumberFontName, size);

    public static Color Lighten(Color color, float amount) => Color.FromArgb(
        color.A,
        color.R + (int)((255 - color.R) * amount),
        color.G + (int)((255 - color.G) * amount),
        color.B + (int)((255 - color.B) * amount));

    public static Color Darken(Color color, float amount) => Color.FromArgb(
        color.A,
        (int)(color.R * (1 - amount)),
        (int)(color.G * (1 - amount)),
        (int)(color.B * (1 - amount)));

    public static Color Lerp(Color from, Color to, float t) => Color.FromArgb(
        (int)(from.A + (to.A - from.A) * t),
        (int)(from.R + (to.R - from.R) * t),
        (int)(from.G + (to.G - from.G) * t),
        (int)(from.B + (to.B - from.B) * t));
}
