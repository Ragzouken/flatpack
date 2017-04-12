using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class DebugToText : MonoBehaviour 
{
    [SerializeField]
    private Text text;
    [SerializeField]
    private bool showAllTraces;

    private List<string> debugs = new List<string>();

    private void Awake()
    {
        Application.logMessageReceived += (log, trace, type) =>
        {
            if (showAllTraces 
             || type == LogType.Error 
             || type == LogType.Exception)
            {
                debugs.Add(string.Format("{0}\n{1}\n", log, trace));
            }
            else
            {
                debugs.Add(string.Format("{0}\n", log));
            }

            //text.text = debugs.Last();
            text.text = string.Join("", debugs.Skip(debugs.Count - 16).Take(16).ToArray());
        };
    }
}
