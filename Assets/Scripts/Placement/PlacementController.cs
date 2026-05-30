using System.Collections.Generic;
using UnityEngine;
using FactoryCity.Core;
using FactoryCity.Grid;
using FactoryCity.Data;
using FactoryCity.Buildings;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FactoryCity.Placement
{
    /// <summary>
    /// Yol + bina yerleştirme — GÖRSEL + GİRDİ tarafı (MonoBehaviour). Fareyle gride
    /// snap'lenip yol/bina prefab'ı koyar/kaldırır ve arkadaki sim verisini
    /// (RoadNetwork grafı, SimWorld.Buildings) günceller. ÖNEMLİ: sim/graf yalnız
    /// veri tutar; prefab instantiate/destroy işi BURADA yapılır (CLAUDE.md İlke 1).
    ///
    /// Girdi: R = yol modu, 1/2/3 = palette'ten bina seç. Yol modunda sol=koy (sürükle),
    /// sağ=kaldır, P=FindPath testi. Bina modunda sol tık = footprint'le yerleştir.
    /// </summary>
    public class PlacementController : MonoBehaviour
    {
        private enum PlaceMode { Road, Building }

        [Header("Referanslar")]
        [Tooltip("Boşsa Camera.main kullanılır.")]
        [SerializeField] private Camera cam;
        [Tooltip("Gride konacak yol prefab'ı (Kenney yol modülü).")]
        [SerializeField] private GameObject roadPrefab;
        [Tooltip("Opsiyonel — konan yollar bu Transform altına toplanır.")]
        [SerializeField] private Transform roadParent;

        [Header("Bina paleti (1/2/3 ile seçilir)")]
        [SerializeField] private List<BuildingDefinition> palette = new();
        [Tooltip("Opsiyonel — konan binalar bu Transform altına toplanır.")]
        [SerializeField] private Transform buildingParent;

        private PlaceMode _mode = PlaceMode.Road;
        private BuildingDefinition _selectedBuilding;

        // Konan yolların GÖRSEL kayıtları (graf DEĞİL). Hücre -> prefab örneği.
        private readonly Dictionary<GridCoord, GameObject> _roadViews = new();

        // Debug: FindPath testi için ilk/son konan yol hücresi.
        private GridCoord? _firstRoadCell;
        private GridCoord? _lastRoadCell;

        // y=0 zemin düzlemi — fiziksel collider gerektirmez.
        private readonly Plane _groundPlane = new Plane(Vector3.up, Vector3.zero);

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
        }

        private void Update()
        {
            // Servisler hazır değilse (GameController henüz kurmadıysa) bekle.
            if (ServiceRegistry.Grid == null || ServiceRegistry.Sim == null) return;

            HandleModeKeys();

            switch (_mode)
            {
                case PlaceMode.Road:
                    if (ServiceRegistry.Sim.Roads == null) return;
                    if (LeftHeld()) TryPlaceRoad();
                    else if (RightHeld()) TryRemoveRoad();
                    if (PathTestPressed()) RunPathTest();
                    break;

                case PlaceMode.Building:
                    if (LeftPressed()) TryPlaceBuilding(); // tek tık (held değil)
                    break;
            }
        }

        // ---- Mod seçimi ----

        private void HandleModeKeys()
        {
            if (RoadModeKey())
            {
                _mode = PlaceMode.Road;
                Debug.Log("Mod: Yol döşeme");
                return;
            }

            int sel = BuildingSelectKey();
            if (sel < 0) return;

            if (palette != null && sel < palette.Count && palette[sel] != null)
            {
                _selectedBuilding = palette[sel];
                _mode = PlaceMode.Building;
                Debug.Log($"Mod: Bina — {_selectedBuilding.displayName} " +
                          $"({_selectedBuilding.footprint.x}x{_selectedBuilding.footprint.y})");
            }
            else
            {
                Debug.Log($"Palette[{sel}] boş — bina atanmamış.");
            }
        }

        // ---- Yol ----

        private void TryPlaceRoad()
        {
            if (roadPrefab == null)
            {
                Debug.LogError("PlacementController: roadPrefab atanmamış (inspector).", this);
                return;
            }
            if (!RaycastToCell(out var cell)) return;

            var grid = ServiceRegistry.Grid;
            if (grid.IsOccupied(cell)) return; // üst üste koyma yok

            var roads = ServiceRegistry.Sim.Roads;

            // GÖRSEL: prefab'ı hücre merkezine koy.
            Vector3 pos = grid.GridToWorld(cell);
            GameObject go = Instantiate(roadPrefab, pos, Quaternion.identity, roadParent);
            _roadViews[cell] = go;

            // VERİ: graf + doluluk. Owner olarak graf düğümünü tutuyoruz.
            roads.AddRoad(cell, grid);
            grid.SetOccupied(cell, roads.GetNodeAt(cell));

            if (_firstRoadCell == null) _firstRoadCell = cell;
            _lastRoadCell = cell;

            Debug.Log($"Yol kondu {cell} — node sayısı: {roads.NodeCount}");
        }

        private void TryRemoveRoad()
        {
            if (!RaycastToCell(out var cell)) return;

            // Sadece BİZİM koyduğumuz yolları kaldır (bina hücrelerine dokunma).
            if (!_roadViews.TryGetValue(cell, out var go)) return;

            Destroy(go);
            _roadViews.Remove(cell);

            ServiceRegistry.Grid.ClearCell(cell);
            ServiceRegistry.Sim.Roads.RemoveRoad(cell);

            Debug.Log($"Yol kaldırıldı {cell} — node sayısı: {ServiceRegistry.Sim.Roads.NodeCount}");
        }

        private void RunPathTest()
        {
            if (_firstRoadCell == null || _lastRoadCell == null)
            {
                Debug.Log("PathTest: önce en az iki yol koy.");
                return;
            }

            var roads = ServiceRegistry.Sim.Roads;
            var from = roads.GetNodeAt(_firstRoadCell.Value);
            var to = roads.GetNodeAt(_lastRoadCell.Value);
            var path = roads.FindPath(from, to);

            if (path == null)
                Debug.Log($"PathTest: {_firstRoadCell.Value} → {_lastRoadCell.Value} YOL YOK.");
            else
                Debug.Log($"PathTest: {_firstRoadCell.Value} → {_lastRoadCell.Value} bulundu, {path.Count} edge.");
        }

        // ---- Bina ----

        private void TryPlaceBuilding()
        {
            if (_selectedBuilding == null) return;
            if (_selectedBuilding.prefab == null)
            {
                Debug.LogError($"BuildingDefinition '{_selectedBuilding.displayName}' prefab'ı atanmamış.", this);
                return;
            }
            if (!RaycastToCell(out var origin)) return;

            var grid = ServiceRegistry.Grid;
            int w = Mathf.Max(1, _selectedBuilding.footprint.x);
            int d = Mathf.Max(1, _selectedBuilding.footprint.y);

            // 1) Footprint hücrelerini topla; TAMAMI boş değilse hiç işgal etme (kısmi yok).
            var cells = new List<GridCoord>(w * d);
            for (int dx = 0; dx < w; dx++)
            {
                for (int dz = 0; dz < d; dz++)
                {
                    var c = new GridCoord(origin.x + dx, origin.z + dz);
                    if (grid.IsOccupied(c))
                    {
                        Debug.Log($"Yerleştirme RED: {_selectedBuilding.displayName} @ {origin} — {c} dolu.");
                        return;
                    }
                    cells.Add(c);
                }
            }

            // 2) Building örneği (saf C# veri) — input/output buffer'lar Paket 3'te.
            var building = new Building
            {
                def = _selectedBuilding,
                origin = origin,
                occupiedCells = cells
            };

            // 3) Doluluk: tüm footprint hücrelerinin sahibi bu bina.
            foreach (var c in cells) grid.SetOccupied(c, building);

            // 4) Sim'e ekle.
            ServiceRegistry.Sim.Buildings.Add(building);

            // 5) GÖRSEL: prefab'ı footprint merkezine koy (görsel taraf, sim değil).
            float cs = grid.CellSize;
            Vector3 center = new Vector3((origin.x + w * 0.5f) * cs, 0f, (origin.z + d * 0.5f) * cs);
            Instantiate(_selectedBuilding.prefab, center, Quaternion.identity, buildingParent);

            Debug.Log($"Bina kondu: {_selectedBuilding.displayName} @ origin {origin}, " +
                      $"hücreler [{string.Join(", ", cells)}], giriş {building.EntranceCell}.");
        }

        // ---- Ortak ----

        /// <summary>Fare ışınını y=0 düzlemine çarpıp hücreyi bulur.</summary>
        private bool RaycastToCell(out GridCoord cell)
        {
            cell = default;
            if (cam == null) return false;

            Ray ray = cam.ScreenPointToRay(MouseScreenPos());
            if (_groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hit = ray.GetPoint(enter);
                cell = ServiceRegistry.Grid.WorldToGrid(hit);
                return true;
            }
            return false;
        }

        // ---- Girdi soyutlaması (yeni Input System birincil; eski de derlenir) ----

        private Vector2 MouseScreenPos()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }

        private bool LeftHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) return Mouse.current.leftButton.isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButton(0);
#else
            return false;
#endif
        }

        private bool LeftPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) return Mouse.current.leftButton.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }

        private bool RightHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) return Mouse.current.rightButton.isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButton(1);
#else
            return false;
#endif
        }

        private bool PathTestPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null) return Keyboard.current.pKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.P);
#else
            return false;
#endif
        }

        private bool RoadModeKey()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null) return Keyboard.current.rKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.R);
#else
            return false;
#endif
        }

        /// <summary>Basılan rakam tuşunun palette index'i (0/1/2); yoksa -1.</summary>
        private int BuildingSelectKey()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame) return 0;
                if (Keyboard.current.digit2Key.wasPressedThisFrame) return 1;
                if (Keyboard.current.digit3Key.wasPressedThisFrame) return 2;
                return -1;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Alpha1)) return 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) return 1;
            if (Input.GetKeyDown(KeyCode.Alpha3)) return 2;
            return -1;
#else
            return -1;
#endif
        }
    }
}
