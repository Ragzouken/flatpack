using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

using System.IO;

public class RawImage
{
    public string name;
    public string path;
    public Sprite sprite;
}

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
    private CanvasGroup layerGroup;
    [SerializeField]
    private GraphicBrowserPanel graphicsBrowser;

    [SerializeField]
    private Slider loadingSlider;

    [SerializeField]
    private InstancePoolSetup graphicsSetup;
    private InstancePool<FlatGraphic> graphics;

    [SerializeField]
    private InstancePoolSetup pinnedSetup;
    private InstancePool<FlatGraphic> pinned;

    private List<string> resourcePaths = new List<string>();
    private List<WWW> resourceLoads = new List<WWW>();
    public Dictionary<string, ImageResource> resources = new Dictionary<string, ImageResource>();

    [SerializeField]
    private List<string> streaming;

    public FlatScene scene;

    private FlatGraphic worldObject = new FlatGraphic { scale = 1 };
    [SerializeField]
    private Transform worldTransform;

    private float prevDepth;

    [SerializeField]
    private GameObject toolbar;
    [SerializeField]
    private GameObject debug;

    [SerializeField]
    private Toggle pinnedToggle;

    private void Awake()
    {
        graphics = graphicsSetup.Finalise<FlatGraphic>();
        pinned = pinnedSetup.Finalise<FlatGraphic>();

        layerDrag.OnBegin += () => prevDepth = selected.depth;
        layerDrag.OnDrag += displacement => selected.depth = prevDepth + displacement.y * 0.01f;
    }

    private void Start()
    {
        Application.RequestUserAuthorization(UserAuthorization.WebCam);

        StartCoroutine(LoadResources());
    }

    public void Save()
    {
        string data = JsonUtility.ToJson(scene);

        File.WriteAllText(Application.persistentDataPath + "/test-scene.json", data);
    }

    public void Load()
    {
        try
        {
            string data = File.ReadAllText(Application.persistentDataPath + "/test-scene.json");

            scene = JsonUtility.FromJson<FlatScene>(data);
        }
        catch (Exception e)
        {
            //Debug.LogFormat("Failed to load scene,");
            //Debug.LogException(e);
        }
    }

    public void Refresh()
    {
        scene.graphics.Sort((a, b) => a.depth.CompareTo(b.depth));
        graphics.SetActive(scene.graphics.Where(g => !g.pinned));
        graphics.Refresh();
        pinned.SetActive(scene.graphics.Where(g => g.pinned));
        pinned.Refresh();
    }

    private void Update()
    {
        if (selected != null)
        {
            if (!selected.pinned && pinnedToggle.isOn)
            {
                selected.position = worldTransform.TransformPoint(selected.position);
                selected.direction += worldObject.direction;
                selected.scale *= worldObject.scale;
                selected.pinned = true;
            }
            else if (selected.pinned && !pinnedToggle.isOn)
            {
                selected.position = worldTransform.InverseTransformPoint(selected.position);
                selected.direction -= worldObject.direction;
                selected.scale /= worldObject.scale;
                selected.pinned = false;
            }
        }

        Refresh();

        if (!playing)
        {
            layerGroup.alpha = selected != null ? 1 : 0.5f;
            layerGroup.blocksRaycasts = selected != null;

            CheckTouchTransform();
            CheckTouchSelect();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Save();
                Application.Quit();
            }
        }
        else
        {
            CheckPlayControls();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                StopPlaying();
            }
        }

        worldTransform.position = (Vector3) worldObject.position + Vector3.back * worldObject.depth;
        worldTransform.localScale = worldObject.scale * Vector3.one;
        worldTransform.localEulerAngles = worldObject.direction * Vector3.forward;
    }

    public void ResetCamera()
    {
        worldObject.position = Vector2.zero;
        worldObject.scale = 1;
        worldObject.direction = 0;
    }

    public IEnumerator LoadFromFile(string file)
    {
        return LoadFromURL("file://" + file);
    }

    public IEnumerator LoadFromURL(string url, bool save=true)
    {
        var load = new WWW(url);

        yield return load;

        var resource = new ImageResource
        {
            name = Path.GetFileNameWithoutExtension(load.url),
            path = load.url,
        };

        if (save)
        {
            resources.Add(url, resource);
        }

        try
        {
            var texture = load.texture;

            resource.sprite = Sprite.Create(texture,
                                            new Rect(0, 0, texture.width, texture.height),
                                            Vector2.zero,
                                            100,
                                            0,
                                            SpriteMeshType.FullRect);

            
        }
        catch (Exception e)
        {
            Debug.LogFormat("Failed to load '{0}'", url);
            Debug.LogException(e);
        }
    }

    public IEnumerator LoadFromSource(GraphicSource source)
    {
        return LoadFromFile(source.path);
    }

    private IEnumerator LoadResources()
    {
        Load();

        var expected = new HashSet<string>(scene.graphics.Select(g => g.graphicURI));

        loadingSlider.maxValue = expected.Count;

        foreach (string file in expected)
        {
            if (file.StartsWith("jar") || file.StartsWith("file"))
            {
                yield return StartCoroutine(LoadFromURL(file));
            }
            else
            {
                yield return StartCoroutine(LoadFromFile(file));
            }

            loadingSlider.value += 1;
        }

        Refresh();
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

    [SerializeField]
    private Sprite failSprite;

    public Sprite GetImageSprite(string uri)
    {
        ImageResource resource;

        if (resources.TryGetValue(uri, out resource))
        {
            return resource.sprite;
        }

        return failSprite;
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
        graphic.depth = scene.graphics.Max(g => g.depth) + 0.01f;

        Select(graphic);

        Refresh();
    }

    public bool playing { get; private set; }

    public void StartPlaying()
    {
        playing = true;
        ResetCamera();
        Deselect();
        Save();

        toolbar.SetActive(false);
        debug.SetActive(false);
    }

    public void StopPlaying()
    {
        playing = false;
        ResetCamera();

        toolbar.SetActive(true);
        debug.SetActive(true);
    }

    #region Play Touch Controls
    
    public void CheckPlayControls()
    {
        if (Input.GetMouseButton(0))
        {
            Vector2 center = new Vector2(Camera.main.pixelWidth,
                                         Camera.main.pixelHeight) * 0.5f;

            Vector2 delta = (Vector2) Input.mousePosition - center;

            worldObject.position -= delta * Time.deltaTime;
        }
    }
    
    #endregion

    #region Editor Selection

    public FlatGraphic selected { get; private set; }

    public void Select(FlatGraphic graphic)
    {
        selected = graphic;
        pinnedToggle.interactable = true;
        pinnedToggle.isOn = graphic.pinned;
    }

    public void Deselect()
    {
        selected = null;
        pinnedToggle.interactable = false;
        pinnedToggle.isOn = false;
    }

    public void DeleteSelected()
    {
        Assert.IsTrue(selected != null, "Deleting with nothing selected!");

        scene.RemoveGraphic(selected);

        Deselect();
    }

    #endregion

    #region Editor Touch Controls

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

                if (hit != null && hit.config != prev)
                {
                    Select(hit.config);
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

        if (this.selected != null)
        {
            nextTouch1 = worldTransform.InverseTransformPoint(nextTouch1);
            nextTouch2 = worldTransform.InverseTransformPoint(nextTouch2);
        }

        bool touch1Begin = Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began;
        bool touch2Begin = Input.touchCount == 2 && Input.GetTouch(1).phase == TouchPhase.Began;

        bool touch1Move = Input.touchCount == 1;

        var selected = this.selected ?? worldObject;

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
