﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

[Serializable]
public class FlatGraphic
{
    public Vector2 position;
    public float direction;
    public float scale = 1;
    public float depth;
    public bool pinned;

    public string graphicURI;
}

[Serializable]
public partial class FlatScene
{
    public List<FlatGraphic> graphics = new List<FlatGraphic>();
}

public partial class FlatScene
{
    public FlatGraphic AddNewGraphic(string URI)
    {
        var graphic = new FlatGraphic
        {
            graphicURI = URI,
        };

        graphics.Add(graphic);

        return graphic;
    }

    public void RemoveGraphic(FlatGraphic graphic)
    {
        graphics.Remove(graphic);
    }
}
