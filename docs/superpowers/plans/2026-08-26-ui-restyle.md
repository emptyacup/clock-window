# ClockWindow UI 重新设计 实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 把 ClockWindow 的三个窗体从默认 WinForms 样式改造成规格文档 `docs/superpowers/specs/2026-08-26-ui-restyle-design.md` 里确定的方向：提醒弹窗用线稿动图背景 + 浅色磨砂文字条；设置窗口 / 编辑规则窗口用统一的自绘浅色卡片风格（深蓝灰强调色 + 线稿装饰图标）。

**架构：** 新增一批与具体窗体无关的可复用小组件（配色常量、圆角路径工具、GIF 裁剪/逐帧播放的纯逻辑、自绘开关/胶囊按钮/圆角输入框/规则卡片行），先用单元测试锁定其中的纯逻辑部分（图像裁剪矩形计算、GIF 帧延迟解析、星期格式化），再逐个重写三个窗体去组合使用这些组件。全程不引入除测试框架外的任何第三方依赖。

**技术栈：** .NET 10 / C# / WinForms / GDI+（`System.Drawing`），单元测试用 xUnit（项目当前没有任何测试基础设施，本次新增）。

**重要说明：**
- 本机 PATH 里的 `dotnet` 是旧的 32 位版本，没有 SDK。所有 `dotnet` 命令必须用完整路径 `"/c/Program Files/dotnet/dotnet.exe"`（Bash 工具下的写法；PowerShell 下用 `"C:\Program Files\dotnet\dotnet.exe"`）。
- 这个目录不是 git 仓库，"commit" 步骤请跳过（不要执行 `git init`，除非用户另有要求），改成人工确认"这一步产出的文件已保存"即可。
- 背景动图 `ClockWindow/Resources/line-art-dance.gif` 已经在头脑风暴阶段下载好并放进项目里了，来源 Pixabay（Pixabay License，免费商用无需署名），不需要重新下载。

---

## 文件结构

新增文件：
- `ClockWindow/UI/Theme.cs` — 全局配色常量
- `ClockWindow/UI/RoundedPathFactory.cs` — 生成圆角矩形 `GraphicsPath` 的共享工具（替代原来 `ReminderPopupForm` 里私有的 `RoundedPath` 方法）
- `ClockWindow/UI/DayDisplay.cs` — 星期几的显示名称/标志位/格式化逻辑（替代原来在 `SettingsForm` 和 `RuleEditForm` 里各自重复的 `DayAbbrev`/`DayFlags`/`FormatDays`）
- `ClockWindow/UI/ImageCoverFit.cs` — 纯逻辑：计算"图片铺满目标区域并居中裁剪"（类似 CSS `object-fit: cover`）时应该截取的源图矩形
- `ClockWindow/UI/GifFrameDelayReader.cs` — 纯逻辑：从多帧 GIF 的 `Image` 对象读出每一帧的播放延迟（毫秒）
- `ClockWindow/UI/ButtonStyler.cs` — 把普通 `Button` 变成自绘扁平圆角按钮的共享方法
- `ClockWindow/UI/ToggleSwitch.cs` — 自绘 iOS 风格开关控件（用于"规则是否启用"）
- `ClockWindow/UI/CapsuleToggle.cs` — 自绘胶囊形文字开关（用于"重复星期"多选）
- `ClockWindow/UI/RoundedTextBox.cs` — 带自绘圆角描边的文本框容器
- `ClockWindow/UI/RuleRowControl.cs` — 设置窗口里一条"规则卡片"的自绘行控件
- `ClockWindow.Tests/ClockWindow.Tests.csproj` — 新增的 xUnit 测试项目
- `ClockWindow.Tests/ImageCoverFitTests.cs`
- `ClockWindow.Tests/GifFrameDelayReaderTests.cs`
- `ClockWindow.Tests/DayDisplayTests.cs`

修改文件：
- `ClockWindow/ClockWindow.csproj` — 把背景动图设为嵌入资源
- `ClockWindow/UI/ReminderPopupForm.cs` — 改成线稿动图背景 + 磨砂文字条
- `ClockWindow/UI/SettingsForm.cs` — 把系统 `ListView` 换成自绘卡片列表
- `ClockWindow/UI/RuleEditForm.cs` — 换成自绘圆角输入框 + 胶囊星期开关
- `ClockWindow.slnx` — 加入新的测试项目
- `README.md` — 补一行素材来源说明

---

### 任务 1：把背景动图设为嵌入资源

**文件：**
- 修改：`ClockWindow/ClockWindow.csproj`

- [ ] **步骤 1：确认动图文件已存在**

运行：
```bash
ls -la "d:/a_my_program/clock-window/ClockWindow/Resources/line-art-dance.gif"
```
预期：文件存在，大小约 268KB（274594 字节）。

- [ ] **步骤 2：在 csproj 里把它设为嵌入资源**

把 `ClockWindow/ClockWindow.csproj` 改成：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>ClockWindow</AssemblyName>
    <RootNamespace>ClockWindow</RootNamespace>
    <ApplicationVisualStyles>true</ApplicationVisualStyles>
    <ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>
    <InvariantGlobalization>true</InvariantGlobalization>
  </PropertyGroup>

  <ItemGroup>
    <EmbeddedResource Include="Resources\line-art-dance.gif">
      <LogicalName>ClockWindow.Resources.line-art-dance.gif</LogicalName>
    </EmbeddedResource>
  </ItemGroup>

