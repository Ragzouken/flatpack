using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

using System.IO;

public class ImageResource
{
    public string name;
    public string path;
    public Sprite sprite;
}

public class Main : MonoBehaviour 
{
    [SerializeField]
    private Slider loadingSlider;

    private List<string> resourcePaths = new List<string>();
    private List<WWW> resourceLoads = new List<WWW>();
    private Dictionary<string, ImageResource> resources = new Dictionary<string, ImageResource>();

    private void Start()
    {
        StartCoroutine(LoadResources());
    }

    private IEnumerator LoadResources()
    {
        yield return StartCoroutine(FindResources("/storage/emulated/0/Download/"));
        yield return StartCoroutine(FindResources("/storage/emulated/0/DCIM/"));
        yield return StartCoroutine(FindResources("/storage/emulated/0/Pictures/"));

        loadingSlider.maxValue = resourcePaths.Count;

        resourceLoads.Clear();

        foreach (string path in resourcePaths)
        {
            yield return null;

            Debug.LogFormat("Queueing {0}", Path.GetFileNameWithoutExtension(path));

            var request = new WWW("file://" + path);

            resourceLoads.Add(request);
        }

        foreach (var load in resourceLoads)
        {
            yield return load;

            try
            {
                string baseName = Path.GetFileNameWithoutExtension(load.url);
                string trueName = baseName;

                int i = 1;

                while (resources.ContainsKey(trueName))
                {
                    trueName = baseName + "_" + i;
                }

                var texture = load.texture;
                var resource = new ImageResource
                {
                    name = trueName,
                    path = load.url,
                    sprite = Sprite.Create(texture,
                                           new Rect(0, 0, texture.width, texture.height),
                                           Vector2.zero,
                                           100,
                                           0,
                                           SpriteMeshType.FullRect),
                };

                resources.Add(trueName, resource);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            loadingSlider.value += 1;
        }

        resourceLoads.Clear();
    }

    private IEnumerator FindResources(string root)
    {
        try
        {
            string[] pngs = Directory.GetFiles(root, "*.png", SearchOption.AllDirectories);
            string[] jpgs = Directory.GetFiles(root, "*.jpg", SearchOption.AllDirectories);

            resourcePaths.AddRange(pngs);
            resourcePaths.AddRange(jpgs);
        }
        catch (DirectoryNotFoundException e)
        {
            Debug.LogFormat("Looked for \"{0}\"", root);
            Debug.LogException(e);
        }

        yield break;
    }
}
