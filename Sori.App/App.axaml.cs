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
using InnerTube.Home;
using InnerTube.Search;
using Sori.Core.Interfaces;
using Sori.Playback;

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
            IHomeService homeService;

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

                homeService = new InnerTubeHomeService(
                    innerTubeClient,
                    contextFactory,
                    new HomeMapper());
            }
            else
            {
                searchService = new MockMusicClient();
                collectionService = new MockCollectionService();
                homeService = new MockHomeService();
            }

            IQueueService queueService = new QueueService();

            IAudioPlaybackService audioPlaybackService = new VlcAudioPlaybackService();
            var youtubeResolver = new YoutubeExplodePlaybackResolver();
            var prefetchingResolver = new PrefetchingPlaybackResolver(youtubeResolver);

            IPlaybackResolver playbackResolver = prefetchingResolver;
            IPrefetchingPlaybackResolver prefetchResolver = prefetchingResolver;

            IPlaybackCoordinator playbackCoordinator = new PlaybackCoordinator(
                playbackResolver,
                audioPlaybackService,
                queueService,
                prefetchResolver);

            var viewModel = new MainWindowViewModel(
                searchService,
                queueService,
                collectionService,
                homeService,
                playbackCoordinator,
                prefetchResolver);
            desktop.MainWindow = new MainWindow { DataContext = viewModel };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
