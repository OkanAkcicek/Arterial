using UnityEngine;
using FactoryCity.Core;
using FactoryCity.Data;
using FactoryCity.Grid;
using FactoryCity.Buildings;
using FactoryCity.Placement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FactoryCity.UI
{
    /// <summary>
    /// Oyuncu komutlarını <see cref="FactoryCity.Trucks.Dispatcher"/>'a İLETİR (MonoBehaviour).
    /// Sim'i yalnız okur, mutasyonu Dispatcher API'siyle yapar (komut yolu — İlke 1).
    /// Debug kıvamında OnGUI paneli (sağ tarafta).
    ///
    /// Akış: "Rota Modu" aç → bir binaya tıkla (kaynak) → ikinci binaya tıkla (hedef) → rota kurulur.
    /// Rota modu açıkken PlacementController bastırılır (tık çakışması olmasın).
    /// </summary>
    public class RouteUI : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private TruckDefinition truckDef;

        private bool _routeMode;
        private Building _pendingSource;
        private Rect _panelRect;

        private readonly Plane _ground = new Plane(Vector3.up, Vector3.zero);

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
        }

        private void OnDisable()
        {
            // Devre dışı kalırsak yerleştirmeyi tekrar serbest bırak.
            PlacementController.InputSuppressed = false;
        }

        private void Update()
        {
            if (!_routeMode) return;
            if (LeftPressed()) HandleRouteClick();
        }

        private void HandleRouteClick()
        {
            var grid = ServiceRegistry.Grid;
            var sim = ServiceRegistry.Sim;
            if (grid == null || sim == null) return;

            if (IsPointerOverPanel()) return;       // UI'a tıklama dünya seçimi sayılmaz
            if (!RaycastCell(out var cell)) return;

            if (grid.GetOwner(cell) is Building b)
            {
                if (_pendingSource == null)
                {
                    _pendingSource = b;
                    Debug.Log($"Rota kaynak seçildi: {b.def.displayName}");
                }
                else if (b == _pendingSource)
                {
                    Debug.Log("Rota: kaynak ile hedef aynı bina olamaz.");
                }
                else
                {
                    var r = sim.Dispatcher.CreateRoute(_pendingSource, b);
                    string it = r.item != null ? r.item.displayName : "?";
                    Debug.Log($"Rota kuruldu: {_pendingSource.def.displayName} → {b.def.displayName} ({it})");
                    _pendingSource = null;
                }
            }
            else
            {
                Debug.Log("Rota: burada bina yok, tekrar dene.");
            }
        }

        private void SetRouteMode(bool on)
        {
            _routeMode = on;
            _pendingSource = null;
            PlacementController.InputSuppressed = on;
        }

        private void OnGUI()
        {
            var sim = ServiceRegistry.Sim;
            if (sim == null) return;
            var disp = sim.Dispatcher;
            if (disp == null) return;

            const float w = 330f;
            _panelRect = new Rect(Screen.width - w - 10, 10, w, Screen.height - 20);
            GUILayout.BeginArea(_panelRect, GUI.skin.box);

            GUILayout.Label("— ROTA YÖNETİMİ —");

            if (GUILayout.Button(_routeMode ? "Rota Modu: AÇIK (kapatmak için tıkla)"
                                            : "Rota Modu: kapalı (açmak için tıkla)"))
                SetRouteMode(!_routeMode);

            if (_routeMode)
                GUILayout.Label(_pendingSource == null
                    ? "→ Kaynak binayı tıkla."
                    : $"→ Kaynak: {_pendingSource.def.displayName}. Şimdi hedefi tıkla.");

            GUILayout.Space(6);
            GUILayout.Label($"Kamyon — toplam: {disp.allTrucks.Count}, boş: {disp.freeTrucks.Count}");
            long buyCost = truckDef != null ? truckDef.buyCost : 0;
            if (GUILayout.Button($"+ Kamyon Al ({buyCost})"))
            {
                if (truckDef == null) Debug.LogError("RouteUI: truckDef atanmamış (inspector).", this);
                else if (disp.BuyTruck(truckDef) == null) Debug.Log($"Yetersiz para — kamyon ({buyCost}) alınamadı.");
            }

            GUILayout.Space(6);
            GUILayout.Label($"Rotalar ({disp.routes.Count}):");
            for (int i = 0; i < disp.routes.Count; i++)
            {
                var r = disp.routes[i];
                string s = r.source != null ? r.source.def.displayName : "?";
                string d = r.dest != null ? r.dest.def.displayName : "?";

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{i}: {s}→{d}  k:{r.assignedTrucks.Count}", GUILayout.Width(196));
                if (GUILayout.Button("+", GUILayout.Width(26)))
                    if (!disp.AssignTruckToRoute(r)) Debug.Log("Boş kamyon yok — önce 'Kamyon Al'.");
                if (GUILayout.Button("-", GUILayout.Width(26)))
                    disp.UnassignTruckFromRoute(r);
                if (GUILayout.Button("x", GUILayout.Width(26)))
                {
                    disp.RemoveRoute(r);
                    GUILayout.EndHorizontal();
                    break; // liste değişti, bu frame'i kes
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }

        // ---- input / raycast ----

        private bool IsPointerOverPanel()
        {
            Vector2 mp = MousePos();                                   // ekran (sol-alt orijin)
            Vector2 gui = new Vector2(mp.x, Screen.height - mp.y);    // GUI (sol-üst orijin)
            return _panelRect.Contains(gui);
        }

        private bool RaycastCell(out GridCoord cell)
        {
            cell = default;
            if (cam == null) return false;
            Ray ray = cam.ScreenPointToRay(MousePos());
            if (_ground.Raycast(ray, out float enter))
            {
                cell = ServiceRegistry.Grid.WorldToGrid(ray.GetPoint(enter));
                return true;
            }
            return false;
        }

        private Vector2 MousePos()
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
    }
}
