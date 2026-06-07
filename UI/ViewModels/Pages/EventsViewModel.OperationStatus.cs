using System.Collections.ObjectModel;
using BizLogic.HeatAllocation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using ServiceLayer.HeatService.Exceptions;
using UI.Resources;

namespace UI.ViewModels.Pages;

public partial class EventsViewModel
{
    private CancellationTokenSource? _operationCts;
    private OperationItem? _operationSummaryRow;
    [ObservableProperty] private bool _isOperationBarOpen;
    [ObservableProperty] private bool _isOperationRunning;
    [ObservableProperty] private bool _isOperationDetailsOpen;
    [ObservableProperty] private bool _isMultiItemOperation;
    [ObservableProperty] private string _operationHeader = string.Empty;
    [ObservableProperty] private string _operationMessage = string.Empty;
    [ObservableProperty] private int _operationProcessedItems;
    [ObservableProperty] private int _operationTotalItems;
    [ObservableProperty] private ObservableCollection<OperationItem> _operationItems = new();
    [ObservableProperty] private ObservableCollection<string> _operationErrors = new();
    public bool IsOperationIndeterminate => IsOperationRunning;
    public bool HasOperationDetails =>
        IsMultiItemOperation
            ? OperationItems.Any(item => item.WarningsCount > 0 || item.ErrorsCount > 0)
            : OperationErrors.Count > 0;

    public bool CanToggleOperationDetails =>
        IsMultiItemOperation && OperationItems.Count > 0 || HasOperationDetails;

