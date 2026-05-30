using System.Collections.Generic;
using UnityEngine;
using FactoryCity.Core;
using FactoryCity.Grid;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FactoryCity.Placement
{
    /// <summary>
    /// Yol döşeme — GÖRSEL + GİRDİ tarafı (MonoBehaviour). Fareyle gride snap'lenen
    /// yol prefab'ı koyar/kaldırır ve arkadaki <see cref="FactoryCity.Roads.RoadNetwork"/>
    /// grafını günceller. ÖNEMLİ: graf yalnız veri tutar; prefab instantiate/destroy
    /// işi burada yapılır — graf görsele bağlanmaz (CLAUDE.md İlke 1).
    /// </summary>
    public class PlacementController : MonoBehaviour
    {
        [Header("Referanslar")]
        [Tooltip("Boşsa Camera.main kullanılır.")]
        [SerializeField] private Camera cam;
        [Tooltip("Gride konacak yol prefab'ı (Kenney yol modülü).")]
        [SerializeField] private GameObject roadPrefab;
        [Tooltip("Opsiyonel — konan yollar bu Transform altına toplanır.")]
        [SerializeField] private Transform roadParent;

        [Header("Ayarlar")]
        [SerializeField] private bool roadMode = true;

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
            if (!roadMode) return;

            // Servisler hazır değilse (GameController henüz kurmadıysa) bekle.
            if (ServiceRegistry.Grid == null || ServiceRegistry.Sim?.Roads == null) return;

            if (LeftHeld()) TryPlaceRoad();
            else if (RightHeld()) TryRemoveRoad();

            if (PathTestPressed()) RunPathTest();
        }

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

            // Sadece BİZİM koyduğumuz yolları kaldır (ileride bina hücrelerine dokunma).
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
    }
}
