using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pool genérico mínimo para componentes 2D (proyectiles, sparks, etc.).
/// Uso:
///   var pool = new PoolSimple2D&lt;HechizoProyectil2D&gt;(prefab, transform, prewarm: 6);
///   var p = pool.Get(pos, rot);
///   pool.Release(p);
/// </summary>
public class PoolSimple2D<T> where T : Component
{
    private readonly T prefab;
    private readonly Transform padre;
    private readonly Queue<T> libres = new Queue<T>();
    private readonly int maxActivos;

    public PoolSimple2D(T prefab, Transform padre, int prewarm = 8, int maxActivos = 64)
    {
        this.prefab = prefab;
        this.padre = padre;
        this.maxActivos = maxActivos;

        for (int i = 0; i < prewarm; i++)
        {
            T item = Object.Instantiate(prefab, padre);
            item.gameObject.SetActive(false);
            libres.Enqueue(item);
        }
    }

    public T Get(Vector3 posicion, Quaternion rotacion)
    {
        T item = libres.Count > 0
            ? libres.Dequeue()
            : Object.Instantiate(prefab, padre);

        item.transform.SetPositionAndRotation(posicion, rotacion);
        item.gameObject.SetActive(true);
        return item;
    }

    public void Release(T item)
    {
        if (item == null) return;
        item.gameObject.SetActive(false);
        if (libres.Count < maxActivos)
            libres.Enqueue(item);
        else
            Object.Destroy(item.gameObject);
    }

    public int LibreCount => libres.Count;
}
