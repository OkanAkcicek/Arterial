# CLAUDE.md — Proje Anayasası
### "Şehir = Fabrika" lojistik oyunu · Unity (C#) · solo geliştirme

> **Bu dosya projenin kök dizinindedir ve Claude Code her oturumda otomatik okur.**
> Tüm paketler (Paket 0–9) bu dosyadaki kurallara, sabitlere ve isimlere uymak ZORUNDADIR.
> Bir paket metni ile bu dosya çelişirse **bu dosya kazanır.** İsim uydurma: aşağıdaki
> Kanonik İsimlendirme Kayıtları'nda olmayan bir tip eklemen gerekiyorsa, oradaki
> konvansiyona uygun isimlendir ve tutarlı kullan.

---

## 1. Bu proje nedir? (vizyon)

Şehrin kendisi bir fabrikadır: **binalar** istasyon, **yollar** konveyör bandı, **kamyonlar**
taşınan paketlerdir. Oyuncu hammaddeyi işleyip ürüne dönüştürür, kamyonlarla limana ulaştırır,
sabit fiyattan satar, kazandığı parayla yeni bina ve bölge açar.

**Çekirdek döngü:** hammadde → fabrikada işle → paket → kamyonla taşı → limanda sat → para →
yeni bina/bölge → daha büyük lojistik ağı.

**İki katman var, derinlikleri FARKLI:**
- **Bina içi = SIĞ katman.** "Kur ve unut." Bina dışarıya sadece bir *çıktı profili* sunar:
  `(ürün tipi, dakika başına üretim hızı, çıkış tamponu)`. İçi bir kez ayarlanır, sonra unutulur.
- **Şehir ağı (lojistik) = DERİN katman.** Oyunun kalbi. Sürekli görünür, sürekli karar:
  fabrikayı nereye koyacağın, yolu nasıl döşeyeceğin, hangi rotaya kaç kamyon atayacağın,
  trafiği nasıl çözeceğin. Optimizasyonun büyük kısmı burada.

**Bu oyunu jenerik tycoon olmaktan ne kurtarıyor (tasarımcı niyeti — koda yansıt):**
1. Üretim **throughput tabanlı** (dk başına X birim, Satisfactory gibi) — timer değil. Üretim
   hızı doğrudan yola akış basıncı yaratır; hızlı bina yolu doldurur, lojistik kararı zorlar.
2. **Trafik/tıkanma** ikinci optimizasyon ekseni. Sadece kamyon eklemek tıkanmaya çarpar;
   oyuncu ağ tasarımına zorlanır ("tall vs wide" dengesi korunur).
3. **Genişleme = yeni lojistik problemi**, "aynının fazlası" değil. Yeni bölge açılınca yeni bir
   ZORUNLU hammadde devreye girer; mevcut ürün artık başka bölgeden gelen bir girdiye bağımlı olur.

---

## 2. Mimari ilkeler (4 KURAL — asla ihlal etme)

**İLKE 1 — Simülasyon ve görsel KESİN ayrı.**
Oyunun "doğru cevabı" (kaç item üretildi, kamyon nerede, ne kadar para) simülasyon katmanında
saf veri olarak yaşar. Görsel katman bu veriyi **sadece okur, asla yazmaz.**

**İLKE 2 — Sabit tick, `Update()` değil.**
Tüm oyun mantığı 10 Hz sabit adımlı tick'te işler. `Update()` sadece görsel/girdi içindir
(ve TickSystem'in accumulator'ı). Mantığı `Update()`'e koyma.

**İLKE 3 — `Simulation/` ve mantık verisi saf C#'tır.**
`Building`, `Truck`, `OutputBuffer`, `RoadNetwork`, `Dispatcher`, `EconomyManager`,
`TrafficModel`, `Region` vb. **MonoBehaviour MİRAS ALMAZ**, `Transform`/`GameObject` tutmaz.
Pozisyonlar `GridCoord` veya `float`/`Vector3` (veri olarak) ile tutulur. Bu, sim'i test edilebilir
ve (ileride) kaydedilebilir kılar.

**İLKE 4 — Üretilen item'lar GameObject DEĞİL.**
Demir, plaka, kömür vb. birer `int` sayaçtır; buffer'larda yaşar. Yalnızca **binalar ve kamyonlar**
görünür GameObject'tir. Her item için GameObject üretmek YASAK — performansı öldürür. DOTS'a
gerek yok; düz MonoBehaviour + object pooling yeterli.

---

## 3. KESİN UYARILAR (en sık yapılan hatalar — bunları yapma)

