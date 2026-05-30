namespace FactoryCity.Grid
{
    /// <summary>
    /// Grid üzerinde tam sayı hücre koordinatı. Saf C# — Dictionary anahtarı
    /// olarak kullanılır (bu yüzden Equals/GetHashCode/== gerekli). Pozisyonlar
    /// dünya konumu değil, hücre indeksi olarak tutulur. (CLAUDE.md İlke 3.)
    /// </summary>
    public struct GridCoord : System.IEquatable<GridCoord>
    {
        public int x, z;

        public GridCoord(int x, int z)
        {
            this.x = x;
            this.z = z;
        }

        /// <summary>4 ortogonal komşu: kuzey, güney, doğu, batı.</summary>
        public GridCoord[] Neighbors() => new[]
        {
            new GridCoord(x, z + 1), // kuzey (+z)
            new GridCoord(x, z - 1), // güney (-z)
            new GridCoord(x + 1, z), // doğu  (+x)
            new GridCoord(x - 1, z), // batı  (-x)
        };

        public bool Equals(GridCoord other) => x == other.x && z == other.z;
        public override bool Equals(object obj) => obj is GridCoord o && Equals(o);
        public override int GetHashCode() => unchecked((x * 397) ^ z);

        public static bool operator ==(GridCoord a, GridCoord b) => a.Equals(b);
        public static bool operator !=(GridCoord a, GridCoord b) => !a.Equals(b);

        public override string ToString() => $"({x},{z})";
    }
}
