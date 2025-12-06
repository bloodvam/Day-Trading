namespace ChartEngine.Transforms
{
    /// <summary>
    /// 横向可视区（按 bar index）
    /// </summary>
    public class VisibleRange
    {
        public int StartIndex { get; private set; }
        public int EndIndex { get; private set; }

        /// <summary>可见 K 线数量</summary>
        public int Count => EndIndex >= StartIndex
            ? EndIndex - StartIndex + 1
            : 0;

        public VisibleRange(int startIndex = 0, int endIndex = -1)
        {
            Set(startIndex, endIndex);
        }

        public void Set(int startIndex, int endIndex)
        {
            if (endIndex < startIndex)
                endIndex = startIndex;

            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }
}
