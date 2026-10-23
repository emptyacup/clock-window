using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using Svg;

namespace ClockWindow.UI;

/// <summary>
/// 提醒弹窗顶部的装饰图：每次随机组合"柔和渐变底 + 几何点缀 + 一种主体素材"，尽量每次打开都不一样。
/// 素材来源：Microsoft Fluent Emoji（MIT）、Open Doodles（CC0），以及用户放在
/// %AppData%\ClockWindow\decorations 下的自定义 SVG。
/// </summary>
internal static class PopupDecoration
{
    private const string EmojiResourcePrefix = "ClockWindow.Decorations.Emoji.";
    private const string DoodleResourcePrefix = "ClockWindow.Decorations.Doodles.";
    // Open Doodles 原图的强调色，渲染前替换成本次配色的强调色
    private const string DoodleAccentColor = "#FF5678";

    private static readonly string CustomFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClockWindow", "decorations");

    private sealed record Palette(Color From, Color To, Color Accent, Color Accent2);

    private static readonly Palette[] Palettes =
    {
        new(Color.FromArgb(238, 242, 255), Color.FromArgb(245, 232, 255), Color.FromArgb(99, 102, 241), Color.FromArgb(168, 85, 247)),   // 薰衣草
        new(Color.FromArgb(255, 241, 230), Color.FromArgb(255, 228, 236), Color.FromArgb(249, 115, 22), Color.FromArgb(236, 72, 153)),   // 蜜桃
        new(Color.FromArgb(230, 255, 245), Color.FromArgb(224, 242, 254), Color.FromArgb(16, 185, 129), Color.FromArgb(14, 165, 233)),   // 薄荷
        new(Color.FromArgb(224, 242, 254), Color.FromArgb(237, 233, 254), Color.FromArgb(59, 130, 246), Color.FromArgb(139, 92, 246)),   // 晴空
        new(Color.FromArgb(254, 249, 195), Color.FromArgb(255, 237, 213), Color.FromArgb(234, 179, 8), Color.FromArgb(249, 115, 22)),    // 柠檬
        new(Color.FromArgb(255, 228, 230), Color.FromArgb(252, 231, 243), Color.FromArgb(244, 63, 94), Color.FromArgb(217, 70, 239)),    // 玫瑰
        new(Color.FromArgb(236, 252, 203), Color.FromArgb(220, 252, 231), Color.FromArgb(101, 163, 13), Color.FromArgb(20, 184, 166)),   // 青草
    };

    private enum Layout { Doodle, EmojiHero, EmojiScatter, Geometric, Custom }

    private static readonly Random Rng = new();
    private static readonly ShuffleBag<string> EmojiBag = new(ListResources(EmojiResourcePrefix), Rng);
    private static readonly ShuffleBag<string> DoodleBag = new(ListResources(DoodleResourcePrefix), Rng);
    private static readonly ShuffleBag<Palette> PaletteBag = new(Palettes, Rng);
    private static Layout? _lastLayout;

    public static Bitmap Render(Size size)
    {
        var palette = PaletteBag.Next();
        var bitmap = new Bitmap(size.Width, size.Height);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        var rect = new Rectangle(Point.Empty, size);
        using (var bg = new LinearGradientBrush(rect, palette.From, palette.To, Rng.Next(0, 360)))
        {
            g.FillRectangle(bg, rect);
        }

        var layout = PickLayout();
        DrawGeometry(g, size, palette, strong: layout == Layout.Geometric);

        switch (layout)
        {
            case Layout.Doodle: DrawDoodle(g, size, palette); break;
            case Layout.EmojiHero: DrawEmojiHero(g, size); break;
            case Layout.EmojiScatter: DrawEmojiScatter(g, size); break;
            case Layout.Custom: DrawCustom(g, size); break;
        }
        return bitmap;
    }

    private static Layout PickLayout()
    {
        var candidates = new List<Layout> { Layout.Geometric };
        if (DoodleBag.Count > 0) candidates.Add(Layout.Doodle);
        if (EmojiBag.Count > 0) { candidates.Add(Layout.EmojiHero); candidates.Add(Layout.EmojiScatter); }
        // 用户自己放了素材时，提高它出现的概率
        if (ListCustomFiles().Length > 0) { candidates.Add(Layout.Custom); candidates.Add(Layout.Custom); }

        if (candidates.Count > 1 && _lastLayout is { } last) candidates.RemoveAll(l => l == last);
        var layout = candidates[Rng.Next(candidates.Count)];
        _lastLayout = layout;
        return layout;
    }

