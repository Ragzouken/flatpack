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
    public string id;
    public Sprite sprite;
}

public class Main : MonoBehaviour 
{
    [SerializeField]
    public GraphicRaycaster viewerRaycaster;
    [SerializeField]
    public GraphicRaycaster creatorRayster;

    [SerializeField]
    private CanvasGroup selectionGroup;

    [SerializeField]
    private InputField storyNameInput;
    [SerializeField]
    private GraphicBrowserPanel graphicsBrowser;

    [SerializeField]
    private GameObject loadingBlocker;
    [SerializeField]
    private Slider loadingSlider;

    [SerializeField]
    private InstancePoolSetup graphicsSetup;
    private InstancePool<FlatGraphic> graphics;

    [SerializeField]
    private InstancePoolSetup pinnedSetup;
    private InstancePool<FlatGraphic> pinned;

    private List<string> resourcePaths = new List<string>();
    public Dictionary<string, ImageResource> resources = new Dictionary<string, ImageResource>();

    public FlatScene scene;

    private FlatGraphic worldObject = new FlatGraphic { scale = 1 };
    [SerializeField]
    private Transform worldTransform;
    [SerializeField]
    private Transform screenTransform;

    private float prevDepth;

    [SerializeField]
    private GameObject titleScreen;
    [SerializeField]
    private GameObject playHUD;
    [SerializeField]
    private GameObject sceneHUD;
    [SerializeField]
    private GameObject debug;

    [SerializeField]
    private CanvasGroup hudGroup;
    private float hudVelocity;

    [SerializeField]
    private CanvasGroup pinnedGroup;
    private float pinnedVelocity;

    [SerializeField]
    private Toggle pinnedToggle;

    [SerializeField]
    private Image grid;

    [SerializeField]
    private AudioSource music;

    private void Awake()
    {
        graphics = graphicsSetup.Finalise<FlatGraphic>();
        pinned = pinnedSetup.Finalise<FlatGraphic>();
    }

    private void Start()
    {
        Application.RequestUserAuthorization(UserAuthorization.WebCam);
    }

    public void Exit()
    {
        Application.Quit();
    }

    public FlatStory story { get; private set; }

    public void CreateFromInput()
    {
        var story = Saves.CreateStory(storyNameInput.text);

        SetStory(story);
    }

    public void Save()
    {
        Saves.SaveStory(story);
    }

    private Coroutine musicPlayback;

    public void PlayMusic(string path)
    {
        musicPlayback = StartCoroutine(PlayMusicCO(path));
    }
        
    public IEnumerator PlayMusicCO(string path)
    {
        var music = new WWW(path);

        AudioType type = AudioType.UNKNOWN;

        if (Path.GetExtension(path) == ".ogg")
        {
            type = AudioType.OGGVORBIS;
        }
        else if (Path.GetExtension(path) == ".mp3")
        {
            type = AudioType.MPEG;
        }
        else
        {
            Debug.LogFormat("Unsure what type '{0}' is.", Path.GetExtension(path));
        }

#if UNITY_WEBGL
        yield return music;

        if (music.error != null)
        {
            Debug.LogErrorFormat("Couldn't load {0} '{1}' - {2}", type, path, music.error);
        }
        else
        {
            this.music.clip = music.GetAudioClipCompressed(false, type);
            this.music.Play();
        }
#else
        yield return null;
        //Debug.LogFormat("{0} {1}", type, path);
        this.music.clip = music.GetAudioClip(false, true, type);

        while (!this.music.clip.isReadyToPlay)
        {
            yield return null;
        }

        if (this.music.clip.loadState == AudioDataLoadState.Failed)
        {
            Debug.LogErrorFormat("Couldn't stream {0} '{1}'", type, path);
        }
        else
        {
            this.music.Play();
        } 
#endif
    }

    public void StopMusic()
    {
        if (musicPlayback != null)
        {
            StopCoroutine(musicPlayback);
        }

        music.Stop();
    }

    public IEnumerator PlayStory(FlatStory story)
    {
        titleScreen.SetActive(false);
        sceneHUD.SetActive(false);

        this.story = story;
        scene = story.scene;

        loading = true;
        yield return StartCoroutine(LoadResources());

        StartPlaying();
    }

