using System.Collections.Generic;
using FactoryCity.Data;
using FactoryCity.Grid;

namespace FactoryCity.Buildings
{
    /// <summary>
    /// Yerleştirilmiş bir bina örneği — SAF C# (MonoBehaviour DEĞİL, CLAUDE.md İlke 3).
    /// Tasarım sayıları <see cref="def"/>'te (SO); burada yalnız örneğe özel durum
    /// (konum, kapladığı hücreler, ileride buffer'lar) tutulur. Görsel GameObject'i
    /// PlacementController/View tarafı tutar — bu sınıf tutmaz.
    /// </summary>
    public class Building
    {
        public BuildingDefinition def;
        public GridCoord origin;               // sol-alt köşe hücresi
        public List<GridCoord> occupiedCells;  // footprint hücreleri

        /// <summary>Yola bağlanılacak hücre = origin + def.entranceOffset.</summary>
        public GridCoord EntranceCell
            => new GridCoord(origin.x + def.entranceOffset.x, origin.z + def.entranceOffset.y);

        // --- Paket 3'te eklenecek (görselsiz üretim) ---
        // public OutputBuffer output;   // çıkış tamponu
        // public InputBuffer input;     // girdi stokları (Factory/Port)
        // private float _accum;         // throughput biriktirici
        // public void Tick(float dt) { ... }  // üretim; Port dalı satış (§5 notu)
    }
}
