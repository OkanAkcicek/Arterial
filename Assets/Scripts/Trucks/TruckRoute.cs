using System.Collections.Generic;
using FactoryCity.Data;
using FactoryCity.Buildings;

namespace FactoryCity.Trucks
{
    /// <summary>
    /// Bir taşıma rotası — kaynak bina → hedef bina, ve bu rotaya atanmış kamyonlar.
    /// Saf C# (CLAUDE.md İlke 3). Oyuncu kurar; Dispatcher işletir.
    /// </summary>
    public class TruckRoute
    {
        public Building source, dest;
        public ItemType item;                       // = source.output.type
        public List<Truck> assignedTrucks = new();
        public int desiredTruckCount;               // = assignedTrucks.Count (bookkeeping)
    }
}
