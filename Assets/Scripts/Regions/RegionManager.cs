using System.Collections.Generic;
using FactoryCity.Grid;
using FactoryCity.Economy;

namespace FactoryCity.Regions
{
    /// <summary>
    /// Bölgeleri yönetir — SAF C# (CLAUDE.md İlke 3). Harita, regionSize×regionSize
    /// hücrelik dikdörtgen bloklara bölünür; <see cref="RegionOf"/> koordinattan O(1)
    /// blok hesabı yapar (hücre listesi taranmaz). Bölge 0 açık başlar.
    /// </summary>
    public class RegionManager
    {
        public List<Region> regions = new();
        public event System.Action<Region> OnRegionUnlocked;

        private int _regionSize = 20;
        private int _countX = 1;
        private int _countZ = 1;

        /// <summary>
        /// Haritayı bloklara böler ve Region listesini doldurur. Bölge 0 (blok 0,0)
        /// açık (unlockCost 0); diğerleri kilitli (unlockCost = baseCost).
        /// </summary>
        public void GenerateGrid(int regionSize, int countX, int countZ, long baseCost)
        {
            _regionSize = regionSize < 1 ? 1 : regionSize;
            _countX = countX < 1 ? 1 : countX;
            _countZ = countZ < 1 ? 1 : countZ;

            regions.Clear();
            int id = 0;
            for (int bz = 0; bz < _countZ; bz++)
            {
                for (int bx = 0; bx < _countX; bx++)
                {
                    var r = new Region
                    {
                        id = id,
                        displayName = $"Bölge {id}",
                        unlocked = (id == 0),
                        unlockCost = (id == 0) ? 0 : baseCost
                    };

                    for (int dz = 0; dz < _regionSize; dz++)
                        for (int dx = 0; dx < _regionSize; dx++)
                            r.cells.Add(new GridCoord(bx * _regionSize + dx, bz * _regionSize + dz));

                    regions.Add(r);
                    id++;
                }
            }
        }

        /// <summary>Koordinatın düştüğü bölge; tanımsızsa (negatif/dışarısı) null.</summary>
        public Region RegionOf(GridCoord c)
        {
            if (c.x < 0 || c.z < 0) return null;
            int bx = c.x / _regionSize;
            int bz = c.z / _regionSize;
            if (bx >= _countX || bz >= _countZ) return null;
            int id = bz * _countX + bx;
            return (id >= 0 && id < regions.Count) ? regions[id] : null;
        }

        /// <summary>Hücre inşa edilebilir mi (tanımlı bir bölgede ve o bölge açık).</summary>
        public bool IsCellBuildable(GridCoord c)
        {
            var r = RegionOf(c);
            return r != null && r.unlocked;
        }

        /// <summary>Parayı düşüp bölgeyi açar; para yetmezse/zaten açıksa false.</summary>
        public bool TryUnlock(Region r, EconomyManager econ)
        {
            if (r == null || r.unlocked) return false;
            if (econ != null && !econ.TrySpend(r.unlockCost)) return false;

            r.unlocked = true;
            OnRegionUnlocked?.Invoke(r);
            return true;
        }
    }
}
