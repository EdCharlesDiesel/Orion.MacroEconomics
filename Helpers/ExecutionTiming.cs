namespace Orion.MacroEconomics.Helpers
{
    public static class ExecutionTiming
    {
        // London + NY overlap (UTC)
        private const int OverlapStartHourUtc = 12;
        private const int OverlapEndHourUtc   = 16;

        // Major session windows (UTC)
        private const int TokyoOpenUtc   = 0;
        private const int TokyoCloseUtc  = 8;
        private const int LondonOpenUtc  = 7;
        private const int LondonCloseUtc = 16;
        private const int NyOpenUtc      = 12;
        private const int NyCloseUtc     = 21;

        public static bool IsGoodLiquidityWindow(DateTime utcNow)
        {
            if (IsWeekend(utcNow))
                return false;

            var hour = utcNow.Hour;
            return hour >= OverlapStartHourUtc && hour <= OverlapEndHourUtc;
        }

        public static bool IsWeekend(DateTime utcNow) =>
            utcNow.DayOfWeek == DayOfWeek.Saturday ||
            utcNow.DayOfWeek == DayOfWeek.Sunday;

        public static bool IsLondonSession(DateTime utcNow) =>
            !IsWeekend(utcNow) && utcNow.Hour >= LondonOpenUtc && utcNow.Hour < LondonCloseUtc;

        public static bool IsNewYorkSession(DateTime utcNow) =>
            !IsWeekend(utcNow) && utcNow.Hour >= NyOpenUtc && utcNow.Hour < NyCloseUtc;

        public static bool IsTokyoSession(DateTime utcNow) =>
            !IsWeekend(utcNow) && utcNow.Hour >= TokyoOpenUtc && utcNow.Hour < TokyoCloseUtc;

        public static string DescribeSession(DateTime utcNow)
        {
            if (IsWeekend(utcNow))            return "Closed (Weekend)";
            if (IsGoodLiquidityWindow(utcNow)) return "London/NY Overlap";
            if (IsLondonSession(utcNow))       return "London";
            if (IsNewYorkSession(utcNow))      return "New York";
            if (IsTokyoSession(utcNow))        return "Tokyo";
            return "Off-Session";
        }
    }
}
