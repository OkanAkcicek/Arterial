using UnityEngine;
using FactoryCity.Grid;
using FactoryCity.Roads;
using FactoryCity.Simulation;

namespace FactoryCity.Core
{
    /// <summary>
    /// Sahne önyükleyici. SimWorld'ü oluşturur, ServiceRegistry'ye kaydeder ve
    /// sabit-adım tick'ini SimWorld.Tick'e bağlar. Sim ile sahne arasındaki
    /// tek köprü budur.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [SerializeField] private TickSystem tickSystem;

        [Tooltip("Grid hücre boyutu (dünya birimi). Kenney modül boyutuna göre ayarla.")]
        [SerializeField] private float cellSize = 4f;

        private SimWorld _sim;

        private void Awake()
        {
            // Grid (saf C#) — CellSize bu obje üzerinden editörden ayarlanır.
            ServiceRegistry.Grid = new GridManager { CellSize = cellSize };

            // Sim + yol grafı. Tick fire olmadan ÖNCE çalışır
            // (Awake -> OnEnable -> ilk Update sırası garanti eder).
            _sim = new SimWorld { Roads = new RoadNetwork() };
            ServiceRegistry.Sim = _sim;
        }

        private void OnEnable()
        {
            if (tickSystem == null)
            {
                Debug.LogError("GameController: TickSystem referansı atanmamış " +
                               "(inspector'dan bağla). Sim ilerlemeyecek.", this);
                return;
            }

            tickSystem.OnTick += _sim.Tick; // kanonik bağlama (CLAUDE.md §5)
            tickSystem.OnTick += LogTick;   // doğrulama amaçlı ayrı abone
        }

        private void OnDisable()
        {
            if (tickSystem == null) return;

            tickSystem.OnTick -= _sim.Tick;
            tickSystem.OnTick -= LogTick;
        }

        /// <summary>Doğrulama: her 10 tick'te bir (≈ saniyede 1) sayacı yazar.</summary>
        private void LogTick()
        {
            if (_sim.TickCount % 10 == 0)
                Debug.Log($"Tick {_sim.TickCount}");
        }
    }
}
