using UnityEngine;

namespace FactoryCity.Data
{
    /// <summary>
    /// Kamyon türü tasarım sayıları. Bu pakette KULLANILMAZ; Paket 4'te (kamyon
    /// hareketi) devreye girer. Şimdi sadece tanım olarak var.
    /// </summary>
    [CreateAssetMenu(menuName = "FactoryCity/TruckDefinition")]
    public class TruckDefinition : ScriptableObject
    {
        public GameObject prefab;
        public int capacity = 20;
        public float speed = 8f;   // birim/sn (CLAUDE.md §4)
        public long buyCost;
    }
}
