using System;
using System.Net.Http;
using App.Dev;
using App.Services;
using App.ViewModels;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using InnerTube;
using InnerTube.Browse;
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

            var innerTubeOptions = new InnerTubeOptions();
            var httpClient = new HttpClient();
            var innerTubeClient = new InnerTubeClient(httpClient, innerTubeOptions);
            var contextFactory = new InnerTubeContextFactory(innerTubeOptions);

            ISearchService searchService;
            ICollectionService collectionService;

            if (useInnerTube)
            {
                Console.WriteLine("InnerTube services created");
                searchService = new InnerTubeSearchService(
                    innerTubeClient,
                    contextFactory,
                    new SearchMapper());

                collectionService = new InnerTubeCollectionService(
                    innerTubeClient,
                    contextFactory,
                    new BrowseMapper());
            }
            else
            {
                searchService = new MockMusicClient();
                collectionService = new MockCollectionService();
            }

            IPlaybackService playbackService = new MockPlaybackService();
            IQueueService queueService = new QueueService();

            var viewModel = new MainWindowViewModel(
                searchService,
                playbackService,
                queueService,
                collectionService);
            desktop.MainWindow = new MainWindow { DataContext = viewModel };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
