using System;
using System.Drawing;

namespace ChartEngine.Transforms
{
    /// <summary>
    /// index ↔ 像素 X 的映射
    /// </summary>
    public class HorizontalScale
    {
        /// <summary>每根 K 线占多少像素（在当前 plotArea 下计算）</summary>
        public float PixelsPerBar { get; private set; }

        public void Update(Rectangle plotArea, VisibleRange range)
        {
            if (range.Count <= 0 || plotArea.Width <= 0)
            {
                PixelsPerBar = 0;
                return;
            }

            PixelsPerBar = (float)plotArea.Width / range.Count;
        }

        public float IndexToX(int index, Rectangle plotArea, VisibleRange range)
        {
            if (range.Count <= 0)
                return plotArea.Left;

            float barWidth = (float)plotArea.Width / range.Count;
            float localIndex = index - range.StartIndex;

            return plotArea.Left + (localIndex + 0.5f) * barWidth;
        }

        public int XToIndex(float x, Rectangle plotArea, VisibleRange range)
        {
            if (range.Count <= 0 || plotArea.Width <= 0)
                return range.StartIndex;

            float barWidth = (float)plotArea.Width / range.Count;
            float localIndex = (x - plotArea.Left) / barWidth - 0.5f;

            int index = range.StartIndex + (int)Math.Floor(localIndex);

            if (index < range.StartIndex) index = range.StartIndex;
            if (index > range.EndIndex) index = range.EndIndex;

            return index;
        }
    }
}
