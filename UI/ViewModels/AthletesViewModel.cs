using System.Linq;
using DataLayer.EfClasses;
using ServiceLayer.AthleteService;

namespace UI.ViewModels;

public class AthletesViewModel : GenericTableViewModel<Athlete, int>
{
    public AthletesViewModel(IAthleteService athleteService) : base(athleteService)
    {
    }

    public string Title => "Спортсмены";

    protected override void InitializeColumns()
    {
        base.InitializeColumns();
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();
        
        ColumnConfigurations.Add(ColumnConfiguration.Create("FirstName", "Имя", 150));
        ColumnConfigurations.Add(ColumnConfiguration.Create("LastName", "Фамилия", 150));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Gender", "Пол", 80));
        ColumnConfigurations.Add(ColumnConfiguration.Create("YearOfBirth", "Год рождения", 120));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Category", "Разряд", 100));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Club.Name", "Клуб", 200));
    }

    protected override List<FieldConfiguration> GetFieldConfigurations()
    {
        return new List<FieldConfiguration>
        {
            FieldConfiguration.Create("FirstName", "Имя", isRequired: true),
            FieldConfiguration.Create("LastName", "Фамилия", isRequired: true),
            FieldConfiguration.Create("Gender", "Пол", isRequired: true),
            FieldConfiguration.Create("YearOfBirth", "Год рождения", isRequired: true),
            FieldConfiguration.Create("Category", "Разряд"),
            FieldConfiguration.Hidden("ClubId"), // Скрываем навигационные свойства
            FieldConfiguration.Hidden("Club"),
            FieldConfiguration.Hidden("Entries"),
            FieldConfiguration.Hidden("RelayPositions")
        };
    }

    protected override List<CustomValidationRule> GetCustomValidationRules()
    {
        return new List<CustomValidationRule>
        {
            new CustomValidationRule
            {
                Validator = entity =>
                {
                    if (entity is Athlete athlete)
                    {
                        if (athlete.YearOfBirth <= 1900 || athlete.YearOfBirth > DateTime.Now.Year)
                        {
                            return new System.ComponentModel.DataAnnotations.ValidationResult("Год рождения должен быть в разумных пределах");
                        }
                    }
                    return System.ComponentModel.DataAnnotations.ValidationResult.Success!;
                },
                ErrorMessage = "Год рождения должен быть в разумных пределах"
            }
        };
    }

    protected override IQueryable<Athlete> ApplySearch(IQueryable<Athlete> query)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return query;

        var searchLower = SearchText.ToLower();
        return query.Where(a => 
            (a.FirstName != null && a.FirstName.ToLower().Contains(searchLower)) ||
            (a.LastName != null && a.LastName.ToLower().Contains(searchLower)) ||
            (a.Club != null && a.Club.Name != null && a.Club.Name.ToLower().Contains(searchLower)));
    }
}

