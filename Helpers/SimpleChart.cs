using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AudioCompressor.Helpers
{
    public class SimpleChart : Panel
    {
        private List<double> _data = new List<double>();
        private double _maxValue = 100;
        private string _title = "نسبة الضغط (%)";
        private string _yAxisTitle = "%";

        public SimpleChart()
        {
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(40, 40, 48);
        }

        public void AddDataPoint(double value)
        {
            _data.Add(value);
            if (value > _maxValue) _maxValue = value;
            if (_data.Count > 100) _data.RemoveAt(0);
            Invalidate();
        }

        public void Clear()
        {
            _data.Clear();
            _maxValue = 100;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // خلفية
            g.Clear(BackColor);

            if (_data.Count < 2) return;

            // هامش
            int marginLeft = 50;
            int marginRight = 20;
            int marginTop = 30;
            int marginBottom = 30;
            int chartWidth = Width - marginLeft - marginRight;
            int chartHeight = Height - marginTop - marginBottom;

            // رسم الشبكة
            using (Pen gridPen = new Pen(Color.FromArgb(60, 60, 80), 1))
            {
                for (int i = 0; i <= 4; i++)
                {
                    int y = marginTop + (int)(chartHeight * i / 4.0);
                    g.DrawLine(gridPen, marginLeft, y, marginLeft + chartWidth, y);
                }
            }

            // رسم الخط البياني
            using (Pen linePen = new Pen(Color.LimeGreen, 2))
            {
                PointF[] points = new PointF[_data.Count];
                for (int i = 0; i < _data.Count; i++)
                {
                    float x = marginLeft + (float)(chartWidth * i / (double)(_data.Count - 1));
                    float y = marginTop + (float)(chartHeight * (1 - _data[i] / _maxValue));
                    points[i] = new PointF(x, y);
                }
                g.DrawLines(linePen, points);
            }

            // محاور
            using (Pen axisPen = new Pen(Color.LightGray, 1))
            {
                g.DrawLine(axisPen, marginLeft, marginTop, marginLeft, marginTop + chartHeight); // Y
                g.DrawLine(axisPen, marginLeft, marginTop + chartHeight, marginLeft + chartWidth, marginTop + chartHeight); // X
            }

            // عناوين
            using (Font titleFont = new Font("Segoe UI", 8, FontStyle.Bold))
            using (Brush textBrush = new SolidBrush(Color.White))
            {
                g.DrawString(_title, titleFont, textBrush, marginLeft + 10, 5);
                // قيم Y
                for (int i = 0; i <= 4; i++)
                {
                    int y = marginTop + (int)(chartHeight * i / 4.0);
                    double val = _maxValue * (1 - i / 4.0);
                    g.DrawString($"{val:F0}", titleFont, textBrush, 5, y - 6);
                }
            }
        }
    }
}