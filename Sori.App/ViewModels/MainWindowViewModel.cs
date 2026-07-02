using System.Collections.ObjectModel;
using System.Threading.Tasks;
using App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IMusicClient _musicClient;

    [ObservableProperty]
    private string searchQuery = "";

    [ObservableProperty]
    private Song? selectedSong;

    [ObservableProperty]
    private Song? currentSong;

    public ObservableCollection<Song> SearchResults { get; } = new();

    public ObservableCollection<Song> Queue { get; } = new();

    public IAsyncRelayCommand SearchCommand { get; }

    public IRelayCommand PlaySelectedCommand { get; }
    public IRelayCommand<Song> PlaySongCommand { get; }

    public MainWindowViewModel()
        : this(new MockMusicClient())
    {
    }

    public MainWindowViewModel(IMusicClient musicClient)
    {
        _musicClient = musicClient;

        SearchCommand = new AsyncRelayCommand(SearchAsync);
        
        PlaySelectedCommand = new RelayCommand(
            PlaySelected,
            () => SelectedSong is not null
        );
        
        PlaySongCommand = new RelayCommand<Song>(PlaySong);
    }

    partial void OnSelectedSongChanged(Song? value)
    {
        PlaySelectedCommand.NotifyCanExecuteChanged();
    }

    private async Task SearchAsync()
    {
        SearchResults.Clear();

        var results = await _musicClient.SearchSongsAsync(SearchQuery);

        foreach (var song in results)
        {
            SearchResults.Add(song);
        }
    }

    private void PlaySelected()
    {
        PlaySong(SelectedSong);
    }
    
    private void PlaySong(Song? song)
    {
        if (song is null)
        {
            return;
        }

        CurrentSong = song;
        if (!Queue.Contains(song))
        {
            Queue.Insert(0, song);
        }
    }
}