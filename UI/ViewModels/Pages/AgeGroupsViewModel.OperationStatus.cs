using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UI.Resources;

namespace UI.ViewModels.Pages;

public partial class AgeGroupsViewModel
{
    private CancellationTokenSource? _operationCts;
    [ObservableProperty] private bool _isOperationBarOpen;
    [ObservableProperty] private bool _isOperationRunning;
    [ObservableProperty] private bool _isOperationDetailsOpen;
    [ObservableProperty] private bool _isMultiItemOperation;
    [ObservableProperty] private string _operationHeader = string.Empty;
    [ObservableProperty] private string _operationMessage = string.Empty;
    [ObservableProperty] private int _operationProcessedItems;
    [ObservableProperty] private int _operationTotalItems;
    [ObservableProperty] private ObservableCollection<string> _operationErrors = new();
    public bool IsOperationIndeterminate => IsOperationRunning;
    public bool HasOperationDetails => OperationErrors.Count > 0;
    public bool CanToggleOperationDetails => HasOperationDetails;
    partial void OnIsOperationRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(IsOperationIndeterminate));
        GenerateCombinedResultsReportsCommand.NotifyCanExecuteChanged();
        CancelOperationCommand.NotifyCanExecuteChanged();
    }

    partial void OnOperationErrorsChanged(ObservableCollection<string> value)
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
        if (IsOperationRunning)
            return;
        IsOperationBarOpen = false;
        IsOperationDetailsOpen = false;
        OperationErrors.Clear();
        OnPropertyChanged(nameof(HasOperationDetails));
        OnPropertyChanged(nameof(CanToggleOperationDetails));
    }

    private async Task RunSingleOperationAsync(
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
    }

    private static Task RunOnUiAsync(Action action) => DispatcherUiHelper.RunOnUiAsync(action);
}
