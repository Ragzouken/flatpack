using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class OnToggledOn : MonoBehaviour
{
    [System.Serializable]
    public class Action : UnityEngine.Events.UnityEvent { };

    [SerializeField]
    private Toggle toggle;

    public Action onToggledOn;
    public Action onToggledOff;

    private void Awake()
    {
        toggle.onValueChanged.AddListener(active =>
        {
            if (active)
            {
                onToggledOn.Invoke();
            }
            else
            {
                onToggledOff.Invoke();
            }
        });
    }
}
