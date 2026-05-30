using System.Collections.Generic;
using UnityEngine;

namespace FactoryCity.Data
{
    /// <summary>
    /// Bir bina türünün TÜM tasarım sayıları (footprint, throughput tabanlı üretim
    /// hızı, tampon, fiyat...). Sim bunu OKUR; denge değişikliği kod değil asset işidir.
    /// </summary>
    [CreateAssetMenu(menuName = "FactoryCity/BuildingDefinition")]
    public class BuildingDefinition : ScriptableObject
    {
        public string displayName;
        public BuildingKind kind;
        public GameObject prefab;                 // editörde Kenney prefab atanır
        public Vector2Int footprint = new(1, 1);  // kapladığı hücre (x=genişlik, y=derinlik→z)
        public Vector2Int entranceOffset;         // yola bağlanacağı hücre (origin'e göre offset)
        public ItemType outputType;               // Source/Factory ürünü
        public float itemsPerMinute;              // üretim hızı (timer DEĞİL — throughput)
        public int bufferCapacity = 50;           // çıkış tamponu kapasitesi
        public List<RecipeInput> recipe = new();  // boşsa hammadde (girdisiz üretir)
        public long buildCost;
        public long unitPrice;                    // yalnız Port için anlamlı
    }
}