    #region 几何点缀

    private static void DrawGeometry(Graphics g, Size size, Palette p, bool strong)
    {
        int alphaScale = strong ? 2 : 1;
        var painters = new List<Action>
        {
            () => DrawSoftBlobs(g, size, p, alphaScale),
            () => DrawWave(g, size, p, alphaScale),
            () => DrawDotGrid(g, size, p, alphaScale),
            () => DrawSparkles(g, size, p, alphaScale),
            () => DrawRings(g, size, p, alphaScale),
            () => DrawConfetti(g, size, p, alphaScale),
        };
        Shuffle(painters);
        int count = strong ? Rng.Next(3, 5) : Rng.Next(1, 3);
        foreach (var paint in painters.Take(count)) paint();
    }

    private static void DrawSoftBlobs(Graphics g, Size size, Palette p, int alphaScale)
    {
        for (int i = 0, n = Rng.Next(2, 4); i < n; i++)
        {
            int d = Rng.Next(size.Height, size.Height * 2);
            var r = new RectangleF(Rng.Next(-d / 2, size.Width - d / 2), Rng.Next(-d / 2, size.Height - d / 3), d, d);
            using var path = new GraphicsPath();
            path.AddEllipse(r);
            // 中心实、边缘透明的径向渐变，模拟模糊光斑
            using var brush = new PathGradientBrush(path)
            {
                CenterColor = Color.FromArgb(Math.Min(255, 45 * alphaScale), i % 2 == 0 ? p.Accent : p.Accent2),
                SurroundColors = new[] { Color.FromArgb(0, p.Accent) },
            };
            g.FillPath(brush, path);
        }
    }

    private static void DrawWave(Graphics g, Size size, Palette p, int alphaScale)
    {
        for (int layer = 0; layer < 2; layer++)
        {
            float baseY = size.Height * (0.55f + layer * 0.15f);
            float amplitude = Rng.Next(6, 16);
            float wavelength = Rng.Next(120, 260);
            float phase = (float)(Rng.NextDouble() * Math.PI * 2);
            using var path = new GraphicsPath();
            var points = new List<PointF>();
            for (int x = 0; x <= size.Width; x += 8)
                points.Add(new PointF(x, baseY + amplitude * MathF.Sin(x / wavelength * MathF.PI * 2 + phase)));
            path.AddLines(points.ToArray());
            path.AddLine(size.Width, size.Height, 0, size.Height);
            path.CloseFigure();
            using var brush = new SolidBrush(Color.FromArgb(Math.Min(255, 28 * alphaScale), layer == 0 ? p.Accent : p.Accent2));
            g.FillPath(brush, path);
        }
    }

    private static void DrawDotGrid(Graphics g, Size size, Palette p, int alphaScale)
    {
        int cols = Rng.Next(5, 9), rows = Rng.Next(3, 5), gap = 12;
        float x0 = Rng.Next(0, Math.Max(1, size.Width - cols * gap));
        float y0 = Rng.Next(10, Math.Max(11, size.Height - rows * gap - 10));
        using var brush = new SolidBrush(Color.FromArgb(Math.Min(255, 70 * alphaScale), p.Accent));
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                g.FillEllipse(brush, x0 + c * gap, y0 + r * gap, 3, 3);
    }

    private static void DrawSparkles(Graphics g, Size size, Palette p, int alphaScale)
    {
        for (int i = 0, n = Rng.Next(3, 7); i < n; i++)
        {
            float cx = Rng.Next(10, size.Width - 10), cy = Rng.Next(10, size.Height - 10);
            float r = Rng.Next(4, 11);
            using var path = new GraphicsPath();
            // 四角星：四个尖端之间用向内收的控制点连成曲线
            path.AddBeziers(new[]
            {
                new PointF(cx, cy - r), new PointF(cx + r * 0.15f, cy - r * 0.15f), new PointF(cx + r * 0.15f, cy - r * 0.15f),
                new PointF(cx + r, cy), new PointF(cx + r * 0.15f, cy + r * 0.15f), new PointF(cx + r * 0.15f, cy + r * 0.15f),
                new PointF(cx, cy + r), new PointF(cx - r * 0.15f, cy + r * 0.15f), new PointF(cx - r * 0.15f, cy + r * 0.15f),
                new PointF(cx - r, cy), new PointF(cx - r * 0.15f, cy - r * 0.15f), new PointF(cx - r * 0.15f, cy - r * 0.15f),
                new PointF(cx, cy - r),
            });
            using var brush = new SolidBrush(Color.FromArgb(Math.Min(255, 110 * alphaScale), i % 2 == 0 ? p.Accent : p.Accent2));
            g.FillPath(brush, path);
        }
    }

