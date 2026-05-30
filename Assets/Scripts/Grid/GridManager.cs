using System.Collections.Generic;
using UnityEngine;

namespace FactoryCity.Grid
{
    /// <summary>
    /// Grid ↔ dünya dönüşümleri ve hücre doluluk kaydı. Saf C# (MonoBehaviour
    /// DEĞİL) ama koordinat dönüşümü için UnityEngine.Vector3 kullanması serbest
    /// (CLAUDE.md §5). GameController tarafından oluşturulur; CellSize oradan ayarlanır.
    ///
    /// Konvansiyon: dünya orijini (0,0,0), (0,0) hücresinin köşesidir; hücre
    /// merkezleri yarım CellSize kayıktır. Böylece GridToWorld/WorldToGrid tam
    /// gidiş-dönüş (round-trip) tutarlıdır.
    /// </summary>
    public class GridManager
    {
        public float CellSize = 4f;

        private readonly Dictionary<GridCoord, object> _occupants = new();

        /// <summary>Hücre merkezinin dünya konumu (y=0).</summary>
        public Vector3 GridToWorld(GridCoord c)
            => new Vector3((c.x + 0.5f) * CellSize, 0f, (c.z + 0.5f) * CellSize);

        /// <summary>Dünya konumunu içeren hücre.</summary>
        public GridCoord WorldToGrid(Vector3 w)
            => new GridCoord(Mathf.FloorToInt(w.x / CellSize), Mathf.FloorToInt(w.z / CellSize));

        public bool IsOccupied(GridCoord c) => _occupants.ContainsKey(c);

        public void SetOccupied(GridCoord c, object owner) => _occupants[c] = owner;

        public void ClearCell(GridCoord c) => _occupants.Remove(c);

        public object GetOwner(GridCoord c) => _occupants.TryGetValue(c, out var o) ? o : null;
    }
}
