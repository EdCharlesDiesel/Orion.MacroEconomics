using Orion.MacroEconomics.Models;

namespace Orion.MacroEconomics.Services;



public class SessionService
{
    public SessionStatus GetSessionStatus()
    {
        var now = DateTime.UtcNow;
        double h = now.Hour + now.Minute / 60.0;
        string time = now.ToString("HH:mm") + " UTC";

        if (h >= 7.0 && h < 9.0)
        {
            return new SessionStatus
            {
                Window = "London Kill Zone",
                Prime = true,
                Color = "#3fb950",
                Icon = "🟢",
                Description = "07:00–09:00 UTC — highest order flow",
                Time = time
            };
        }

        if (h >= 12.0 && h < 14.0)
        {
            return new SessionStatus
            {
                Window = "NY Kill Zone",
                Prime = true,
                Color = "#3fb950",
                Icon = "🟢",
                Description = "12:00–14:00 UTC — NY open sweep",
                Time = time
            };
        }

        if (h >= 15.0 && h < 17.0)
        {
            return new SessionStatus
            {
                Window = "London Close",
                Prime = false,
                Color = "#e3b341",
                Icon = "🟡",
                Description = "15:00–17:00 UTC — trend continuation",
                Time = time
            };
        }

        if (h >= 0.0 && h < 3.0)
        {
            return new SessionStatus
            {
                Window = "Tokyo Session",
                Prime = false,
                Color = "#f85149",
                Icon = "🔴",
                Description = "00:00–03:00 UTC — low probability, avoid",
                Time = time
            };
        }

        return new SessionStatus
        {
            Window = "Dead Zone",
            Prime = false,
            Color = "#484f58",
            Icon = "⚫",
            Description = "Between sessions",
            Time = time
        };
    }

    public List<CorrelationWarning> CheckCorrelationExposure(
        List<TradeSetup> openTrades, string direction, string instrument)
    {
        var warnings = new List<CorrelationWarning>();

        foreach (var (groupName, groupInstruments) in Instrument.CorrelationGroups)
        {
            if (!groupInstruments.Contains(instrument)) continue;

            var conflicts = openTrades
                .Where(t => groupInstruments.Contains(t.Instrument) &&
                           t.Direction == direction &&
                           t.Instrument != instrument)
                .ToList();

            if (conflicts.Any())
            {
                warnings.Add(new CorrelationWarning
                {
                    Group = groupName,
                    Conflicting = string.Join(", ", conflicts.Select(t => t.Instrument)),
                    Count = conflicts.Count
                });
            }
        }

        return warnings;
    }
}