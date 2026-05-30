using System.Collections.Generic;
using UnityEngine;

namespace FactoryCity.View
{
    /// <summary>
    /// Basit GameObject havuzu (normal class, MonoBehaviour DEĞİL — §5). Oynanış
    /// sırasında Instantiate/Destroy yerine pasif örnekleri yeniden kullanır → GC baskısı düşük.
    /// </summary>
    public class ObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Stack<GameObject> _available = new();

        public ObjectPool(GameObject prefab, int prewarm, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
            for (int i = 0; i < prewarm; i++)
            {
                var go = Object.Instantiate(_prefab, _parent);
                go.SetActive(false);
                _available.Push(go);
            }
        }

        /// <summary>Pasif birini aktive et; yoksa yeni instantiate et.</summary>
        public GameObject Get()
        {
            GameObject go = _available.Count > 0
                ? _available.Pop()
                : Object.Instantiate(_prefab, _parent);
            go.SetActive(true);
            return go;
        }

        /// <summary>Deaktive edip havuza geri koy.</summary>
        public void Release(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            _available.Push(go);
        }
    }
}
