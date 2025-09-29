using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Tilemaps;
using UnityEngine;
using static Define;

public class ObjectManager
{
	#region Id를 용한 Dic
	// 추후에 서버 붙으면 자주 이용할 오브젝트 매니저
	//Dictionary<int, GameObject> _objects = new Dictionary<int, GameObject>();
	//Dictionary<int, Item> _items = new Dictionary<int, Item>();

	//public void Add(int id, GameObject go)
	//{
	//	var r = _objects.ContainsKey(id);
	//	if (r == true)
	//		return;

	//	_objects.Add(id, go);


	//}

	//public void Remove(int id)
	//{
	//	_objects.Remove(id);
	//}

	//public GameObject Find(Func<GameObject, bool> condition)
	//{
	//	foreach (GameObject obj in _objects.Values)
	//	{
	//		if (condition.Invoke(obj))
	//			return obj;
	//	}

	//	return null;
	//}

	//public GameObject Find(int id)
	//{
	//	GameObject obj = null;
	//	_objects.TryGetValue(id, out obj);
	//	if (obj == null)
	//		return null;

	//	return obj;
	//}
	#endregion

	public HashSet<GameObject> _objects { get; private set; } = new HashSet<GameObject>();

    public EventHandler OnAdd;
    public EventHandler OnRemove;




    public void Add(GameObject go)
	{
		_objects.Add(go);
	}

	public void Remove(GameObject go)
	{
		_objects.Remove(go);
	}

	public GameObject Find(Func<GameObject, bool> condition)
	{
		foreach (GameObject obj in _objects)
		{
			if (condition.Invoke(obj))
				return obj;
		}

		return null;
	}

	public List<GameObject> FindList(Func<GameObject, bool> condition)
	{
		List<GameObject> list = new List<GameObject>();

		foreach (GameObject obj in _objects)
		{
			if (condition.Invoke(obj))
				list.Add(obj);
		}

		return list;
	}

	public void Clear()
	{
		_objects.Clear();
	}

    public IEnumerable<GameObject> GetObjectList()
    {
        return _objects;
    }

    public IEnumerable<T> GetObjectList<T>() where T : GameEntity
    {
        return _objects
            .Where(obj => obj.GetType() == typeof(T))
            .Cast<T>();
    }

    // 여러 이름으로 검색 (GameObject 그대로 반환)
    public IEnumerable<GameObject> GetObjectListByName(string[] names)
    {
        return _objects.Where(unit => names.Contains(unit.name));
    }

    // 여러 이름으로 검색 (특정 컴포넌트 T 반환)
    public IEnumerable<T> GetObjectListByName<T>(string[] names) where T : Component
    {
        return _objects
            .Where(unit => names.Contains(unit.name))
            .Select(unit => unit.GetComponent<T>())
            .Where(c => c != null);
    }

    // 단일 이름 검색 (GameObject 그대로 반환)
    public GameObject GetObjectByName(string name)
    {
        return _objects.FirstOrDefault(unit => unit.name == name);
    }

    // 단일 이름 검색 (특정 컴포넌트 T 반환)
    public T GetObjectByName<T>(string name) where T : Component
    {
        return _objects
            .Where(unit => unit.name == name)
            .Select(unit => unit.GetComponent<T>())
            .FirstOrDefault();
    }
}
