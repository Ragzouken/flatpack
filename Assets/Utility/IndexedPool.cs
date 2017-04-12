using UnityEngine;
using System.Collections.Generic;

public class IndexedPool<TInstance>
    where TInstance : Component
{
    public TInstance prefab;
    public Transform parent;

    private List<TInstance> instances = new List<TInstance>();

    public int count { get; private set; }

    public TInstance this[int index]
    {
        get
        {
            return instances[index];
        }
    }

    public IndexedPool(TInstance prefab, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent ?? prefab.transform.parent;
    }

    private TInstance CreateInstance()
    {
        var instance = Object.Instantiate(prefab);

        instance.transform.SetParent(parent, false);

        return instance;
    }

    public void SetActive(int count)
    {
        for (int i = instances.Count; i < count; ++i)
        {
            instances.Add(CreateInstance());
        }

        for (int i = this.count; i < count; ++i)
        {
            instances[i].gameObject.SetActive(true);
        }

        for (int i = count; i < instances.Count; ++i)
        {
            instances[i].gameObject.SetActive(false);
        }

        this.count = count;
    }

    public void MapActive(System.Action<int, TInstance> action)
    {
        for (int i = 0; i < count; ++i)
        {
            action(i, instances[i]);
        }
    }
}
