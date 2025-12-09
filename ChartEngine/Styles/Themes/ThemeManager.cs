using System;
using System.Collections.Generic;

namespace ChartEngine.Styles.Themes
{
    public class ThemeManager
    {
        private static ThemeManager _instance;
        private readonly Dictionary<string, IChartTheme> _themes;
        private IChartTheme _currentTheme;

        public static ThemeManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ThemeManager();
                return _instance;
            }
        }

        public IChartTheme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _currentTheme = value;
                OnThemeChanged?.Invoke(this, new ThemeChangedEventArgs(_currentTheme));
            }
        }

        public event EventHandler<ThemeChangedEventArgs> OnThemeChanged;

        private ThemeManager()
        {
            _themes = new Dictionary<string, IChartTheme>();

            RegisterTheme(new LightTheme());
            RegisterTheme(new DarkTheme());

            _currentTheme = _themes["Light"];
        }

        public void RegisterTheme(IChartTheme theme)
        {
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));

            _themes[theme.Name] = theme;
        }

        public IChartTheme GetTheme(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (_themes.TryGetValue(name, out var theme))
                return theme;

            throw new ArgumentException($"主题 '{name}' 未找到");
        }

        public void SwitchTheme(string themeName)
        {
            CurrentTheme = GetTheme(themeName);
        }

        public IEnumerable<string> GetAllThemeNames()
        {
            return _themes.Keys;
        }

        public void SwitchToLight()
        {
            SwitchTheme("Light");
        }

        public void SwitchToDark()
        {
            SwitchTheme("Dark");
        }

        public void ApplyTheme(Controls.ChartControls.ChartControl chart, IChartTheme theme)
        {
            if (chart == null)
                throw new ArgumentNullException(nameof(chart));
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));

            chart.CandleStyle.UpColor = theme.CandleStyle.UpColor;
            chart.CandleStyle.DownColor = theme.CandleStyle.DownColor;
            chart.CandleStyle.WickColor = theme.CandleStyle.WickColor;
            chart.CandleStyle.BodyBorderColor = theme.CandleStyle.BodyBorderColor;

            chart.VolumeStyle.UpColor = theme.VolumeStyle.UpColor;
            chart.VolumeStyle.DownColor = theme.VolumeStyle.DownColor;
            chart.VolumeStyle.BorderColor = theme.VolumeStyle.BorderColor;

            chart.Invalidate();
        }

        public void ApplyCurrentTheme(Controls.ChartControls.ChartControl chart)
        {
            ApplyTheme(chart, CurrentTheme);
        }
    }

    public class ThemeChangedEventArgs : EventArgs
    {
        public IChartTheme NewTheme { get; }

        public ThemeChangedEventArgs(IChartTheme newTheme)
        {
            NewTheme = newTheme;
        }
    }
}
