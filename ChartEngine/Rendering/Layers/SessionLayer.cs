// ChartEngine/Rendering/Layers/SessionLayer.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using ChartEngine.Config;
using ChartEngine.Data.Models;
using ChartEngine.Interfaces;
using ChartEngine.Rendering;
using ChartEngine.Styles.Core;
using ChartEngine.Utils;

namespace ChartEngine.Rendering.Layers
{
    /// <summary>
    /// 交易时段图层
    /// 负责绘制盘前/盘中/盘后的背景色，以及日期分隔线
    /// </summary>
    public class SessionLayer : IChartLayer
    {
        public string Name => "Session";
        public bool IsVisible { get; set; } = true;
        public int ZOrder { get; set; } = 0;

        private readonly SessionStyle _style;
        private readonly TradingSessionConfig _config;
        private readonly RenderResourcePool _resourcePool;

        public SessionLayer(
            SessionStyle style,
            TradingSessionConfig config,
            RenderResourcePool pool)
        {
            _style = style ?? SessionStyle.GetDarkThemeDefault();
            _config = config ?? TradingSessionConfig.GetUSStockDefault();
            _resourcePool = pool ?? new RenderResourcePool();
        }

        public void Render(ChartRenderContext ctx)
        {
            if (!IsVisible)
                return;

            if (ctx?.Series?.Bars == null || ctx.Series.Count == 0)
                return;

            // 1. 绘制时段背景色
            RenderSessionBackgrounds(ctx);

            // 2. 绘制日期分隔线（仅日内级别）
            if (IsIntradayTimeFrame(ctx.Series))
            {
                RenderDateSeparators(ctx);
            }
        }

        /// <summary>
        /// 绘制时段背景色
        /// </summary>
        private void RenderSessionBackgrounds(ChartRenderContext ctx)
        {
            var bars = ctx.Series.Bars;
            var range = ctx.VisibleRange;

            var (safeStart, safeEnd) = BoundsChecker.GetSafeIndexRange(
                range.StartIndex, range.EndIndex, bars.Count);

            if (safeStart > safeEnd)
                return;

            // 计算绘制区域（主图+成交量）
            int top = Math.Min(ctx.PriceArea.Top, ctx.VolumeArea.Top);
            int bottom = Math.Max(ctx.PriceArea.Bottom, ctx.VolumeArea.Bottom);
            int height = bottom - top;

            // 计算每根K线的宽度
            float barWidth = range.Count > 0
                ? (float)ctx.PriceArea.Width / range.Count
                : 0;

            if (barWidth <= 0)
                return;

            // 逐根K线绘制背景矩形
            for (int i = safeStart; i <= safeEnd; i++)
            {
                try
                {
                    var bar = bars[i];

                    if (!BoundsChecker.IsValidBar(bar))
                        continue;

                    // 判断时段
                    SessionType session = GetSessionType(bar.Timestamp.TimeOfDay);

                    // 获取颜色
                    Color bgColor = GetSessionColor(session);
                    var brush = _resourcePool.GetBrush(bgColor);

                    // 计算这根K线的矩形区域
                    float xCenter = ctx.Transform.IndexToX(i, ctx.PriceArea);
                    float xLeft = xCenter - barWidth / 2f;

                    // 边界保护
                    xLeft = Math.Max(xLeft, ctx.PriceArea.Left);
                    float width = Math.Min(barWidth, ctx.PriceArea.Right - xLeft);

                    if (width > 0)
                    {
                        ctx.Graphics.FillRectangle(brush, xLeft, top, width, height);
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }

        /// <summary>
        /// 绘制日期分隔线
        /// </summary>
        private void RenderDateSeparators(ChartRenderContext ctx)
        {
            var bars = ctx.Series.Bars;
            var range = ctx.VisibleRange;

            var (safeStart, safeEnd) = BoundsChecker.GetSafeIndexRange(
                range.StartIndex, range.EndIndex, bars.Count);

            if (safeStart >= safeEnd)
                return;

            int top = Math.Min(ctx.PriceArea.Top, ctx.VolumeArea.Top);
            int bottom = Math.Max(ctx.PriceArea.Bottom, ctx.VolumeArea.Bottom);

            var pen = _resourcePool.GetStyledPen(
                _style.DateSeparatorColor,
                _style.DateSeparatorWidth,
                _style.DateSeparatorStyle);

            // 找日期边界
            for (int i = safeStart + 1; i <= safeEnd; i++)
            {
                try
                {
                    if (bars[i].Timestamp.Date != bars[i - 1].Timestamp.Date)
                    {
                        float x = ctx.Transform.IndexToX(i, ctx.PriceArea);

                        if (x >= ctx.PriceArea.Left && x <= ctx.PriceArea.Right)
                        {
                            ctx.Graphics.DrawLine(pen, x, top, x, bottom);
                        }
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }

        /// <summary>
        /// 判断时段类型
        /// </summary>
        private SessionType GetSessionType(TimeSpan time)
        {
            if (time >= _config.PreMarketStart && time < _config.RegularStart)
                return SessionType.PreMarket;

            if (time >= _config.RegularStart && time < _config.RegularEnd)
                return SessionType.Regular;

            if (time >= _config.RegularEnd && time < _config.AfterHoursEnd)
                return SessionType.AfterHours;

            return SessionType.Closed;
        }

        /// <summary>
        /// 获取时段颜色
        /// </summary>
        private Color GetSessionColor(SessionType type)
        {
            return type switch
            {
                SessionType.PreMarket => _style.PreMarketBackColor,
                SessionType.Regular => _style.RegularBackColor,
                SessionType.AfterHours => _style.AfterHoursBackColor,
                SessionType.Closed => _style.ClosedBackColor,
                _ => _style.RegularBackColor
            };
        }

        /// <summary>
        /// 判断是否为日内级别（直接从 Bar 的 TimeFrame 属性获取）
        /// </summary>
        private bool IsIntradayTimeFrame(ISeries series)
        {
            if (series == null || series.Count == 0)
                return true;

            // 🔥 直接从第一根 K线获取 TimeFrame
            var timeFrame = series.Bars[0].TimeFrame;

            // 小于 Day 级别就显示日期分隔线
            return timeFrame < TimeFrame.Day;
        }
    }

    /// <summary>
    /// 交易时段类型
    /// </summary>
    internal enum SessionType
    {
        PreMarket,
        Regular,
        AfterHours,
        Closed
    }
}