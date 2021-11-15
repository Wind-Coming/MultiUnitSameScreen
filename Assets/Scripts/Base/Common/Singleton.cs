using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

/// <summary>
/// 用于单例脚本
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class SingleInstance<T> where T : new()
{
    protected static T mInstance;

    public static T Instance
    {
        get
        {
            if(mInstance == null)
            {
                mInstance = new T();
            }
            return mInstance;
        }
        
    }

    public static void Destroy()
    {
        mInstance = default(T);
    }
}

/// <summary>
/// 用于不被销毁的单例对象
/// </summary>
/// <typeparam name="T"></typeparam>
public class SingletonDontDestory<T> : MonoBehaviour where T : MonoBehaviour
{
    protected static T instance;

    /**
       Returns the instance of this singleton.
    */
    public static T Instance
    {
        get
        {
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Debug.LogError("发生了单件类多次生成的情况：" + this.ToString());
            Destroy(this.gameObject);
        }
    }
}

/// <summary>
/// 随场景销毁而销毁
/// </summary>
/// <typeparam name="T"></typeparam>
public class SingletonDestory<T> : MonoBehaviour where T : MonoBehaviour
{
    protected static T instance;

    /**
       Returns the instance of this singleton.
    */
    public static T Instance
    {
        get
        {
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
        }
        else if (instance != this)
        {
            Debug.LogError("发生了单件类多次生成的情况：" + this.ToString());
            Destroy(this.gameObject);
        }
    }
}

/// <summary>
/// 没有的话自动创建，且会随场景的销毁而销毁
/// </summary>
/// <typeparam name="T"></typeparam>
public class SingletonAutoCreate<T> : MonoBehaviour where T : SingletonAutoCreate<T>
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                 _instance = new GameObject(typeof(T).ToString()).AddComponent<T>();
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
        }
        else
        {
            Debug.LogError("发生了单件类多次生成的情况：" + this.ToString());
            DestroyImmediate(gameObject);
        }
    }
}

/// <summary>
/// 没有的话自动创建，且会随场景的销毁而销毁
/// </summary>
/// <typeparam name="T"></typeparam>
[ExecuteInEditMode]
public class ScriptSingletonWithEditor<T> : MonoBehaviour where T : ScriptSingletonWithEditor<T>
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType(typeof(T)) as T;
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
        }
        else
        {
            Debug.LogError("发生了单件类多次生成的情况：" + this.ToString());
        }
    }
}

public class SystemSingleton<T> : MonoBehaviour where T : SystemSingleton<T>
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }

    public static void CreateInstance()
    {
        if (Instance == null)
            new GameObject().AddComponent<T>();
    }

    public static void DestoryInstance()
    {
        if (Instance != null)
        {
            GameObject.Destroy(Instance.gameObject);
        }
    }
}