- ❌ Item'ı GameObject yapma. (İlke 4) — en kritik hata budur.
- ❌ Sim sınıfına `using UnityEngine;` MonoBehaviour bağımlılığı sokma. (İlke 3)
- ❌ Görsel koddan (View/UI) sim verisine YAZMA; sadece oku. (İlke 1)
- ❌ Oyun mantığını `Update()`'e koyma; tick'te işlet. (İlke 2)
- ❌ A* / NavMesh / fizik motoru / DOTS / ECS kurma. Yol bulma = BFS. Kamyon = waypoint takibi.
- ❌ DI framework (Zenject vb.) kurma. Erişim `ServiceRegistry` (statik) üzerinden.
- ❌ İstenmeyen "ekstra özellik" ekleme (tech tree, fiyat dalgalanması, sözleşme, ses, save).
  Her paket SADECE kendi kapsamını yapar; kapsam dışını "Bu pakette YAPMA" bölümü belirtir.
- ❌ Bana (kullanıcıya) tasarım sorusu sorma. Tüm kararları kendin ver, makul varsayım yap, devam et.
- ✅ Her paket sonunda: derlenme hatası yok + paketin "Kabul kriteri"ni sağla + bana kısa bir
  "editör kurulum" listesi yaz (hangi prefab/SO/sahne ayarı gerekiyor).

---

## 4. Sabitler ve konvansiyonlar

- **Tick hızı:** 10 Hz → `TickSystem.TickRate = 10f`, `TickSystem.TickDelta = 0.1f` (saniye).
  Dakikada 600 tick. Üretim dönüşümü: `itemsPerMinute / 60f * dt` birim/tick.
- **Kök namespace:** `FactoryCity`. Alt namespace = klasör adı: `FactoryCity.Core`,
  `FactoryCity.Simulation`, `FactoryCity.Grid`, `FactoryCity.Roads`, `FactoryCity.Buildings`,
  `FactoryCity.Trucks`, `FactoryCity.Traffic`, `FactoryCity.Economy`, `FactoryCity.Regions`,
  `FactoryCity.Placement`, `FactoryCity.View`, `FactoryCity.Data`, `FactoryCity.UI`.
- **Klasör yapısı (Assets/Scripts altında):**
  ```
  Core/  Simulation/  Grid/  Roads/  Buildings/  Trucks/  Traffic/  Economy/  Regions/  Placement/  View/  Data/  UI/
  ```
- **ScriptableObject menü yolu:** `[CreateAssetMenu(menuName="FactoryCity/...")]`.
- **CellSize:** `GridManager.CellSize` (float, varsayılan 4f) — editörde Kenney modül boyutuna
  göre ayarlanır. Tüm grid<->dünya dönüşümleri bunu kullanır.
- **Hız/oran sabitleri:** kamyon taban hızı 8 birim/sn (`TruckDefinition.speed`), yükleme/boşaltma
  5 birim/sn (`Truck.LoadRate`), tampon kapasitesi varsayılan 50 (`BuildingDefinition.bufferCapacity`).

---

## 5. KANONİK İSİMLENDİRME KAYITLARI (tek doğruluk kaynağı)

Aşağıdaki isimler bağlayıcıdır. Her paket bu isimleri AYNEN kullanır.

### Core/
| Tip | Tür | Anahtar üyeler |
|---|---|---|
| `TickSystem` | MonoBehaviour | `const float TickRate=10f; const float TickDelta=1f/TickRate; event System.Action OnTick;` |
| `GameController` | MonoBehaviour | önyükleyici; `SimWorld` oluşturur, manager'ları `ServiceRegistry`'ye atar, `TickSystem.OnTick += Sim.Tick;` |
| `ServiceRegistry` | static class | `static SimWorld Sim; static GridManager Grid; static EconomyManager Economy;` |

### Simulation/
| Tip | Tür | Anahtar üyeler |
|---|---|---|
| `SimWorld` | saf C# | `long TickCount; List<Building> Buildings; List<Truck> Trucks; RoadNetwork Roads; Dispatcher Dispatcher; EconomyManager Economy; TrafficModel Traffic; RegionManager Regions; void Tick();` |

> **Kamyon listesi sahipliği (kesin karar):** Kanonik liste `SimWorld.Trucks`'tır. `Dispatcher`
> bu listeye **referans tutar** (kurucusuna geçilir), kendi listesini kopyalamaz. `Dispatcher`
> ayrıca `freeTrucks` (rotaya atanmamışlar) listesini ayrı tutar. `BuyTruck` yeni kamyonu hem
> `SimWorld.Trucks`'a hem `freeTrucks`'a ekler.