</Project>
```

（只新增了最后一个 `<ItemGroup>`，其余属性不变。`LogicalName` 显式固定了资源名，后面 `GifFrameDelayReader`/`ReminderPopupForm` 里用字符串常量 `"ClockWindow.Resources.line-art-dance.gif"` 读取时就不用猜测 MSBuild 的默认命名规则。）

- [ ] **步骤 3：验证能正常构建**

运行：
```bash
cd "d:/a_my_program/clock-window/ClockWindow" && "/c/Program Files/dotnet/dotnet.exe" build
```
预期：`已成功生成`，0 个错误（此时资源还没被任何代码读取，只是打包进去，构建应该照常通过）。

- [ ] **步骤 4：保存确认**（非 git 仓库，跳过 commit）

---

### 任务 2：创建 ClockWindow.Tests 测试项目并接入解决方案

**文件：**
- 创建：`ClockWindow.Tests/ClockWindow.Tests.csproj`
- 修改：`ClockWindow.slnx`

- [ ] **步骤 1：用官方模板脚手架生成项目**

运行：
```bash
cd "d:/a_my_program/clock-window" && "/c/Program Files/dotnet/dotnet.exe" new xunit -n ClockWindow.Tests -o ClockWindow.Tests
```
预期：生成 `ClockWindow.Tests/ClockWindow.Tests.csproj`、`ClockWindow.Tests/UnitTest1.cs`、`ClockWindow.Tests/GlobalUsings.cs`（用模板自带的包版本，不要手写猜测版本号）。

- [ ] **步骤 2：删除模板自带的占位测试文件**

```bash
rm "d:/a_my_program/clock-window/ClockWindow.Tests/UnitTest1.cs"
```

- [ ] **步骤 3：编辑生成的 csproj —— 改目标框架、加窗体支持、加对主项目的引用、加测试用图片资源**

打开 `ClockWindow.Tests/ClockWindow.Tests.csproj`，在 `<PropertyGroup>` 里把
```xml
<TargetFramework>net10.0</TargetFramework>
```
改成：
```xml
<TargetFramework>net10.0-windows</TargetFramework>
<UseWindowsForms>true</UseWindowsForms>
```
（`ImageCoverFit`/`GifFrameDelayReader` 用到 `System.Drawing.Image`，在非 windows TFM 下部分 GDI+ API 会抛 `PlatformNotSupportedException`，必须用 `net10.0-windows`。）

然后在文件末尾 `</Project>` 之前加两个 `<ItemGroup>`：

```xml
  <ItemGroup>
    <ProjectReference Include="..\ClockWindow\ClockWindow.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="..\ClockWindow\Resources\line-art-dance.gif" Link="TestAssets\line-art-dance.gif" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
```

（测试用的 GIF 直接链接主项目里已有的那份文件，不重复拷贝一份到测试项目目录里，避免两份文件不同步。）

- [ ] **步骤 4：加入解决方案文件**

把 `ClockWindow.slnx` 改成：

```xml
<Solution>
  <Project Path="ClockWindow/ClockWindow.csproj" />
  <Project Path="ClockWindow.Tests/ClockWindow.Tests.csproj" />
</Solution>
```

- [ ] **步骤 5：验证测试项目能跑起来（此时应该是 0 个测试）**

运行：
```bash
cd "d:/a_my_program/clock-window" && "/c/Program Files/dotnet/dotnet.exe" test
```
预期：构建成功，`0 个测试` 或类似输出（因为还没写任何测试），没有编译错误。

- [ ] **步骤 6：保存确认**（非 git 仓库，跳过 commit）

---

### 任务 3：共享基础设施 —— Theme / RoundedPathFactory / DayDisplay

**文件：**
- 创建：`ClockWindow/UI/Theme.cs`
- 创建：`ClockWindow/UI/RoundedPathFactory.cs`
- 创建：`ClockWindow/UI/DayDisplay.cs`
- 测试：`ClockWindow.Tests/DayDisplayTests.cs`

- [ ] **步骤 1：创建 Theme.cs**

```csharp
using System.Drawing;

namespace ClockWindow.UI;

internal static class Theme
{
    public static readonly Color CardBackground = Color.White;
    public static readonly Color RowBackground = Color.FromArgb(247, 248, 250);
    public static readonly Color RowBackgroundSelected = Color.FromArgb(238, 240, 244);
    public static readonly Color Border = Color.FromArgb(230, 232, 235);
    public static readonly Color TitleText = Color.FromArgb(45, 49, 56);
    public static readonly Color BodyText = Color.FromArgb(75, 79, 88);
    public static readonly Color SubtleText = Color.FromArgb(138, 143, 160);
    public static readonly Color Accent = Color.FromArgb(75, 85, 99);
    public static readonly Color AccentHover = Color.FromArgb(95, 105, 120);
    public static readonly Color SecondaryButtonBackground = Color.FromArgb(240, 241, 243);
    public static readonly Color SecondaryButtonHover = Color.FromArgb(228, 230, 234);
    public static readonly Color SecondaryButtonText = Color.FromArgb(75, 79, 88);
}
```

- [ ] **步骤 2：创建 RoundedPathFactory.cs**

```csharp
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
}
```

- [ ] **步骤 3：先写 DayDisplay 的失败测试**

创建 `ClockWindow.Tests/DayDisplayTests.cs`：

```csharp
using ClockWindow.Models;
using ClockWindow.UI;
using Xunit;

namespace ClockWindow.Tests;

public class DayDisplayTests
{
    [Fact]
    public void FormatAbbrev_NoDays_ReturnsDash()
    {
        Assert.Equal("-", DayDisplay.FormatAbbrev(Days.None));
    }

    [Fact]
    public void FormatAbbrev_SingleDay_ReturnsThatDayAbbrev()
    {
        Assert.Equal("五", DayDisplay.FormatAbbrev(Days.Friday));
    }

    [Fact]
    public void FormatAbbrev_MultipleDays_ReturnsInWeekOrder()
    {
        var days = Days.Friday | Days.Monday | Days.Sunday;
        Assert.Equal("一 五 日", DayDisplay.FormatAbbrev(days));
    }

    [Fact]
    public void FlagsAndFullNames_HaveSevenEntriesInMondayToSundayOrder()
    {
        Assert.Equal(7, DayDisplay.Flags.Length);
        Assert.Equal(7, DayDisplay.FullNames.Length);
        Assert.Equal(Days.Monday, DayDisplay.Flags[0]);
        Assert.Equal(Days.Sunday, DayDisplay.Flags[6]);
        Assert.Equal("周一", DayDisplay.FullNames[0]);
        Assert.Equal("周日", DayDisplay.FullNames[6]);
    }
}
```

- [ ] **步骤 4：运行测试，确认因为 DayDisplay 不存在而编译失败**

```bash
cd "d:/a_my_program/clock-window" && "/c/Program Files/dotnet/dotnet.exe" test
```
预期：编译错误，提示找不到类型/命名空间 `ClockWindow.UI.DayDisplay`。

- [ ] **步骤 5：创建 DayDisplay.cs 让测试通过**

```csharp
using ClockWindow.Models;

namespace ClockWindow.UI;

