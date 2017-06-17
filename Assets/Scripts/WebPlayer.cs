using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class AutoPlay
{
    public string storyID;
}

public class WebPlayer : MonoBehaviour 
{
    [SerializeField]
    private Main main;
    [SerializeField]
    private GameObject exitButton;

    private IEnumerator Start()
    {
#if !UNITY_WEBGL
        yield break;
#endif

        var autoRequest = new WWW(Application.streamingAssetsPath + "/autoplay.json");

        yield return autoRequest;

        if (autoRequest.error != null)
        {
            Debug.LogFormat("Can't load autoplay: {0}", autoRequest.error);
            yield break;
        }

        var autoplay = JsonUtility.FromJson<AutoPlay>(autoRequest.text);
        var storyRequest = new WWW(Application.streamingAssetsPath + "/" + autoplay.storyID + "/story.json");

        yield return storyRequest;

        if (storyRequest.error != null)
        {
            Debug.LogFormat("Can't load story '{0}': {1}", autoplay.storyID, storyRequest.error);
            yield break;
        }

        var story = JsonUtility.FromJson<FlatStory>(storyRequest.text);
        story.blurb = new FlatBlurb { id = autoplay.storyID };

        exitButton.SetActive(false);

        yield return StartCoroutine(main.PlayStory(story)) ;
    }
}
