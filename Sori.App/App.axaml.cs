using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using App.Dev;
using App.Services;
using App.ViewModels;
using Sori.Core.Interfaces;

namespace App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ISearchService searchService = new MockMusicClient();
            IPlaybackService playbackService = new MockPlaybackService();
            IQueueService queueService = new QueueService();

            var viewModel = new MainWindowViewModel(searchService, playbackService, queueService);
            desktop.MainWindow = new MainWindow { DataContext = viewModel };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
