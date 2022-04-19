using UnityEngine;
using UnityEngine.Pool;

public class ComponentPool<T> where T : Component
{
	public IObjectPool<T> Value { get; }

	private Transform _parent;
	private T _prefab;

	public ComponentPool(Transform parent, T prefab, bool collectionCheck, int defaultCapacity, int maxSize)
	{
		_parent = parent;
		_prefab = prefab;
		Value = new ObjectPool<T>(CreatePooledItem, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject,
			collectionCheck, defaultCapacity, maxSize);
	}

	T CreatePooledItem()
	{
		var element = Object.Instantiate(_prefab, _parent);
		element.gameObject.SetActive(false);

		return element;
	}

	void OnReturnedToPool(T element)
	{
		element.gameObject.SetActive(false);
	}

	void OnTakeFromPool(T element)
	{
		element.gameObject.SetActive(true);
	}

	void OnDestroyPoolObject(T element)
	{
		Object.Destroy(element.gameObject);
	}
}