using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace App.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IHomeService _homeService;

    [ObservableProperty] private bool isHomeLoading;

    [ObservableProperty] private string? homeError;

    public HomeViewModel(IHomeService homeService)
    {
        _homeService = homeService;

        LoadHomeCommand = new AsyncRelayCommand(LoadHomeAsync);

        _ = LoadHomeAsync();
    }

    public ObservableCollection<HomeSection> HomeSections { get; } = new();

    public IAsyncRelayCommand LoadHomeCommand { get; }

    private async Task LoadHomeAsync()
    {
        IsHomeLoading = true;
        HomeError = null;

        try
        {
            var response = await _homeService.GetHomeAsync();
            HomeSections.Clear();
            foreach (var section in response.Sections)
            {
                HomeSections.Add(section);
            }
        }
        catch (Exception ex)
        {
            HomeError = ex.Message;
        }
        finally
        {
            IsHomeLoading = false;
        }
    }
}
