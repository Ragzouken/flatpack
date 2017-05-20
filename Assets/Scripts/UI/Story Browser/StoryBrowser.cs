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

    [SerializeField]
    private InstancePoolSetup thumbnailsSetup;
    private InstancePool<FlatBlurb> thumbnails;

    private void Awake()
    {
        thumbnails = thumbnailsSetup.Finalise<FlatBlurb>();
    }

    private void OnEnable()
    {
        Saves.RefreshBlurbs();

        var blurbs = Saves.blurbs.Values.OrderByDescending(blurb => blurb.modified).ToList();

        thumbnails.SetActive(blurbs);
        thumbnails.Refresh();
    }

    public void CreateStory()
    {

    }

    public void OpenStory(FlatBlurb blurb)
    {
        var story = Saves.LoadStory(blurb);

        main.SetStory(story);

        gameObject.SetActive(false);
    }
}
