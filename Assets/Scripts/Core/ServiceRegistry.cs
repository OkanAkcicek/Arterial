using FactoryCity.Grid;
using FactoryCity.Simulation;

namespace FactoryCity.Core
{
    /// <summary>
    /// Statik servis bulucu — simülasyon manager'larına tek erişim noktası.
    /// DI framework KULLANILMAZ (CLAUDE.md §7). Manager'lar başlangıçta
    /// GameController tarafından kaydedilir, her yerden statik okunur.
    /// </summary>
    public static class ServiceRegistry
    {
        public static SimWorld Sim { get; set; }

        // --- Kanonik üyeler (CLAUDE.md §5) — ilgili paketlerde eklenecek ---
        public static GridManager Grid { get; set; }          // Paket 1
        // public static EconomyManager Economy { get; set; }  // Paket 6
    }
}
