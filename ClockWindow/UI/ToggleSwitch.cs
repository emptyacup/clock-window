using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClockWindow.UI;

internal class ToggleSwitch : Control
{
    private static readonly Color TrackOff = Color.FromArgb(228, 228, 231);
    private static readonly Color TrackOffHover = Color.FromArgb(212, 212, 216);

    private bool _checked;
    private bool _hover;
    // 0 = 关，1 = 开；切换时用计时器把滑块平滑移过去
    private float _progress;
    private readonly System.Windows.Forms.Timer _animationTimer = new() { Interval = 15 };

    public event EventHandler? CheckedChanged;

    [DefaultValue(false)]
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            if (IsHandleCreated && Visible) _animationTimer.Start();
            else _progress = value ? 1f : 0f;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                  ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                  ControlStyles.SupportsTransparentBackColor, true);
        Size = new Size(36, 20);
        Cursor = Cursors.Hand;
        BackColor = Color.Transparent;
        Click += (_, _) => Checked = !Checked;
        _animationTimer.Tick += OnAnimationTick;
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        float target = _checked ? 1f : 0f;
        _progress += Math.Sign(target - _progress) * 0.2f;
        if (Math.Abs(target - _progress) < 0.2f)
        {
            _progress = target;
            _animationTimer.Stop();
        }
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var trackRect = new RectangleF(0, 0, Width - 1f, Height - 1f);
        using (var trackPath = RoundedPathFactory.Create(trackRect, Height / 2f))
        {
            var offColor = _hover ? TrackOffHover : TrackOff;
            using var trackBrush = new LinearGradientBrush(trackRect,
                Theme.Lerp(offColor, Theme.AccentStart, _progress),
                Theme.Lerp(offColor, Theme.AccentEnd, _progress), 0f);
            g.FillPath(trackBrush, trackPath);
        }

        float knobDiameter = Height - 5f;
        float knobX = 2f + _progress * (Width - 1f - knobDiameter - 4f);
        using (var shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
        {
            g.FillEllipse(shadowBrush, knobX, 2.8f, knobDiameter, knobDiameter);
        }
        using var knobBrush = new SolidBrush(Color.White);
        g.FillEllipse(knobBrush, knobX, 2f, knobDiameter, knobDiameter);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationTimer.Stop();
            _animationTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
