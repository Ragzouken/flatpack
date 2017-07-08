using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using System.IO;

public class FlatBlurb 
{
    [NonSerialized]
    public string id;
    public string name;
    public int graphics = -1;
    public DateTime modified;
}

public class FlatStory
{
    [NonSerialized]
    public FlatBlurb blurb;
    public FlatScene scene;
    public Vector2 resolution;
    public List<string> graphics = new List<string>();
    public string musicID;
}

public static class Saves 
{
    public static string root
    {
        get
        {
            return Application.persistentDataPath + "/stories";
        }
    }

    public static Dictionary<string, FlatBlurb> blurbs 
        = new Dictionary<string, FlatBlurb>();

    public static string Sanitize(string name)
    {
        return string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
    }

     public static void RefreshAndroidFile(string path)
     {
#if UNITY_ANDROID
        if(!File.Exists(path))
             return;
 
         using (AndroidJavaClass jcUnityPlayer = new AndroidJavaClass ("com.unity3d.player.UnityPlayer"))
         using (AndroidJavaObject joActivity = jcUnityPlayer.GetStatic<AndroidJavaObject> ("currentActivity"))
         using (AndroidJavaObject joContext = joActivity.Call<AndroidJavaObject> ("getApplicationContext"))
         using (AndroidJavaClass jcMediaScannerConnection = new AndroidJavaClass ("android.media.MediaScannerConnection"))
         jcMediaScannerConnection.CallStatic("scanFile", joContext, new string[] { path }, null, null);
#endif
    }

    public static string GetGraphicPath(string id)
    {
        return Application.persistentDataPath + "/imported/" + id + ".png";
    }

    public static void RefreshBlurbs()
    {
        Directory.CreateDirectory(root);

        foreach (string folder in Directory.GetDirectories(root))
        {
            string id = Path.GetFileName(folder);

            try
            {
                string data = File.ReadAllText(folder + "/blurb.json");
                var blurb = JsonUtility.FromJson<FlatBlurb>(data);

                blurb.id = id;

                if (blurbs.ContainsKey(id))
                {
                    blurbs[id].name = blurb.name;
                    blurbs[id].modified = blurb.modified;
                }
                else
                {
                    blurbs[id] = blurb;
                }
            }
            catch (FileNotFoundException)
            {
                // this isn't a story, no biggie
            }
        }
    }

    public static FlatStory CreateStory(string name)
    {
        string id = Sanitize(name) + "-" + Guid.NewGuid().ToString();

        var story = new FlatStory
        {
            blurb = new FlatBlurb { id = id, name = name, graphics = 0 },
            scene = new FlatScene(),
        };

        SaveStory(story);

        blurbs[id] = story.blurb;

        return story;
    }

    public static void SaveStory(FlatStory story, string location = null)
    {
        string folder = location ?? Path.Combine(root, story.blurb.id);
        Directory.CreateDirectory(folder);
        string blurbPath = Path.Combine(folder, "blurb.json");
        string storyPath = Path.Combine(folder, "story.json");

        var graphics = new HashSet<string>(story.graphics);
        graphics.UnionWith(story.scene.graphics.Select(g => g.graphicURI));

        story.graphics.Clear();
        story.graphics.AddRange(graphics);

        story.blurb.graphics = story.graphics.Count;
        story.blurb.modified = DateTime.UtcNow;

        File.WriteAllText(blurbPath, JsonUtility.ToJson(story.blurb));
        File.WriteAllText(storyPath, JsonUtility.ToJson(story));
    }

    public static FlatStory LoadStory(FlatBlurb blurb)
    {
        string folder = Path.Combine(root, blurb.id);
        string storyPath = Path.Combine(folder, "story.json");

        string data = File.ReadAllText(storyPath);
        var story = JsonUtility.FromJson<FlatStory>(data);
        story.blurb = blurb;

        return story;
    }

    public static void Copy(string sourceDirectory, string targetDirectory)
    {
        DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
        DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

        CopyAll(diSource, diTarget);
    }

    public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        // Copy each file into the new directory.
        foreach (FileInfo fi in source.GetFiles())
        {
            Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            RefreshAndroidFile(Path.Combine(target.FullName, fi.Name));
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir =
                target.CreateSubdirectory(diSourceSubDir.Name);
            CopyAll(diSourceSubDir, nextTargetSubDir);
        }
    }

    public static string musicRoot
    {
        get
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#elif UNITY_ANDROID
            return "/storage/emulated/0/Download";
#else
            return Application.persistentDataPath + "/music";
#endif
        }
    }

    public static string exportRoot
    {
        get
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#elif UNITY_ANDROID
            return "/storage/emulated/0/Download";
#else
            return Application.persistentDataPath + "/stories";
#endif
        }
    }

    public static IEnumerator ExportStory(FlatStory story,
                                          Action OnComplete=null)
    {
        string root = exportRoot;
        root = Path.Combine(root, "Flatpack Exports");
        string name = Sanitize(story.blurb.name);
        string folder = Path.Combine(root, name);

        Directory.CreateDirectory(folder);

        var request = new WWW(Application.streamingAssetsPath + "/flatweb.zip");

        yield return request;

        string zipPath = root + "/flatweb.zip";

        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
            RefreshAndroidFile(zipPath);
        }

        File.WriteAllBytes(zipPath, request.bytes);
        RefreshAndroidFile(zipPath);

        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
            RefreshAndroidFile(folder);
        }
        ZipUtil.Unzip(zipPath, root);
        Directory.Move(root + "/flatweb", folder);

        string dest = folder + "/StreamingAssets/" + name;
        Directory.CreateDirectory(dest);

        File.WriteAllText(folder + "/StreamingAssets/autoplay.json", 
            JsonUtility.ToJson(new AutoPlay
        {
            storyID = name,
        }));

        foreach (string id in story.graphics)
        {
            File.Copy(GetGraphicPath(id), dest + "/" + id + ".png", true);
        }

        if (!string.IsNullOrEmpty(story.musicID))
        {
            File.Copy("/storage/emulated/0/Download/" + story.musicID, dest + "/" + story.musicID, true);
        }

        SaveStory(story, location: dest);

        //RefreshAndroidFile(dest + "/story.json");
        //RefreshAndroidFile(dest + "/blurb.json");

        foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
        {
            RefreshAndroidFile(file);
        }

        if (OnComplete != null)
        {
            OnComplete();
        }
    }

    public static FlatBlurb CopyStory(FlatBlurb blurb)
    {
        string name = blurb.name + " Copy";

        var story = LoadStory(blurb);
        story.blurb = new FlatBlurb
        {
            id = Sanitize(name) + "-" + Guid.NewGuid().ToString(),
            graphics = blurb.graphics,
            modified = DateTime.UtcNow,
            name = name,
        };

        SaveStory(story);

        blurbs[story.blurb.id] = story.blurb;

        return story.blurb;
    }
    
    public static void DeleteStory(FlatBlurb blurb)
    {
        string folder = Path.Combine(root, blurb.id);
        Directory.Delete(folder, true);

        blurbs.Remove(blurb.id);
    }
}
