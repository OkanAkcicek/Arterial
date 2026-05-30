using System.Collections.Generic;
using FactoryCity.Buildings;
using FactoryCity.Core;
using FactoryCity.Roads;

namespace FactoryCity.Simulation
{
    /// <summary>
    /// Simülasyonun yetkili durumu ve tek sabit-adım giriş noktası.
    /// SAF C# — MonoBehaviour miras ALMAZ, UnityEngine tiplerine
    /// (Transform/GameObject) bağımlı OLMAZ. (CLAUDE.md İlke 3.)
    /// Bu sayede sim test edilebilir ve ileride kaydedilebilir kalır.
    /// </summary>
    public class SimWorld
    {
        public long TickCount;

        // --- Kanonik üyeler (CLAUDE.md §5) — ilgili paketlerde eklenecek ---
        public List<Building> Buildings = new();   // Paket 2 — yerleştirilmiş binalar
        // public List<Truck> Trucks;           // Paket 4   (kamyon listesi sahibi: BURASI)
        public RoadNetwork Roads;               // Paket 1 — yol grafı (saf C# graf verisi)
        // public Dispatcher Dispatcher;        // Paket 5
        // public EconomyManager Economy;       // Paket 6
        // public TrafficModel Traffic;         // Paket 8
        // public RegionManager Regions;        // Paket 7

        /// <summary>
        /// Simülasyonu tam olarak bir tick ilerletir. TickSystem.OnTick'ten
        /// (10 Hz) çağrılır. Tick sırası SABİTTİR — bkz. CLAUDE.md §6.
        /// </summary>
        public void Tick()
        {
            TickCount++;

            // --- Kanonik tick sırası (CLAUDE.md §6) — paketler ilerledikçe açılacak ---
            // Traffic.Recompute(Trucks);                                   // 1) Paket 8
            foreach (var b in Buildings) b.Tick(TickSystem.TickDelta);      // 2) üretim (Paket 3)
            // Dispatcher.Tick();                                           // 3) Paket 5
            // foreach (var t in Trucks) t.Tick(TickSystem.TickDelta);      // 4) Paket 4/5
        }
    }
}
