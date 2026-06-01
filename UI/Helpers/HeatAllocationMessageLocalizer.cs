using System.Globalization;
using BizLogic.HeatLogic;
using UI.Resources;

namespace UI.Helpers;

public static class HeatAllocationMessageLocalizer
{
    public static string Localize(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        return message switch
        {
            HeatAllocationMessageKeys.HeatsReallocated => Strings.HeatAllocation_Warning_HeatsReallocated,
            HeatAllocationMessageKeys.NoEntriesForEvent => Strings.HeatAllocation_Warning_NoEntriesForEvent,
            "Heats were reallocated" => Strings.HeatAllocation_Warning_HeatsReallocated,
            "There are no entries for this swim event" => Strings.HeatAllocation_Warning_NoEntriesForEvent,
            _ when message.Contains("database is locked", StringComparison.OrdinalIgnoreCase) =>
                Strings.HeatAllocation_Error_DatabaseLocked,
            _ => Strings.ResourceManager.GetString(message, CultureInfo.CurrentUICulture) ?? message
        };
    }

    public static IReadOnlyList<string> LocalizeAll(IEnumerable<string> messages) =>
        messages.Select(Localize).ToList();
}
