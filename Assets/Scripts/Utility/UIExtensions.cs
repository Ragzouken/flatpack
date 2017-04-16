using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;
using UnityEngine.EventSystems;

public static partial class UIExtensions
{
    private static List<RaycastResult> hits = new List<RaycastResult>();

    public static bool IsPointBlocked(this GraphicRaycaster raycaster,
                                      Vector2 point)
    {
        return raycaster.Raycast(point).Count > 0;
    }

    public static List<RaycastResult> Raycast(this GraphicRaycaster raycaster,
                                              Vector2 point,
                                              List<RaycastResult> hits=null)
    {
        hits = hits ?? UIExtensions.hits;

        var pointer = new PointerEventData(EventSystem.current);
        pointer.position = point;

        hits.Clear();
        raycaster.Raycast(pointer, hits);

        return hits;
    }
}