internal static class DayDisplay
{
    public static readonly string[] Abbrev = { "一", "二", "三", "四", "五", "六", "日" };
    public static readonly string[] FullNames = { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
    public static readonly Days[] Flags =
    {
        Days.Monday, Days.Tuesday, Days.Wednesday, Days.Thursday,
        Days.Friday, Days.Saturday, Days.Sunday,
    };

    public static string FormatAbbrev(Days days)
    {
        var parts = new List<string>();
        for (int i = 0; i < Flags.Length; i++)
        {
            if ((days & Flags[i]) != 0) parts.Add(Abbrev[i]);
        }
        return parts.Count == 0 ? "-" : string.Join(" ", parts);
    }
}
```

- [ ] **步骤 6：运行测试确认全部通过**

```bash
cd "d:/a_my_program/clock-window" && "/c/Program Files/dotnet/dotnet.exe" test
```
预期：4 个测试全部 PASS。

- [ ] **步骤 7：保存确认**（非 git 仓库，跳过 commit）

---

### 任务 4：ImageCoverFit（图片裁剪矩形计算，纯逻辑，TDD）

**文件：**
- 创建：`ClockWindow/UI/ImageCoverFit.cs`
- 测试：`ClockWindow.Tests/ImageCoverFitTests.cs`

- [ ] **步骤 1：先写失败的测试**

创建 `ClockWindow.Tests/ImageCoverFitTests.cs`：

```csharp
using System.Drawing;
using ClockWindow.UI;
using Xunit;

namespace ClockWindow.Tests;

public class ImageCoverFitTests
{
    [Fact]
    public void PortraitImage_WideTarget_CropsTopAndBottom_Centered()
    {
        var rect = ImageCoverFit.GetCoverSourceRect(new Size(100, 200), new Size(100, 50));

        Assert.Equal(0, rect.X);
        Assert.Equal(100, rect.Width);
        Assert.Equal(50, rect.Height);
        Assert.Equal(75, rect.Y);
    }

    [Fact]
    public void PortraitImage_WideTarget_FocusYZero_CropsFromTop()
    {
        var rect = ImageCoverFit.GetCoverSourceRect(new Size(100, 200), new Size(100, 50), focusY: 0f);

        Assert.Equal(0, rect.Y);
        Assert.Equal(50, rect.Height);
    }

    [Fact]
    public void LandscapeImage_TallTarget_CropsLeftAndRight_Centered()
    {
        var rect = ImageCoverFit.GetCoverSourceRect(new Size(200, 100), new Size(50, 100));

        Assert.Equal(0, rect.Y);
        Assert.Equal(100, rect.Height);
        Assert.Equal(50, rect.Width);
        Assert.Equal(75, rect.X);
    }

    [Fact]
    public void SameAspectRatio_ReturnsFullImage()
    {
        var rect = ImageCoverFit.GetCoverSourceRect(new Size(400, 224), new Size(200, 112));

        Assert.Equal(new Rectangle(0, 0, 400, 224), rect);
    }

    [Fact]
    public void RealBackgroundImageSize_400x224Target_CropsHorizontalBandFromPortraitSource()
    {
        var rect = ImageCoverFit.GetCoverSourceRect(new Size(315, 560), new Size(400, 224), focusY: 0.3f);

        Assert.Equal(315, rect.Width);
        Assert.Equal(176, rect.Height);
        Assert.InRange(rect.Y, 0, 560 - 176);
    }
}
```

- [ ] **步骤 2：运行测试确认因为类不存在而失败**

```bash
cd "d:/a_my_program/clock-window" && "/c/Program Files/dotnet/dotnet.exe" test --filter ImageCoverFitTests
```
预期：编译错误，找不到 `ClockWindow.UI.ImageCoverFit`。

- [ ] **步骤 3：实现 ImageCoverFit.cs**

```csharp
using System.Drawing;

namespace ClockWindow.UI;

internal static class ImageCoverFit
{
    public static Rectangle GetCoverSourceRect(Size imageSize, Size targetSize, float focusX = 0.5f, float focusY = 0.5f)
    {
        if (imageSize.Width <= 0 || imageSize.Height <= 0 || targetSize.Width <= 0 || targetSize.Height <= 0)
            return new Rectangle(Point.Empty, imageSize);

        double imageRatio = (double)imageSize.Width / imageSize.Height;
        double targetRatio = (double)targetSize.Width / targetSize.Height;

        if (imageRatio > targetRatio)
        {
            int cropWidth = (int)Math.Round(imageSize.Height * targetRatio);
            int maxX = imageSize.Width - cropWidth;
            int x = Clamp((int)Math.Round(maxX * focusX), 0, maxX);
            return new Rectangle(x, 0, cropWidth, imageSize.Height);
        }
        else
        {
            int cropHeight = (int)Math.Round(imageSize.Width / targetRatio);
            int maxY = imageSize.Height - cropHeight;
            int y = Clamp((int)Math.Round(maxY * focusY), 0, maxY);
            return new Rectangle(0, y, imageSize.Width, cropHeight);
        }
    }

    private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
}
```

- [ ] **步骤 4：运行测试确认全部通过**

```bash
cd "d:/a_my_program/clock-window" && "/c/Program Files/dotnet/dotnet.exe" test --filter ImageCoverFitTests
```
预期：5 个测试全部 PASS。

- [ ] **步骤 5：保存确认**（非 git 仓库，跳过 commit）

---

### 任务 5：GifFrameDelayReader（GIF 帧延迟解析，纯逻辑，TDD）

**文件：**
- 创建：`ClockWindow/UI/GifFrameDelayReader.cs`
- 测试：`ClockWindow.Tests/GifFrameDelayReaderTests.cs`

- [ ] **步骤 1：先写失败的测试**

创建 `ClockWindow.Tests/GifFrameDelayReaderTests.cs`：

```csharp
using System.Drawing;
using ClockWindow.UI;
using Xunit;

namespace ClockWindow.Tests;

public class GifFrameDelayReaderTests
{
    [Fact]
    public void SingleFrameImage_ReturnsSingleZeroDelay()
    {
        using var bitmap = new Bitmap(4, 4);

        var delays = GifFrameDelayReader.ReadFrameDelaysMs(bitmap);

        Assert.Single(delays);
        Assert.Equal(0, delays[0]);
    }

    [Fact]
    public void RealAnimatedGif_ReturnsOneDelayPerFrame()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestAssets", "line-art-dance.gif");
        using var gif = Image.FromFile(path);

        var delays = GifFrameDelayReader.ReadFrameDelaysMs(gif);
        var expectedFrameCount = gif.GetFrameCount(System.Drawing.Imaging.FrameDimension.Time);

        Assert.Equal(expectedFrameCount, delays.Length);
    }

    [Fact]
    public void RealAnimatedGif_AllDelaysAreAtLeastMinimum()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestAssets", "line-art-dance.gif");
        using var gif = Image.FromFile(path);

        var delays = GifFrameDelayReader.ReadFrameDelaysMs(gif);

