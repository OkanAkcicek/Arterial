namespace FactoryCity.Roads
{
    /// <summary>
    /// İki komşu yol düğümü arasındaki kenar. <see cref="length"/> dünya birimi
    /// mesafedir (komşu hücreler arası = CellSize). <see cref="occupancy"/> trafik
    /// için sonraki paketlerde (Paket 8) kullanılacak; şimdilik 0.
    /// </summary>
    public class RoadEdge
    {
        public RoadNode a, b;
        public float length;
        public int occupancy; // Paket 8 (trafik) — şimdilik 0

        public RoadEdge(RoadNode a, RoadNode b, float length)
        {
            this.a = a;
            this.b = b;
            this.length = length;
        }
    }
}
