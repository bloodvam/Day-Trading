using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using ChartEngine.Interfaces;

namespace ChartEngine.Rendering
{
    /// <summary>
    /// 增量渲染器 - 只渲染脏区域
    /// </summary>
    public class IncrementalRenderer
    {
        private readonly DirtyRegionManager _dirtyRegionManager;
        private Bitmap _backBuffer;
        private Graphics _backBufferGraphics;
        private bool _isBackBufferValid = false;

        public IncrementalRenderer()
        {
            _dirtyRegionManager = new DirtyRegionManager();
        }

        /// <summary>
        /// 获取脏区域管理器
        /// </summary>
        public DirtyRegionManager DirtyRegions => _dirtyRegionManager;

        /// <summary>
        /// 检查后备缓冲区是否有效
        /// </summary>
        public bool IsBackBufferValid => _isBackBufferValid && _backBuffer != null;

        /// <summary>
        /// 初始化后备缓冲区
        /// </summary>
        public void InitializeBackBuffer(int width, int height)
        {
            if (width <= 0 || height <= 0)
                return;

            // 如果尺寸没变，不需要重新创建
            if (_backBuffer != null &&
                _backBuffer.Width == width &&
                _backBuffer.Height == height &&
                _isBackBufferValid)
            {
                return;
            }

            // 释放旧的缓冲区
            DisposeBackBuffer();

            // 创建新的缓冲区
            _backBuffer = new Bitmap(width, height);
            _backBufferGraphics = Graphics.FromImage(_backBuffer);

            // 设置高质量渲染
            _backBufferGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            _backBufferGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            _isBackBufferValid = false; // 新缓冲区需要重绘

            // 标记全屏脏
            _dirtyRegionManager.MarkFullScreen(
                new Rectangle(0, 0, width, height),
                "后备缓冲区重新创建");
        }

        /// <summary>
        /// 渲染到后备缓冲区（增量渲染）
        /// </summary>
        public void RenderToBackBuffer(
            Action<Graphics, Rectangle> renderCallback,
            Rectangle clipRect = default)
        {
            if (_backBuffer == null || _backBufferGraphics == null)
                return;

            if (!_dirtyRegionManager.HasDirtyRegions)
                return; // 没有脏区域，无需渲染

            // 获取优化后的脏区域
            var dirtyBounds = _dirtyRegionManager.GetOptimizedDirtyBounds();

            foreach (var bounds in dirtyBounds)
            {
                // 设置裁剪区域
                _backBufferGraphics.SetClip(bounds);

                // 清除该区域
                _backBufferGraphics.Clear(Color.Transparent);

                // 调用渲染回调
                renderCallback?.Invoke(_backBufferGraphics, bounds);

                // 重置裁剪
                _backBufferGraphics.ResetClip();
            }

            _isBackBufferValid = true;
            _dirtyRegionManager.Clear();
        }

        /// <summary>
        /// 渲染全部到后备缓冲区（全屏渲染）
        /// </summary>
        public void RenderFullToBackBuffer(Action<Graphics> renderCallback)
        {
            if (_backBuffer == null || _backBufferGraphics == null)
                return;

            // 清除整个缓冲区
            _backBufferGraphics.Clear(Color.Transparent);

            // 调用渲染回调
            renderCallback?.Invoke(_backBufferGraphics);

            _isBackBufferValid = true;
            _dirtyRegionManager.Clear();
        }

        /// <summary>
        /// 从后备缓冲区复制到屏幕
        /// </summary>
        public void CopyToScreen(Graphics screenGraphics, Rectangle destRect)
        {
            if (_backBuffer == null || !_isBackBufferValid)
                return;

            screenGraphics.DrawImage(
                _backBuffer,
                destRect,
                0, 0,
                _backBuffer.Width,
                _backBuffer.Height,
                GraphicsUnit.Pixel);
        }

        /// <summary>
        /// 从后备缓冲区复制指定区域到屏幕
        /// </summary>
        public void CopyRegionToScreen(
            Graphics screenGraphics,
            Rectangle sourceRect,
            Rectangle destRect)
        {
            if (_backBuffer == null || !_isBackBufferValid)
                return;

            screenGraphics.DrawImage(
                _backBuffer,
                destRect,
                sourceRect,
                GraphicsUnit.Pixel);
        }

        /// <summary>
        /// 仅复制脏区域到屏幕（进一步优化）
        /// </summary>
        public void CopyDirtyRegionsToScreen(Graphics screenGraphics)
        {
            if (_backBuffer == null || !_isBackBufferValid)
                return;

            var dirtyBounds = _dirtyRegionManager.GetOptimizedDirtyBounds();

            foreach (var bounds in dirtyBounds)
            {
                screenGraphics.DrawImage(
                    _backBuffer,
                    bounds,
                    bounds,
                    GraphicsUnit.Pixel);
            }
        }

        /// <summary>
        /// 使缓冲区无效（需要重绘）
        /// </summary>
        public void InvalidateBackBuffer(string reason = "")
        {
            _isBackBufferValid = false;

            if (_backBuffer != null)
            {
                _dirtyRegionManager.MarkFullScreen(
                    new Rectangle(0, 0, _backBuffer.Width, _backBuffer.Height),
                    reason);
            }
        }

        /// <summary>
        /// 释放后备缓冲区
        /// </summary>
        public void DisposeBackBuffer()
        {
            _backBufferGraphics?.Dispose();
            _backBufferGraphics = null;

            _backBuffer?.Dispose();
            _backBuffer = null;

            _isBackBufferValid = false;
        }

        /// <summary>
        /// 获取渲染统计信息
        /// </summary>
        public string GetStatistics()
        {
            string bufferInfo = _backBuffer != null
                ? $"{_backBuffer.Width}x{_backBuffer.Height}, Valid: {_isBackBufferValid}"
                : "未初始化";

            return $"后备缓冲: {bufferInfo}, {_dirtyRegionManager.GetStatistics()}";
        }

        /// <summary>
        /// 获取后备缓冲区的副本（用于调试）
        /// </summary>
        public Bitmap GetBackBufferCopy()
        {
            if (_backBuffer == null)
                return null;

            return new Bitmap(_backBuffer);
        }
    }
}