        Assert.All(delays, d => Assert.True(d >= 20, $"delay {d} 小于最小值 20ms"));
    }
}
```

- [ ] **步骤 2：运行测试确认因为类不存在而失败**

```bash
cd "d:/a_my_program/clock-window" && "/c/Program Files/dotnet/dotnet.exe" test --filter GifFrameDelayReaderTests
```
预期：编译错误，找不到 `ClockWindow.UI.GifFrameDelayReader`。

- [ ] **步骤 3：实现 GifFrameDelayReader.cs**

```csharp
using System.Drawing;
using System.Drawing.Imaging;

namespace ClockWindow.UI;

internal static class GifFrameDelayReader
{
    private const int FrameDelayPropertyId = 0x5100;
    private const int MinDelayMs = 20;

    public static int[] ReadFrameDelaysMs(Image image)
    {
        var frameCount = image.GetFrameCount(FrameDimension.Time);
        if (frameCount <= 1) return new[] { 0 };

        if (!image.PropertyIdList.Contains(FrameDelayPropertyId))
            return Enumerable.Repeat(100, frameCount).ToArray();

        var raw = image.GetPropertyItem(FrameDelayPropertyId).Value!;
        var delays = new int[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            var hundredths = BitConverter.ToInt32(raw, i * 4);
            delays[i] = Math.Max(MinDelayMs, hundredths * 10);
        }
        return delays;
    }
}
```

- [ ] **步骤 4：运行测试确认全部通过**

```bash
cd "d:/a_my_program/clock-window" && "/c/Program Files/dotnet/dotnet.exe" test --filter GifFrameDelayReaderTests
```
预期：3 个测试全部 PASS（第二个测试里 `line-art-dance.gif` 的真实帧数通常在几十帧左右，只要和 `GetFrameCount` 返回值相等即可，不用关心具体数字）。

- [ ] **步骤 5：跑一次全量测试确认没有破坏前面任务的测试**

```bash
cd "d:/a_my_program/clock-window" && "/c/Program Files/dotnet/dotnet.exe" test
```
预期：全部测试（DayDisplay 4 个 + ImageCoverFit 5 个 + GifFrameDelayReader 3 个 = 12 个）PASS。

- [ ] **步骤 6：保存确认**（非 git 仓库，跳过 commit）

---

### 任务 6：ButtonStyler —— 自绘扁平圆角按钮

**文件：**
- 创建：`ClockWindow/UI/ButtonStyler.cs`

这是纯 UI 样式副作用代码（把已有 `Button` 变成圆角），没有能有效断言的输出，不写单元测试，靠任务 12/13 里实际用到它之后跑程序肉眼验证。

- [ ] **步骤 1：创建 ButtonStyler.cs**

```csharp
using System.Drawing;
using System.Windows.Forms;

namespace ClockWindow.UI;

internal static class ButtonStyler
{
    public static void ApplyFlatRounded(Button button, Color backColor, Color hoverColor, Color foreColor, int radius = 8)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = hoverColor;
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.Cursor = Cursors.Hand;
        button.Font = new Font("Microsoft YaHei", 9.5f);

        void ApplyRegion(object? sender, EventArgs e)
        {
            button.Region?.Dispose();
            if (button.Width > 0 && button.Height > 0)
            {
                using var path = RoundedPathFactory.Create(button.Width, button.Height, radius);
                button.Region = new Region(path);
            }
        }

        button.Resize += ApplyRegion;
        ApplyRegion(button, EventArgs.Empty);
    }
}
```

- [ ] **步骤 2：确认整个解决方案仍然能构建**

```bash
cd "d:/a_my_program/clock-window" && "/c/Program Files/dotnet/dotnet.exe" build
```
预期：0 个错误（此时还没有窗体用到 `ButtonStyler`，纯粹确认新文件本身语法正确）。

- [ ] **步骤 3：保存确认**（非 git 仓库，跳过 commit）

---

### 任务 7：ToggleSwitch —— 自绘开关控件

**文件：**
- 创建：`ClockWindow/UI/ToggleSwitch.cs`

- [ ] **步骤 1：创建 ToggleSwitch.cs**

```csharp
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClockWindow.UI;

internal class ToggleSwitch : Control
{
    private bool _checked;

    public event EventHandler? CheckedChanged;

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                  ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(34, 18);
        Cursor = Cursors.Hand;
        BackColor = Color.Transparent;
        Click += (_, _) => Checked = !Checked;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var trackColor = _checked ? Theme.Accent : Color.FromArgb(209, 213, 219);
        using (var trackPath = RoundedPathFactory.Create(Width, Height, Height))
        using (var trackBrush = new SolidBrush(trackColor))
        {
            e.Graphics.FillPath(trackBrush, trackPath);
        }

        int knobDiameter = Height - 4;
        int knobX = _checked ? Width - knobDiameter - 2 : 2;
        using var knobBrush = new SolidBrush(Color.White);
        e.Graphics.FillEllipse(knobBrush, knobX, 2, knobDiameter, knobDiameter);
    }
}
```

- [ ] **步骤 2：确认整个解决方案仍然能构建**

```bash
cd "d:/a_my_program/clock-window" && "/c/Program Files/dotnet/dotnet.exe" build
```
预期：0 个错误。

- [ ] **步骤 3：保存确认**（非 git 仓库，跳过 commit）

---

### 任务 8：CapsuleToggle —— 自绘胶囊形文字开关

**文件：**
- 创建：`ClockWindow/UI/CapsuleToggle.cs`

- [ ] **步骤 1：创建 CapsuleToggle.cs**

```csharp
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClockWindow.UI;

internal class CapsuleToggle : Control
{
    private bool _checked;

    public event EventHandler? CheckedChanged;

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public CapsuleToggle(string text)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                  ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Text = text;
        Size = new Size(56, 26);
        Font = new Font("Microsoft YaHei", 9f);
        Cursor = Cursors.Hand;
        BackColor = Color.Transparent;
        Click += (_, _) => Checked = !Checked;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var backColor = _checked ? Theme.Accent : Theme.RowBackground;
        var foreColor = _checked ? Color.White : Theme.BodyText;

        using (var path = RoundedPathFactory.Create(Width, Height, Height))
        using (var brush = new SolidBrush(backColor))
        {
            e.Graphics.FillPath(brush, path);
        }

