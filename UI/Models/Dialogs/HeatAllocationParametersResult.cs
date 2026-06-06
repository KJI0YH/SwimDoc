using System.Collections.ObjectModel;
using System.Collections.Specialized;
using BizLogic.HeatLogic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UI.Resources;
using UI.Services;

namespace UI.Models.Dialogs;

public sealed class HeatAllocationParametersResult(HeatOrder heatOrder, int minHeatSize)
{
    public HeatOrder HeatOrder { get; } = heatOrder;
    public int MinHeatSize { get; } = minHeatSize;
}
