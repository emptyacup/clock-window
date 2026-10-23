# ClockWindow

托盘常驻的桌面提醒弹窗应用（C# WinForms），替代原来 `friday-notify.ps1` + 任务计划程序的方案。

## 功能

- 系统托盘常驻，右键菜单：设置规则 / 开机自启 / 退出
- 规则 = 标题 + 文案库（随机选一条显示）+ 星期几（多选）+ 时间点
- 默认内置一条"周五祝福"规则（周五 17:00，从旧脚本迁移的 26 句祝福语）
- 界面风格：参考 Linear / Raycast 的现代风格，全部 GDI+ 自绘（无第三方 UI 库，仅用 Svg.NET 渲染装饰素材）——无边框窗口 + 自绘标题栏、中性灰底白卡片、细描边、靛蓝→紫渐变强调色
- 弹窗样式：每次随机生成的 SVG 装饰头图（Fluent Emoji / 手绘涂鸦 / 几何纹样）+ 渐隐过渡到白色文字区的圆角卡片，淡入出现，停靠屏幕右上角，5 分钟无操作自动关闭
- 开机自启开关（默认开启，写注册表 `HKCU\...\Run`）
- 单实例保护（重复启动会提示"已在运行"）

## 运行

已发布的单文件 exe 在 `publish\ClockWindow.exe`，双击即可运行（首次启动会自动开启开机自启）。

## 开发 / 重新构建

```bash
cd ClockWindow
dotnet build                    # 调试构建
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ../publish
```

> 本机的 `dotnet` 未加入系统 PATH（PATH 里是旧的 32 位版本），需要用完整路径 `"C:\Program Files\dotnet\dotnet.exe"`，或自行修正 PATH 顺序。

## 后续清理

新应用已内置迁移后的周五祝福规则，旧的任务计划程序任务 `FridayReminder` 和 `C:\Users\86185\Scripts\friday-notify.ps1` 没有被自动删除。确认新应用运行正常后，可以自己在"任务计划程序"里删除该任务、按需清理旧脚本。

## 素材与第三方组件

提醒弹窗顶部的装饰图每次随机生成（柔和渐变底 + 随机几何点缀 + 一种主体素材，素材用洗牌方式轮换，尽量不重复），实现见 `ClockWindow/UI/PopupDecoration.cs`。内置素材：

- `ClockWindow/Resources/Decorations/emoji/`：精选 70 个 [Microsoft Fluent Emoji](https://github.com/microsoft/fluentui-emoji)（Flat 风格），MIT 协议，许可证文本随程序发布在 `ThirdPartyNotices/FluentEmoji-LICENSE.txt`
- `ClockWindow/Resources/Decorations/doodles/`：32 张 [Open Doodles](https://www.opendoodles.com/)（Pablo Stanley），CC0 协议；SVG 由 [react-open-doodles](https://github.com/lunahq/react-open-doodles)（MIT）的组件源码转换而来，强调色在运行时随配色替换

**自定义素材**：把 `.svg` 文件放进 `%AppData%\ClockWindow\decorations\`，之后的弹窗会随机用到（出现概率比内置素材高）。无法解析的文件会被跳过。

> 没有使用 unDraw：它的许可证禁止批量下载以及把插画打包进应用里分发。

SVG 渲染使用 [Svg.NET](https://github.com/svg-net/SVG)（NuGet 包 `Svg`，MS-PL 协议）。
