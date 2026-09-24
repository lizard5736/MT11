using Grip.Core.Localization;

namespace Grip.Core.Energy;

/// <summary>An active keep-awake session: forever, or until a moment.</summary>
public sealed record KeepAwakeSession(DateTimeOffset Started, DateTimeOffset? Until)
{
    public bool IsIndefinite => Until == null;

    public bool IsExpired(DateTimeOffset now) => Until is { } until && now >= until;

    public TimeSpan Remaining(DateTimeOffset now) =>
        Until is { } until ? (until > now ? until - now : TimeSpan.Zero) : TimeSpan.MaxValue;

    public static KeepAwakeSession ForMinutes(int minutes, DateTimeOffset now) =>
        minutes <= 0 ? new KeepAwakeSession(now, null) : new KeepAwakeSession(now, now.AddMinutes(minutes));

    /// <summary>Until the next occurrence of a wall-clock time (tomorrow if it already passed today).</summary>
    public static KeepAwakeSession UntilTime(TimeOnly time, DateTimeOffset now)
    {
        // "now" carries the user's offset (DateTimeOffset.Now), so its wall clock is the local one.
        var target = new DateTimeOffset(now.Date + time.ToTimeSpan(), now.Offset);
        if (target <= now) target = target.AddDays(1);
        return new KeepAwakeSession(now, target);
    }
}

public static class KeepAwakeText
{
    /// <summary>Chip label for a preset length: "15 мин", "1 ч", "Без срока".</summary>
    public static string PresetLabel(int minutes, Localizer loc)
    {
        if (minutes <= 0) return loc["keepAwake.preset.infinite"];
        if (minutes < 60) return $"{minutes} {loc["time.min"]}";
        if (minutes % 60 == 0) return $"{minutes / 60} {loc["time.h"]}";
        return $"{minutes / 60} {loc["time.h"]} {minutes % 60} {loc["time.min"]}";
    }

    /// <summary>The status line under the switch.</summary>
    public static string Status(KeepAwakeSession? session, DateTimeOffset now, Localizer loc)
    {
        if (session == null) return loc["keepAwake.off"];
        if (session.IsIndefinite) return loc["keepAwake.indefinite"];
        var until = session.Until!.Value.ToLocalTime();
        var clock = until.ToString("HH:mm", loc.Culture);
        if (until.Date != now.ToLocalTime().Date) clock = $"{clock} ({loc["keepAwake.until.tomorrow"]})";
        return loc.Format("keepAwake.remaining", loc.Duration(session.Remaining(now)), clock);
    }

    /// <summary>Parses "18:30", "1830", "18.30" or "18" into a time of day.</summary>
    public static bool TryParseClock(string? text, out TimeOnly time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var t = text.Trim().Replace('.', ':').Replace(' ', ':');
        int hours, minutes = 0;
        if (t.Contains(':'))
        {
            var parts = t.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !int.TryParse(parts[0], out hours) || !int.TryParse(parts[1], out minutes)) return false;
        }
        else if (t.Length is 3 or 4 && int.TryParse(t, out var packed))
        {
            hours = packed / 100;
            minutes = packed % 100;
        }
        else if (!int.TryParse(t, out hours)) return false;

        if (hours is < 0 or > 23 || minutes is < 0 or > 59) return false;
        time = new TimeOnly(hours, minutes);
        return true;
    }
}
