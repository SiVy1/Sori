using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sori.Core.Enums;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace App.ViewModels;

public partial class QueueViewModel : ObservableObject
{
    private readonly IQueueService _queueService;

    [ObservableProperty] private int queueCurrentIndex = -1;

    public QueueViewModel(IQueueService queueService)
    {
        _queueService = queueService;

        _queueService.Changed += (_, _) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                SyncQueueFromService();
                NotifyQueuePropertiesChanged();
            });
        };

        ToggleShuffleCommand = new RelayCommand(ToggleShuffle);
        CycleRepeatModeCommand = new RelayCommand(CycleRepeatMode);
        ToggleRadioCommand = new RelayCommand(ToggleRadio);

        SyncQueueFromService();
        NotifyQueuePropertiesChanged();
    }

    public ObservableCollection<Song> Queue { get; } = new();
    public ObservableCollection<Song> UpNextItems { get; } = new();
    public ObservableCollection<Song> RadioItems { get; } = new();

    public bool ShuffleEnabled => _queueService.ShuffleEnabled;

    public RepeatMode RepeatMode => _queueService.RepeatMode;

    public string RepeatModeText => RepeatMode switch
    {
        RepeatMode.Off => "Repeat Off",
        RepeatMode.All => "Repeat All",
        RepeatMode.One => "Repeat One",
        _ => "Repeat"
    };

    public string ShuffleText => ShuffleEnabled ? "Shuffle On" : "Shuffle Off";

    public bool RadioEnabled => _queueService.RadioEnabled;

    public string RadioText => RadioEnabled ? "Radio On" : "Radio Off";

    public bool IsRadioActive => RadioEnabled;

    public bool HasRadioItems => RadioItems.Count > 0;

    public bool CanGoNext => _queueService.Items.Count > 0;

    public bool CanGoPrevious => _queueService.Items.Count > 0;

    public bool IsRepeatActive => _queueService.RepeatMode != RepeatMode.Off;

    public string QueueIndexText =>
        QueueCurrentIndex >= 0 && _queueService.Items.Count > 0
            ? $"{QueueCurrentIndex + 1} / {_queueService.Items.Count}"
            : "";

    public Song? NowPlayingItem =>
        QueueCurrentIndex >= 0 && QueueCurrentIndex < Queue.Count
            ? Queue[QueueCurrentIndex]
            : null;

    public bool IsQueueEmpty => Queue.Count == 0;

    public bool IsNowPlayingVisible => NowPlayingItem is not null;

    public IRelayCommand ToggleShuffleCommand { get; }
    public IRelayCommand CycleRepeatModeCommand { get; }
    public IRelayCommand ToggleRadioCommand { get; }

    private void SyncQueueFromService()
    {
        Queue.Clear();
        UpNextItems.Clear();
        RadioItems.Clear();

        foreach (var song in _queueService.Items) Queue.Add(song);

        var current = _queueService.Current;
        if (current is not null)
        {
            var idx = Queue.Select((s, i) => (s, i)).FirstOrDefault(x => x.s.Id == current.Id).i;
            QueueCurrentIndex = idx;

            var userCount = _queueService.UserItemCount;

            if (idx < userCount)
            {
                // Current is in user queue
                for (int i = idx + 1; i < userCount && i < Queue.Count; i++)
                {
                    UpNextItems.Add(Queue[i]);
                }
                for (int i = userCount; i < Queue.Count; i++)
                {
                    RadioItems.Add(Queue[i]);
                }
            }
            else
            {
                // Current is in radio queue
                for (int i = idx + 1; i < Queue.Count; i++)
                {
                    RadioItems.Add(Queue[i]);
                }
            }
        }
        else
        {
            QueueCurrentIndex = -1;
            // No current playing; show all radio items
            var userCount = _queueService.UserItemCount;
            for (int i = userCount; i < Queue.Count; i++)
            {
                RadioItems.Add(Queue[i]);
            }
        }

        OnPropertyChanged(nameof(QueueIndexText));
    }

    private void NotifyQueuePropertiesChanged()
    {
        OnPropertyChanged(nameof(QueueCurrentIndex));
        OnPropertyChanged(nameof(QueueIndexText));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(ShuffleEnabled));
        OnPropertyChanged(nameof(RepeatMode));
        OnPropertyChanged(nameof(RepeatModeText));
        OnPropertyChanged(nameof(ShuffleText));
        OnPropertyChanged(nameof(RadioEnabled));
        OnPropertyChanged(nameof(RadioText));
        OnPropertyChanged(nameof(IsRadioActive));
        OnPropertyChanged(nameof(HasRadioItems));
        OnPropertyChanged(nameof(NowPlayingItem));
        OnPropertyChanged(nameof(IsQueueEmpty));
        OnPropertyChanged(nameof(IsNowPlayingVisible));
    }

    private void ToggleShuffle()
    {
        _queueService.ToggleShuffle();
        NotifyQueuePropertiesChanged();
    }

    private void CycleRepeatMode()
    {
        _queueService.CycleRepeatMode();
        NotifyQueuePropertiesChanged();
    }

    private void ToggleRadio()
    {
        _queueService.ToggleRadio();
        NotifyQueuePropertiesChanged();
    }
}
