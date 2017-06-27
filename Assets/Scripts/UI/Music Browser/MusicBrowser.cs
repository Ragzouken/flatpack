using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;
using System.IO;

public class MusicBrowser : MonoBehaviour 
{
    [SerializeField]
    private Main main;

    [SerializeField]
    private ScrollRect scroll;
    [SerializeField]
    private InstancePoolSetup thumbnailsSetup;
    private InstancePool<string> thumbnails;

    public string Selected { get; private set; }

    private void Awake()
    {
        Initialise();
    }

    private void Initialise()
    {
        if (thumbnails == null)
        {
            thumbnails = thumbnailsSetup.Finalise<string>();
        }
    }

    public static IEnumerable<string> GetMusicListing()
    {
#if UNITY_WEBGL
        yield break;
#else
        string folder = "/storage/emulated/0/Download/";

        var oggs = Directory.GetFiles(folder, "*.ogg");
        var wavs = Directory.GetFiles(folder, "*.mp3");

        return oggs.Concat(wavs).Select(file => Path.GetFileName(file));
#endif
    }

    public void OnEnable()
    {
        Refresh();
    }

    public void OnDisable()
    {
        main.StopMusic();
        main.story.musicID = Selected;
    }

    public void Refresh()
    {
        Initialise();

        var ids = GetMusicListing().ToList();

        thumbnails.SetActive(ids);

        if (!ids.Contains(Selected))
        {
            Selected = null;
        }

        Choose(main.story.musicID);
    }

    public void Choose(string id, bool toggle = false)
    {
        if (Selected == id && toggle)
        {
            Selected = null;
        }
        else
        {
            Selected = id;
        }

        thumbnails.Refresh();

        if (!string.IsNullOrEmpty(Selected))
        {
            main.PlayMusic(main.GetMusicPath(Selected));
        }
        else
        {
            main.StopMusic();
        }
    }
}
