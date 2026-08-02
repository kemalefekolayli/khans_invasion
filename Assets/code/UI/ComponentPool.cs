using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Small reusable pool for short-lived popup components.
/// Preallocates <paramref name="baseSize"/> items under a pooled parent object,
/// expands up to 2x the base size if needed, and never destroys active items.
/// </summary>
public class ComponentPool<T> where T : Component
{
    private readonly List<T> pool = new List<T>();
    private readonly int baseSize;
    private readonly Func<Transform, T> factory;
    private readonly Transform poolParent;

    public ComponentPool(string poolName, Transform owner, int baseSize, Func<Transform, T> factory)
    {
        this.baseSize = baseSize;
        this.factory = factory;

        poolParent = new GameObject(poolName).transform;
        poolParent.SetParent(owner);

        for (int i = 0; i < baseSize; i++)
        {
            pool.Add(CreateNew());
        }
    }

    /// <summary>
    /// Returns an inactive pooled item, or a newly created one if the pool is not full.
    /// Returns null if every item is in use at the maximum pool size.
    /// </summary>
    public T Get()
    {
        foreach (T item in pool)
        {
            if (!item.gameObject.activeInHierarchy)
            {
                return item;
            }
        }

        if (pool.Count < baseSize * 2)
        {
            T item = CreateNew();
            pool.Add(item);
            return item;
        }

        return null;
    }

    /// <summary>
    /// Returns an active item back to the pool by deactivating it.
    /// </summary>
    public void Return(T item)
    {
        if (item != null)
        {
            item.gameObject.SetActive(false);
        }
    }

    private T CreateNew()
    {
        T item = factory(poolParent);
        item.gameObject.SetActive(false);
        return item;
    }
}
