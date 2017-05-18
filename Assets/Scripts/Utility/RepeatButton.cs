using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.EventSystems;

public class RepeatButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField]
    private Button button;

    public float delay = .5f;

    private float timer;
    private bool held;
    private PointerEventData click;

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        timer = 0;
        held = true;
        click = eventData;
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        held = false;
    }

    private void OnEnabled()
    {
        held = false;
    }

    private void Update()
    {
        if (held)
        {
            timer += Time.deltaTime;

            while (timer > delay)
            {
                timer -= delay;

                button.OnPointerClick(click);
            }
        }
    }
}
