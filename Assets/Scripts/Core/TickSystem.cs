using UnityEngine;

namespace FactoryCity.Core
{
    /// <summary>
    /// Sabit adımlı simülasyon saati. Gerçek kare süresini biriktirir ve
    /// <see cref="OnTick"/> olayını sabit 10 Hz'de tetikler.
    /// TÜM oyun mantığı bu olaya bağlı çalışır — asla Update() içinde değil.
    /// (CLAUDE.md İlke 2.)
    /// </summary>
    public class TickSystem : MonoBehaviour
    {
        public const float TickRate = 10f;          // saniyede tick sayısı
        public const float TickDelta = 1f / TickRate; // bir tick = 0.1 sn

        /// <summary>Her sabit adımda tetiklenir. Sim mantığı buna abone olur.</summary>
        public event System.Action OnTick;

        // Time.deltaTime burada SADECE accumulator için kullanılır (CLAUDE.md §7).
        private float _accumulator;

        private void Update()
        {
            _accumulator += Time.deltaTime;

            // Bir karede birden fazla tick birikmiş olabilir; hepsini boşalt.
            while (_accumulator >= TickDelta)
            {
                _accumulator -= TickDelta;
                OnTick?.Invoke();
            }
        }
    }
}
