using System.Collections.Generic;
using UnityEngine;
using FactoryCity.Core;
using FactoryCity.Trucks;

namespace FactoryCity.View
{
    /// <summary>
    /// Tick'ler arası interpolasyon faktörünü (<see cref="Alpha"/>) üretir VE TruckView
    /// havuzunu yönetir (§5). Sim'i yalnız OKUR (İlke 1). Sim 10 Hz tick'te ilerler;
    /// görsel 60 FPS'te bu iki tick durumu arasında yumuşatır.
    ///
    /// Kamyon görselleri ObjectPool'dan gelir (oynanış sırasında Instantiate/Destroy yok).
    /// SimWorld.Trucks'ı aynalar: yeni kamyona havuzdan view verir, çıkan kamyonu havuza iade eder.
    /// (Paket 5'teki geçici TruckViewManager'ın yerini alır.)
    /// </summary>
    public class ViewSync : MonoBehaviour
    {
        [SerializeField] private TickSystem tickSystem;
        [Tooltip("Opsiyonel — kamyon görselleri bu Transform altına toplanır.")]
        [SerializeField] private Transform truckParent;

        /// <summary>Tick'ler arası interpolasyon faktörü [0,1]. TruckView okur.</summary>
        public float Alpha { get; private set; }

        private float _timeSinceTick;
        private readonly Dictionary<Truck, GameObject> _views = new();
        private readonly Dictionary<GameObject, ObjectPool> _pools = new(); // prefab -> havuz
        private readonly List<Truck> _toRemove = new();

        private void OnEnable()
        {
            if (tickSystem != null) tickSystem.OnTick += OnTick;
        }

        private void OnDisable()
        {
            if (tickSystem != null) tickSystem.OnTick -= OnTick;
        }

        private void OnTick() => _timeSinceTick = 0f;

        private void Update()
        {
            _timeSinceTick += Time.deltaTime;
            Alpha = Mathf.Clamp01(_timeSinceTick / TickSystem.TickDelta);

            SyncTruckViews();
        }

        private void SyncTruckViews()
        {
            var sim = ServiceRegistry.Sim;
            if (sim == null) return;

            // 1) Pozisyonu hazır, görseli olmayan kamyonlara havuzdan view ver.
            foreach (var t in sim.Trucks)
            {
                if (_views.ContainsKey(t)) continue;
                if (!t.PositionReady) continue;             // ilk tick'i bekle → origin flaşı yok
                if (t.def == null || t.def.prefab == null) continue;

                GameObject go = GetPool(t.def.prefab).Get();
                go.transform.position = t.currWorldPos;

                var view = go.GetComponent<TruckView>();
                if (view == null) view = go.AddComponent<TruckView>();
                view.Bind(t, this);

                _views[t] = go;
            }

            // 2) Sim'den çıkmış kamyonların görselini havuza iade et.
            _toRemove.Clear();
            foreach (var kv in _views)
                if (!sim.Trucks.Contains(kv.Key))
                    _toRemove.Add(kv.Key);

            for (int i = 0; i < _toRemove.Count; i++)
            {
                var t = _toRemove[i];
                if (_views.TryGetValue(t, out var go) && go != null)
                {
                    var view = go.GetComponent<TruckView>();
                    if (view != null) view.Unbind();

                    GameObject prefab = t.def != null ? t.def.prefab : null;
                    if (prefab != null) GetPool(prefab).Release(go);
                    else Destroy(go);
                }
                _views.Remove(t);
            }
        }

        private ObjectPool GetPool(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new ObjectPool(prefab, 0, truckParent);
                _pools[prefab] = pool;
            }
            return pool;
        }
    }
}
