using UnityEngine;
using FactoryCity.Trucks;

namespace FactoryCity.View
{
    /// <summary>
    /// Bir <see cref="Truck"/> sim örneğinin GÖRSELİ (MonoBehaviour). Sim'i yalnız OKUR
    /// (CLAUDE.md İlke 1). Konumu, kamyonun iki tick durumu arasında <see cref="ViewSync.Alpha"/>
    /// ile interpolasyon yaparak çizer → 10 Hz sim'e rağmen 60 FPS'te pürüzsüz hareket.
    /// ViewSync tarafından havuzdan alınıp <see cref="Bind"/> ile bağlanır.
    /// </summary>
    public class TruckView : MonoBehaviour
    {
        public Truck truck;        // ViewSync bağlar
        public ViewSync viewSync;

        public void Bind(Truck t, ViewSync vs)
        {
            truck = t;
            viewSync = vs;
        }

        public void Unbind()
        {
            truck = null;
            viewSync = null;
        }

        private void Update()
        {
            if (truck == null) return;

            float a = viewSync != null ? viewSync.Alpha : 1f;
            transform.position = Vector3.Lerp(truck.prevWorldPos, truck.currWorldPos, a);

            // Hareket yönüne yumuşak dönüş.
            Vector3 dir = truck.currWorldPos - truck.prevWorldPos;
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        }
    }
}
