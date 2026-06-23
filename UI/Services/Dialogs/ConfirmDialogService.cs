using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using UI.Helpers.Dialogs;
using UI.Resources;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace UI.Services.Dialogs;

public class ConfirmDialogService(
    EfCoreContext dbContext,
    IContentDialogService contentDialogService) : IConfirmDialogService
{
    public async Task<bool> ConfirmDeleteIfOfficialResultsAffectedAsync<TEntity>(
        IReadOnlyList<int> ids,
        CancellationToken cancellationToken = default)
    {
        var count = await CountOfficialResultEntriesAffectedAsync<TEntity>(ids, cancellationToken);
        return await ConfirmIfNeededAsync(
            count,
            title: Strings.Confirm_Title_Delete,
            primaryButtonText: Strings.Common_Delete,
            content: BuildDeleteMessage(count),
            cancellationToken);
    }

    public async Task<bool> ConfirmHeatReformIfOfficialResultsExistAsync(
        int swimEventId,
        string eventDisplayName,
        CancellationToken cancellationToken = default)
    {
        var count = await CountOfficialResultEntriesInEventHeatsAsync(swimEventId, cancellationToken);
        return await ConfirmIfNeededAsync(
            count,
            title: Strings.Confirm_Title_HeatAllocation,
            primaryButtonText: Strings.Common_Allocate,
            content: BuildHeatReformMessage(eventDisplayName, count),
            cancellationToken);
    }

    private Task<int> CountOfficialResultEntriesAffectedAsync<TEntity>(
        IReadOnlyList<int> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return Task.FromResult(0);
        var query = BuildEntityDeleteImpactQuery(typeof(TEntity).Name, ids);
        return query is null
            ? Task.FromResult(0)
            : query.CountAsync(cancellationToken);
    }

    private IQueryable<Entry>? BuildEntityDeleteImpactQuery(string entityName, IReadOnlyList<int> ids) =>
        entityName switch
        {
            nameof(Entry) => OfficialEntriesQuery().Where(e => ids.Contains(e.Id)),
            nameof(Athlete) => OfficialEntriesQuery().Where(e =>
                (e.AthleteId != null && ids.Contains(e.AthleteId.Value)) ||
                (e.Relay != null && e.Relay.Positions.Any(p => ids.Contains(p.AthleteId)))),
            nameof(Club) => OfficialEntriesQuery().Where(e =>
                (e.Athlete != null && e.Athlete.ClubId != null && ids.Contains(e.Athlete.ClubId.Value)) ||
                (e.Relay != null && ids.Contains(e.Relay.ClubId))),
            nameof(SwimEvent) => OfficialEntriesQuery().Where(e =>
                e.SwimEventId != null && ids.Contains(e.SwimEventId.Value)),
            nameof(SwimStyle) => OfficialEntriesQuery().Where(e =>
                ids.Contains(e.SwimStyleId) ||
                (e.SwimEvent != null && ids.Contains(e.SwimEvent.SwimStyleId))),
            nameof(AgeGroup) => OfficialEntriesQuery().Where(e =>
                e.SwimEvent != null && ids.Contains(e.SwimEvent.AgeGroupId)),
            nameof(Heat) => OfficialEntriesQuery().Where(e =>
                e.HeatPosition != null && ids.Contains(e.HeatPosition.HeatId)),
            _ => null
        };

    private IQueryable<Entry> OfficialEntriesQuery() =>
        dbContext.Entries.AsNoTracking()
            .Where(e => e.Status >= EntryStatus.FINISH);

    private Task<int> CountOfficialResultEntriesInEventHeatsAsync(int swimEventId, CancellationToken cancellationToken) =>
        OfficialEntriesQuery()
            .Where(e => e.HeatPosition != null)
            .Where(e => e.HeatPosition!.Heat.SwimEventId == swimEventId)
            .CountAsync(cancellationToken);

    private async Task<bool> ConfirmIfNeededAsync(
        int affectedCount,
        string title,
        string primaryButtonText,
        string content,
        CancellationToken cancellationToken)
    {
        if (affectedCount <= 0)
            return true;
        var dialog = new ContentDialog
        {
            Title = title,
            Content = DialogContentFactory.CreateMessageContent(content),
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = Strings.Common_Cancel,
            DefaultButton = ContentDialogButton.Close
        };
        var result = await contentDialogService.ShowAsync(dialog, cancellationToken);
        return result == ContentDialogResult.Primary;
    }

    private static string BuildDeleteMessage(int count)
    {
        var entriesWord = count == 1
            ? Strings.Common_Entry_Accusative_Singular
            : count is >= 2 and <= 4
                ? Strings.Common_Entry_Accusative_Few
                : Strings.Common_Entry_Accusative_Many;
        return string.Format(
            Strings.Confirm_DeleteOfficialResults_MessageFormat,
            count,
            entriesWord);
    }

    private static string BuildHeatReformMessage(string eventDisplayName, int count)
    {
        var entriesWord = count == 1
            ? Strings.Common_Entry_Accusative_Singular
            : count is >= 2 and <= 4
                ? Strings.Common_Entry_Accusative_Few
                : Strings.Common_Entry_Accusative_Many;
        return string.Format(
            Strings.Confirm_HeatAllocationOfficialResults_MessageFormat,
            eventDisplayName,
            count,
            entriesWord);
    }
}
