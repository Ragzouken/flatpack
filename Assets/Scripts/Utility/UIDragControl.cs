using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;
using UnityEngine.EventSystems;

public class UIDragControl : MonoBehaviour,
                             IPointerDownHandler,
                             IPointerUpHandler,
                             IDragHandler
{
    public bool dragging { get; private set; }
    public Vector2 origin { get; private set; }
    public Vector2 displacement { get; private set; }

    public event Action OnBegin = delegate { };
    public event Action OnEnd = delegate { };
    public event Action<Vector2> OnDrag = delegate { };

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        origin = eventData.pressPosition;
        dragging = true;

        OnBegin();
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        OnEnd();

        dragging = false;
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        displacement = eventData.position - eventData.pressPosition;

        OnDrag(displacement);
    }
}
