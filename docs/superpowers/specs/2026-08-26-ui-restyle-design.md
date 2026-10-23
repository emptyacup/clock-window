# ClockWindow UI 重新设计

日期：2026-08-26
状态：已批准，待实现

## 背景

用户反馈 ClockWindow（C# WinForms 托盘提醒应用）的界面太丑。项目现状：

- `UI/ReminderPopupForm.cs`：提醒弹窗，已有基础样式（白底圆角卡片、蓝色强调色 #6C8FD6）
- `UI/SettingsForm.cs`：设置窗口，纯默认 WinForms 控件（系统 `ListView` + 勾选框 + 灰色系统按钮），是"丑"的主要来源
- `UI/RuleEditForm.cs`：编辑规则弹窗，同样是纯默认控件

项目为单文件发布、无第三方 UI 库依赖（`ClockWindow.csproj` 只引用 `Microsoft.NET.Sdk` + WinForms），本次设计不引入新依赖，全部用原生 WinForms/GDI+ 手绘实现。

通过 brainstorming 技能的可视化伴侣工具（浏览器原型）与用户过了两轮方案：

1. 第一轮：浅色卡片风 / Win11 Fluent 风 / 深色极简风 三个方向，用户全部否决，改要"带动漫背景图，最好是动图"。
2. 第二轮：明确动漫背景**只用在提醒弹窗**，图片来源为"帮我在网上找免费可商用的"。
3. 第三轮：提醒弹窗给出三种背景方案（满屏动图+深色文字条 / 满屏线稿动图+浅色磨砂文字条 / 渐变卡片+角落吉祥物），用户选定 **B：满屏线稿风动图 + 浅色磨砂文字条**。
4. 第四轮：设置窗口/编辑规则窗口给出"纯净卡片风"与"卡片风+线稿装饰"两版，用户选定 **B：卡片风 + 线稿小装饰**（呼应弹窗的线稿气质）。

## 设计决定

### 1. 提醒弹窗 ReminderPopupForm

- 背景：`Resources/line-art-dance.gif`（来源 Pixabay，Pixabay License，免费商用无需署名，原始尺寸 315×560）作为整卡背景动图，铺满 400×224 圆角卡片
  - 用 GDI+ 手动实现类似 CSS `object-fit: cover` 的居中裁剪缩放（源图是竖版，裁剪横向留白部分，保证人物主体居中不变形）
  - GIF 逐帧播放：读取 `PropertyItem`（`PropertyTagFrameDelay`，0x5100）拿到每帧延迟，用 `System.Windows.Forms.Timer` 按帧延迟触发 `SelectActiveFrame` + `Invalidate()` 重绘；弹窗关闭时必须停止并释放这个 Timer（参考现有 `_autoCloseTimer` 的 `Dispose` 模式），避免弹窗关闭后动画线程继续占用 CPU
  - 图片作为嵌入资源随 exe 打包，不在运行时联网加载
- 文字区：保留原有"底部浅色磨砂条"思路——用半透明白到透明的渐变叠加在底部，标题/正文/"知道了"按钮的位置、字体、点击行为不变，只是底色从纯白卡片换成"动图+磨砂条"
- 关闭按钮 ✕：改成小圆形半透明深色底（在任意图案上都能看清），其余交互不变
- 其余行为不变：右上角堆叠、5 分钟自动关闭（`_autoCloseTimer`）、`TopMost`、`FormBorderStyle.None` + `GraphicsPath` 圆角

### 2. 设置窗口 SettingsForm

- 原生 `ListView`（含系统 `CheckBoxes`）整体替换为自绘卡片列表：
  - 一个可滚动容器（`FlowLayoutPanel` 或 `Panel` + 手动布局）承载若干"规则行"控件
  - 每个规则行：圆角浅灰底（#F7F8FA）、标题（加粗）+ 副标题（"周五 · 17:00 · 26 条文案"格式，复用现有 `FormatDays` 逻辑）+ 右侧自绘开关（替代原来的勾选框，语义不变：切换即 `rule.Enabled = ...; _store.Save(_rules)`）
  - 行的单选高亮、双击进入编辑，语义与现有 `_listView.SelectedItems` / `DoubleClick` 逻辑等价迁移
- 底部按钮（新增/编辑/删除/立即测试/关闭）换成自绘扁平圆角按钮，主色用深蓝灰 #4B5563，次要按钮用浅灰底 #F0F1F3
- 标题栏加一个静态线稿小图标（GDI+ 直接画的简笔时钟：一个圆 + 两根指针，呼应弹窗的线稿气质），标题下加一条细分隔线（#2D3138 15% 透明度）
- 交互行为、数据流（`RuleStore.Save`）不变

### 3. 编辑规则窗口 RuleEditForm

- 沿用设置窗口同一套配色和圆角风格：
  - 文本框外包一层自绘圆角边框容器（`TextBox.BorderStyle = None`，外层 `Panel` 自绘圆角描边），标题/文案库输入框同款
  - "星期几"的 7 个 `CheckBox` 换成小号自绘胶囊按钮（选中态深蓝灰底白字，未选中态浅灰底）
  - 保存/取消按钮换成同款自绘扁平按钮
- 字段结构、校验逻辑（标题非空、至少一条文案、至少选一天）不变

### 4. 资源与归属

- `line-art-dance.gif` 放入 `ClockWindow/Resources/`，设置为嵌入资源（`EmbeddedResource` 或 `Content` + `CopyToOutputDirectory`，具体按 .NET 单文件发布的兼容方式选择）
- README 补充一行素材来源说明（Pixabay License + 原始链接），非强制但保持规范

## 范围外（本次不做）

- 不支持每条规则单独配置不同的背景图（所有弹窗共用同一张线稿动图）
- 不改动托盘右键菜单、`RuleStore`/`Scheduler`/`AutoStartHelper` 等业务逻辑层代码
- 不引入任何第三方 UI 库或 NuGet 依赖

## 风险

- 自绘控件（开关、卡片行、圆角输入框、GIF 逐帧动画）比单纯换配色的实现量大，涉及多处 GDI+ 手绘代码
- GIF 逐帧重绘需要注意 CPU 占用，弹窗关闭必须正确释放 Timer 和 Image 资源，避免内存/句柄泄漏（多个弹窗可能同时叠加显示，见 `OpenPopups` 静态列表）
- 源图是竖版 315×560，裁剪成 400×224 横版展示后人物主体是否始终居中好看，需要实现后目测微调裁剪参数
