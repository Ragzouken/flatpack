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
    private InstancePool<Story> thumbnails;

    private void Awake()
    {
        thumbnails = thumbnailsSetup.Finalise<Story>();
    }

    private void Start()
    {
        thumbnails.SetActive(new Story { name = "je suis un ananas" },
                             new Story { name = "un ananas ne parle pas" },
                             new Story { name = "incroyable, c'est pas possible!" },
                             new Story { name = "c'est possible" },
                             new Story { name = "musiksagen vom brandonhugel" },
                             new Story { name = "somewhere i return" });
    }

    public void OpenStory(Story story)
    {
        gameObject.SetActive(false);
    }
}
