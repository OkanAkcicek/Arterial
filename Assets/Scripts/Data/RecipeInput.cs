namespace FactoryCity.Data
{
    /// <summary>
    /// Reçete girdisi: 1 çıktı üretmek için gereken (item türü, miktar). SO DEĞİL —
    /// BuildingDefinition içinde liste olarak serileşir.
    /// </summary>
    [System.Serializable]
    public class RecipeInput
    {
        public ItemType type;
        public int amountPerOutput; // ör. 1 plaka için 2 demir → 2
    }
}
