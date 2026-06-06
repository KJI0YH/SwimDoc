using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AgeGroupService;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using ServiceLayer.PointScoreProvider;
using ServiceLayer.SwimStyleService;
using UI.Services;
using UI.ViewModels;
using UI.ViewModels.Pages;
using UI.Views.Dialogs.Markers.AddEdit;

namespace UI.ViewModels.Pages.Data;

public class AthletesByAgeGroupViewModel : AthletesViewModel
{
    private readonly IAgeGroupService _ageGroupService;
    private readonly IAddEditWindowFactory _windowFactory;
    private int? _ageGroupId;
    private bool _ageGroupLoaded;
    private Gender _ageGroupGender;
    private int _birthYearMin;
    private int _birthYearMax;

    public AthletesByAgeGroupViewModel(IAthleteService athleteService, IAgeGroupService ageGroupService)
        : base(athleteService)
    {
        _ageGroupService = ageGroupService;
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    public void SetAgeGroupId(int? ageGroupId)
    {
        _ageGroupId = ageGroupId;
        _ageGroupLoaded = false;

        if (ageGroupId.HasValue)
        {
            var ageGroup = _ageGroupService.Query().FirstOrDefault(ag => ag.Id == ageGroupId.Value);
            if (ageGroup is not null)
            {
                _ageGroupGender = ageGroup.Gender;
                _birthYearMin = ageGroup.BirthYearMin ?? 0;
                _birthYearMax = ageGroup.BirthYearMax ?? int.MaxValue;
                _ageGroupLoaded = true;
            }
        }

        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<Athlete> ApplyQuery(IQueryable<Athlete> query)
    {
        query = base.ApplyQuery(query);
        if (!_ageGroupId.HasValue || !_ageGroupLoaded)
            return query.Where(_ => false);

        return query.Where(a =>
            a.YearOfBirth >= _birthYearMin &&
            a.YearOfBirth <= _birthYearMax &&
            (_ageGroupGender == Gender.Mixed || a.Gender == _ageGroupGender));
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var result = _windowFactory.CreateAndShow<AthleteAddEditWindow>(id);
        if (result == true)
            _ = LoadDataAsync();
    }
}
