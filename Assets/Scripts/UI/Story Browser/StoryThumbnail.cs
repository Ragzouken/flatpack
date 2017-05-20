using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class StoryThumbnail : InstanceView<FlatBlurb>
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
        titleText.text = string.Format("{0}\n<size=42>({1} pictures)</size>", config.name, config.graphics);
    }
}
