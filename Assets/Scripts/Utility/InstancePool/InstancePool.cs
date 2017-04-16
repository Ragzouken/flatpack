using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public abstract class InstancePool<TConfig, TInstance>
    where TInstance : IConfigView<TConfig>
{    
    protected Dictionary<TConfig, TInstance> instances
        = new Dictionary<TConfig, TInstance>();
    protected List<TInstance> spare = new List<TInstance>();

    protected abstract TInstance CreateNew();

    protected TInstance FindNew(TConfig config)
    {
        TInstance instance;

        if (spare.Count > 0)
        {
            instance = spare[spare.Count - 1];
            spare.RemoveAt(spare.Count - 1);
        }
        else
        {
            instance = CreateNew();
        }

        Configure(config, instance);

        return instance;
    }

    public TInstance Get(TConfig config)
    {
        TInstance instance;

        if (!instances.TryGetValue(config, out instance))
        {
            instance = FindNew(config);
        }

        return instance;
    }

    public bool Discard(TConfig config)
    {
        TInstance instance;

        if (instances.TryGetValue(config, out instance))
        {
            instances.Remove(config);
            spare.Add(instance);

            Cleanup(config, instance);

            return true;
        }

        return false;
    }

    public void Clear()
    {
        foreach (var pair in instances)
        {
            Cleanup(pair.Key, pair.Value);
        }

        spare.AddRange(instances.Values);
        instances.Clear();
    }

    protected virtual void Configure(TConfig config, TInstance instance)
    {
        instances.Add(config, instance);
        instance.SetConfig(config);
    }

    protected virtual void Cleanup(TConfig config, TInstance instance)
    {
        instance.Cleanup();
    }

    public void Refresh()
    {
        MapActive(i => i.Refresh());
    }

    public void SetActive(params TConfig[] configs)
    {
        SetActive(configs);
    }

    private bool locked;
    private HashSet<TConfig> setActiveTempSet = new HashSet<TConfig>();
    private List<TConfig> setActiveTempList = new List<TConfig>();

    public virtual void SetActive(IEnumerable<TConfig> active)
    {
        Assert.IsFalse(locked, "GC OPTIMISATION MEANS THESE CALLS CANNOT BE NESTED!!");

        locked = true;
        setActiveTempSet.Clear();
        setActiveTempList.Clear();

        var collection = setActiveTempSet;
        var existing = setActiveTempList;

        if (active.Any()) collection.UnionWith(active);
        existing.AddRange(instances.Keys);

        for (int i = 0; i < existing.Count; ++i)
        {
            TConfig config = setActiveTempList[i];

            if (!collection.Contains(config)) Discard(config);
        }

        locked = false;

        foreach (TConfig shortcut in active)
        {
            Get(shortcut);
        }
    }

    public void MapActive(System.Action<TInstance> action)
    {
        foreach (TInstance instance in instances.Values)
        {
            action(instance);
        }
    }

    public bool IsActive(TConfig shortcut)
    {
        return instances.ContainsKey(shortcut);
    }

    public bool DoIfActive(TConfig shortcut, 
                           System.Action<TInstance> action)
    {
        TInstance instance;

        if (instances.TryGetValue(shortcut, out instance))
        {
            action(instance);

            return true;
        }
        else
        {
            return false;
        }
    }
}

[Serializable]
public class InstancePoolSetup
{
    public GameObject prefab;
    public Transform parent;

    private TInstance GetConcretePrefab<TInstance>()
        where TInstance : class
    {
        var prefab = this.prefab.GetComponent<TInstance>();

        Assert.IsNotNull(prefab, string.Format("Prefab {0} is missing {1} component", 
                                               this.prefab.name,
                                               typeof(TInstance).Name));

        return prefab;
    }

    public InstancePool<TConfig> Finalise<TConfig>(bool sort=true)
    {
        var prefab = GetConcretePrefab<InstanceView<TConfig>>();

        return new InstancePool<TConfig>(prefab, parent, sort: sort);
    }

    public AnonymousPool<TConfig, TInstance> Finalise<TConfig, TInstance>(Action<TConfig, TInstance> Configure = null,
                                                                          Action<TConfig, TInstance> Refresh   = null,
                                                                          Action<TInstance>          Cleanup   = null,
                                                                          bool sort=true)
        where TInstance : Component
    {
        var prefab = GetConcretePrefab<TInstance>();

        return new AnonymousPool<TConfig, TInstance>(prefab,
                                                     parent,
                                                     Configure,
                                                     Refresh,
                                                     Cleanup,
                                                     sort: sort);
    }
}

