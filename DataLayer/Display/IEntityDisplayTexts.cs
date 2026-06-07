using System.Globalization;

namespace DataLayer.Display;

public interface IEntityDisplayTexts
{
    CultureInfo Culture { get; }

    string GetEnumDisplay(Enum value);

    string SwimStyleDisplayNameFormat { get; }

    string SwimEventDisplayNameFormat { get; }

    string AgeGroupDisplayNameFormat { get; }

    string AgeGroupYearRangeOpen { get; }

    string AgeGroupYearRangeOpenWithSuffix { get; }

    string AgeGroupYearRangeWithSuffixFormat { get; }

    string AgeGroupYearRangeOlderThan { get; }

    string AgeGroupYearRangeYoungerThan { get; }

    string PersonalParen { get; }

    string MissingParticipantParen { get; }
}
