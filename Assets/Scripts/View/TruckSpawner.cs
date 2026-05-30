using UnityEngine;
using FactoryCity.Core;
using FactoryCity.Data;
using FactoryCity.Simulation;
using FactoryCity.Buildings;
using FactoryCity.Trucks;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FactoryCity.View
{
    /// <summary>
    /// DEBUG yardımcı (Paket 4): 'T' tuşuyla ilk Source → ilk Factory arasına bir kamyon
    /// spawn'lar. Sim <see cref="Truck"/>'ı oluşturup SimWorld.Trucks'a ekler, prefab'tan
    /// görsel üretip <see cref="TruckView"/> bağlar. Paket 5'te yerini RouteUI + Dispatcher alır.
    /// </summary>
    public class TruckSpawner : MonoBehaviour
    {
        [SerializeField] private TruckDefinition truckDef;

        private void Update()
        {
            if (SpawnKeyPressed()) SpawnTruck();
        }

        private void SpawnTruck()
        {
            if (truckDef == null) { Debug.LogError("TruckSpawner: truckDef atanmamış (inspector).", this); return; }
            if (truckDef.prefab == null) { Debug.LogError("TruckSpawner: truckDef.prefab atanmamış.", this); return; }

            var sim = ServiceRegistry.Sim;
            if (sim == null) return;

            Building source = FindFirst(sim, BuildingKind.Source);
            Building dest = FindFirst(sim, BuildingKind.Factory);
            if (source == null || dest == null)
            {
                Debug.LogError("TruckSpawner: en az bir Source ve bir Factory bina yerleştir.");
                return;
            }

            // Sim kamyonu (saf C#) — kanonik liste SimWorld.Trucks. Görselini artık
            // ViewSync üretir (havuzdan, def.prefab'tan); burada view oluşturmuyoruz.
            var truck = new Truck(truckDef) { source = source, dest = dest };
            sim.Trucks.Add(truck);

            Debug.Log($"Kamyon spawn (DEBUG): {source.def.displayName} → {dest.def.displayName} (kapasite {truck.capacity}).");
        }

        private static Building FindFirst(SimWorld sim, BuildingKind kind)
        {
            foreach (var b in sim.Buildings)
                if (b.def.kind == kind) return b;
            return null;
        }

        private bool SpawnKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null) return Keyboard.current.tKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.T);
#else
            return false;
#endif
        }
    }
}
