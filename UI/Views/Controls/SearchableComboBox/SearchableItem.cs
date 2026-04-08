namespace UI.Views.Controls.SearchableComboBox;

public class SearchableItem
{
    public object? Value { get; set; }
    public string DisplayText { get; set; } = string.Empty;

    public override string ToString()
    {
        return DisplayText;
    }
}