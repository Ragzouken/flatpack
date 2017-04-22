﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class GraphicBrowserPanel : MonoBehaviour 
{
    [SerializeField]
    private Main main;

    [SerializeField]
    private InstancePoolSetup thumbnailsSetup;
    private InstancePool<ImageResource> thumbnails;

    private void Awake()
    {
        thumbnails = thumbnailsSetup.Finalise<ImageResource>();
    }

    public void OnEnable()
    {
        thumbnails.SetActive(main.resources.Values);
    }

    public void Choose(ImageResource resource)
    {
        main.CreateGraphic(resource);
        gameObject.SetActive(false);
    }
}