using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class Story
{
    public string name;
    public string path;
}

public class StoryThumbnail : InstanceView<Story>
{
    [SerializeField]
    private StoryBrowser browser;

    [SerializeField]
    private Text titleText;
    [SerializeField]
    private Button openButton;

    private void Awake()
    {
        openButton.onClick.AddListener(() => browser.OpenStory(config));
    }

    public override void Refresh()
    {
        titleText.text = config.name;
    }
}
