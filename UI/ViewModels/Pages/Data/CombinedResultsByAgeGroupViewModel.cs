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
using UI.ViewModels;
using UI.ViewModels.Pages;

namespace UI.ViewModels.Pages.Data;

public class CombinedResultsByAgeGroupViewModel : CombinedResultsViewModel
{
    public override bool ShowAgeGroupSelector => false;
    public void SetAgeGroupId(int? ageGroupId)
    {
        if (!ageGroupId.HasValue)
        {
            SelectedAgeGroup = null;
            return;
        }
        SelectedAgeGroup = AgeGroupService.Query()
            .FirstOrDefault(ageGroup => ageGroup.Id == ageGroupId.Value);
    }
}
