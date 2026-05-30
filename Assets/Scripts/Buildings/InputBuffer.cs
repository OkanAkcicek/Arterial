using System.Collections.Generic;
using FactoryCity.Data;

namespace FactoryCity.Buildings
{
    /// <summary>
    /// Çoklu tip girdi stoğu — SAF C# (CLAUDE.md İlke 3). Her tip için ayrı sayaç;
    /// tip başına <see cref="capacityPerType"/> tavanı var. Item'lar int sayaç (İlke 4).
    /// </summary>
    public class InputBuffer
    {
        private readonly Dictionary<ItemType, int> _stock = new();
        public int capacityPerType = 100;

        /// <summary>Taşmadan ekler; EKLENEMEYEN miktarı döndürür.</summary>
        public int Add(ItemType t, int n)
        {
            if (n <= 0) return 0;
            if (t == null) return n;           // ekleyemedik

            _stock.TryGetValue(t, out int cur);
            int space = capacityPerType - cur;
            if (space <= 0) return n;
            int added = n < space ? n : space;
            _stock[t] = cur + added;
            return n - added;
        }

        /// <summary>1 çıktılık girdinin tamamı stokta var mı?</summary>
        public bool HasEnough(List<RecipeInput> recipe)
        {
            if (recipe == null) return true;
            foreach (var ri in recipe)
            {
                if (ri == null || ri.type == null) continue;
                _stock.TryGetValue(ri.type, out int cur);
                if (cur < ri.amountPerOutput) return false;
            }
            return true;
        }

        /// <summary>1 çıktılık girdiyi düşer. (Önce <see cref="HasEnough"/> doğrulanmalı.)</summary>
        public void Consume(List<RecipeInput> recipe)
        {
            if (recipe == null) return;
            foreach (var ri in recipe)
            {
                if (ri == null || ri.type == null) continue;
                _stock.TryGetValue(ri.type, out int cur);
                int left = cur - ri.amountPerOutput;
                _stock[ri.type] = left < 0 ? 0 : left;
            }
        }

        public int GetCount(ItemType t)
        {
            if (t == null) return 0;
            _stock.TryGetValue(t, out int cur);
            return cur;
        }

        /// <summary>Tüm (tip, miktar) stokları — DebugHud/Port okur.</summary>
        public IEnumerable<(ItemType, int)> GetAllStocks()
        {
            foreach (var kv in _stock)
                yield return (kv.Key, kv.Value);
        }

        public void ClearAll() => _stock.Clear();
    }
}
