using System.Collections.Generic;
using FactoryCity.Core;
using FactoryCity.Roads;
using FactoryCity.Trucks;

namespace FactoryCity.Traffic
{
    /// <summary>
    /// Yol tıkanıklığı modeli — SAF C# (CLAUDE.md İlke 3, Mathf YOK). Her tick başında
    /// tüm edge.occupancy sıfırlanır, hareket halindeki her kamyonun bulunduğu edge +1
    /// olur. <see cref="SpeedMultiplier"/> dolu segmentte hızı düşürür → "sadece kamyon
    /// ekle" tıkanmaya çarpar, oyuncu ağ tasarımına zorlanır (vizyon §1).
    /// </summary>
    public class TrafficModel
    {
        public void Recompute(List<Truck> trucks)
        {
            // 1) Tüm bilinen edge'lerin doluluğunu sıfırla (her edge tek sefer — AllEdges dedupe'lu).
            var roads = ServiceRegistry.Sim?.Roads;
            if (roads != null)
                foreach (var e in roads.AllEdges())
                    e.occupancy = 0;

            // 2) Hareket halindeki her kamyon, üzerinde olduğu edge'i +1.
            if (trucks == null) return;
            foreach (var t in trucks)
            {
                if (t.state != TruckState.ToSource &&
                    t.state != TruckState.ToDest &&
                    t.state != TruckState.Returning)
                    continue;

                var path = t.path;
                if (path != null && t.edgeIndex >= 0 && t.edgeIndex < path.Count)
                    path[t.edgeIndex].occupancy++;
            }
        }

        /// <summary>
        /// Edge'in doluluğuna göre hız çarpanı: occupancy arttıkça düşer.
        /// 1/(1+occ*0.5), [0.2, 1] aralığına kıstırılmış. (Manuel clamp — sim saf C#.)
        /// </summary>
        public float SpeedMultiplier(RoadEdge e)
        {
            if (e == null) return 1f;
            float m = 1f / (1f + e.occupancy * 0.5f);
            if (m < 0.2f) m = 0.2f;
            if (m > 1f) m = 1f;
            return m;
        }
    }
}
