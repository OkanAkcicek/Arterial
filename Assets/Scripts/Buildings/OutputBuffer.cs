using FactoryCity.Data;

namespace FactoryCity.Buildings
{
    /// <summary>
    /// Tek tip çıkış tamponu — SAF C# (CLAUDE.md İlke 3). Item'lar GameObject değil,
    /// burada <see cref="count"/> int sayacında yaşar (İlke 4).
    /// </summary>
    public class OutputBuffer
    {
        public ItemType type;
        public int count;
        public int capacity;

        /// <summary>Taşmadan ekler; EKLENEMEYEN miktarı döndürür.</summary>
        public int Add(int n)
        {
            if (n <= 0) return 0;
            int space = capacity - count;
            if (space <= 0) return n;        // yer yok → hepsi eklenemedi
            int added = n < space ? n : space;
            count += added;
            return n - added;
        }

        /// <summary>En fazla <see cref="count"/> kadar çeker; ÇEKİLEN miktarı döndürür.</summary>
        public int Remove(int n)
        {
            if (n <= 0) return 0;
            int taken = n < count ? n : count;
            count -= taken;
            return taken;
        }

        /// <summary>Tamponu boşaltır; çekilen toplam miktarı döndürür.</summary>
        public int RemoveAll()
        {
            int all = count;
            count = 0;
            return all;
        }
    }
}
