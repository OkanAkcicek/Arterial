using System.Collections.Generic;
using FactoryCity.Grid;

namespace FactoryCity.Roads
{
    /// <summary>
    /// Yol grafında bir düğüm — tek bir yol hücresine karşılık gelir. Saf C#
    /// graf verisi (CLAUDE.md İlke 3); görsel/prefab tutmaz.
    /// </summary>
    public class RoadNode
    {
        public GridCoord pos;
        public List<RoadEdge> edges = new();

        public RoadNode(GridCoord pos)
        {
            this.pos = pos;
        }
    }
}
