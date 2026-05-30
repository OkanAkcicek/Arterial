using UnityEngine;
using FactoryCity.Core;
using FactoryCity.Trucks;

namespace FactoryCity.View
{
    /// <summary>
    /// Bir <see cref="Truck"/> sim örneğinin GÖRSELİ (MonoBehaviour). Sim'i yalnız
    /// OKUR, asla yazmaz (CLAUDE.md İlke 1). Bu pakette düz okuma; tick'ler arası
    /// yumuşatma/interpolasyon Paket 9'da (ViewSync) gelecek.
    /// </summary>
    public class TruckView : MonoBehaviour
    {
        [Tooltip("Spawner tarafından atanır. View sadece okur.")]
        public Truck truck;

        private Vector3 _lastPos;
        private bool _hasLast;

        private void Update()
        {
            if (truck == null) return;

            var grid = ServiceRegistry.Grid;
            if (grid == null) return;

            Vector3 pos = truck.WorldPosition(grid);
            transform.position = pos;

            // Hareket yönüne dön (opsiyonel). Yumuşatma Paket 9'da.
            if (_hasLast)
            {
                Vector3 delta = pos - _lastPos;
                if (delta.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            }
            _lastPos = pos;
            _hasLast = true;
        }
    }
}