        using var textBrush = new SolidBrush(foreColor);
        var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        e.Graphics.DrawString(Text, Font, textBrush, ClientRectangle, format);
    }
}
```

- [ ] **步骤 2：确认整个解决方案仍然能构建**

```bash
cd "d:/a_my_program/clock-window" && "/c/Program Files/dotnet/dotnet.exe" build
```
预期：0 个错误。

- [ ] **步骤 3：保存确认**（非 git 仓库，跳过 commit）

---

### 任务 9：RoundedTextBox —— 带圆角描边的文本框容器

**文件：**
- 创建：`ClockWindow/UI/RoundedTextBox.cs`

- [ ] **步骤 1：创建 RoundedTextBox.cs**

```csharp
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClockWindow.UI;

internal class RoundedTextBox : Panel
{
    public TextBox InnerTextBox { get; }

    public new string Text
    {
        get => InnerTextBox.Text;
        set => InnerTextBox.Text = value;
    }

    public RoundedTextBox(bool multiline = false)
    {
        BackColor = Color.White;
        Padding = multiline ? new Padding(10, 8, 10, 8) : new Padding(10, 6, 10, 6);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                  ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        InnerTextBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Multiline = multiline,
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei", 9.5f),
            ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
        };
        Controls.Add(InnerTextBox);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedPathFactory.Create(Width - 1, Height - 1, 10);
        using var pen = new Pen(Theme.Border, 1);
        e.Graphics.DrawPath(pen, path);
    }
}
```

（`Panel` 本身背景是白色、宿主窗体背景也会设成白色，所以这里不需要给外层做 Region 裁剪——圆角描边线画出来的视觉效果已经够用，四角一点点方形直角在白底上不会露出来。）

- [ ] **步骤 2：确认整个解决方案仍然能构建**

```bash
cd "d:/a_my_program/clock-window" && "/c/Program Files/dotnet/dotnet.exe" build
```
预期：0 个错误。

- [ ] **步骤 3：保存确认**（非 git 仓库，跳过 commit）

---

### 任务 10：RuleRowControl —— 设置窗口的规则卡片行

**文件：**
- 创建：`ClockWindow/UI/RuleRowControl.cs`

- [ ] **步骤 1：创建 RuleRowControl.cs**

```csharp
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ClockWindow.Models;

namespace ClockWindow.UI;

internal class RuleRowControl : Panel
{
    public ReminderRule Rule { get; }
    public ToggleSwitch EnabledToggle { get; }

    private bool _selected;
    public bool Selected
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            Invalidate();
        }
    }

    public RuleRowControl(ReminderRule rule)
    {
        Rule = rule;
        Size = new Size(0, 56);
        Margin = new Padding(0, 0, 0, 8);
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                  ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        EnabledToggle = new ToggleSwitch
        {
            Checked = rule.Enabled,
            Location = new Point(Math.Max(0, Width - 50), 19),
        };
        Controls.Add(EnabledToggle);

        Resize += (_, _) =>
        {
            EnabledToggle.Location = new Point(Width - EnabledToggle.Width - 16, 19);
            ApplyRegion();
        };
        ApplyRegion();
    }

    private void ApplyRegion()
    {
        Region?.Dispose();
        if (Width > 0 && Height > 0)
        {
            using var path = RoundedPathFactory.Create(Width, Height, 12);
            Region = new Region(path);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(_selected ? Theme.RowBackgroundSelected : Theme.RowBackground);

        using var titleBrush = new SolidBrush(Theme.TitleText);
        using var subtitleBrush = new SolidBrush(Theme.SubtleText);
        using var titleFont = new Font("Microsoft YaHei", 10f, FontStyle.Bold);
        using var subtitleFont = new Font("Microsoft YaHei", 8.5f);

        e.Graphics.DrawString(Rule.Title, titleFont, titleBrush, new PointF(16, 8));
        e.Graphics.DrawString(Subtitle(), subtitleFont, subtitleBrush, new PointF(16, 30));
    }

    private string Subtitle()
    {
        var days = DayDisplay.FormatAbbrev(Rule.DaysOfWeek);
        var time = DateTime.Today.Add(Rule.Time).ToString("HH:mm");
        return $"{days} · {time} · {Rule.Messages.Count} 条文案";
    }
}
```

- [ ] **步骤 2：确认整个解决方案仍然能构建**

```bash
cd "d:/a_my_program/clock-window" && "/c/Program Files/dotnet/dotnet.exe" build
```
预期：0 个错误。

- [ ] **步骤 3：保存确认**（非 git 仓库，跳过 commit）

---

### 任务 11：重写 ReminderPopupForm —— 线稿动图背景 + 磨砂文字条

**文件：**
- 修改：`ClockWindow/UI/ReminderPopupForm.cs`（整体重写）

- [ ] **步骤 1：用下面的内容整体替换 `ClockWindow/UI/ReminderPopupForm.cs`**

```csharp
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

namespace ClockWindow.UI;

public class ReminderPopupForm : Form
{
    private const string BackgroundResourceName = "ClockWindow.Resources.line-art-dance.gif";
    // 线稿人物主体大致偏图片上方，居中裁剪会切到下半身空白区域，所以往上偏一点
    private const float BackgroundFocusY = 0.3f;
    private const int PanelTop = 112;

    private static readonly Lazy<byte[]> BackgroundGifBytes = new(LoadBackgroundGifBytes);
    private static readonly List<ReminderPopupForm> OpenPopups = new();

    private static readonly Color ColorTitle = Color.FromArgb(45, 49, 56);
    private static readonly Color ColorBody = Color.FromArgb(75, 79, 88);
    private static readonly Color ColorAccent = Color.FromArgb(108, 143, 214);
    private static readonly Color ColorAccentHover = Color.FromArgb(130, 163, 228);
    private static readonly Color ColorCloseBg = Color.FromArgb(60, 64, 72);

    private readonly string _title;
    private readonly string _message;
    private readonly Image _backgroundImage;
    private readonly int[] _frameDelaysMs;
    private int _frameIndex;
    private readonly System.Windows.Forms.Timer? _frameTimer;
    private readonly System.Windows.Forms.Timer _autoCloseTimer;
    private GraphicsPath? _formPath;
    private GraphicsPath? _buttonPath;