    public void SetStory(FlatStory story)
    {
        sceneHUD.SetActive(true);

        this.story = story;
        scene = story.scene;

        loading = true;
        StartCoroutine(LoadResources());
    }

    public void Refresh()
    {
        scene.graphics.Sort((a, b) => a.depth.CompareTo(b.depth));
        graphics.SetActive(scene.graphics.Where(g => !g.pinned));
        graphics.Refresh();
        pinned.SetActive(scene.graphics.Where(g => g.pinned));
        pinned.Refresh();
    }

    private bool loading = true;

    private void Update()
    {
        if (loading)
        {
            return;
        }

        selectionGroup.interactable = selected != null;

        if (selected != null)
        {
            if (!selected.pinned && pinnedToggle.isOn)
            {
                var pos = worldTransform.TransformPoint(selected.position);
                selected.position = screenTransform.InverseTransformPoint(pos);
                selected.direction += worldObject.direction;
                selected.scale *= worldObject.scale;
                selected.pinned = true;
            }
            else if (selected.pinned && !pinnedToggle.isOn)
            {
                var pos = screenTransform.TransformPoint(selected.position);
                selected.position = worldTransform.InverseTransformPoint(pos);
                selected.direction -= worldObject.direction;
                selected.scale /= worldObject.scale;
                selected.pinned = false;
            }
        }

        Refresh();

        {
            float delta = Mathf.Abs(Mathf.Log(worldObject.scale, 2));
            bool showPinned = delta < 0.25f;

            float target = showPinned ? 0.75f : 0f;

            pinnedGroup.alpha = playing ? 1f : Mathf.SmoothDamp(pinnedGroup.alpha, target, ref pinnedVelocity, .25f);
            pinnedGroup.blocksRaycasts = showPinned;
        }

        if (!playing)
        {
            CheckTouchTransform();
            CheckTouchSelect();

            story.resolution = new Vector2(Screen.width, Screen.height);
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

        {
            float target = (oneFinger || twoFinger) ? .05f : 1f;
            hudGroup.alpha = Mathf.SmoothDamp(hudGroup.alpha, target, ref hudVelocity, .1f);
        }

        Vector2 screen = new Vector2(Screen.width, Screen.height);
        Vector2 center = screen * 0.5f;
        Vector2 local;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(worldTransform as RectTransform, center, null, out local);

        local.x -= local.x % 128;
        local.y -= local.y % 128;
        grid.transform.localPosition = local;

        float scale = Mathf.Ceil(1f / worldObject.scale);

        grid.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 4096 * scale);
        grid.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 4096 * scale);
    }

    public void ResetCamera()
    {
        worldObject.position = Vector2.zero;
        worldObject.scale = 1;
        worldObject.direction = 0;
    }

    public void InsertImported(string id, Texture2D texture)
    {
        var resource = new ImageResource
        {
            id = id,
        };

        resource.sprite = Sprite.Create(texture,
                                        new Rect(0, 0, texture.width, texture.height),
                                        Vector2.zero,
                                        100,
                                        0,
                                        SpriteMeshType.FullRect);

        resources.Add(id, resource);

        story.graphics.Add(id);

        graphicsBrowser.Refresh();
        graphicsBrowser.ScrollToBottom();
    }

    public IEnumerator LoadFromImported(string id)
    {
#if UNITY_EDITOR || UNITY_ANDROID
        return LoadFromURL("file://" + Application.persistentDataPath + "/imported/" + id + ".png", id: id);
#else
        return LoadFromURL(Application.streamingAssetsPath + "/" + story.blurb.id + "/" + id + ".png", id: id);
#endif
        
    }

    public string GetMusicPath(string id)
    {
#if UNITY_EDITOR || UNITY_ANDROID
        return "file:///storage/emulated/0/Download/" + id;
#else
        return Application.streamingAssetsPath + "/" + story.blurb.id + "/" + id;
#endif
    }

    public IEnumerator LoadFromFile(string file)
    {
        return LoadFromURL("file://" + file);
    }

    public IEnumerator LoadFromURL(string url, bool save=true, string id="")
    {
        if (resources.ContainsKey(url))
        {
            yield break;
        }

        var load = new WWW(url);

        yield return load;

        if (load.error != null)
        {
            Debug.LogFormat("Failed to load '{0}'", url);
        }

        var resource = new ImageResource
        {
            id = id,
        };

        if (id != "")
        {
            resources.Add(id, resource);
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

            graphicsBrowser.Refresh();
        }
        catch (Exception e)
        {
            Debug.LogFormat("Failed to load '{0}'", url);
            Debug.LogException(e);
        }
    }

    private IEnumerator LoadResources()
    {
        loadingBlocker.SetActive(true);
        loading = true;

        var expected = new HashSet<string>(scene.graphics.Select(g => g.graphicURI));
        expected.UnionWith(story.graphics);
        expected.ExceptWith(resources.Keys);

        loadingSlider.maxValue = expected.Count;

        if (story.musicID != null)
        {
            loadingSlider.maxValue += 1;
        }

        foreach (string file in expected)
        {
            if (file.StartsWith("jar") || file.StartsWith("file"))
            {
                Debug.LogErrorFormat("Legacy story file no longer supported :(");
            }
            else
            {
                yield return StartCoroutine(LoadFromImported(file));
            }

            loadingSlider.value += 1;
        }

#if UNITY_WEBGL
        if (story.musicID != null)
        {
            yield return StartCoroutine(PlayMusicCO(GetMusicPath(story.musicID)));
        }
#endif

        Refresh();

        loading = false;
        loadingBlocker.SetActive(false);
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

    public void CreateGraphic(ImageResource resource)
    {
        var graphic = scene.AddNewGraphic(resource.id);

        Vector2 screen = new Vector2(Camera.main.pixelWidth,
                                     Camera.main.pixelHeight) * 0.5f;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(worldTransform as RectTransform,
                                                                screen,
                                                                null,
                                                                out graphic.position);

        //Debug.Log(graphic.position);

        graphic.direction = -worldObject.direction;
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

        playHUD.SetActive(true);
        sceneHUD.SetActive(false);
        debug.SetActive(false);

#if !UNITY_WEBGL
        if (string.IsNullOrEmpty(story.musicID))
        {
            PlayMusic(GetMusicPath(story.musicID));
        }
#endif
    }

    public void StopPlaying()
    {
        playing = false;
        ResetCamera();

        playHUD.SetActive(false);
        sceneHUD.SetActive(true);
        debug.SetActive(true);

        music.Stop();
    }

#region Play Touch Controls

    private bool playTouchHeld;
    private Vector2 playTouchOrigin;

    public void CheckPlayControls()
    {
        float speed = 256;

        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            worldObject.position -= Vector2.left * speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            worldObject.position -= Vector2.right * speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            worldObject.position -= Vector2.up * speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            worldObject.position -= Vector2.down * speed * Time.deltaTime;
        }

        if (Input.GetMouseButton(0))
        {
            if (playTouchHeld)
            {
                Vector2 delta = (Vector2) Input.mousePosition - playTouchOrigin;

                worldObject.position -= delta * Time.deltaTime;
            }
            else
            {
                playTouchHeld = true;
                playTouchOrigin = Input.mousePosition;
            }
        }
        else
        {
            playTouchHeld = false;
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

    public void SendSelectedForward()
    {
        var objects = scene.graphics.OrderByDescending(o => o.depth).ToList();

        if (selected != null)
        {
            int index = objects.IndexOf(selected);

            if (index >= 2)
            {
                selected.depth = Mathf.Lerp(objects[index - 2].depth, objects[index - 1].depth, 0.5f);
            }
            else if (index >= 1)
            {
                selected.depth = objects[0].depth + 100;
            }
            else
            {
                selected.depth += 100;
            }
        }
    }

    public void SendSelectedBack()
    {
        var objects = scene.graphics.OrderByDescending(o => o.depth).ToList();

        if (selected != null)
        {
            int index = objects.IndexOf(selected);
            int count = objects.Count;

            if (count - index - 1 >= 2)
            {
                selected.depth = Mathf.Lerp(objects[index + 2].depth, objects[index + 1].depth, 0.5f);
            }
            else if (count - index - 1 >= 1)
            {
                selected.depth = objects[count - 1].depth - 100;
            }
            else
            {
                selected.depth -= 100;
            }
        }
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
        bool mouseBegin = Input.GetMouseButtonDown(0);

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

        if ((touch1Begin || mouseBegin) && !oneFinger && !blocked1)
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
            //Vector2 b = prevTouch2 - basePosition;
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

            worldObject.direction = 0;
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
