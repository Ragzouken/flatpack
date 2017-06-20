using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;
using UnityEngine.Events;

public class OnEnableTrigger : MonoBehaviour 
{
    [Serializable]
    public class Event : UnityEvent { }

    public Event onEnable;

    private void OnEnable()
    {
        onEnable.Invoke();
    }
}