    public ReminderPopupForm(string title, string message)
    {
        _title = title;
        _message = message;

        FormBorderStyle = FormBorderStyle.None;
        ClientSize = new Size(400, 224);
        TopMost = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        DoubleBuffered = true;

        _formPath = RoundedPathFactory.Create(ClientSize.Width, ClientSize.Height, 24);
        Region = new Region(_formPath);

        _backgroundImage = Image.FromStream(new MemoryStream(BackgroundGifBytes.Value));
        _frameDelaysMs = GifFrameDelayReader.ReadFrameDelaysMs(_backgroundImage);
        if (_frameDelaysMs.Length > 1)
        {
            _frameTimer = new System.Windows.Forms.Timer { Interval = _frameDelaysMs[0] };
            _frameTimer.Tick += OnFrameTick;
            _frameTimer.Start();
        }

        Paint += DrawContent;

        var close = new Label
        {
            Text = "✕",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.White,
            BackColor = ColorCloseBg,
            Size = new Size(26, 26),
            Location = new Point(358, 14),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
        };
        using (var closeRegionPath = RoundedPathFactory.Create(close.Width, close.Height, close.Height))
        {
            close.Region = new Region(closeRegionPath);
        }
        close.Click += (_, _) => Close();
        Controls.Add(close);

        var button = new Button
        {
            Text = "知道了",
            Font = new Font("Microsoft YaHei", 9.5f),
            Size = new Size(96, 34),
            Location = new Point(288, 176),
            BackColor = ColorAccent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = ColorAccentHover;
        _buttonPath = RoundedPathFactory.Create(button.Width, button.Height, 16);
        button.Region = new Region(_buttonPath);
        button.Click += (_, _) => Close();
        Controls.Add(button);

        _autoCloseTimer = new System.Windows.Forms.Timer { Interval = 300_000 };
        _autoCloseTimer.Tick += (_, _) => { _autoCloseTimer.Stop(); Close(); };
        _autoCloseTimer.Start();

        Shown += (_, _) => Activate();
        FormClosed += OnFormClosed;
    }

    private void OnFrameTick(object? sender, EventArgs e)
    {
        _frameIndex = (_frameIndex + 1) % _frameDelaysMs.Length;
        _backgroundImage.SelectActiveFrame(FrameDimension.Time, _frameIndex);
        _frameTimer!.Interval = _frameDelaysMs[_frameIndex];
        Invalidate();
    }

    private void DrawContent(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var sourceRect = ImageCoverFit.GetCoverSourceRect(_backgroundImage.Size, ClientSize, focusY: BackgroundFocusY);
        var destRect = new Rectangle(Point.Empty, ClientSize);
        e.Graphics.DrawImage(_backgroundImage, destRect, sourceRect, GraphicsUnit.Pixel);

        var panelRect = new Rectangle(0, PanelTop, ClientSize.Width, ClientSize.Height - PanelTop);
        using (var panelBrush = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
        {
            e.Graphics.FillRectangle(panelBrush, panelRect);
        }

        using var titleFont = new Font("Microsoft YaHei", 13f, FontStyle.Bold);
        using var bodyFont = new Font("Microsoft YaHei", 9.5f);
        using var titleBrush = new SolidBrush(ColorTitle);
        using var bodyBrush = new SolidBrush(ColorBody);

        e.Graphics.DrawString(_title, titleFont, titleBrush, new PointF(24, PanelTop + 14));
        e.Graphics.DrawString(_message, bodyFont, bodyBrush, new RectangleF(25, PanelTop + 48, 250, 44));
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Location = NextPosition();
        OpenPopups.Add(this);
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        OpenPopups.Remove(this);
        _autoCloseTimer.Stop();
        _autoCloseTimer.Dispose();
        _frameTimer?.Stop();
        _frameTimer?.Dispose();
        _backgroundImage.Dispose();
        _formPath?.Dispose();
        _buttonPath?.Dispose();
    }

    private Point NextPosition()
    {
        var area = Screen.PrimaryScreen!.WorkingArea;
        var stackOffset = OpenPopups.Count * (Height + 12);
        return new Point(area.Right - Width - 24, area.Top + 24 + stackOffset);
    }

    private static byte[] LoadBackgroundGifBytes()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(BackgroundResourceName)
            ?? throw new InvalidOperationException($"未找到嵌入资源 {BackgroundResourceName}，请检查 csproj 的 EmbeddedResource 配置");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
```

- [ ] **步骤 2：确认整个解决方案仍然能构建**

```bash
cd "d:/a_my_program/clock-window" && "/c/Program Files/dotnet/dotnet.exe" build
```
预期：0 个错误。

- [ ] **步骤 3：手动运行验证**

在 `Program.cs` 里找一种方式临时验证（比如项目原本的托盘应用启动流程），或者直接写一个一次性的 launcher 来 `new ReminderPopupForm("周五祝福", "周末快乐，好好休息一下～").ShowDialog();` 手动跑一下（跑完记得改回去，不要把测试代码留在 `Program.cs` 里）。用 `dotnet run` 启动，检查：
- 背景动图在卡片里铺满且循环播放
- 底部磨砂文字条上标题和正文清晰可读，没有被图片盖住
- 关闭按钮和"知道了"按钮位置正确、可点击
- 人物主体没有被裁剪矩形切得很奇怪（如果不好看，回到任务 4 调整 `BackgroundFocusY` 常量，不用改逻辑代码）

预期：弹窗在屏幕右上角出现，动图播放流畅，文字清晰。

- [ ] **步骤 4：保存确认**（非 git 仓库，跳过 commit）

---

### 任务 12：重写 SettingsForm —— 自绘卡片列表

**文件：**
- 修改：`ClockWindow/UI/SettingsForm.cs`（整体重写）

- [ ] **步骤 1：用下面的内容整体替换 `ClockWindow/UI/SettingsForm.cs`**

```csharp
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ClockWindow.Models;
using ClockWindow.Services;

namespace ClockWindow.UI;

public class SettingsForm : Form
{
    private readonly RuleStore _store;
    private readonly List<ReminderRule> _rules;
    private readonly FlowLayoutPanel _rowsPanel;
    private RuleRowControl? _selectedRow;

    public SettingsForm(RuleStore store, List<ReminderRule> rules)
    {
        _store = store;
        _rules = rules;

        Text = "ClockWindow - 提醒规则";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(560, 400);
        MinimumSize = new Size(480, 340);
        BackColor = Color.White;
        Font = new Font("Microsoft YaHei", 9f);

        var titleLabel = new Label
        {
            Text = "提醒规则",
            Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold),
            ForeColor = Theme.TitleText,
            Location = new Point(20, 16),
            AutoSize = true,
        };
        Controls.Add(titleLabel);

        var divider = new Panel
        {
            BackColor = Theme.Border,
            Location = new Point(20, 46),
            Size = new Size(60, 2),
        };
        Controls.Add(divider);

        Paint += DrawHeaderIcon;

        _rowsPanel = new FlowLayoutPanel
        {
            Location = new Point(20, 60),
            Size = new Size(520, 280),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.White,
        };
        _rowsPanel.Resize += (_, _) =>
        {
            foreach (Control c in _rowsPanel.Controls) c.Width = _rowsPanel.ClientSize.Width - 4;
        };
        Controls.Add(_rowsPanel);

        var addButton = NewButton("新增", Theme.Accent, Theme.AccentHover, Color.White);
        var editButton = NewButton("编辑", Theme.SecondaryButtonBackground, Theme.SecondaryButtonHover, Theme.SecondaryButtonText);
        var deleteButton = NewButton("删除", Theme.SecondaryButtonBackground, Theme.SecondaryButtonHover, Theme.SecondaryButtonText);
        var testButton = NewButton("立即测试", Theme.SecondaryButtonBackground, Theme.SecondaryButtonHover, Theme.SecondaryButtonText);
        var closeButton = NewButton("关闭", Theme.SecondaryButtonBackground, Theme.SecondaryButtonHover, Theme.SecondaryButtonText);

        addButton.Location = new Point(20, 356);
        editButton.Location = new Point(100, 356);
        deleteButton.Location = new Point(180, 356);
        testButton.Location = new Point(260, 356);
        closeButton.Location = new Point(468, 356);
        addButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        editButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        deleteButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        testButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

        addButton.Click += OnAdd;
        editButton.Click += (_, _) => EditSelected();
        deleteButton.Click += OnDelete;
        testButton.Click += OnTest;
        closeButton.Click += (_, _) => Close();

        Controls.Add(addButton);
        Controls.Add(editButton);
        Controls.Add(deleteButton);
        Controls.Add(testButton);
        Controls.Add(closeButton);

        RefreshList();
    }

    private static Button NewButton(string text, Color back, Color hover, Color fore)
    {
        var button = new Button { Text = text, Size = new Size(72, 30) };
        ButtonStyler.ApplyFlatRounded(button, back, hover, fore);
        return button;
    }

    private void DrawHeaderIcon(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Color.FromArgb(150, Theme.TitleText), 1.5f);
        var rect = new Rectangle(ClientSize.Width - 46, 12, 26, 26);
        e.Graphics.DrawEllipse(pen, rect);
        var center = new Point(rect.X + 13, rect.Y + 13);
        e.Graphics.DrawLine(pen, center, new Point(center.X, center.Y - 8));
        e.Graphics.DrawLine(pen, center, new Point(center.X + 6, center.Y + 4));
    }

    private void RefreshList()
    {
        _rowsPanel.Controls.Clear();
        _selectedRow = null;
        foreach (var rule in _rules)
        {
            var row = new RuleRowControl(rule) { Width = _rowsPanel.ClientSize.Width - 4 };
            row.Click += (_, _) => SelectRow(row);
            row.DoubleClick += (_, _) => { SelectRow(row); EditSelected(); };
            row.EnabledToggle.CheckedChanged += (_, _) =>
            {
                rule.Enabled = row.EnabledToggle.Checked;
                _store.Save(_rules);
            };
            _rowsPanel.Controls.Add(row);
        }
    }

    private void SelectRow(RuleRowControl row)
    {
        if (_selectedRow is { IsDisposed: false }) _selectedRow.Selected = false;
        _selectedRow = row;
        row.Selected = true;
    }

    private void OnAdd(object? sender, EventArgs e)
    {
        var rule = new ReminderRule();
        using var form = new RuleEditForm(rule);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _rules.Add(rule);
            _store.Save(_rules);
            RefreshList();
        }
    }

