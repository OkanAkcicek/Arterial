using System.Collections.Generic;
using UnityEngine;
using FactoryCity.Core;
using FactoryCity.Regions;

namespace FactoryCity.View
{
    /// <summary>
    /// Bölgelerin GÖRSELİ + açma UI'ı (MonoBehaviour). Sim'i yalnız OKUR; bölge açmayı
    /// RegionManager.TryUnlock komutuyla yapar (komut yolu — İlke 1).
    /// - Kilitli bölgelerin üstüne (opsiyonel) karartma quad'ı koyar; açılınca kaldırır.
    /// - Alt-orta OnGUI paneli: her bölge için durum + "Aç (maliyet)" butonu (para yetmezse pasif).
    /// </summary>
    public class RegionView : MonoBehaviour
    {
        [Tooltip("Opsiyonel — kilitli bölgeyi karartan malzeme. Boşsa görsel overlay çizilmez (kilit kuralı yine çalışır).")]
        [SerializeField] private Material lockedOverlayMaterial;
        [SerializeField] private float overlayY = 0.05f;

        private readonly Dictionary<Region, GameObject> _overlays = new();
        private bool _built;
        private bool _subscribed;

        private void Update()
        {
            var sim = ServiceRegistry.Sim;
            if (sim?.Regions == null) return;

            if (!_subscribed)
            {
                sim.Regions.OnRegionUnlocked += OnUnlocked;
                _subscribed = true;
            }

            if (!_built && lockedOverlayMaterial != null)
                BuildOverlays();
        }

        private void OnDestroy()
        {
            var sim = ServiceRegistry.Sim;
            if (sim?.Regions != null && _subscribed)
                sim.Regions.OnRegionUnlocked -= OnUnlocked;
        }

        private void BuildOverlays()
        {
            var grid = ServiceRegistry.Grid;
            var sim = ServiceRegistry.Sim;
            if (grid == null || sim?.Regions == null) return;

            foreach (var r in sim.Regions.regions)
            {
                if (r.unlocked || _overlays.ContainsKey(r) || r.cells.Count == 0) continue;

                int minX = int.MaxValue, maxX = int.MinValue, minZ = int.MaxValue, maxZ = int.MinValue;
                foreach (var c in r.cells)
                {
                    if (c.x < minX) minX = c.x;
                    if (c.x > maxX) maxX = c.x;
                    if (c.z < minZ) minZ = c.z;
                    if (c.z > maxZ) maxZ = c.z;
                }

                float cs = grid.CellSize;
                float wMinX = minX * cs, wMaxX = (maxX + 1) * cs;
                float wMinZ = minZ * cs, wMaxZ = (maxZ + 1) * cs;
                Vector3 center = new Vector3((wMinX + wMaxX) * 0.5f, overlayY, (wMinZ + wMaxZ) * 0.5f);

                var go = GameObject.CreatePrimitive(PrimitiveType.Plane); // 10x10, +Y normal
                go.name = $"LockOverlay_{r.id}";
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                go.transform.SetParent(transform, false);
                go.transform.position = center;
                go.transform.localScale = new Vector3((wMaxX - wMinX) / 10f, 1f, (wMaxZ - wMinZ) / 10f);
                go.GetComponent<MeshRenderer>().sharedMaterial = lockedOverlayMaterial;

                _overlays[r] = go;
            }
            _built = true;
        }

        private void OnUnlocked(Region r)
        {
            if (_overlays.TryGetValue(r, out var go) && go != null) Destroy(go);
            _overlays.Remove(r);
        }

        private void OnGUI()
        {
            var sim = ServiceRegistry.Sim;
            if (sim?.Regions == null) return;
            var econ = ServiceRegistry.Economy;

            const float w = 380f;
            float h = 44f + sim.Regions.regions.Count * 24f;
            var area = new Rect((Screen.width - w) * 0.5f, Screen.height - h - 10f, w, h);
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label("— BÖLGELER (aç = para harca) —");

            foreach (var r in sim.Regions.regions)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{r.displayName}: {(r.unlocked ? "AÇIK" : "kilitli")}", GUILayout.Width(160));
                if (!r.unlocked)
                {
                    bool canAfford = econ == null || econ.money >= r.unlockCost;
                    GUI.enabled = canAfford;
                    if (GUILayout.Button($"Aç ({r.unlockCost})", GUILayout.Width(160)))
                        sim.Regions.TryUnlock(r, econ);
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }
    }
}
