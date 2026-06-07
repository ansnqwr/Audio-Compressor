using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AudioCompressor.Helpers
{
    public class RoundedButton : Button
    {
        public int CornerRadius { get; set; } = 10;
        public Color HoverColor { get; set; } = Color.FromArgb(50, 120, 200);
        private Color _originalBackColor;

        public RoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.FromArgb(60, 60, 80);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _originalBackColor = BackColor;
            Height = 35;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width, Height);
            var path = GetRoundedRectangle(rect, CornerRadius);
            this.Region = new Region(path);
            using (var brush = new SolidBrush(BackColor))
                e.Graphics.FillPath(brush, path);
            TextRenderer.DrawText(e.Graphics, Text, Font, rect, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        protected override void OnMouseEnter(System.EventArgs e)
        {
            BackColor = HoverColor;
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(System.EventArgs e)
        {
            BackColor = _originalBackColor;
            base.OnMouseLeave(e);
        }

        private GraphicsPath GetRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}