    private void EditSelected()
    {
        if (_selectedRow == null) return;
        var rule = _selectedRow.Rule;

        using var form = new RuleEditForm(rule);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _store.Save(_rules);
            RefreshList();
        }
    }

    private void OnDelete(object? sender, EventArgs e)
    {
        if (_selectedRow == null) return;
        var rule = _selectedRow.Rule;

        var confirm = MessageBox.Show(this, $"确定删除规则「{rule.Title}」吗？", "确认删除",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        _rules.Remove(rule);
        _store.Save(_rules);
        RefreshList();
    }

    private void OnTest(object? sender, EventArgs e)
    {
        if (_selectedRow == null) return;
        var rule = _selectedRow.Rule;
        if (rule.Messages.Count == 0) return;

        var message = rule.Messages[Random.Shared.Next(rule.Messages.Count)];
        var popup = new ReminderPopupForm(rule.Title, message);
        popup.Show();
    }
}
```

- [ ] **步骤 2：确认整个解决方案仍然能构建**

```bash
cd "d:/a_my_program/clock-window" && "/c/Program Files/dotnet/dotnet.exe" build
```
预期：0 个错误。

- [ ] **步骤 3：手动运行验证**

```bash
cd "d:/a_my_program/clock-window/ClockWindow" && "/c/Program Files/dotnet/dotnet.exe" run
```
托盘图标右键"设置规则..."打开窗口，检查：
- 规则列表以卡片形式展示，标题/副标题/开关都正确
- 点一下开关能切换启用状态，关掉窗口重新打开状态还在（验证确实写回了 `RuleStore`）
- 单击选中一行、双击弹出编辑窗口
- 新增/删除/立即测试按钮功能正常
- 缩放窗口大小，列表和按钮布局不错位

预期：功能和原来的 `ListView` 版本完全等价，只是外观变了。

- [ ] **步骤 4：保存确认**（非 git 仓库，跳过 commit）

---

### 任务 13：重写 RuleEditForm —— 圆角输入框 + 胶囊星期开关

**文件：**
- 修改：`ClockWindow/UI/RuleEditForm.cs`（整体重写）

- [ ] **步骤 1：用下面的内容整体替换 `ClockWindow/UI/RuleEditForm.cs`**

```csharp
using System.Drawing;
using System.Windows.Forms;
using ClockWindow.Models;

namespace ClockWindow.UI;

public class RuleEditForm : Form
{
    private readonly RoundedTextBox _titleBox;
    private readonly CapsuleToggle[] _dayToggles;
    private readonly DateTimePicker _timePicker;
    private readonly RoundedTextBox _messagesBox;
    private readonly ToggleSwitch _enabledToggle;

    public ReminderRule Rule { get; }