    private static void DrawRings(Graphics g, Size size, Palette p, int alphaScale)
    {
        for (int i = 0, n = Rng.Next(1, 3); i < n; i++)
        {
            int d = Rng.Next(40, 110);
            using var pen = new Pen(Color.FromArgb(Math.Min(255, 50 * alphaScale), p.Accent2), Rng.Next(2, 5));
            g.DrawEllipse(pen, Rng.Next(-d / 3, size.Width - d / 2), Rng.Next(-d / 3, size.Height - d / 2), d, d);
        }
    }

    private static void DrawConfetti(Graphics g, Size size, Palette p, int alphaScale)
    {
        for (int i = 0, n = Rng.Next(8, 15); i < n; i++)
        {
            var state = g.Save();
            g.TranslateTransform(Rng.Next(0, size.Width), Rng.Next(0, size.Height));
            g.RotateTransform(Rng.Next(0, 180));
            using var brush = new SolidBrush(Color.FromArgb(Math.Min(255, 90 * alphaScale), i % 2 == 0 ? p.Accent : p.Accent2));
            using var path = RoundedPathFactory.Create(new RectangleF(-5, -2, 10, 4), 2);
            g.FillPath(brush, path);
            g.Restore(state);
        }
    }

    #endregion

    #region 主体素材

    private static void DrawDoodle(Graphics g, Size size, Palette p)
    {
        var svg = ReadResource(DoodleBag.Next()).Replace(DoodleAccentColor, ToHex(p.Accent), StringComparison.OrdinalIgnoreCase);
        var doc = SvgDocument.FromSvg<SvgDocument>(svg);
        var dims = doc.GetDimensions();
        // 涂鸦画布（1024×768）四周留白很多且各张不一样：先按较大尺寸渲染，裁掉透明边，
        // 再把真正有内容的部分等比塞进头图，人物既不会太小，也不会被上下裁掉
        const int renderHeight = 480;
        using var image = doc.Draw((int)(renderHeight * dims.Width / dims.Height), renderHeight);
        var content = FindOpaqueBounds(image);
        if (content.IsEmpty) return;

        const int verticalPadding = 12;
        float scale = Math.Min((size.Height - verticalPadding * 2f) / content.Height, size.Width * 0.6f / content.Width);
        int w = (int)(content.Width * scale), h = (int)(content.Height * scale);
        int minX = size.Width * 2 / 5, maxX = size.Width - w - 52;
        int x = maxX > minX ? Rng.Next(minX, maxX) : Math.Max(0, maxX);
        g.DrawImage(image, new Rectangle(x, (size.Height - h) / 2, w, h), content, GraphicsUnit.Pixel);
    }

