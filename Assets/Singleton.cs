using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    // Getter
    public static T Instance
    {
        get
        {
            if (instance == null)
                Debug.LogError("No instance of " + typeof(T) + " exists in the scene.");
            return instance;
        }
    }

    // Create the reference in Awake()
    protected void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            Init();
        }
        else
        {
            Debug.LogWarning("An instance of " + typeof(T) + " already exists in the scene. Self-destructing.");
            Destroy(gameObject);
        }
    }

    // Destroy the reference in OnDestroy()
    protected void OnDestroy()
    {
        if (this == instance)
        {
            instance = null;
        }
    }

    // Init will replace the funtionality of Awake()
    protected virtual void Init(){}
}
