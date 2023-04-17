namespace MAUIDnr1.Pages;
public partial class Index : ComponentBase
{
    [Inject]
    private NavigationManager _navigationManager { get; set; }

    [Inject]
    private ApiService _apiService { get; set; }

    // we read 20 records at a time when loading more shows
    private int RecordsToRead { get; set; } = 20;

    protected string StatusMessage {get; set;} = string.Empty;

    protected bool Downloading = false;

    /// <summary>
    /// EpisodeFilter needs to be defined here in Index
    /// because it needs to call StateHasChanged after
    /// being set. We are using the AppState.EpisodeFilter
    /// string property as a backing field, so the state
    /// of the property will be maintained between navigations.
    /// </summary>
    protected string EpisodeFilter
    {
        get => AppState.EpisodeFilter;
        set
        {
            if (AppState.EpisodeFilter != value)
            {
                // Make sure we are showing all the shows
                AppState.ShowPlayListOnly = false;
                AppState.ShowPlayListOnlyText = "Show Playlist";

                AppState.EpisodeFilter = value;
                AppState.LastShowNumber = 0;
                AppState.AllShows.Clear();
                try
                {
                    // Are we online?
                    AppState.GetOnlineStatus();

                    // Call GetNextBatchOfShows asynchronously
                    var t = Task.Run(() => GetNextBatchOfShows());
                    t.Wait();

                    // Update the UI
                    StateHasChanged();
                }
                catch { }
            }
        }
    }

    public bool IsDownloaded(Show show)
    {
        // exit if there is no url specified
        if (!string.IsNullOrEmpty(show.Mp3Url))
        {
            // This is where we are storing local audio files
            string cacheDir = FileSystem.Current.CacheDirectory;

            // get the fully qualified path to the local file
            var fileName = show.Mp3Url.Substring(8).Replace("/", "-");
            var localFile = $"{cacheDir}\\{fileName}";

            return System.IO.File.Exists(localFile);
        }
        else
            return false;
    }

    public int PlaylistEpisodesNotDownloaded
    {
        get
        {
            if (AppState.SelectedPlayList == null) 
                return 0;
            var count = 0;
            foreach (var show in AppState.SelectedPlayList.Shows)
            {
                if (!IsDownloaded(show))
                {
                    count++;
                }
            }
            return count;
        }
    }

    public async Task DownloadPlaylist()
    {
        if (!AppState.IsOnline) return;
        if (AppState.SelectedPlayList == null) return;
        Downloading = true;

        // This is where we are storing local audio files
        string cacheDir = FileSystem.Current.CacheDirectory;
        int count = 0;
        int total = PlaylistEpisodesNotDownloaded;
        foreach (var show in AppState.SelectedPlayList.Shows)
        {
            if (!IsDownloaded(show))
            {
                // download to cache
                count++;
                StatusMessage = $"Downloading {count} of {total}";
                await InvokeAsync(StateHasChanged);

                // Download the show with details so the data is cached
                var thisShow = await _apiService.GetShowWithDetails(show.ShowNumber);

                // get the fully qualified path to the local file
                var fileName = show.Mp3Url.Substring(8).Replace("/", "-");
                var localFile = $"{cacheDir}\\{fileName}";

                // this code downloads the file from the URL
                using (var client = new HttpClient())
                {
                    var uri = new Uri(show.Mp3Url);
                    var response = await client.GetAsync(show.Mp3Url);
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        var fileInfo = new FileInfo(localFile);
                        using (var fileStream = fileInfo.OpenWrite())
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
                }
            }
        }
        StatusMessage = "";
        Downloading = false;
    }

    protected void PlaySelectedPlayList()
    {
        var url = $"details/playlist";
        _navigationManager.NavigateTo(url);
    }


    /// <summary>
    /// Disable the reset (filter) button if the
    /// EpisodeFilter property is not set
    /// </summary>
    protected bool ResetButtonDisabled
    {
        get => (EpisodeFilter == "");
    }