    /// <summary>找出图片中非透明像素的包围盒。</summary>
    private static Rectangle FindOpaqueBounds(Bitmap bitmap)
    {
        var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            var pixels = new int[data.Stride / 4 * data.Height];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            int strideInPixels = data.Stride / 4;
            int left = bitmap.Width, top = bitmap.Height, right = -1, bottom = -1;
            for (int y = 0; y < bitmap.Height; y++)
            {
                int row = y * strideInPixels;
                for (int x = 0; x < bitmap.Width; x++)
                {
                    // 忽略几乎透明的抗锯齿边缘像素
                    if ((pixels[row + x] >>> 24) < 16) continue;
                    if (x < left) left = x;
                    if (x > right) right = x;
                    if (y < top) top = y;
                    if (y > bottom) bottom = y;
                }
            }
            return right < 0 ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static void DrawEmojiHero(Graphics g, Size size)
    {
        // 先画四周散落的小 emoji，再画中间偏右的大 emoji 压在上面
        for (int i = 0, n = Rng.Next(3, 6); i < n; i++)
        {
            int s = Rng.Next(20, 32);
            DrawEmoji(g, EmojiBag.Next(), new PointF(Rng.Next(60, size.Width - 20), Rng.Next(14, size.Height - 30)), s, Rng.Next(-30, 30), 0.55f);
        }
        int hero = Rng.Next(76, 92);
        DrawEmoji(g, EmojiBag.Next(), new PointF(Rng.Next(size.Width / 2, size.Width - hero / 2 - 50), size.Height / 2f - 4), hero, Rng.Next(-12, 12), 1f);
    }

    private static void DrawEmojiScatter(Graphics g, Size size)
    {
        // 同一批里挑 3~4 种 emoji 反复撒，比每个都不同更有"一套"的感觉
        var picks = Enumerable.Range(0, Rng.Next(3, 5)).Select(_ => EmojiBag.Next()).ToArray();
        int cols = 7, rows = 3;
        float cellW = size.Width / (float)cols, cellH = (size.Height - 16) / (float)rows;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (Rng.NextDouble() < 0.3) continue;
                float cx = (c + 0.5f) * cellW + Rng.Next(-10, 10) + (r % 2 == 0 ? 0 : cellW / 2);
                float cy = (r + 0.5f) * cellH + Rng.Next(-6, 6);
                DrawEmoji(g, picks[Rng.Next(picks.Length)], new PointF(cx, cy), Rng.Next(24, 38), Rng.Next(-35, 35), (float)(0.7 + Rng.NextDouble() * 0.3));
            }
        }
    }

    private static void DrawEmoji(Graphics g, string resourceName, PointF center, int size, float angle, float opacity)
    {
        var doc = SvgDocument.FromSvg<SvgDocument>(ReadResource(resourceName));
        using var image = doc.Draw(size, size);
        var state = g.Save();
        g.TranslateTransform(center.X, center.Y);
        g.RotateTransform(angle);
        DrawWithOpacity(g, image, new RectangleF(-size / 2f, -size / 2f, size, size), opacity);
        g.Restore(state);
    }

    private static void DrawCustom(Graphics g, Size size)
    {
        var files = ListCustomFiles();
        if (files.Length == 0) return;
        var file = files[Rng.Next(files.Length)];
        try
        {
            var doc = SvgDocument.Open(file);
            var dims = doc.GetDimensions();
            if (dims.Width <= 0 || dims.Height <= 0) return;
            // 等比缩放塞进右侧 2/3 区域，上下各留一点边
            float scale = Math.Min(size.Width * 0.66f / dims.Width, (size.Height - 24) / dims.Height);
            int w = Math.Max(1, (int)(dims.Width * scale)), h = Math.Max(1, (int)(dims.Height * scale));
            using var image = doc.Draw(w, h);
            g.DrawImage(image, size.Width - w - 24, (size.Height - h) / 2);
        }
        catch (Exception)
        {
            // 用户放进来的文件可能不是合法 SVG，或用到了 Svg.NET 不支持的特性；跳过，保留几何背景即可
        }
    }

    #endregion

    #region 工具

    private static void DrawWithOpacity(Graphics g, Image image, RectangleF dest, float opacity)
    {
        if (opacity >= 0.999f)
        {
            g.DrawImage(image, dest);
            return;
        }
        using var attributes = new System.Drawing.Imaging.ImageAttributes();
        attributes.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix { Matrix33 = opacity });
        var points = new[] { dest.Location, new PointF(dest.Right, dest.Top), new PointF(dest.Left, dest.Bottom) };
        g.DrawImage(image, points, new RectangleF(0, 0, image.Width, image.Height), GraphicsUnit.Pixel, attributes);
    }

    private static string[] ListResources(string prefix) =>
        Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(n => n.StartsWith(prefix, StringComparison.Ordinal)).ToArray();

    private static string ReadResource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"未找到嵌入资源 {name}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string[] ListCustomFiles()
    {
        try
        {
            return Directory.Exists(CustomFolder) ? Directory.GetFiles(CustomFolder, "*.svg") : Array.Empty<string>();
        }
        catch (IOException) { return Array.Empty<string>(); }
        catch (UnauthorizedAccessException) { return Array.Empty<string>(); }
    }

    private static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    #endregion
}
