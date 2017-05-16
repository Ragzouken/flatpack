using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

using System.IO;

public class GraphicSource
{
    public string path;
    public Coroutine thumbnailLoad;
    public Sprite thumbnail;
}

public class RawBrowserPanel : MonoBehaviour 
{
    [SerializeField]
    private Main main;

    [SerializeField]
    private RawImporterPanel importer;

    [SerializeField]
    private Sprite defaultThumbnail;

    [SerializeField]
    private InstancePoolSetup thumbnailsSetup;
    private InstancePool<GraphicSource> thumbnails;

    private Coroutine thumbnailLoad;
    private Sprite activeThumbnail;

    private void Awake()
    {
        thumbnails = thumbnailsSetup.Finalise<GraphicSource>();
    }

    private Dictionary<string, GraphicSource> sources 
        = new Dictionary<string, GraphicSource>();
    
    public IEnumerator DiscoverFile(string file)
    {
        if (!sources.ContainsKey(file))
        {
            var source = new GraphicSource
            {
                path = file,
                thumbnail = defaultThumbnail,
            };

            source.thumbnailLoad = main.StartCoroutine(LoadThumbnail(source));

            sources.Add(file, source);

            yield return source.thumbnailLoad;
        }
    }

    private IEnumerator LoadThumbnail(GraphicSource source)
    {
        var load = new WWW("file://" + source.path);

        yield return load;

        var texture = load.texture;

        TextureScale.PointMax(texture, 64, 64);

        source.thumbnail = Sprite.Create(texture,
                                         new Rect(0, 0, texture.width, texture.height),
                                         Vector2.zero,
                                         100,
                                         0,
                                         SpriteMeshType.FullRect);

        Refresh();
    }

    public IEnumerator Rediscover()
    {
        string root = "/storage/emulated/0/DCIM/";

        //try
        {
            string[] pngs = Directory.GetFiles(root, "*.png", SearchOption.AllDirectories);
            string[] jpgs = Directory.GetFiles(root, "*.jpg", SearchOption.AllDirectories);

            foreach (string file in jpgs)
            {
                yield return DiscoverFile(file);
            }
        }
        //catch (DirectoryNotFoundException)
        {
          //  Debug.LogFormat("Couldn't find \"{0}\"", root);
        }

        Refresh();
    }

    public void Refresh()
    {
        thumbnails.SetActive(sources.Values.Where(s => !main.resources.ContainsKey("file://" + s.path)));
        thumbnails.Refresh();
    }

    public void OnEnable()
    {
        main.StartCoroutine(Rediscover());
        Refresh();
    }

    public void Select(GraphicSource source)
    {
        thumbnails.Discard(source);
        //main.StartCoroutine(main.LoadFromSource(source));
        gameObject.SetActive(false);
        importer.gameObject.SetActive(true);
        importer.OpenSource(source);
    }
}
