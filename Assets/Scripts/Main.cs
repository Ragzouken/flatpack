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
    public GraphicRaycaster viewerRaycaster;
    [SerializeField]
    public GraphicRaycaster creatorRayster;

    [SerializeField]
    private UIDragControl layerDrag;
    [SerializeField]
    private GraphicBrowserPanel graphicsBrowser;

    [SerializeField]
    private Slider loadingSlider;

    [SerializeField]
    private InstancePoolSetup graphicsSetup;
    private InstancePool<FlatGraphic> graphics;

    private List<string> resourcePaths = new List<string>();
    private List<WWW> resourceLoads = new List<WWW>();
    public Dictionary<string, ImageResource> resources = new Dictionary<string, ImageResource>();

    public FlatScene scene;

    private FlatGraphic cameraControl;

    private float prevDepth;

    private void Awake()
    {
        graphics = graphicsSetup.Finalise<FlatGraphic>();

        layerDrag.OnBegin += () => prevDepth = selected.depth;
        layerDrag.OnDrag += displacement => selected.depth = prevDepth + displacement.y * 0.01f;
    }

    private void Start()
    {
        StartCoroutine(LoadResources());
    }

    private void Update()
    {
        scene.graphics.Sort((a, b) => a.depth.CompareTo(b.depth));

        graphics.SetActive(scene.graphics);
        graphics.Refresh();

        if (selected != null)
        {
            CheckTouchTransform();
        }

        CheckTouchSelect();
    }

    private IEnumerator LoadResources()
    {
        FindResources("/storage/emulated/0/Download/");
        FindResources("/storage/emulated/0/DCIM/");
        FindResources("/storage/emulated/0/Pictures/");
        FindResources("C:/Users/mark/Pictures/kooltool aesthetics");

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
        catch (DirectoryNotFoundException)
        {
            Debug.LogFormat("Couldn't find \"{0}\"", root);
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

    public void CreateGraphic(ImageResource resource)
    {
        var graphic = scene.AddNewGraphic(resource.path);
        graphic.position = new Vector2(Camera.main.pixelWidth,
                                       Camera.main.pixelHeight) * 0.5f;

        Select(graphic);

        graphics.SetActive(scene.graphics);
        graphics.Refresh();
    }

    #region Selection

    public FlatGraphic selected { get; private set; }

    public void Select(FlatGraphic graphic)
    {
        selected = graphic;
    }

    public void Deselect()
    {
        selected = null;
    }

    public void DeleteSelected()
    {
        Assert.IsTrue(selected != null, "Deleting with nothing selected!");

        scene.RemoveGraphic(selected);

        Deselect();
    }

    #endregion

    #region Touch Controls

    private bool tapping;
    private Vector2 tapPosition;
    private float holdTime = 0;

    private void CheckTouchSelect()
    {
        // if there's a single touch just beginning, and it is not blocked
        // by the ui, this is the start of a tap
        if (Input.touchCount == 1 
         && Input.GetTouch(0).phase == TouchPhase.Began
         && !creatorRayster.IsPointBlocked(Input.GetTouch(0).position))
        {
            tapping = true;
            holdTime = 0;
            tapPosition = Input.GetTouch(0).position;
        }

        // track how long we have been holding the press
        if (tapping)
        {
            holdTime += Time.deltaTime;
        }

        // if it's too long, it's not a tap
        if (holdTime > .5f)
        {
            tapping = false;
        }

        // if we are tapping, and the tap touch is ending
        if (Input.touchCount > 0 
         && Input.GetTouch(0).phase == TouchPhase.Ended
         && tapping)
        {
            // check if the touch moved too much to be considered a tap
            float delta = (Input.GetTouch(0).position - tapPosition).magnitude;

            if (delta < 5f)
            {
                var prev = selected;

                Deselect();
                
                var hits = viewerRaycaster.Raycast(tapPosition);
                var hit = hits.Select(h => h.gameObject.GetComponent<GraphicView>())
                              .OfType<GraphicView>()
                              .FirstOrDefault();

                if (hit != null)
                {
                    if (hit.config == selected)
                    {
                        Deselect();
                    }
                    else
                    {
                        Select(hit.config);
                    }
                }
            }

            tapping = false;
        }
    }

    private bool oneFinger;
    private bool twoFinger;
    private Vector2 prevTouch1, prevTouch2;
    private float baseScale, baseAngle;
    private Vector2 basePosition;

    private void ResetGestures()
    {
        oneFinger = false;
        twoFinger = false;
    }

    private void CheckTouchTransform()
    {
        Vector2 nextTouch1 = Vector2.zero;
        Vector2 nextTouch2 = Vector2.zero;

        bool mouse = false;

        if (Input.touchCount > 0)
        {
            nextTouch1 = Input.GetTouch(0).position;
        }
        else if (Input.GetMouseButton(0))
        {
            nextTouch1 = Input.mousePosition;
            mouse = true;
        }

        if (Input.touchCount > 1)
        {
            nextTouch2 = Input.GetTouch(1).position;
        }

        bool blocked1 = creatorRayster.IsPointBlocked(nextTouch1);
        bool blocked2 = creatorRayster.IsPointBlocked(nextTouch2);

        bool touch1Begin = Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began;
        bool touch2Begin = Input.touchCount == 2 && Input.GetTouch(1).phase == TouchPhase.Began;

        bool touch1Move = Input.touchCount == 1;

        if ((touch1Begin || mouse) && !oneFinger && !blocked1)
        {
            prevTouch1 = nextTouch1;
            basePosition = selected.position;

            oneFinger = true;
        }
        else if ((touch1Move || mouse) && oneFinger)
        {
            selected.position = basePosition - prevTouch1 + nextTouch1;
        }
        else
        {
            oneFinger = false;
        }

        if (touch2Begin && !twoFinger && !blocked2)
        {
            twoFinger = true;

            prevTouch1 = nextTouch1;
            prevTouch2 = nextTouch2;

            baseScale = selected.scale;
            baseAngle = selected.direction;
            basePosition = selected.position;
        }
        else if (Input.touchCount == 2 && twoFinger)
        {
            Vector2 a = prevTouch1 - basePosition;
            Vector2 b = prevTouch2 - basePosition;
            Vector2 c = prevTouch2 - prevTouch1;

            float prevD = (prevTouch2 - prevTouch1).magnitude;
            float nextD = (nextTouch2 - nextTouch1).magnitude;
            float scaleMult = nextD / prevD;

            float prevAngle = Angle(c);
            float nextAngle = Angle(nextTouch2 - nextTouch1);
            float deltaAngle = Mathf.DeltaAngle(prevAngle, nextAngle);

            Vector2 nexta = Rotate(a * scaleMult, deltaAngle);
            Vector2 nextO = nextTouch1 - nexta;

            selected.scale = Mathf.Max(0.1f, baseScale * scaleMult);
            selected.direction = baseAngle + deltaAngle;
            selected.position = nextO;
        }
        else
        {
            twoFinger = false;
        }
    }

    private static float Angle(Vector2 vector)
    {
        return Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
    }

    private static Vector2 Rotate(Vector2 vector, float angle)
    {
        float d = vector.magnitude;
        float a = Angle(vector);

        a += angle;
        a *= Mathf.Deg2Rad;

        return new Vector2(d * Mathf.Cos(a), d * Mathf.Sin(a));
    }

    #endregion
}