public class AnonymousView<TConfig, TInstance> : IConfigView<TConfig>
{
    public TConfig config { get; private set; }
    public TInstance instance { get; private set; }

    private Action<TConfig, TInstance> DoConfigure;
    private Action<TConfig, TInstance> DoRefresh;
    private Action<TInstance> DoCleanup;

    public AnonymousView(TInstance instance,
                         Action<TConfig, TInstance> Configure = null,
                         Action<TConfig, TInstance> Refresh   = null,
                         Action<TInstance>          Cleanup   = null)
    {
        this.instance = instance;

        DoConfigure = Configure ?? delegate { };
        DoRefresh   = Refresh   ?? delegate { };
        DoCleanup   = Cleanup   ?? delegate { };
    }

    public void SetConfig(TConfig config)
    {
        this.config = config;

        DoConfigure(config, instance);
    }

    public void Refresh()
    {
        DoRefresh(config, instance);
    }

    public void Cleanup()
    {
        DoCleanup(instance);
    }
}

public class AnonymousPool<TConfig, TInstance> : InstancePool<TConfig, AnonymousView<TConfig, TInstance>>
    where TInstance : Component
{
    protected TInstance prefab;
    protected Transform parent;
    protected bool sort;

    protected Action<TConfig, TInstance> DoConfigure;
    protected Action<TConfig, TInstance> DoRefresh;
    protected Action<TInstance>          DoCleanup;

    public AnonymousPool(TInstance prefab,
                         Transform parent,
                         Action<TConfig, TInstance> Configure = null,
                         Action<TConfig, TInstance> Refresh   = null,
                         Action<TInstance>          Cleanup   = null,
                         bool sort=true)
    {
        this.prefab = prefab;
        this.parent = parent;
        this.sort = sort;

        DoConfigure = Configure ?? delegate { };
        DoRefresh   = Refresh   ?? delegate { };
        DoCleanup   = Cleanup   ?? delegate { };
    }

    protected override AnonymousView<TConfig, TInstance> CreateNew()
    {
        var instance = UnityEngine.Object.Instantiate(prefab);

        return new AnonymousView<TConfig, TInstance>(instance,
                                                     DoConfigure,
                                                     DoRefresh,
                                                     DoCleanup);
    }

    protected override void Configure(TConfig config, AnonymousView<TConfig, TInstance> instance)
    {
        instance.instance.transform.SetParent(parent, false);
        instance.instance.gameObject.SetActive(true);

        base.Configure(config, instance);
    }

    protected override void Cleanup(TConfig config, AnonymousView<TConfig, TInstance> instance)
    {
        instance.instance.gameObject.SetActive(false);

        base.Cleanup(config, instance);
    }

    public override void SetActive(IEnumerable<TConfig> active)
    {
        base.SetActive(active);

        if (sort)
        {
            foreach (TConfig shortcut in active)
            {
                Get(shortcut).instance.transform.SetAsLastSibling();
            }
        }
    }
}

public class InstancePool<TConfig> : InstancePool<TConfig, InstanceView<TConfig>>
{
    protected InstanceView<TConfig> prefab;
    protected Transform parent;
    protected bool sort;

    public InstancePool(InstanceView<TConfig> prefab, Transform parent, bool sort=true)
    {
        this.prefab = prefab;
        this.parent = parent;
        this.sort = sort;
    }

    protected override InstanceView<TConfig> CreateNew()
    {
        return UnityEngine.Object.Instantiate(prefab);
    }

    protected override void Configure(TConfig config, InstanceView<TConfig> instance)
    {
        instance.transform.SetParent(parent, false);
        instance.gameObject.SetActive(true);

        base.Configure(config, instance);
    }

    protected override void Cleanup(TConfig config, InstanceView<TConfig> instance)
    {
        instance.gameObject.SetActive(false);

        base.Cleanup(config, instance);
    }

    public override void SetActive(IEnumerable<TConfig> active)
    {
        base.SetActive(active);

        if (sort)
        {
            foreach (TConfig shortcut in active)
            {
                Get(shortcut).transform.SetAsLastSibling();
            }
        }
    }
}

