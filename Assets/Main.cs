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
    [SerializeField]
    private InstancePoolSetup resourceThumbsSetup;
    private InstancePool<ImageResource> resourceThumbs;

    [SerializeField]
    private InstancePoolSetup graphicsSetup;
    private InstancePool<FlatGraphic> graphics;

    private List<string> resourcePaths = new List<string>();
    private List<WWW> resourceLoads = new List<WWW>();
    private Dictionary<string, ImageResource> resources = new Dictionary<string, ImageResource>();

    public FlatScene scene;

    private void Awake()
    {
        resourceThumbs = resourceThumbsSetup.Finalise<ImageResource>();
        graphics = graphicsSetup.Finalise<FlatGraphic>(sort: false);
    }

    private void Start()
    {
        StartCoroutine(LoadResources());
    }

    private void Update()
    {
        
    }

    private IEnumerator LoadResources()
    {
        FindResources("/storage/emulated/0/Download/");
        FindResources("/storage/emulated/0/DCIM/");
        FindResources("/storage/emulated/0/Pictures/");

        loadingSlider.maxValue = resourcePaths.Count;

        resourceLoads.Clear();
        resourceLoads.AddRange(resourcePaths.Select(path => new WWW("file://" + path)));

        foreach (var load in resourceLoads)
        {
            yield return load;

            try
            {
                var texture = load.texture;
                var resource = new ImageResource
                {
                    name = Path.GetFileNameWithoutExtension(load.url),
                    path = load.url,
                    sprite = Sprite.Create(texture,
                                           new Rect(0, 0, texture.width, texture.height),
                                           Vector2.zero,
                                           100,
                                           0,
                                           SpriteMeshType.FullRect),
                };

                resources.Add(load.url, resource);
                resourceThumbs.SetActive(resources.Values);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            loadingSlider.value += 1;
        }

        resourceLoads.Clear();

        graphics.SetActive(scene.graphics);
    }

    private void FindResources(string root)
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
    }

    public ImageResource GetImageResource(string uri)
    {
        ImageResource resource;

        if (!resources.TryGetValue(uri, out resource))
        {
            resource = resources.First().Value;
        }

        return resource;
    }
}
