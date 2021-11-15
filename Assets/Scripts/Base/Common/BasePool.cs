﻿using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// this copy from https://bitbucket.org/Unity-Technologies/ui/src/b5f9aae6ff7c2c63a521a1cb8b3e3da6939b191b/UnityEngine.UI/UI/Core/Utility/ObjectPool.cs?at=5.3&fileviewer=file-view-default
/// </summary>
/// <typeparam name="T"></typeparam>
public class BasePool<T> where T : new()
{
    private readonly Stack<T> m_Stack = new Stack<T>();
    private readonly System.Action<T> m_ActionOnGet;
    private readonly System.Action<T> m_ActionOnRelease;

    public int countAll { get; private set; }
    public int countActive { get { return countAll - countInactive; } }
    public int countInactive { get { return m_Stack.Count; } }

    public BasePool(Action<T> actionOnGet, Action<T> actionOnRelease)
    {
        m_ActionOnGet = actionOnGet;
        m_ActionOnRelease = actionOnRelease;
    }

    public T Get()
    {
        T element;
        if (m_Stack.Count == 0)
        {
            element = new T();
            countAll++;
        }
        else
        {
            element = m_Stack.Pop();
        }
        if (m_ActionOnGet != null)
            m_ActionOnGet(element);
        return element;
    }

    public void Release(T element)
    {
        if (m_Stack.Count > 0 && ReferenceEquals(m_Stack.Peek(), element))
            Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");
        if (m_ActionOnRelease != null)
            m_ActionOnRelease(element);
        m_Stack.Push(element);
    }
}

/// <summary>
/// this copy from https://bitbucket.org/Unity-Technologies/ui/src/b5f9aae6ff7c2c63a521a1cb8b3e3da6939b191b/UnityEngine.UI/UI/Core/Utility/ListPool.cs?at=5.3&fileviewer=file-view-default
/// </summary>
/// <typeparam name="T"></typeparam>
public static class ListPool<T>
{
    // Object pool to avoid allocations.
    private static readonly BasePool<List<T>> s_ListPool = new BasePool<List<T>>(null, l => l.Clear());

    public static List<T> Get()
    {
        return s_ListPool.Get();
    }

    public static void Release(List<T> toRelease)
    {
        s_ListPool.Release(toRelease);
    }
}

public interface IReleaseToPool
{
    void ReleaseToPool();
}

public interface IReset
{
    void Reset();
}


public static class ClassPool<T> where T : IReset, new()
{
    private static readonly BasePool<T> _objectPool = new BasePool<T>(null, m_ActionOnRelease);

    public static T Get()
    {
        return _objectPool.Get();
    }

    private static void m_ActionOnRelease(T op)
    {
        op.Reset();
    }

    public static void Release(T element)
    {
        _objectPool.Release(element);
    }

}