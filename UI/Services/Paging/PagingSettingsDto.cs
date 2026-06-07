using System.IO;
using System.Text.Json;
using UI.Resources;

namespace UI.Services.Paging;

internal sealed class PagingSettingsDto
{
    public Dictionary<string, int>? PageSizes { get; set; }
}
