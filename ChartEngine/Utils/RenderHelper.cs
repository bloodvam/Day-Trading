using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ChartEngine.Utils
{
    /// <summary>
    /// 渲染辅助工具类
    /// 提供常用的绘制辅助方法，避免重复代码
    /// </summary>
    public static class RenderHelper
    {
        /// <summary>
        /// 绘制虚线
        /// </summary>
        public static void DrawDashedLine(Graphics g, Pen pen, float x1, float y1, float x2, float y2)
        {
            var oldDashStyle = pen.DashStyle;
            pen.DashStyle = DashStyle.Dash;
            g.DrawLine(pen, x1, y1, x2, y2);
            pen.DashStyle = oldDashStyle;
        }

        /// <summary>
        /// 绘制虚线（PointF 版本）
        /// </summary>
        public static void DrawDashedLine(Graphics g, Pen pen, PointF p1, PointF p2)
        {
            DrawDashedLine(g, pen, p1.X, p1.Y, p2.X, p2.Y);
        }

        /// <summary>
        /// 绘制点线
        /// </summary>
        public static void DrawDottedLine(Graphics g, Pen pen, float x1, float y1, float x2, float y2)
        {
            var oldDashStyle = pen.DashStyle;
            pen.DashStyle = DashStyle.Dot;
            g.DrawLine(pen, x1, y1, x2, y2);
            pen.DashStyle = oldDashStyle;
        }

        /// <summary>
        /// 绘制带阴影的文本
        /// </summary>
        public static void DrawTextWithShadow(
            Graphics g,
            string text,
            Font font,
            Brush brush,
            float x,
            float y,
            int shadowOffset = 1)
        {
            // 绘制阴影
            using (var shadowBrush = new SolidBrush(Color.FromArgb(100, Color.Black)))
            {
                g.DrawString(text, font, shadowBrush, x + shadowOffset, y + shadowOffset);
            }
            // 绘制文本
            g.DrawString(text, font, brush, x, y);
        }

        /// <summary>
        /// 绘制带背景的文本
        /// </summary>
        public static void DrawTextWithBackground(
            Graphics g,
            string text,
            Font font,
            Brush textBrush,
            Brush backgroundBrush,
            float x,
            float y,
            int padding = 2)
        {
            // 测量文本尺寸
            var size = g.MeasureString(text, font);

            // 绘制背景
            g.FillRectangle(
                backgroundBrush,
                x - padding,
                y - padding,
                size.Width + padding * 2,
                size.Height + padding * 2
            );

            // 绘制文本
            g.DrawString(text, font, textBrush, x, y);
        }

        /// <summary>
        /// 测量文本尺寸
        /// </summary>
        public static SizeF MeasureString(Graphics g, string text, Font font)
        {
            return g.MeasureString(text, font);
        }

        /// <summary>
        /// 绘制居中文本
        /// </summary>
        public static void DrawCenteredText(
            Graphics g,
            string text,
            Font font,
            Brush brush,
            RectangleF rect)
        {
            var size = g.MeasureString(text, font);
            float x = rect.X + (rect.Width - size.Width) / 2;
            float y = rect.Y + (rect.Height - size.Height) / 2;
            g.DrawString(text, font, brush, x, y);
        }

        /// <summary>
        /// 绘制右对齐文本
        /// </summary>
        public static void DrawRightAlignedText(
            Graphics g,
            string text,
            Font font,
            Brush brush,
            float rightX,
            float y,
            int padding = 5)
        {
            var size = g.MeasureString(text, font);
            float x = rightX - size.Width - padding;
            g.DrawString(text, font, brush, x, y);
        }

        /// <summary>
        /// 绘制圆角矩形
        /// </summary>
        public static void DrawRoundedRectangle(
            Graphics g,
            Pen pen,
            Rectangle rect,
            int radius)
        {
            using (var path = CreateRoundedRectanglePath(rect, radius))
            {
                g.DrawPath(pen, path);
            }
        }

        /// <summary>
        /// 填充圆角矩形
        /// </summary>
        public static void FillRoundedRectangle(
            Graphics g,
            Brush brush,
            Rectangle rect,
            int radius)
        {
            using (var path = CreateRoundedRectanglePath(rect, radius))
            {
                g.FillPath(brush, path);
            }
        }

        /// <summary>
        /// 创建圆角矩形路径
        /// </summary>
        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        /// <summary>
        /// 限制值在指定范围内
        /// </summary>
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// 限制值在指定范围内（double 版本）
        /// </summary>
        public static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// 线性插值
        /// </summary>
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        /// <summary>
        /// 判断两个浮点数是否近似相等
        /// </summary>
        public static bool ApproximatelyEqual(float a, float b, float epsilon = 0.0001f)
        {
            return Math.Abs(a - b) < epsilon;
        }

        /// <summary>
        /// 格式化价格显示
        /// </summary>
        public static string FormatPrice(double price, int decimals = 2)
        {
            return price.ToString($"F{decimals}");
        }

        /// <summary>
        /// 格式化成交量显示（自动转换单位）
        /// </summary>
        public static string FormatVolume(double volume)
        {
            if (volume >= 1_000_000_000)
                return $"{volume / 1_000_000_000:F2}B";
            if (volume >= 1_000_000)
                return $"{volume / 1_000_000:F2}M";
            if (volume >= 1_000)
                return $"{volume / 1_000:F2}K";
            return volume.ToString("F0");
        }

        /// <summary>
        /// 创建渐变画刷（垂直）
        /// </summary>
        public static LinearGradientBrush CreateVerticalGradient(
            Rectangle rect,
            Color topColor,
            Color bottomColor)
        {
            if (rect.Height <= 0)
                rect.Height = 1;

            return new LinearGradientBrush(
                rect,
                topColor,
                bottomColor,
                LinearGradientMode.Vertical
            );
        }

        /// <summary>
        /// 创建渐变画刷（水平）
        /// </summary>
        public static LinearGradientBrush CreateHorizontalGradient(
            Rectangle rect,
            Color leftColor,
            Color rightColor)
        {
            if (rect.Width <= 0)
                rect.Width = 1;

            return new LinearGradientBrush(
                rect,
                leftColor,
                rightColor,
                LinearGradientMode.Horizontal
            );
        }
    }
}