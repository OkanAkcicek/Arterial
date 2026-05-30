using System.Collections.Generic;
using FactoryCity.Grid;

namespace FactoryCity.Regions
{
    /// <summary>
    /// Harita üzerinde açılabilir bir bölge — SAF C# (CLAUDE.md İlke 3).
    /// <see cref="cells"/> bu bölgeye ait grid hücreleridir.
    /// </summary>
    public class Region
    {
        public int id;
        public string displayName;
        public List<GridCoord> cells = new();
        public bool unlocked;
        public long unlockCost;
    }
}
