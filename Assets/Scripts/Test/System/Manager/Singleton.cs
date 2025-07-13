using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component
{
    private static T instance;
    private static bool applicationIsQuitting = false;
    
    public static T Instance
    {
        get
        {
            if (applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                return null;
            }
            
            if (instance == null)
            {
                // 먼저 현재 씬에서 찾기
                instance = (T)FindObjectOfType(typeof(T));
                
                // 현재 씬에서 찾지 못했다면, DontDestroyOnLoad 씬에서 찾기
                if (instance == null)
                {
                    // 모든 씬에서 찾기 (DontDestroyOnLoad 포함)
                    T[] instances = FindObjectsOfType<T>();
                    if (instances.Length > 0)
                    {
                        instance = instances[0];
                        Debug.Log($"[Singleton] Found existing instance of '{typeof(T)}' in DontDestroyOnLoad scene.");
                    }
                }
                
                // 여전히 찾지 못했다면 새로 생성
                if (instance == null)
                {
                    SetupInstance();
                }
            }
            return instance;
        }
    }

    public virtual void Awake()
    {
        RemoveDuplicates();
    }

    private static void SetupInstance()
    {
        // 이미 인스턴스가 있는지 다시 한번 확인
        instance = (T)FindObjectOfType(typeof(T));
        
        if (instance == null)
        {
            // DontDestroyOnLoad 씬에서도 확인
            T[] instances = FindObjectsOfType<T>();
            if (instances.Length > 0)
            {
                instance = instances[0];
                Debug.Log($"[Singleton] Found existing instance of '{typeof(T)}' in DontDestroyOnLoad scene.");
                return;
            }
            
            // 새로 생성
            GameObject gameObj = new GameObject();
            gameObj.name = typeof(T).Name;
            instance = gameObj.AddComponent<T>();
            DontDestroyOnLoad(gameObj);
            
            Debug.Log($"[Singleton] Created new instance of '{typeof(T)}' with DontDestroyOnLoad.");
        }
    }
    
    private void RemoveDuplicates()
    {
        if (instance == null)
        {
            instance = this as T;
            
            // DontDestroyOnLoad 설정
            if (transform.parent != null && transform.root != null) 
            { 
                DontDestroyOnLoad(this.transform.root.gameObject); 
            }
            else 
            { 
                DontDestroyOnLoad(this.gameObject); 
            }
            
            Debug.Log($"[Singleton] Instance '{typeof(T)}' set with DontDestroyOnLoad.");
        }
        else if (instance != this)
        {
            Debug.LogWarning($"[Singleton] Duplicate instance of '{typeof(T)}' found. Destroying duplicate.");
            Destroy(gameObject);
        }
    }
    
    private void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }
    
    private void OnDestroy()
    {
        if (instance == this)
        {
            applicationIsQuitting = true;
        }
    }
}
