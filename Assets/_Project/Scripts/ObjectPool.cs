// ObjectPool.cs (generic simple pool for Targets)
using System.Collections.Generic;
using UnityEngine;


public class ObjectPool<T> where T : Component
{
    readonly T prefab; readonly Transform parent; readonly Stack<T> stack = new();
    public ObjectPool(T prefab, int initial, Transform parent) { this.prefab = prefab; this.parent = parent; for (int i = 0; i < initial; i++) stack.Push(Object.Instantiate(prefab, parent)); }
    public T Get() { return stack.Count > 0 ? Activate(stack.Pop()) : Activate(Object.Instantiate(prefab, parent)); }
    T Activate(T t) { t.gameObject.SetActive(true); return t; }
    public void Return(T t) { t.gameObject.SetActive(false); stack.Push(t); }
}

