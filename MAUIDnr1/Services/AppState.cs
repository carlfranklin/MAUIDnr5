#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
#endif

public static class AppState
{
    public static readonly int WindowWidth = 900;
    public static readonly int WindowHeight = 600;
#if WINDOWS
    public static Microsoft.UI.Windowing.AppWindow AppWindow { get; set; }
#endif

    // list of all playlists
    public static ObservableCollection<PlayList> PlayLists { get; set; }
            = new ObservableCollection<PlayList>();

    // currently selected playlist
    public static PlayList SelectedPlayList { get; set; }

    // path to the json file
    private static string playListJsonFile = "";

    // when true, and a playlist is selected, determines
    // whether or not to show all the playlist shows on main page.
    // otherwise, the current set of shows (filtered or not)
    // is shown.
    public static bool ShowPlayListOnly = false;

    // text shown in the "Show Playlist" toggle button.
    // see ToggleShowPlaylistOnly()
    public static string ShowPlayListOnlyText = "Show Playlist";

    // the set of shows shown on the main page
    public static ObservableCollection<Show> AllShows { get; set; }
        = new ObservableCollection<Show>();

    // the set of all show numbers used to get the next set
    // of shows when "Load More Shows" button is clicked
    public static List<int> ShowNumbers { get; set; } = new List<int>();

    // the last show number shown on the main page
    public static int LastShowNumber { get; set; }

    // backing field for the EpisodeFilter property on the 
    // main page
    public static string EpisodeFilter { get; set; } = "";

    // private fields used to save the state before replacing
    // the current view with the selected playlist shows.
    // see ToggleShowPlaylistOnly()
    private static string AllShowsBackupString { get; set; } = "";
    private static int LastShowNumberBackup;
    private static string BackupEpisodeFilter { get; set; } = "";

    // tells the UI (and therefore the ApiService) whether or
    // not we are online. 
    // Call AppState.GetOnlineStatus(); to set it
    public static bool IsOnline;

    /// <summary>
    /// Used to display either "Add" or "Remove" buttons
    /// for playlist shows.
    /// </summary>
    /// <param name="show"></param>
    /// <returns></returns>
    public static bool SelectedPlayListContainsShow(Show show)
    {
        var match = (from x in SelectedPlayList.Shows
                     where x.Id == show.Id
                     select x).FirstOrDefault();

        return (match != null);
    }

    /// <summary>
    /// Add every show in AllShows to the selected playlist
    /// </summary>
    public static void AddAllToPlaylist()
    {
        if (SelectedPlayList == null || ShowPlayListOnly == true) return;
        foreach (var show in AllShows)
        {
            if (!SelectedPlayListContainsShow(show))
            {
                SelectedPlayList.Shows.Add(show);
            }
        }
        SavePlaylists();
    }

    /// <summary>
    /// Remove every show in AllShows from the selected playlsit
    /// </summary>
    public static void RemoveAllFromPlaylist()
    {
        if (SelectedPlayList == null || ShowPlayListOnly == true) return;
        foreach (var show in AllShows)
        {
            if (SelectedPlayListContainsShow(show))
            {
                RemoveShowFromPlaylist(show);
            }
        }
    }

    /// <summary>
    /// Add a show to the selected playlist
    /// </summary>
    /// <param name="show"></param>
    public static void AddShowToPlaylist(Show show)
    {
        if (SelectedPlayList == null) return;
        SelectedPlayList.Shows.Add(show);
        SavePlaylists();
    }

    /// <summary>
    /// Remove a show from the selected playlist
    /// </summary>
    /// <param name="show"></param>
    public static void RemoveShowFromPlaylist(Show show)
    {
        if (SelectedPlayList == null) return;
        // the show objects may not be the same, so select by Id
        var match = (from x in SelectedPlayList.Shows
                     where x.Id == show.Id
                     select x).FirstOrDefault();
        if (match != null)
        {
            SelectedPlayList.Shows.Remove(match);
            SavePlaylists();
        }
    }

    /// <summary>
    /// Switches the list of shows in Main page to/from
    /// the shows in the selected playlist
    /// </summary>
    public static void ToggleShowPlaylistOnly()
    {
        // Toggle
        ShowPlayListOnly = !ShowPlayListOnly;

        if (ShowPlayListOnly)
        {
            // Save the current state
            BackupEpisodeFilter = EpisodeFilter;    // filter
            AllShowsBackupString = JsonConvert.SerializeObject(AllShows);   // shows
            LastShowNumberBackup = LastShowNumber;  // last show number
                                                    // clear the filter
            EpisodeFilter = "";
            // change the set of displayed shows
            AllShows = new ObservableCollection<Show>(SelectedPlayList.Shows);
            // change the last show number
            LastShowNumber = 0;
            // change the button text
            ShowPlayListOnlyText = "Show All";
        }
        else
        {
            // restore state from backup values
            EpisodeFilter = BackupEpisodeFilter;
            AllShows = JsonConvert.DeserializeObject<ObservableCollection<Show>>(AllShowsBackupString);
            LastShowNumber = LastShowNumberBackup;
            // change the button text
            ShowPlayListOnlyText = "Show Playlist";
        }
    }

    /// <summary>
    /// Load the list of playlists from local Json file
    /// </summary>
    public static void LoadPlaylists()
    {
        string cacheDir = FileSystem.Current.CacheDirectory;
        playListJsonFile = $"{cacheDir}\\playlists.json";
        try
        {
            if (System.IO.File.Exists(playListJsonFile))
            {
                string json = System.IO.File.ReadAllText(playListJsonFile);
                var list = JsonConvert.DeserializeObject<List<PlayList>>(json);
                PlayLists = new ObservableCollection<PlayList>(list);
            }
        }
        catch (Exception ex)
        {

        }
    }

    /// <summary>
    /// Save list of playlists to local Json file
    /// </summary>
    public static void SavePlaylists()
    {
        if (playListJsonFile == "")
            LoadPlaylists();

        var list = PlayLists.ToList();
        try
        {
            var json = JsonConvert.SerializeObject(list);
            System.IO.File.WriteAllText(playListJsonFile, json);
        }
        catch (Exception ex)
        {

        }
    }

    /// <summary>
    /// We have to access Connectivity.Current.NetworkAccess
    /// on the main UI thread.
    /// </summary>
    public static void GetOnlineStatus()
    {
        if (MainThread.IsMainThread)
            IsOnline = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        else
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsOnline = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
            });
    }
}