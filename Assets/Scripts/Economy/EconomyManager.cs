namespace FactoryCity.Economy
{
    /// <summary>
    /// Oyuncunun para kasası — SAF C# (CLAUDE.md İlke 3). Port satışları buraya
    /// <see cref="Add"/> ile yazar; bina/kamyon maliyetleri (Paket 7) <see cref="TrySpend"/>
    /// kullanacak. UI <see cref="OnMoneyChanged"/>'e abone olup yalnız OKUR (İlke 1).
    /// </summary>
    public class EconomyManager
    {
        public long money;
        public event System.Action<long> OnMoneyChanged;

        public void Add(long n)
        {
            money += n;
            OnMoneyChanged?.Invoke(money);
        }

        public bool TrySpend(long n)
        {
            if (money < n) return false;
            money -= n;
            OnMoneyChanged?.Invoke(money);
            return true;
        }
    }
}
