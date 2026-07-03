using System;
using System.Net.Http;
using App.Dev;
using App.Services;
using App.ViewModels;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using InnerTube;
using InnerTube.Search;
using Sori.Core.Interfaces;

namespace App;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var useInnerTube = true;

            ISearchService searchService;

            if (useInnerTube)
            {
                Console.WriteLine("InnerTubeSearchService created");
                var innerTubeOptions = new InnerTubeOptions();

                var innerTubeClient = new InnerTubeClient(
                    new HttpClient(),
                    innerTubeOptions);

                var contextFactory = new InnerTubeContextFactory(innerTubeOptions);
                var searchMapper = new SearchMapper();

                searchService = new InnerTubeSearchService(
                    innerTubeClient,
                    contextFactory,
                    searchMapper);
            }
            else
            {
                searchService = new MockMusicClient();
            }

            IPlaybackService playbackService = new MockPlaybackService();
            IQueueService queueService = new QueueService();

            var viewModel = new MainWindowViewModel(searchService, playbackService, queueService);
            desktop.MainWindow = new MainWindow { DataContext = viewModel };
        }

        base.OnFrameworkInitializationCompleted();
    }
}