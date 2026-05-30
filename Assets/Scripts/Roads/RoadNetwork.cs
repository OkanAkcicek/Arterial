using System.Collections.Generic;
using FactoryCity.Grid;

namespace FactoryCity.Roads
{
    /// <summary>
    /// Yol grafı (node + edge) — SADECE veri. Saf C# (CLAUDE.md İlke 3); prefab
    /// instantiate/destroy işi görsel tarafa (PlacementController) aittir.
    /// Yol bulma BFS'tir (A* DEĞİL — ağ küçük, CLAUDE.md §3).
    /// </summary>
    public class RoadNetwork
    {
        private readonly Dictionary<GridCoord, RoadNode> _nodes = new();
        private readonly HashSet<RoadEdge> _edges = new();

        // Eşit uzunluktaki yollar arasında tie-break için (paralel yollar dağılsın — Paket 8).
        private readonly System.Random _rng = new();

        /// <summary>Debug/HUD için düğüm sayısı.</summary>
        public int NodeCount => _nodes.Count;

        /// <summary>
        /// Verilen hücrede yol düğümü oluşturur ve bitişik mevcut yol hücreleriyle
        /// kenar bağlar. Edge uzunluğu = CellSize (komşu hücreler arası mesafe).
        /// </summary>
        public void AddRoad(GridCoord c, GridManager grid)
        {
            if (_nodes.ContainsKey(c)) return; // zaten yol var

            var node = new RoadNode(c);
            _nodes[c] = node;

            foreach (var n in c.Neighbors())
            {
                if (!_nodes.TryGetValue(n, out var other)) continue;

                var edge = new RoadEdge(node, other, grid.CellSize);
                node.edges.Add(edge);
                other.edges.Add(edge);
                _edges.Add(edge);
            }
        }

        /// <summary>Düğümü ve ona bağlı tüm kenarları temizler.</summary>
        public void RemoveRoad(GridCoord c)
        {
            if (!_nodes.TryGetValue(c, out var node)) return;

            foreach (var e in node.edges)
            {
                var other = (e.a == node) ? e.b : e.a;
                other.edges.Remove(e);
                _edges.Remove(e);
            }

            node.edges.Clear();
            _nodes.Remove(c);
        }

        public RoadNode GetNodeAt(GridCoord c)
            => _nodes.TryGetValue(c, out var node) ? node : null;

        /// <summary>Bir hücreye (ör. bina giriş hücresi) en yakın yol düğümü.</summary>
        public RoadNode NearestNode(GridCoord c)
        {
            RoadNode best = null;
            int bestDist = int.MaxValue;

            foreach (var node in _nodes.Values)
            {
                // Manhattan mesafesi — yollar ortogonal grid üzerinde.
                int d = System.Math.Abs(node.pos.x - c.x) + System.Math.Abs(node.pos.z - c.z);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = node;
                    if (d == 0) break;
                }
            }

            return best;
        }

        /// <summary>
        /// <paramref name="from"/> → <paramref name="to"/> arası kenar listesi (BFS).
        /// Aynı düğümse boş liste; yol yoksa null döner.
        /// </summary>
        public List<RoadEdge> FindPath(RoadNode from, RoadNode to)
        {
            if (from == null || to == null) return null;
            if (from == to) return new List<RoadEdge>();

            var cameFromEdge = new Dictionary<RoadNode, RoadEdge>();
            var cameFromNode = new Dictionary<RoadNode, RoadNode>();
            var visited = new HashSet<RoadNode> { from };
            var queue = new Queue<RoadNode>();
            queue.Enqueue(from);

            bool found = false;
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (cur == to) { found = true; break; }

                // Komşuları rastgele başlangıç ofsetiyle gez: BFS yine EN KISA yolu bulur,
                // ama eşit uzunluktaki yollar arasında seçim değişir → kamyonlar paralel
                // yollara dağılır (trafik rahatlar). Determinizm yerine yük dengesi tercih.
                int cnt = cur.edges.Count;
                int off = cnt > 1 ? _rng.Next(cnt) : 0;
                for (int k = 0; k < cnt; k++)
                {
                    var e = cur.edges[(off + k) % cnt];
                    var nxt = (e.a == cur) ? e.b : e.a;
                    if (!visited.Add(nxt)) continue; // zaten görüldü

                    cameFromEdge[nxt] = e;
                    cameFromNode[nxt] = cur;
                    queue.Enqueue(nxt);
                }
            }

            if (!found) return null;

            // Kenarları `to`'dan `from`'a geri izleyip ters çevir.
            var path = new List<RoadEdge>();
            var walk = to;
            while (walk != from)
            {
                path.Add(cameFromEdge[walk]);
                walk = cameFromNode[walk];
            }
            path.Reverse();
            return path;
        }

        /// <summary>Tüm benzersiz kenarlar (Paket 8 trafik modeli okuyacak).</summary>
        public IEnumerable<RoadEdge> AllEdges() => _edges;
    }
}