    /// <summary>
    /// Reset button was selected
    /// </summary>
    protected void ResetFilter()
    {
        // Make sure we are showing all the shows
        AppState.ShowPlayListOnly = false;
        AppState.ShowPlayListOnlyText = "Show Playlist";

        // Clear the show data and episode filter
        AppState.ShowNumbers.Clear();
        AppState.AllShows.Clear();
        EpisodeFilter = "";
    }

    /// <summary>
    /// Adds to the existing list of shows (AllShows) based on the filter
    /// </summary>
    /// <returns></returns>
    protected async Task GetNextBatchOfFilteredShows()
    {
        // get the next batch
        var nextBatch = await
            _apiService.GetFilteredShows(EpisodeFilter,
                AppState.AllShows.Count, RecordsToRead);

        // bail if we didn't return any
        if (nextBatch == null || nextBatch.Count == 0) return;

        // Add them to the list.
        // NOTE: ObservableCollection<> does not implement AddRange
        foreach (var show in nextBatch)
        {
            AppState.AllShows.Add(show);
        }
    }

    /// <summary>
    /// "Load More Shows" button was selected
    /// </summary>
    /// <returns></returns>
    protected async Task LoadMoreShows()
    {
        AppState.GetOnlineStatus();
        await GetNextBatchOfShows();
    }

    /// <summary>
    /// Filtered or not, get the next batch
    /// </summary>
    /// <returns></returns>
    protected async Task GetNextBatchOfShows()
    {
        // Filter?
        if (EpisodeFilter != "")
        {
            // Defer to GetNextBatchOfFilteredShows()
            await GetNextBatchOfFilteredShows();
            return;
        }

        // No shows loaded?
        if (AppState.ShowNumbers.Count == 0)
        {
            // Get the shownumbers
            AppState.ShowNumbers = await _apiService.GetShowNumbers();
            // return if there are none
            if (AppState.ShowNumbers == null || AppState.ShowNumbers.Count == 0) return;
            // Set the last show number
            AppState.LastShowNumber = AppState.ShowNumbers.First<int>() + 1;
        }

        // At this point, we have no filter AND we have show numbers.

        // create the request
        var request = new GetByShowNumbersRequest()
        {
            ShowName = "dotnetrocks",
            Indexes = (from x in AppState.ShowNumbers
                       where x < AppState.LastShowNumber
                       && x >= (AppState.LastShowNumber - RecordsToRead)
                       select x).ToList()
        };

        // get the next batch
        var nextBatch = await _apiService.GetByShowNumbers(request);

        // bail if nothing is returned
        if (nextBatch == null || nextBatch.Count == 0) return;

        // Add to AllShows.
        // NOTE: ObservableCollection<> does NOT implement AddRange
        foreach (var show in nextBatch)
        {
            AppState.AllShows.Add(show);
        }

        // Set the LastShowNumber
        AppState.LastShowNumber = nextBatch.Last<Show>().ShowNumber;
    }

    /// <summary>
    /// Show the details
    /// </summary>
    /// <param name="ShowNumber"></param>
    protected void NavigateToDetailPage(int ShowNumber)
    {
        var url = $"details/{ShowNumber}";
        _navigationManager.NavigateTo(url);
    }

    /// <summary>
    /// Show the playlist page
    /// </summary>
    protected void NavigateToPlayListPage()
    {
        _navigationManager.NavigateTo("playlists");
    }


    /// <summary>
    /// Only load playlists and get next batch of shows
    /// if there are no shows loaded yet.
    /// </summary>
    /// <returns></returns>
    protected override async Task OnInitializedAsync()
    {
        if (AppState.AllShows.Count == 0)
        {
            AppState.LoadPlaylists();
            AppState.GetOnlineStatus();
            await GetNextBatchOfShows();
        }
    }
}