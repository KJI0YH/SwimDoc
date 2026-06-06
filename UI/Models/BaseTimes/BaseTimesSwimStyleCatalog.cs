using DataLayer.EfClasses;

namespace UI.Models.BaseTimes;

internal static class BaseTimesSwimStyleCatalog
{
    internal static readonly SwimStyleSpec[] ScmMenWomen =
    [
        new(0, 50, Stroke.Free),
        new(0, 100, Stroke.Free),
        new(0, 200, Stroke.Free),
        new(0, 400, Stroke.Free),
        new(0, 800, Stroke.Free),
        new(0, 1500, Stroke.Free),
        new(0, 50, Stroke.Back),
        new(0, 100, Stroke.Back),
        new(0, 200, Stroke.Back),
        new(0, 50, Stroke.Breast),
        new(0, 100, Stroke.Breast),
        new(0, 200, Stroke.Breast),
        new(0, 50, Stroke.Fly),
        new(0, 100, Stroke.Fly),
        new(0, 200, Stroke.Fly),
        new(0, 100, Stroke.Medley),
        new(0, 200, Stroke.Medley),
        new(0, 400, Stroke.Medley),
        new(4, 50, Stroke.Free),
        new(4, 100, Stroke.Free),
        new(4, 200, Stroke.Free),
        new(4, 50, Stroke.Medley),
        new(4, 100, Stroke.Medley),
    ];

    internal static readonly SwimStyleSpec[] LcmMenWomen =
    [
        new(0, 50, Stroke.Free),
        new(0, 100, Stroke.Free),
        new(0, 200, Stroke.Free),
        new(0, 400, Stroke.Free),
        new(0, 800, Stroke.Free),
        new(0, 1500, Stroke.Free),
        new(0, 50, Stroke.Back),
        new(0, 100, Stroke.Back),
        new(0, 200, Stroke.Back),
        new(0, 50, Stroke.Breast),
        new(0, 100, Stroke.Breast),
        new(0, 200, Stroke.Breast),
        new(0, 50, Stroke.Fly),
        new(0, 100, Stroke.Fly),
        new(0, 200, Stroke.Fly),
        new(0, 200, Stroke.Medley),
        new(0, 400, Stroke.Medley),
        new(4, 100, Stroke.Free),
        new(4, 200, Stroke.Free),
        new(4, 100, Stroke.Medley),
    ];

    internal static readonly SwimStyleSpec[] ScmMixedRelay =
    [
        new(4, 50, Stroke.Free),
        new(4, 50, Stroke.Medley),
    ];

    internal static readonly SwimStyleSpec[] LcmMixedRelay =
    [
        new(4, 100, Stroke.Free),
        new(4, 100, Stroke.Medley),
    ];
}
