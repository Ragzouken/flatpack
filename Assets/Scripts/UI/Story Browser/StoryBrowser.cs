using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class StoryBrowser : MonoBehaviour 
{
    [SerializeField]
    private Main main;

    [Header("Selected Story")]
    [SerializeField]
    private Text titleText;
    [SerializeField]
    private Text descriptionText;
    [SerializeField]
    private CanvasGroup buttonGroup;

    [Header("Browse")]
    [SerializeField]
    private CanvasGroup fadeStoriesGroup;
    [SerializeField]
    private InstancePoolSetup thumbnailsSetup;
    private InstancePool<FlatBlurb> thumbnails;

    public FlatBlurb Selected { get; private set; }

    private void Awake()
    {
        thumbnails = thumbnailsSetup.Finalise<FlatBlurb>();
    }

    private void OnEnable()
    {
        Saves.RefreshBlurbs();

        Refresh();

        SelectStory(null);
    }
    
    private void Refresh()
    {
        var blurbs = Saves.blurbs.Values.OrderByDescending(blurb => blurb.modified).ToList();

        thumbnails.SetActive(blurbs);
        thumbnails.Refresh();
    }

    public void CreateStory()
    {

    }

    public void SelectStory(FlatBlurb blurb)
    {
        Selected = blurb;

        titleText.text = blurb != null ? blurb.name : "Select a story to edit or create a new one";

        fadeStoriesGroup.alpha = Selected != null ? 0.5f : 1f;
        buttonGroup.interactable = Selected != null;

        thumbnails.Refresh();
    }

    public void OpenSelected()
    {
        Assert.IsNotNull(Selected, "No story selected to open!");

        var story = Saves.LoadStory(Selected);

        main.SetStory(story);

        gameObject.SetActive(false);
    }

    public void ExportSelected()
    {
        Assert.IsNotNull(Selected, "No story selected to export!");

        var story = Saves.LoadStory(Selected);
        Saves.ExportStory(story);
    }

    public void CopySelected()
    {
        Assert.IsNotNull(Selected, "No story selected to copy!");

        var copy = Saves.CopyStory(Selected);
        
        Refresh();

        SelectStory(copy);
    }

    public void DeleteSelected()
    {
        Assert.IsNotNull(Selected, "No story selected to delete!");

        Saves.DeleteStory(Selected);

        SelectStory(null);
        Refresh();
    }
}
