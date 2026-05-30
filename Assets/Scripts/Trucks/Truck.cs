using System.Collections.Generic;
using UnityEngine;
using FactoryCity.Core;
using FactoryCity.Data;
using FactoryCity.Grid;
using FactoryCity.Roads;
using FactoryCity.Buildings;

namespace FactoryCity.Trucks
{
    /// <summary>
    /// Bir kamyonun SİM mantığı — saf veri + durum makinesi. MonoBehaviour DEĞİL
    /// (CLAUDE.md İlke 3). Vector3'ü yalnız DÜNYA KONUMU verisi olarak kullanır
    /// (İlke 3 buna izin verir); Transform/GameObject tutmaz. Yük int sayaçtır (İlke 4).
    /// Görselini TruckView yalnız OKUR.
    ///
    /// Döngü: ToSource → Loading → ToDest → Unloading → Returning → Idle → ...
    /// </summary>
    public class Truck
    {
        public TruckDefinition def;
        public int capacity;
        public ItemType cargoType;
        public int cargo;
        public Building source, dest;        // Paket 5'te TruckRoute'a taşınacak; şimdi doğrudan
        public TruckState state = TruckState.Idle;

        public List<RoadEdge> path;          // mevcut hedefe giden edge listesi
        public int edgeIndex;                // path içindeki indeks
        public float edgeProgress;           // 0..1 mevcut edge üzerinde
        public float loadAccum;              // kısmi yükleme/boşaltma biriktirici
        public const float LoadRate = 5f;    // birim/sn (CLAUDE.md §4)

        // public TruckRoute route;                    // Paket 5
        // public Vector3 prevWorldPos, currWorldPos;  // Paket 9 (interpolasyon)

        // Kamyonun park ettiği / mevcut edge'in GİRİŞ node'u. Yön takibi için şart:
        // path edge'leri yönsüz olduğundan ilerleme yönünü bu belirler.
        private RoadNode _currentNode;

        public Truck(TruckDefinition def)
        {
            this.def = def;
            capacity = def != null ? def.capacity : 0;
        }

        public void Tick(float dt)
        {
            var roads = ServiceRegistry.Sim?.Roads;
            if (roads == null) return;

            // İlk tick: kaynağın yol node'una park et.
            if (_currentNode == null && source != null)
                _currentNode = roads.NearestNode(source.EntranceCell);

            switch (state)
            {
                case TruckState.Idle:
                    if (source == null || dest == null) return;
                    BeginLeg();
                    state = TruckState.ToSource;
                    break;

                case TruckState.ToSource:
                    if (path == null) GoTo(source, roads);
                    if (MoveAlongPath(dt)) { state = TruckState.Loading; loadAccum = 0f; }
                    break;

                case TruckState.Loading:
                    DoLoading(dt);
                    break;

                case TruckState.ToDest:
                    if (path == null) GoTo(dest, roads);
                    if (MoveAlongPath(dt)) { state = TruckState.Unloading; loadAccum = 0f; }
                    break;

                case TruckState.Unloading:
                    DoUnloading(dt);
                    break;

                case TruckState.Returning:
                    if (path == null) GoTo(source, roads);
                    if (MoveAlongPath(dt)) state = TruckState.Idle;
                    break;
            }
        }

        private void DoLoading(float dt)
        {
            if (source == null) { BeginLeg(); state = TruckState.ToDest; return; }

            loadAccum += LoadRate * dt;
            while (loadAccum >= 1f && cargo < capacity)
            {
                int got = source.output.Remove(1);
                if (got <= 0) break;             // kaynak şu an boş
                cargo += got;
                cargoType = source.output.type;
                loadAccum -= 1f;
            }

            bool full = cargo >= capacity;
            bool sourceEmpty = source.output.count <= 0;
            // Doluysa, ya da eldeki yükle kaynak boşaldıysa → hedefe. Yük 0 ise bekle.
            if (full || (sourceEmpty && cargo > 0))
            {
                loadAccum = 0f;
                BeginLeg();
                state = TruckState.ToDest;
            }
        }

        private void DoUnloading(float dt)
        {
            if (dest == null || cargo <= 0) { FinishUnload(); return; }

            loadAccum += LoadRate * dt;
            while (loadAccum >= 1f && cargo > 0)
            {
                int notAdded = dest.input.Add(cargoType, 1);
                if (notAdded > 0) break;         // hedef girdi tamponu dolu → bekle
                cargo--;
                loadAccum -= 1f;
            }

            if (cargo <= 0) FinishUnload();
        }

        private void FinishUnload()
        {
            cargo = 0;
            cargoType = null;
            loadAccum = 0f;
            BeginLeg();
            state = TruckState.Returning;
        }

        /// <summary>Yeni yol bacağına hazırlık: path'i temizle, ilerlemeyi sıfırla.</summary>
        private void BeginLeg()
        {
            path = null;
            edgeIndex = 0;
            edgeProgress = 0f;
        }

        private void GoTo(Building target, RoadNetwork roads)
        {
            RoadNode dstNode = target != null ? roads.NearestNode(target.EntranceCell) : null;
            path = (_currentNode != null && dstNode != null)
                ? roads.FindPath(_currentNode, dstNode)
                : null;
            edgeIndex = 0;
            edgeProgress = 0f;
        }

        /// <summary>
        /// Mevcut path üzerinde ilerler. Vardıysa true. Rota yoksa (null) false (bekle);
        /// aynı node ise (boş path) anında varış. Trafik çarpanı Paket 8'de (şimdi = 1).
        /// </summary>
        public bool MoveAlongPath(float dt)
        {
            if (path == null) return false;          // rota yok → bekle
            if (path.Count == 0) return true;        // aynı node → anında varış
            if (edgeIndex >= path.Count) return true;

            RoadEdge e = path[edgeIndex];
            float len = Mathf.Max(e.length, 0.01f);
            edgeProgress += (def.speed * dt) / len;  // * trafik çarpanı (Paket 8)

            if (edgeProgress >= 1f)
            {
                edgeProgress = 0f;
                _currentNode = OtherEnd(e, _currentNode); // bu edge'in çıkış node'una vardık
                edgeIndex++;
                if (edgeIndex >= path.Count) return true;
            }
            return false;
        }

        /// <summary>Mevcut edge üzerinde dünya konumu (giriş→çıkış Lerp). View OKUR.</summary>
        public Vector3 WorldPosition(GridManager grid)
        {
            if (path != null && path.Count > 0 && edgeIndex < path.Count && _currentNode != null)
            {
                RoadEdge e = path[edgeIndex];
                RoadNode exit = OtherEnd(e, _currentNode);
                Vector3 a = grid.GridToWorld(_currentNode.pos);
                Vector3 b = grid.GridToWorld(exit.pos);
                return Vector3.Lerp(a, b, edgeProgress);
            }

            if (_currentNode != null) return grid.GridToWorld(_currentNode.pos);

            // Henüz park etmediyse kaynağın yol node'una düş.
            var roads = ServiceRegistry.Sim?.Roads;
            RoadNode n = (roads != null && source != null) ? roads.NearestNode(source.EntranceCell) : null;
            return n != null ? grid.GridToWorld(n.pos) : Vector3.zero;
        }

        private static RoadNode OtherEnd(RoadEdge e, RoadNode from)
            => (e.a == from) ? e.b : e.a;
    }
}
