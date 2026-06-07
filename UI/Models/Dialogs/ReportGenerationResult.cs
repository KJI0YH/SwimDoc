using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using UI.Resources;

namespace UI.Models.Dialogs;

public sealed class ReportGenerationResult(bool entry, bool start, bool finish, string outputFilePath)
{
    public bool IncludeEntryList { get; } = entry;
    public bool IncludeStartList { get; } = start;
    public bool IncludeFinishList { get; } = finish;
    public string OutputFilePath { get; } = outputFilePath;
}
