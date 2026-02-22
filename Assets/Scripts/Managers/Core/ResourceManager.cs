using UnityEngine;

public class ResourceManager : IManager
{
    public void Init()
    {
    }

    public void Clear()
    {
    }

    public T Load<T>(string path) where T : UnityEngine.Object
    {
        // 프리퀄 체크 (Pooling 시스템이 있다면 원본을 먼저 확인)
        if (typeof(T) == typeof(GameObject) && Managers.Pool != null)
        {
            string name = GetNameFromPath(path);
            GameObject original = Managers.Pool.GetOriginal(name);
            if (original != null) return original as T;
        }

        return Resources.Load<T>(path);
    }

    public GameObject Instantiate(string path, Transform parent = null)
    {
        GameObject original = Load<GameObject>($"Prefabs/{path}");
        if (original == null) return null;

        return Instantiate(original, parent);
    }

    public GameObject Instantiate(GameObject original, Transform parent = null)
    {
        if (original == null) return null;

        // 설계 원칙: Poolable 체크 후 분기 처리
        // 직접 Managers.Pool.Pop을 부르지 않고 주입된 시스템을 이용함
        if (Managers.Pool != null && original.GetComponent<Poolable>() != null)
            return Managers.Pool.Pop(original, parent).gameObject;

        GameObject go = Object.Instantiate(original, parent);
        go.name = original.name;
        return go;
    }

    public GameObject Instantiate(GameObject go, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        GameObject obj = Instantiate(go, parent);
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        return obj;
    }

    public GameObject Instantiate(GameObject go, Quaternion rotation, Transform parent = null)
    {
        GameObject obj = Instantiate(go, parent);
        obj.transform.rotation = rotation;
        return obj;
    }

    public GameObject Instantiate(GameObject go, Vector3 position, Transform parent = null)
    {
        GameObject obj = Instantiate(go, parent);
        obj.transform.position = position;
        return obj;
    }

    public T Instantiate<T>(GameObject go, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
    {
        go.transform.position = position;
        go.transform.rotation = rotation;

        return Instantiate<T>(go, parent);
    }

    public T Instantiate<T>(GameObject go, Vector3 position, Transform parent = null) where T : Component
    {
        go.transform.position = position;

        return Instantiate<T>(go, parent);

    }

    public T Instantiate<T>(GameObject go, Quaternion rotation, Transform parent = null) where T : Component
    {
        go.transform .rotation = rotation;

        return Instantiate<T>(go, parent);
    }

    public T Instantiate<T>(GameObject go, Transform parent = null) where T : Component
    {
        return Instantiate(go, parent).GetComponent<T>();
    }

    public void Destroy(GameObject go)
    {
        if (go == null)
            return;

        if(Managers.Pool != null)
        {
            Poolable poolable = go.GetComponent<Poolable>();
            if (poolable != null)
            {
                Managers.Pool.Push(poolable);
                return;
            }
        }

        Object.Destroy(go);
    }

    private string GetNameFromPath(string path)
    {
        int index = path.LastIndexOf('/');
        return index >= 0 ? path.Substring(index + 1) : path;
    }
}


// ResourceManager 자체도 인터페이스로 관리 (다른 곳에서 참조용)
public interface IResourceService
{
    T Load<T>(string path) where T : UnityEngine.Object;
    GameObject Instantiate(string path, Transform parent = null);
    void Destroy(GameObject go);
}