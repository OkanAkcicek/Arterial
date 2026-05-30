using System.Collections.Generic;
using FactoryCity.Core;
using FactoryCity.Data;
using FactoryCity.Buildings;

namespace FactoryCity.Trucks
{
    /// <summary>
    /// Rotaları ve kamyon havuzunu yöneten "aptal" dağıtıcı — saf C# (CLAUDE.md İlke 3).
    /// Akıllı/otomatik denge YOK: oyuncu kaç kamyon atadıysa o kadar çalıştırır.
    ///
    /// Kamyon listesi sahipliği (§5/§7): <see cref="allTrucks"/>, SimWorld.Trucks'a
    /// REFERANSTIR (kurucuda geçilir, kopyalanmaz). <see cref="freeTrucks"/> ayrı tutulur.
    /// </summary>
    public class Dispatcher
    {
        public List<TruckRoute> routes = new();
        public List<Truck> allTrucks;            // = SimWorld.Trucks (referans)
        public List<Truck> freeTrucks = new();   // henüz rotaya atanmamışlar

        public Dispatcher(List<Truck> trucks)
        {
            allTrucks = trucks; // kanonik listeye referans
        }

        public TruckRoute CreateRoute(Building src, Building dst)
        {
            var r = new TruckRoute
            {
                source = src,
                dest = dst,
                item = src != null ? src.output?.type : null
            };
            routes.Add(r);
            return r;
        }

        public void RemoveRoute(TruckRoute r)
        {
            if (r == null) return;

            // Rotadaki kamyonları serbest bırak (durdur, havuza döndür).
            foreach (var t in r.assignedTrucks)
            {
                ResetTruck(t);
                if (!freeTrucks.Contains(t)) freeTrucks.Add(t);
            }
            r.assignedTrucks.Clear();
            r.desiredTruckCount = 0;
            routes.Remove(r);
        }

        /// <summary>freeTrucks'tan bir kamyonu rotaya ekler; boş kamyon yoksa false.</summary>
        public bool AssignTruckToRoute(TruckRoute r)
        {
            if (r == null || freeTrucks.Count == 0) return false;

            int last = freeTrucks.Count - 1;
            Truck t = freeTrucks[last];
            freeTrucks.RemoveAt(last);

            r.assignedTrucks.Add(t);
            r.desiredTruckCount = r.assignedTrucks.Count;

            t.route = r;
            t.source = r.source;
            t.dest = r.dest;   // Idle ise sonraki tick'te döngüye girer
            return true;
        }

        /// <summary>Rotadan bir kamyonu serbest bırakır (havuza döndürür).</summary>
        public void UnassignTruckFromRoute(TruckRoute r)
        {
            if (r == null || r.assignedTrucks.Count == 0) return;

            int last = r.assignedTrucks.Count - 1;
            Truck t = r.assignedTrucks[last];
            r.assignedTrucks.RemoveAt(last);
            r.desiredTruckCount = r.assignedTrucks.Count;

            ResetTruck(t);
            if (!freeTrucks.Contains(t)) freeTrucks.Add(t);
        }

        /// <summary>Yeni kamyon üretir; allTrucks (=SimWorld.Trucks) + freeTrucks'a ekler.
        /// Para PAKET 7'de bağlanacak (şimdi bedava).</summary>
        public Truck BuyTruck(TruckDefinition def)
        {
            // Maliyet (Paket 7): parayı düş; yetmezse kamyon üretme.
            var economy = ServiceRegistry.Economy;
            if (economy != null && def != null && !economy.TrySpend(def.buyCost))
                return null;

            var t = new Truck(def);
            allTrucks.Add(t);   // kanonik liste = SimWorld.Trucks
            freeTrucks.Add(t);
            return t;
        }

        public void Tick()
        {
            // Aptal dispatcher: atanmış bir kamyon Idle olduysa rotanın source/dest'ini
            // ver ki state machine yeniden çalışsın. Döngüdeki kamyona dokunma.
            foreach (var r in routes)
                foreach (var t in r.assignedTrucks)
                    if (t.state == TruckState.Idle)
                    {
                        t.source = r.source;
                        t.dest = r.dest;
                    }
        }

        // Serbest bırakılan kamyonu temiz Idle'a çek (durur, yüksüz; eldeki yük düşer).
        private static void ResetTruck(Truck t)
        {
            t.route = null;
            t.state = TruckState.Idle;
            t.source = null;
            t.dest = null;
            t.cargo = 0;
            t.cargoType = null;
            t.path = null;
            t.edgeIndex = 0;
            t.edgeProgress = 0f;
            t.loadAccum = 0f;
        }
    }
}
