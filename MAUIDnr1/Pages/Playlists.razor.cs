namespace MAUIDnr1.Pages;
public partial class Playlists : ComponentBase
{
    [Inject]
    private NavigationManager _navigationManager { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    // are we adding, editing, or neither?
    protected PlaylistEditAction PlaylistEditAction { get; set; }

    // used to disable the command buttons if we're adding or editing
    protected bool CommandButtonsDisabled =>
        PlaylistEditAction != PlaylistEditAction.None;

    // This is the PlayList object we use to add or edit
    protected PlayList PlayListToAddOrEdit;

    /// <summary>
    /// Called from the UI to move a show up in the playlist order
    /// </summary>
    /// <param name="show"></param>
    protected void MoveUp(Show show)
    {
        var index = PlayListToAddOrEdit.Shows.IndexOf(show);
        PlayListToAddOrEdit.Shows.RemoveAt(index);
        PlayListToAddOrEdit.Shows.Insert(index - 1, show);
    }

    /// <summary>
    /// Called from the UI to move a show down in the playlist order
    /// </summary>
    /// <param name="show"></param>
    protected void MoveDown(Show show)
    {
        var index = PlayListToAddOrEdit.Shows.IndexOf(show);
        PlayListToAddOrEdit.Shows.RemoveAt(index);
        PlayListToAddOrEdit.Shows.Insert(index + 1, show);
    }

    /// <summary>
    /// Go back
    /// </summary>
    protected void NavigateHome()
    {
        _navigationManager.NavigateTo("/");
    }

    /// <summary>
    /// Set the selected playlist when selected from the <select> element
    /// </summary>
    /// <param name="args"></param>
    protected async Task PlayListSelected(ChangeEventArgs args)
    {
        AppState.SelectedPlayList = (from x in AppState.PlayLists
                                     where x.Id.ToString() == args.Value.ToString()
                                     select x).FirstOrDefault();
        if (AppState.ShowPlayListOnly)
        {
            AppState.ToggleShowPlaylistOnly();
        }
    }

    /// <summary>
    /// Because PlayListSelected won't fire when there is only one item in the list
    /// </summary>
    protected async Task PlayListsClicked()
    {
        if (AppState.PlayLists.Count == 1)
        {
            AppState.SelectedPlayList = AppState.PlayLists.First();
            if (AppState.ShowPlayListOnly)
            {
                AppState.ToggleShowPlaylistOnly();
            }
        }
    }

    /// <summary>
    /// Add a PlayList
    /// </summary>
    protected async Task AddButtonClicked()
    {
        // Create a new PlayList
        PlayListToAddOrEdit = new PlayList();
        PlayListToAddOrEdit.Id = PlayList.CreateGuid(); // don't forget this!
        PlayListToAddOrEdit.DateCreated = DateTime.Now;
        PlaylistEditAction = PlaylistEditAction.Adding;
        await JSRuntime.InvokeVoidAsync("SetFocus", "InputName");
    }

    /// <summary>
    /// Edit the SelectedPlayList
    /// </summary>
    protected async Task EditButtonClicked()
    {
        // Clone it, so we don't clobber it accidentally.
        PlayListToAddOrEdit = (PlayList)AppState.SelectedPlayList.Clone();
        PlaylistEditAction = PlaylistEditAction.Editing;
        await JSRuntime.InvokeVoidAsync("SetFocus", "InputName");
    }

    /// <summary>
    /// Easy Peasy
    /// </summary>
    protected void DeleteButtonClicked()
    {
        AppState.PlayLists.Remove(AppState.SelectedPlayList);
        AppState.SavePlaylists();
        AppState.SelectedPlayList = null;
        PlaylistEditAction = PlaylistEditAction.None;
        if (AppState.ShowPlayListOnly)
        {
            AppState.ToggleShowPlaylistOnly();
        }
    }

    /// <summary>
    /// Commit the Add or Edit action
    /// </summary>
    protected void SubmitPlayListClicked()
    {
        if (PlaylistEditAction == PlaylistEditAction.Adding)
        {
            // Simply add the new PlayList.
            AppState.PlayLists.Add(PlayListToAddOrEdit);
            // Select it
            int index = AppState.PlayLists.IndexOf(PlayListToAddOrEdit);
            AppState.SelectedPlayList = AppState.PlayLists[index];
        }
        else if (PlaylistEditAction == PlaylistEditAction.Editing)
        {
            // Get the index of the selected play list
            int index = AppState.PlayLists.IndexOf(AppState.SelectedPlayList);
            // Replace it in the list
            AppState.PlayLists[index] = PlayListToAddOrEdit;
            // Get the new object reference
            AppState.SelectedPlayList = AppState.PlayLists[index];
        }
        // Save the data!
        AppState.SavePlaylists();
        PlaylistEditAction = PlaylistEditAction.None;
    }

    /// <summary>
    /// Easy Peasy
    /// </summary>
    protected void CancelButtonPressed()
    {
        PlayListToAddOrEdit = null;
        PlaylistEditAction = PlaylistEditAction.None;
    }
}