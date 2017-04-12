using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public interface IConfigView<TConfig>
{
    TConfig config { get; }

    void SetConfig(TConfig config);
    void Cleanup();
    void Refresh();
}

public abstract class InstanceView<TConfig> : MonoBehaviour, IConfigView<TConfig>
{
    public TConfig config { get; private set; }

    public void SetConfig(TConfig config)
    {
        this.config = config;

        Configure();
    }

    protected virtual void Configure() { Refresh(); }
    public virtual void Cleanup() { }
    public virtual void Refresh() { }
}

