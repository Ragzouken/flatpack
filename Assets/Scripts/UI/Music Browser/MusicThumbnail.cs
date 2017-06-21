using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class MusicThumbnail : InstanceView<string> 
{
    [SerializeField]
    private MusicBrowser browser;
    [SerializeField]
    private Text label;
    [SerializeField]
    private Button button;
    [SerializeField]
    private GameObject activeObject;

    private void Awake()
    {
        button.onClick.AddListener(() => browser.Choose(config, toggle: true));
    }

    public override void Refresh()
    {
        label.text = config;
        activeObject.SetActive(browser.Selected == config);
    }
}
