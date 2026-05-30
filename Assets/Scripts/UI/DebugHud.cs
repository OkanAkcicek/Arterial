using UnityEngine;
using FactoryCity.Core;
using FactoryCity.Data;
using FactoryCity.Buildings;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FactoryCity.UI
{
    /// <summary>
    /// Sim verisini OnGUI ile EKRANA YAZAR — yalnız OKUR (CLAUDE.md İlke 1).
    /// Tek istisna: 'I' tuşuyla ilk fabrikanın input buffer'ına hammadde enjekte eden
    /// DEBUG komutu — kamyon henüz yokken recipe tüketimini test etmek için. Bu bir
    /// girdi/komut yoludur (Update'te), render yan etkisi değil; Paket 4-5'te kamyonlar
    /// girdi taşımayı devralınca bu enjeksiyon gereksizleşir.
    /// </summary>
    public class DebugHud : MonoBehaviour
    {
        [SerializeField] private int injectAmount = 100;

        private string _lastMsg;

        private void Update()
        {
            if (InjectKeyPressed()) InjectIntoFirstFactory();
        }

        // DEBUG komutu — ilk recipe'li fabrikaya girdi yükler (kamyon taklidi).
        private void InjectIntoFirstFactory()
        {
            var sim = ServiceRegistry.Sim;
            if (sim == null) return;

            foreach (var b in sim.Buildings)
            {
                if (b.def.kind != BuildingKind.Factory) continue;
                if (b.def.recipe == null || b.def.recipe.Count == 0) continue;

                foreach (var ri in b.def.recipe)
                {
                    if (ri?.type == null) continue;
                    b.input.Add(ri.type, injectAmount);
                }

                _lastMsg = $"[DEBUG] '{b.def.displayName}' fabrikasına her girdiden {injectAmount} birim enjekte edildi.";
                Debug.Log(_lastMsg);
                return;
            }

            _lastMsg = "[DEBUG] Enjekte edilecek (recipe'li) fabrika bulunamadı.";
            Debug.Log(_lastMsg);
        }

        private void OnGUI()
        {
            var sim = ServiceRegistry.Sim;
            if (sim == null) return;

            var rect = new Rect(10, 10, 440, Screen.height - 20);
            GUILayout.BeginArea(rect, GUI.skin.box);

            GUILayout.Label($"FactoryCity — Tick {sim.TickCount}");
            GUILayout.Label($"Bina sayısı: {sim.Buildings.Count}    [I] = ilk fabrikaya girdi enjekte");
            GUILayout.Space(6);

            for (int i = 0; i < sim.Buildings.Count; i++)
            {
                var b = sim.Buildings[i];
                GUILayout.Label($"#{i}  {b.def.displayName}  [{b.def.kind}]  @ {b.origin}");

                if (b.def.kind != BuildingKind.Port && b.output != null)
                {
                    string outName = b.output.type != null ? b.output.type.displayName : "—";
                    GUILayout.Label($"      çıkış: {outName}  {b.output.count}/{b.output.capacity}");
                }

                if (b.input != null)
                {
                    foreach (var (type, qty) in b.input.GetAllStocks())
                        GUILayout.Label($"      girdi: {(type != null ? type.displayName : "?")} = {qty}");
                }
            }

            if (!string.IsNullOrEmpty(_lastMsg))
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(_lastMsg);
            }

            GUILayout.EndArea();
        }

        private bool InjectKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null) return Keyboard.current.iKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.I);
#else
            return false;
#endif
        }
    }
}
