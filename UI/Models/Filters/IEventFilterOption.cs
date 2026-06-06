using CommunityToolkit.Mvvm.ComponentModel;

namespace UI.Models.Filters;

public interface IEventFilterOption
{
    bool IsSelected { get; set; }
}
