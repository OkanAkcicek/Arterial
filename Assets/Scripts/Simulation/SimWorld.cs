using System.Collections.Generic;
using FactoryCity.Buildings;
using FactoryCity.Core;
using FactoryCity.Economy;
using FactoryCity.Regions;
using FactoryCity.Roads;
using FactoryCity.Traffic;
using FactoryCity.Trucks;

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
        public List<Truck> Trucks = new();       // Paket 4 — kamyon listesi sahibi (§5)
        public RoadNetwork Roads;               // Paket 1 — yol grafı (saf C# graf verisi)
        public Dispatcher Dispatcher;           // Paket 5 — allTrucks = Trucks referansı (§5/§7)
        public EconomyManager Economy = new();   // Paket 6 — sabit fiyat satış kasası
        public TrafficModel Traffic = new();     // Paket 8 — yol doluluğu / hız çarpanı
        public RegionManager Regions = new();    // Paket 7 — bölge açma + bölge bağımlılığı

        public SimWorld()
        {
            // Dispatcher kanonik kamyon listesine (Trucks) REFERANS tutar (§5/§7).
            Dispatcher = new Dispatcher(Trucks);
        }

        /// <summary>
        /// Simülasyonu tam olarak bir tick ilerletir. TickSystem.OnTick'ten
        /// (10 Hz) çağrılır. Tick sırası SABİTTİR — bkz. CLAUDE.md §6.
        /// </summary>
        public void Tick()
        {
            TickCount++;

            // --- Kanonik tick sırası (CLAUDE.md §6) — paketler ilerledikçe açılacak ---
            Traffic.Recompute(Trucks);                                      // 1) yol dolulukları (Paket 8)
            foreach (var b in Buildings) b.Tick(TickSystem.TickDelta);      // 2) üretim (Paket 3)
            Dispatcher.Tick();                                              // 3) boş kamyonlara iş ata (Paket 5)
            foreach (var t in Trucks) t.Tick(TickSystem.TickDelta);         // 4) kamyon hareketi (Paket 4)
        }
    }
}
