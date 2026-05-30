using System.Collections.Generic;
using FactoryCity.Core;
using FactoryCity.Data;
using FactoryCity.Grid;

namespace FactoryCity.Buildings
{
    /// <summary>
    /// Yerleştirilmiş bir bina örneği — SAF C# (MonoBehaviour DEĞİL, CLAUDE.md İlke 3).
    /// Throughput tabanlı üretim: Source girdisiz üretir, Factory reçeteyle girdi
    /// tüketip çıktı üretir, Port (Paket 6) satar. Item'lar GameObject değil,
    /// buffer'larda int sayaçtır (İlke 4). Görsel GameObject'i View/Placement tutar.
    /// </summary>
    public class Building
    {
        public BuildingDefinition def;
        public GridCoord origin;               // sol-alt köşe hücresi
        public List<GridCoord> occupiedCells;  // footprint hücreleri

        public OutputBuffer output;
        public InputBuffer input;
        private float _accum;                  // üretilecek birim biriktirici (throughput)

        private const float AccumCap = 2f;     // birikme tavanı (şişmeyi önler)

        public Building(BuildingDefinition def, GridCoord origin, List<GridCoord> occupiedCells)
        {
            this.def = def;
            this.origin = origin;
            this.occupiedCells = occupiedCells;

            output = new OutputBuffer { type = def.outputType, capacity = def.bufferCapacity };
            input = new InputBuffer();
        }

        /// <summary>Yola bağlanılacak hücre = origin + def.entranceOffset.</summary>
        public GridCoord EntranceCell
            => new GridCoord(origin.x + def.entranceOffset.x, origin.z + def.entranceOffset.y);

        /// <summary>
        /// Bir tick'lik üretim. dt = TickSystem.TickDelta. Throughput dönüşümü:
        /// tick başına def.itemsPerMinute/60*dt birim biriktirir (CLAUDE.md §4).
        /// </summary>
        public void Tick(float dt)
        {
            if (def.kind == BuildingKind.Port)
            {
                SellEverything();                          // input'taki her şeyi sat (Paket 6)
                return;
            }
            if (output.count >= output.capacity) return;   // tampon dolu → dur

            bool needsInput = def.recipe != null && def.recipe.Count > 0;
            if (needsInput && !input.HasEnough(def.recipe)) return; // girdi yok → dur

            _accum += def.itemsPerMinute / 60f * dt;       // birim biriktir
            if (_accum > AccumCap) _accum = AccumCap;      // şişmeyi önle
            if (_accum < 1f) return;

            // (int) cast = pozitif değer için floor; Mathf'e gerek yok (sim saf C# kalsın).
            int units = (int)_accum;

            // Çıkış kapasitesini aşma.
            int space = output.capacity - output.count;
            int producible = units < space ? units : space;

            int produced = 0;
            for (int i = 0; i < producible; i++)
            {
                if (needsInput)
                {
                    if (!input.HasEnough(def.recipe)) break; // girdi bitti
                    input.Consume(def.recipe);
                }
                output.Add(1);
                produced++;
            }

            _accum -= produced; // yalnızca gerçekten ürettiğin kadar düş
        }

        // Port: input buffer'daki HER ŞEYİ sabit fiyattan (def.unitPrice) satar ve temizler.
        // EconomyManager erişimi ServiceRegistry üzerinden (CLAUDE.md §7). Fiyat, satılan
        // item tipinden bağımsız tek unitPrice (slice kararı).
        private void SellEverything()
        {
            var economy = ServiceRegistry.Economy;
            if (economy != null)
            {
                foreach (var (_, qty) in input.GetAllStocks())
                {
                    if (qty <= 0) continue;
                    economy.Add(qty * def.unitPrice);
                }
            }
            input.ClearAll();
        }
    }
}
