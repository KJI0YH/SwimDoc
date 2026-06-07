using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.AgeGroupService;
using ServiceLayer.EntryService;
using UI.Models;

namespace UI.Models.CombinedResults;

public sealed record CombinedResultsEventColumnView(int EventId, string Header);