### Grid/
| Tip | Tür | Anahtar üyeler |
|---|---|---|
| `GridCoord` | struct (saf C#) | `int x, z;` + `IEquatable`, `==/!=`, `GetHashCode`, `Neighbors()` (4 ortogonal komşu) |
| `GridManager` | saf C# | `float CellSize=4f; Vector3 GridToWorld(GridCoord); GridCoord WorldToGrid(Vector3); bool IsOccupied(GridCoord); void SetOccupied(GridCoord,object); void ClearCell(GridCoord); object GetOwner(GridCoord);` |

### Roads/
| Tip | Tür | Anahtar üyeler |
|---|---|---|
| `RoadNode` | saf C# | `GridCoord pos; List<RoadEdge> edges;` |
| `RoadEdge` | saf C# | `RoadNode a, b; float length; int occupancy;` |
| `RoadNetwork` | saf C# | `void AddRoad(GridCoord,GridManager); void RemoveRoad(GridCoord); List<RoadEdge> FindPath(RoadNode,RoadNode) /*BFS*/; RoadNode GetNodeAt(GridCoord); RoadNode NearestNode(GridCoord); IEnumerable<RoadEdge> AllEdges();` |

### Data/ (ScriptableObject'ler + yardımcılar)
| Tip | Tür | Anahtar üyeler |
|---|---|---|
| `ItemType` | ScriptableObject | `string displayName; Sprite icon;` |
| `RecipeInput` | `[Serializable]` class | `ItemType type; int amountPerOutput;` |
| `BuildingKind` | enum | `Source, Factory, Port` |
| `BuildingDefinition` | ScriptableObject | `string displayName; BuildingKind kind; GameObject prefab; Vector2Int footprint; Vector2Int entranceOffset; ItemType outputType; float itemsPerMinute; int bufferCapacity; List<RecipeInput> recipe; long buildCost; long unitPrice;` |
| `TruckDefinition` | ScriptableObject | `GameObject prefab; int capacity; float speed; long buyCost;` |

### Buildings/
| Tip | Tür | Anahtar üyeler |
|---|---|---|
| `OutputBuffer` | saf C# | `ItemType type; int count; int capacity; int Add(int); int Remove(int); int RemoveAll();` |
| `InputBuffer` | saf C# | `int capacityPerType; int Add(ItemType,int); bool HasEnough(List<RecipeInput>); void Consume(List<RecipeInput>); int GetCount(ItemType); IEnumerable<(ItemType,int)> GetAllStocks(); void ClearAll();` |
| `Building` | saf C# | `BuildingDefinition def; GridCoord origin; List<GridCoord> occupiedCells; GridCoord EntranceCell; OutputBuffer output; InputBuffer input; float _accum; void Tick(float dt);` |

> Port: ayrı sınıf DEĞİL. `Building.Tick` içinde `def.kind == BuildingKind.Port` dalı ile
> işlenir; input buffer'daki her şeyi `ServiceRegistry.Economy.Add(qty*unitPrice)` ile satıp temizler.

### Trucks/
| Tip | Tür | Anahtar üyeler |
|---|---|---|
| `TruckState` | enum | `Idle, ToSource, Loading, ToDest, Unloading, Returning` |
| `Truck` | saf C# | `TruckDefinition def; int capacity; ItemType cargoType; int cargo; Building source, dest; TruckRoute route; TruckState state; List<RoadEdge> path; int edgeIndex; float edgeProgress; float loadAccum; const float LoadRate=5f; Vector3 prevWorldPos, currWorldPos; void Tick(float dt); Vector3 WorldPosition(GridManager); bool MoveAlongPath(float dt);` |
| `TruckRoute` | saf C# | `Building source, dest; ItemType item; List<Truck> assignedTrucks; int desiredTruckCount;` |
| `Dispatcher` | saf C# | `List<TruckRoute> routes; List<Truck> allTrucks /*=SimWorld.Trucks referansı*/; List<Truck> freeTrucks; TruckRoute CreateRoute(Building,Building); void RemoveRoute(TruckRoute); bool AssignTruckToRoute(TruckRoute); void UnassignTruckFromRoute(TruckRoute); Truck BuyTruck(TruckDefinition); void Tick();` |

### Economy/
| Tip | Tür | Anahtar üyeler |
|---|---|---|
| `EconomyManager` | saf C# | `long money; event System.Action<long> OnMoneyChanged; void Add(long); bool TrySpend(long);` |

### Traffic/
| Tip | Tür | Anahtar üyeler |
|---|---|---|
| `TrafficModel` | saf C# | `void Recompute(List<Truck>); float SpeedMultiplier(RoadEdge);` |

### Regions/
| Tip | Tür | Anahtar üyeler |
|---|---|---|
| `Region` | saf C# | `int id; string displayName; List<GridCoord> cells; bool unlocked; long unlockCost;` |
| `RegionManager` | saf C# | `List<Region> regions; Region RegionOf(GridCoord); bool IsCellBuildable(GridCoord); bool TryUnlock(Region,EconomyManager); event System.Action<Region> OnRegionUnlocked;` |

### Placement/ · View/ · UI/
| Tip | Tür | Anahtar üyeler |
|---|---|---|
| `PlacementController` | MonoBehaviour | yol/bina koyma-kaldırma; girdi (mouse raycast → `WorldToGrid`); para/bölge kontrolü |
| `TruckView` | MonoBehaviour | bir `Truck`'ı OKUR; `transform.position = Vector3.Lerp(prevWorldPos, currWorldPos, ViewSync.Alpha)` |
| `ViewSync` | MonoBehaviour | tick'ler arası interpolasyon faktörü `float Alpha`; `TruckView` havuzunu yönetir |
| `ObjectPool` | normal class | `ObjectPool(GameObject prefab,int prewarm,Transform parent); GameObject Get(); void Release(GameObject);` |
| `DebugHud` | MonoBehaviour | sim verisini OnGUI ile yazar (buffer'lar, para, kamyon durumu) — sadece okur |
| `RouteUI` | MonoBehaviour | oyuncu komutlarını `Dispatcher`'a iletir (rota kur, kamyon al/ata) |

---

## 6. Kanonik tick sırası (Paket 8 sonrası NİHAİ hali)

`SimWorld.Tick()` her tick'te şu sırayla işler:
```csharp
public void Tick() {
    TickCount++;
    Traffic.Recompute(Trucks);                              // 1) yol dolulukları (son konumlara göre)
    foreach (var b in Buildings) b.Tick(TickSystem.TickDelta); // 2) üretim
    Dispatcher.Tick();                                      // 3) boş kamyonlara iş ata
    foreach (var t in Trucks) t.Tick(TickSystem.TickDelta); // 4) kamyon hareketi (trafik çarpanını kullanır)
}
```
> Paket 3'te yalnızca (2), Paket 5'te (2)+(3), Paket 8'de (1) eklenir. Erken paketlerde
> olmayan satırları yorum olarak yer tut. dt KAYNAĞI her zaman `TickSystem.TickDelta`'dır.

---

## 7. Erişim/sahiplik kararları (belirsizlik bırakma)

- Manager'lara erişim **`ServiceRegistry`** üzerinden (statik). DI yok.
- `Building.Tick` (Port satışı) → `ServiceRegistry.Economy`.
- `Truck.MoveAlongPath` (trafik çarpanı) → `ServiceRegistry.Sim.Traffic`.
- `PlacementController` → `ServiceRegistry.Grid`, `ServiceRegistry.Sim` (Roads/Buildings/Regions/Economy).
- Kamyon listesi sahibi: `SimWorld.Trucks` (bkz. §5 not). `Dispatcher.allTrucks` buna referanstır.
- `dt` = `TickSystem.TickDelta`, her zaman. `Time.deltaTime` SADECE TickSystem accumulator'ında
  ve View interpolasyon/dönüşünde kullanılır.

---

## 8. Paket sırası (geliştirme yol haritası)

0. İskelet (TickSystem, SimWorld, ServiceRegistry, GameController)
1. Grid + yol döşeme (GridManager, RoadNetwork, PlacementController yol modu)
2. Bina yerleştirme + veri SO'ları (BuildingDefinition vb., Building, footprint)
3. Üretim simülasyonu görselsiz (OutputBuffer, InputBuffer, Building.Tick, recipe)
4. Kamyon hareketi (Truck, TruckState machine, MoveAlongPath, TruckView minimal)
5. Rota atama + Dispatcher (TruckRoute, Dispatcher, RouteUI)
6. Liman + Ekonomi (Port dalı, EconomyManager) — **çekirdek döngü tamamlanır**
7. Bina/bölge açma (maliyet entegrasyonu, Region/RegionManager, bölge bağımlılığı)
8. Trafik (TrafficModel, tick sırası güncellemesi, MoveAlongPath çarpanı)
9. View cilası (ViewSync interpolasyon, ObjectPool, yön dönüşü)

**Sıra zorunludur.** Bir paket "Kabul kriteri"ni geçmeden sonrakine geçme. Özellikle Paket 3
(görselsiz üretim) atlanmaz — sayıların doğruluğu kamyonlardan ÖNCE kanıtlanır.

---

## 9. Bir paket işlerken
1. Bu dosyadaki ilkelere, kesin kurallara ve kanonik isimlere uy.
2. Sadece o paketin kapsamını yap; "Bu pakette YAPMA" listesine dokunma.
3. Karar gerektiren her yerde makul varsayım yap, kullanıcıya sorma.
4. Bitince: derleme temiz + Kabul kriteri sağlanıyor + kullanıcıya "editör kurulum" listesi yaz.