    partial void OnIsOperationRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(IsOperationIndeterminate));
        HeatAllocationCommand.NotifyCanExecuteChanged();
        GenerateReportsCommand.NotifyCanExecuteChanged();
        CalculateStartTimesCommand.NotifyCanExecuteChanged();
        CancelOperationCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsMultiItemOperationChanged(bool value)
    {
        OnPropertyChanged(nameof(HasOperationDetails));
        OnPropertyChanged(nameof(CanToggleOperationDetails));
    }

    partial void OnOperationErrorsChanged(ObservableCollection<string> value)
    {
        OnPropertyChanged(nameof(HasOperationDetails));
        OnPropertyChanged(nameof(CanToggleOperationDetails));
    }

    partial void OnOperationItemsChanged(ObservableCollection<OperationItem> value)
    {
        OnPropertyChanged(nameof(HasOperationDetails));
        OnPropertyChanged(nameof(CanToggleOperationDetails));
    }

    [RelayCommand(CanExecute = nameof(CanCancelOperation))]
    private void CancelOperation() => _operationCts?.Cancel();
    private bool CanCancelOperation() => IsOperationRunning;

    [RelayCommand]
    private void ToggleOperationDetails() => IsOperationDetailsOpen = !IsOperationDetailsOpen;

    [RelayCommand]
    private void DismissOperationBar()
    {
        if (IsOperationRunning) return;
        IsOperationBarOpen = false;
        IsOperationDetailsOpen = false;
        OperationItems.Clear();
        OperationErrors.Clear();
        _operationSummaryRow = null;
        OnPropertyChanged(nameof(HasOperationDetails));
        OnPropertyChanged(nameof(CanToggleOperationDetails));
    }

    private void RecalculateOperationSummary()
    {
        var dataItems = OperationItems.Where(item => !item.IsSummaryRow).ToList();
        _operationSummaryRow ??= new OperationItem(Strings.Import_Summary_Total) { IsSummaryRow = true };
        if (!OperationItems.Contains(_operationSummaryRow))
            OperationItems.Add(_operationSummaryRow);
        _operationSummaryRow.EventName = Strings.Import_Summary_Total;
        _operationSummaryRow.HeatsCreatedCount = dataItems.Sum(item => item.HeatsCreatedCount ?? 0);
        _operationSummaryRow.WarningsCount = dataItems.Sum(item => item.WarningsCount);
        _operationSummaryRow.ErrorsCount = dataItems.Sum(item => item.ErrorsCount);
        _operationSummaryRow.Warnings = [];
        _operationSummaryRow.Errors = [];
        _operationSummaryRow.IsDetailsOpen = false;
    }

    private async Task<OperationRunResult> RunMultiItemOperationAsync(
        string header,
        string preparingMessageFormat,
        string processingMessageFormat,
        string finishedMessageFormat,
        string canceledHeader,
        IReadOnlyList<SwimEvent> events,
        Func<SwimEvent, CancellationToken, Task<OperationItemOutcome>> processOne)
    {
        if (events.Count == 0)
            return new OperationRunResult(false, false);
        _operationCts?.Cancel();
        _operationCts = new CancellationTokenSource();
        var token = _operationCts.Token;
        IsMultiItemOperation = true;
        OperationErrors.Clear();
        OperationItems = new ObservableCollection<OperationItem>(
            events.Select(swimEvent => new OperationItem(EntityDisplayFormatter.FormatSwimEvent(swimEvent))));
        _operationSummaryRow = null;
        RecalculateOperationSummary();
        OperationTotalItems = events.Count;
        OperationProcessedItems = 0;
        IsOperationRunning = true;
        IsOperationBarOpen = true;
        IsOperationDetailsOpen = false;
        OperationHeader = header;
        OperationMessage = string.Format(preparingMessageFormat, OperationTotalItems);
        CancelOperationCommand.NotifyCanExecuteChanged();
        var hadFailure = false;
        var hadWarnings = false;
        var skippedCount = 0;
        var canceled = false;
        try
        {
            await Task.Run(async () =>
            {
                foreach (var (item, swimEvent) in OperationItems.Where(i => !i.IsSummaryRow).Zip(events))
                {
                    token.ThrowIfCancellationRequested();
                    await RunOnUiAsync(() =>
                    {
                        item.Status = OperationItemStatus.Processing;
                        OperationMessage = string.Format(
                            processingMessageFormat,
                            item.EventName,
                            OperationProcessedItems + 1,
                            OperationTotalItems);
                    });
                    try
                    {
                        var outcome = await processOne(swimEvent, token).ConfigureAwait(false);
                        await RunOnUiAsync(() =>
                        {
                            ApplyOutcome(item, outcome);
                            OperationProcessedItems++;
                            RecalculateOperationSummary();
                        });
                        hadFailure |= outcome.Status == OperationItemStatus.Failed;
                        hadWarnings |= outcome.Status == OperationItemStatus.CompletedWithWarnings;
                        if (outcome.Status == OperationItemStatus.Skipped)
                            skippedCount++;
                        if (outcome.Status == OperationItemStatus.Failed)
                            break;
                    }
                    catch (OperationCanceledException)
                    {
                        await RunOnUiAsync(() =>
                        {
                            item.Status = OperationItemStatus.Canceled;
                            OperationProcessedItems++;
                            MarkUnfinishedOperationItemsCanceled();
                            RecalculateOperationSummary();
                        });
                        throw;
                    }
                    catch (Exception ex)
                    {
                        await RunOnUiAsync(() =>
                        {
                            item.Status = OperationItemStatus.Failed;
                            item.Errors = [HeatAllocationMessageLocalizer.Localize(ex.Message)];
                            item.ErrorsCount = 1;
                            OperationProcessedItems++;
                            RecalculateOperationSummary();
                        });
                        hadFailure = true;
                        break;
                    }
                }
            }, token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }
        finally
        {
            IsOperationRunning = false;
            CancelOperationCommand.NotifyCanExecuteChanged();
            RecalculateOperationSummary();
            IsOperationDetailsOpen = HasOperationDetails;
            OnPropertyChanged(nameof(HasOperationDetails));
            if (canceled)
            {
                OperationHeader = canceledHeader;
                OperationMessage = string.Format(
                    Strings.Operation_Canceled_MessageFormat,
                    OperationProcessedItems,
                    OperationTotalItems);
            }
            else
            {
                OperationMessage = string.Format(
                    finishedMessageFormat,
                    OperationProcessedItems,
                    OperationTotalItems);
                if (hadFailure)
                    OperationHeader = Strings.Operation_Finished_WithErrors_Header;
                else if (hadWarnings)
                    OperationHeader = Strings.Operation_Finished_WithWarnings_Header;
                else if (skippedCount == events.Count)
                    OperationHeader = Strings.Operation_Finished_AllSkipped_Header;
                else
                    OperationHeader = Strings.Operation_Finished_Success_Header;
            }
        }
        return new OperationRunResult(canceled, hadFailure);
    }

    private async Task<OperationRunResult> RunSingleOperationAsync(
        string header,
        string runningMessage,
        string finishedMessage,
        string canceledHeader,
        Func<CancellationToken, Task<OperationItemOutcome>> process)
    {
        _operationCts?.Cancel();
        _operationCts = new CancellationTokenSource();
        var token = _operationCts.Token;
        IsMultiItemOperation = false;
        OperationItems.Clear();
        OperationErrors.Clear();
        OperationTotalItems = 1;
        OperationProcessedItems = 0;
        IsOperationRunning = true;
        IsOperationBarOpen = true;
        IsOperationDetailsOpen = false;
        OperationHeader = header;
        OperationMessage = runningMessage;
        CancelOperationCommand.NotifyCanExecuteChanged();
        var canceled = false;
        var hadFailure = false;
        try
        {
            await Task.Run(async () =>
            {
                var outcome = await process(token).ConfigureAwait(false);
                await RunOnUiAsync(() =>
                {
                    OperationProcessedItems = 1;
                    if (outcome.Status == OperationItemStatus.Failed)
                    {
                        OperationErrors = new ObservableCollection<string>(outcome.Errors);
                        OperationHeader = Strings.Operation_Finished_WithErrors_Header;
                        OperationMessage = outcome.Errors.FirstOrDefault() ??
                                           Strings.Operation_Finished_WithErrors_Header;
                        IsOperationDetailsOpen = true;
                        hadFailure = true;
                    }
                    else
                    {
                        OperationHeader = Strings.Operation_Finished_Success_Header;
                        OperationMessage = finishedMessage;
                    }
                });
            }, token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            canceled = true;
            OperationHeader = canceledHeader;
            OperationMessage = string.Format(
                Strings.Operation_Canceled_MessageFormat,
                OperationProcessedItems,
                OperationTotalItems);
        }
        catch (Exception ex)
        {
            hadFailure = true;
            OperationErrors = new ObservableCollection<string> { ex.Message };
            OperationHeader = Strings.Operation_Finished_WithErrors_Header;
            OperationMessage = ex.Message;
            IsOperationDetailsOpen = true;
        }
        finally
        {
            IsOperationRunning = false;
            CancelOperationCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(HasOperationDetails));
        }
        return new OperationRunResult(canceled, hadFailure);
    }

    private static Task RunOnUiAsync(Action action) => DispatcherUiHelper.RunOnUiAsync(action);
    private void MarkUnfinishedOperationItemsCanceled()
    {
        foreach (var item in OperationItems.Where(i => !i.IsSummaryRow))
        {
            if (item.Status is OperationItemStatus.Pending or OperationItemStatus.Processing)
                item.Status = OperationItemStatus.Canceled;
        }
    }

    private static void ApplyOutcome(OperationItem item, OperationItemOutcome outcome)
    {
        item.Status = outcome.Status;
        item.Warnings = outcome.Warnings;
        item.Errors = outcome.Errors;
        item.WarningsCount = outcome.Warnings.Count;
        item.ErrorsCount = outcome.Errors.Count;
        item.HeatsCreatedCount = outcome.HeatsCreatedCount;
    }

    public static OperationItemOutcome OutcomeFromHeatAllocation(HeatAllocationOutDto result)
    {
        var warnings = HeatAllocationMessageLocalizer.LocalizeAll(
            result.Warnings?.Where(message => message.Length > 0) ?? []);
        var errors = HeatAllocationMessageLocalizer.LocalizeAll(
            result.Errors?.Where(message => message.Length > 0) ?? []);
        var heatsCreated = result.Heats.Count;
        if (errors.Count > 0)
            return OperationItemOutcome.Failed(errors, warnings, heatsCreated);
        return warnings.Count > 0
            ? OperationItemOutcome.WithWarnings(warnings, heatsCreated)
            : OperationItemOutcome.Success(heatsCreated);
    }

    public static OperationItemOutcome OutcomeFromHeatAllocationException(HeatAllocationException ex)
    {
        var errors = HeatAllocationMessageLocalizer.LocalizeAll(
            ex.Errors
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Cast<string>());
        return OperationItemOutcome.Failed(errors);
    }
}
