using System;
using cAlgo.API;

namespace cAlgo
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None), Cloud("Cyber Cycle", "Trigger")]
    public class EhlersAdaptiveCyberCycle : Indicator
    {
        private IndicatorDataSeries _s, _c, _dp;

        private double _q;

        private double _ip;

        private double _p;

        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("Alpha", DefaultValue = 0.07)]
        public double Alpha { get; set; }

        [Output("Cyber Cycle", LineColor = "Green", PlotType = PlotType.Line)]
        public IndicatorDataSeries CyberCycle { get; set; }

        [Output("Trigger", LineColor = "Red", PlotType = PlotType.Line)]
        public IndicatorDataSeries Trigger { get; set; }

        protected override void Initialize()
        {
            _s = CreateDataSeries();
            _c = CreateDataSeries();
            _dp = CreateDataSeries();
        }

        public override void Calculate(int index)
        {
            _s[index] = (Source[index] + 2 * Source[index - 1] + 2 * Source[index - 2] + Source[index - 3]) / 6;

            _c[index] = index < 7
                ? (Source[index] - 2 * Source[index - 1] + Source[index - 2]) / 4.0
                : ((1 - 0.5 * Alpha) * (1 - 0.5 * Alpha) * (_s[index] - 2 * _s[index - 1] + _s[index - 2]) + 2 * (1 - Alpha) * _c[index - 1] - (1 - Alpha) * (1 - Alpha) * _c[index - 2]);

            var qPrevious = _q;

            _q = (.0962 * _c[index] + 0.5769 * _c[index - 2] - 0.5769 * _c[index - 4] - .0962 * _c[index - 6]) * (0.5 + .08 * (double.IsNaN(_ip) ? 0 : _ip));

            var dp = _q != 0 && qPrevious != 0 ? (_c[index - 3] / _q - _c[index - 4] / qPrevious) / (1 + _c[index - 3] * _c[index - 4] / (_q * qPrevious)) : 0;

            if (dp < 0.1)
            {
                _dp[index] = 0.1;
            }
            else
            {
                _dp[index] = dp > 1.1 ? 1.1 : dp;
            }

            var md = Med(_dp[index], _dp[index - 1], Med(_dp[index - 2], _dp[index - 3], _dp[index - 4]));

            var dc = md == 0 ? 15 : 6.28318 / md + 0.5;

            var ipPrevious = double.IsNaN(_ip) ? 0 : _ip;

            _ip = .33 * dc + .67 * ipPrevious;

            var pPrevious = double.IsNaN(_p) ? 0 : _p;

            _p = .15 * _ip + .85 * pPrevious;

            var a1 = 2.0 / (_p + 1);

            var cyberCycle = (1 - 0.5 * a1) * (1 - 0.5 * Alpha) * (_s[index] - 2 * _s[index - 1] + _s[index - 2]) + 2 * (1 - a1) * CyberCycle[index - 1] - (1 - a1) * (1 - a1) * CyberCycle[index - 2];

            CyberCycle[index] = double.IsNaN(cyberCycle) ? (Source[index] - 2 * Source[index - 1] + Source[index - 2]) / 4.0 : cyberCycle;

            Trigger[index] = CyberCycle[index - 1];
        }

        private double Med(double x, double y, double z)
        {
            return (x + y + z) - Math.Min(x, Math.Min(y, z)) - Math.Max(x, Math.Max(y, z));
        }
    }
}