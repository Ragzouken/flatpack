using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;
using System.IO;

public class FlatBlurb
{
    [NonSerialized]
    public string id;
    public string name;
    public DateTime modified;
}

public class FlatStory
{
    [NonSerialized]
    public FlatBlurb blurb;
    public FlatScene scene;
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
            blurb = new FlatBlurb { id = id, name = name },
            scene = new FlatScene(),
        };

        SaveStory(story);

        return story;
    }

    public static void SaveStory(FlatStory story)
    {
        string folder = Path.Combine(root, story.blurb.id);
        Directory.CreateDirectory(folder);
        string blurbPath = Path.Combine(folder, "blurb.json");
        string storyPath = Path.Combine(folder, "story.json");

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
}
