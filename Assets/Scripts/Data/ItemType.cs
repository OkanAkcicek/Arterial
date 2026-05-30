using UnityEngine;

namespace FactoryCity.Data
{
    /// <summary>
    /// Bir item türü (Demir, Plaka, Kömür...). Item'ın KENDİSİ GameObject değildir;
    /// buffer'larda int sayaç olarak yaşar (CLAUDE.md İlke 4). Bu SO yalnız tür
    /// kimliği + görsel veridir.
    /// </summary>
    [CreateAssetMenu(menuName = "FactoryCity/ItemType")]
    public class ItemType : ScriptableObject
    {
        public string displayName; // "Demir", "Plaka", "Kömür"
        public Sprite icon;        // opsiyonel
    }
}