    public RuleEditForm(ReminderRule rule)
    {
        Rule = rule;

        Text = "编辑提醒规则";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;
        Font = new Font("Microsoft YaHei", 9f);
        ClientSize = new Size(360, 420);

        var titleLabel = NewLabel("标题", 16, 16);
        _titleBox = new RoundedTextBox { Location = new Point(16, 36), Size = new Size(328, 34), Text = rule.Title };

        var daysLabel = NewLabel("重复星期", 16, 82);
        _dayToggles = new CapsuleToggle[7];
        for (int i = 0; i < 7; i++)
        {
            _dayToggles[i] = new CapsuleToggle(DayDisplay.FullNames[i])
            {
                Location = new Point(16 + (i % 4) * 84, 104 + (i / 4) * 34),
                Size = new Size(76, 28),
                Checked = (rule.DaysOfWeek & DayDisplay.Flags[i]) != 0,
            };
            Controls.Add(_dayToggles[i]);
        }

        var timeLabel = NewLabel("触发时间", 16, 176);
        _timePicker = new DateTimePicker
        {
            Location = new Point(16, 196),
            Width = 120,
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Value = DateTime.Today.Add(rule.Time),
        };

        var messagesLabel = NewLabel("文案库（每行一条，触发时随机选一条）", 16, 232);
        _messagesBox = new RoundedTextBox(multiline: true)
        {
            Location = new Point(16, 252),
            Size = new Size(328, 100),
            Text = string.Join(Environment.NewLine, rule.Messages),
        };

        var enabledLabel = NewLabel("启用此规则", 16, 366);
        _enabledToggle = new ToggleSwitch { Location = new Point(120, 364), Checked = rule.Enabled };

        var okButton = new Button { Text = "保存", Location = new Point(188, 364), Size = new Size(75, 32), DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = "取消", Location = new Point(269, 364), Size = new Size(75, 32), DialogResult = DialogResult.Cancel };
        ButtonStyler.ApplyFlatRounded(okButton, Theme.Accent, Theme.AccentHover, Color.White);
        ButtonStyler.ApplyFlatRounded(cancelButton, Theme.SecondaryButtonBackground, Theme.SecondaryButtonHover, Theme.SecondaryButtonText);
        okButton.Click += OnSave;

        AcceptButton = okButton;
        CancelButton = cancelButton;

        Controls.Add(titleLabel);
        Controls.Add(_titleBox);
        Controls.Add(daysLabel);
        Controls.Add(timeLabel);
        Controls.Add(_timePicker);
        Controls.Add(messagesLabel);
        Controls.Add(_messagesBox);
        Controls.Add(enabledLabel);
        Controls.Add(_enabledToggle);
        Controls.Add(okButton);
        Controls.Add(cancelButton);
    }

    private static Label NewLabel(string text, int x, int y) =>
        new() { Text = text, Location = new Point(x, y), AutoSize = true, ForeColor = Theme.TitleText };

    private void OnSave(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_titleBox.Text))
        {
            MessageBox.Show(this, "标题不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        var messages = _messagesBox.Text
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(m => m.Trim())
            .Where(m => m.Length > 0)
            .ToList();

        if (messages.Count == 0)
        {
            MessageBox.Show(this, "文案库不能为空，至少填写一条", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        var days = Days.None;
        for (int i = 0; i < _dayToggles.Length; i++)
        {
            if (_dayToggles[i].Checked) days |= DayDisplay.Flags[i];
        }

        if (days == Days.None)
        {
            MessageBox.Show(this, "至少选择一个重复星期", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        Rule.Title = _titleBox.Text.Trim();
        Rule.Messages = messages;
        Rule.DaysOfWeek = days;
        Rule.Time = _timePicker.Value.TimeOfDay;
        Rule.Enabled = _enabledToggle.Checked;
    }
}
```

- [ ] **步骤 2：确认整个解决方案仍然能构建**

```bash
cd "d:/a_my_program/clock-window" && "/c/Program Files/dotnet/dotnet.exe" build
```
预期：0 个错误。

- [ ] **步骤 3：手动运行验证**

在设置窗口点"新增"或"编辑"打开这个窗体，检查：
- 标题输入框、文案库多行输入框是圆角描边、能正常输入
- 7 个星期胶囊按钮点击能切换选中状态（选中态深色底白字）
- 保存时的三个校验（标题非空 / 文案非空 / 至少选一天）都还生效
- 保存后设置窗口的卡片列表正确刷新出新内容

预期：功能和原来的 `CheckBox` 版本完全等价，只是外观变了。

- [ ] **步骤 4：保存确认**（非 git 仓库，跳过 commit）

---

### 任务 14：整体验证、发布验证、README 素材来源说明

**文件：**
- 修改：`README.md`

- [ ] **步骤 1：跑一次全量单元测试**

```bash
cd "d:/a_my_program/clock-window" && "/c/Program Files/dotnet/dotnet.exe" test
```
预期：全部 12 个测试 PASS。

- [ ] **步骤 2：跑一次完整 Release 发布，确认单文件发布也能正常打包嵌入资源**

```bash
cd "d:/a_my_program/clock-window/ClockWindow" && "/c/Program Files/dotnet/dotnet.exe" publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ../publish
```
预期：发布成功，`../publish/ClockWindow.exe` 生成。

- [ ] **步骤 3：运行发布出来的单文件 exe，确认背景动图能正常显示**（验证 `EmbeddedResource` 在单文件发布模式下确实被正确打包和读取，这是任务 1 里提到的关键风险点）

```bash
"/d/a_my_program/clock-window/publish/ClockWindow.exe"
```
托盘图标右键"设置规则..." → 选中"周五祝福" → "立即测试"，确认弹窗背景动图正常播放（不是空白或报错）。

- [ ] **步骤 4：在 README 里补一行素材来源说明**

在 `README.md` 的"功能"小节里，把这一行：
```markdown
- 弹窗样式与旧脚本一致：圆角卡片，停靠屏幕右上角，5 分钟无操作自动关闭
```
改成：
```markdown
- 弹窗样式：线稿风动图背景 + 浅色磨砂文字条的圆角卡片，停靠屏幕右上角，5 分钟无操作自动关闭；设置窗口/编辑窗口是配套的自绘浅色卡片风格
```

在文件末尾加一个小节：
```markdown
## 素材来源

提醒弹窗背景动图 `ClockWindow/Resources/line-art-dance.gif` 来自 [Pixabay](https://pixabay.com/gifs/anime-line-art-dancing-4578/)，遵循 [Pixabay License](https://pixabay.com/service/license-summary/)，免费商用无需署名。
```

- [ ] **步骤 5：保存确认**（非 git 仓库，跳过 commit）

---

## 完成标准

- `dotnet test` 全部通过（12 个测试）
- `dotnet build` 和 Release 单文件 `dotnet publish` 都能成功
- 三个窗体（提醒弹窗、设置窗口、编辑规则窗口）手动运行验证外观和交互都符合规格文档里定下的方向，且原有功能（增删改查规则、开关启用、立即测试、5 分钟自动关闭、开机自启逻辑不受影响）没有回归
