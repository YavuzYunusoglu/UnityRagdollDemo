# RagdollDemo — Yeni Sohbet Devir Notu

Son güncelleme: 2026-09-08

## Son çalışma: düşüş/kalkış ve kamera yumuşatma

Kullanıcı bu çalışma için açıkça uygulama ve geliştirme yetkisi verdi. `PhysicsCharacterBalanceController`, `ActiveRagdollPoseDriver` ve `ShoulderCameraController` birlikte güncellendi. Sabit lift/zorla rotasyon yerine atalet hesabıyla tork ve zemin desteği; aşamalı toplanma pozu; göğüs üzerinden yumuşak kamera takibi eklendi. Ayrıntılar ve test sınırları: [RECOVERY_SMOOTHING_REVIEW.md](RECOVERY_SMOOTHING_REVIEW.md). Son hareket hissi kullanıcı tarafından henüz onaylanmadı. Önceki yürüyüş/motor temel değerleri korunur.

Sahne kaydedildi (`Dirty=False`), geçici test nesneleri bırakılmadı. İleri/geri/yan düşüş, sendeleme, zeminsiz ortam ve alçak tavan senaryolarının kayıtları `Diagnostics/RecoveryReview/acceptance-*` altında. Inspector recovery süresi `1.65`, maksimum `4`, diklik toleransı `0.5` derece. Önceki sahne ve scriptler aynı klasörde yedeklendi.

## Hedef ve çalışma biçimi

Hedef, R.E.P.O. yönünde kontrollü fakat çevre ve darbelere fiziksel tepki veren bir karakterdir. Human: Fall Flat kadar sürekli gevşek bir his istenmiyor.

Proje sahibi Unity biliyor. Fizik ve kod sistemlerini öğrenmek istediği için mekanizma ve gerekçe açıklanmalı; aşırı temel tıklama tarifleri verilmemeli. 2026-09-08 çalışma kararı: kodlama ile Inspector/sahne ayarlarını proje sahibi yapacak. Codex inceleme, bağlantı haritası, telemetri, doğrulama ve dokümantasyon gibi angarya işleri üstlenecek; proje sahibi açıkça "uygula" demedikçe kod veya ayar değiştirmeyecek.

Karar ve teşhisler yalnızca proje içindeki sahne, script, Unity Console ve `Diagnostics` telemetrisi üzerinden yürütülmelidir.

## Önce okunacak dosyalar

- `PROJECT_DIRECTION.md`
- `PHYSICS_CHARACTER_CHECKLIST.md`
- `Assets/Scripts/Character/PhysicsCharacterMotor.cs`
- `Assets/Scripts/Character/PhysicsCharacterTelemetry.cs`
- `Assets/Scripts/Character/ActiveRagdollPoseDriver.cs`
- `Assets/Scripts/Character/PhysicsBoneFollower.cs`
- `Assets/Scripts/Character/ShoulderCameraController.cs`

Aktif sahne: `Assets/Scenes/PhysicsCharacterDemo.unity`

## Çalışan sistemler

- Rigidbody tabanlı Player motoru, kamera yönünde WASD ve sağ omuz kamerası.
- Mouse kilidi, yaw kontrolü ve hızlı dönüş sırasında dış torktan ayrılmış kontrollü Player yaw hedefi.
- Göğüs, kafa, kollar, ön kollar, uyluklar, kavallar ve ayaklardan oluşan fizik proxy zinciri.
- Proxy rotasyonlarını görsel kemiklere aktaran follower.
- İleri/geri procedural gait ve A/D için küçük yan adım pozu.
- Düşük sürtünmeli uzuv Physics Material'ı ve kapalı fizik self-collision katmanı.
- Rampa ve 12/18/21 cm basamakları çıkan step solver. Step sırasında tüm bağlı fizik gövdeleri aynı dikey miktarda taşınıyor.
- Hareket ve fren ivmesini Player ile birlikte bütün bağlı dinamik fizik gövdelerine eşit uygulayan motor davranışı.
- Play başladığında kendisini otomatik kuran ve zaman damgalı CSV üreten fizik telemetrisi.

## Çözülen sorun: dur-kalkta sert savrulma

### Eski görünen davranış

Giriş bırakıldığında veya yeniden hareket edildiğinde bacaklar kökün gerisinde kalıyor, gövde eğiliyor ve fizik zinciri sert biçimde nötr poza toparlanıyordu.

Önceki videolar:

- `C:/Users/aeria/Videos/NVIDIA/Desktop/Desktop 2026.09.06 - 00.00.26.03.mp4`
- `C:/Users/aeria/Videos/NVIDIA/Desktop/Desktop 2026.09.06 - 00.11.02.06.mp4`

### Daha önce elenen veya sonuç vermeyen denemeler

- `ActiveRagdollPoseDriver` Play sırasında kapatıldığında sorun devam etti. Procedural hedef güncellemesi sorunun oluşması için gerekli değildi.
- Göğüs damper'ını tek başına artırmak sorunu çözmedi.
- Motor acceleration/braking değerlerini tek başına düşürmek kullanıcı gözleminde sorunu çözmedi.
- Kilitli açısal eksenlerde Slerp yerine X & YZ drive denemesi sorunu çözmedi.
- Eski telemetri yalnızca kök hızını ölçtüğü ve gerçek input geçişini yakalayamadığı için nedeni ayıramadı.

### 2026-09-08 telemetri teşhisi

`PhysicsCharacterTelemetry.cs` eklendi. Aynı `FixedUpdate` zaman çizelgesinde şunları CSV'ye kaydediyor:

- Gerçek hareket input'u.
- Desired velocity, hedef ivme ve uygulanan ivme.
- Grounded, step solver etkinliği ve step yükselme miktarı.
- Player, ChestPhysics, iki ThighPhysics, iki ShinPhysics ve iki FootPhysics dünya hızları.
- Fizik gövdelerinin Player'a göre açıları ve göreli açısal hızları.
- Kamera ile Player arasındaki konum farkı.
- İvmenin yalnız köke mi yoksa tüm bağlı gövdelere mi uygulandığı.

Karşılaştırmada kullanılan kayıtlar:

- Kök-only baseline: `Diagnostics/physics-telemetry-20260908-020103.csv`
  - `3791` örnek, `75.80 sn`.
  - Karşılaştırmaya tam hıza ulaşmış altı W bırakma geçişi alındı.
- Tüm bağlı gövdeler: `Diagnostics/physics-telemetry-20260908-020839.csv`
  - `2626` örnek, `52.50 sn`.
  - Karşılaştırmaya tam hıza ulaşmış beş W bırakma geçişi alındı.

Baseline'da Player yaklaşık `3.85 m/s` hızdan `0.12 sn` içinde durdu. İlk kök fren örneğinden `20 ms` sonra göğüs ve uyluk sapması başladı; ardından kaval ve ayaklarda daha büyük hız/açı farkları oluştu. Bu zaman sırası, sert toparlanmanın joint drive ile başlamadığını; hareket ivmesinin yalnız Player Rigidbody'sine verilmesiyle fizik zincirinin joint'ler üzerinden gecikmeli taşınmasından doğduğunu gösterdi.

W bırakma pencerelerindeki ortalama tepe değerler:

| Bölge | Hız farkı: kök-only → tüm rig | Açı değişimi: kök-only → tüm rig | Göreli açısal hız: kök-only → tüm rig |
| --- | ---: | ---: | ---: |
| Chest | `0.441 → 0.053 m/s` | `8.83° → 3.98°` | `2.17 → 0.23 rad/s` |
| Thigh ortalaması | `0.462 → 0.292 m/s` | `16.96° → 11.67°` | `2.34 → 1.48 rad/s` |
| Shin ortalaması | `1.879 → 0.868 m/s` | `31.84° → 13.88°` | `3.87 → 1.74 rad/s` |
| Foot ortalaması | `2.839 → 1.171 m/s` | `27.17° → 7.65°` | `4.33 → 1.28 rad/s` |

### Uygulanan düzeltme

`PhysicsCharacterMotor` içinde `accelerateConnectedBodies = true` eklendi. Motor hâlâ Player'ın yatay hızından aynı capped velocity correction ivmesini hesaplıyor; ancak `ForceMode.Acceleration` artık yalnız Player'a değil, `CacheCharacterBodies()` tarafından bulunan bütün bağlı ve dinamik Rigidbody'lere eşit uygulanıyor.

Bu yaklaşım bütün rig'in merkez hareketini birlikte hızlandırıp frenliyor. Joint'ler artık locomotion ivmesini uzuvlara gecikmeli aktarmak zorunda kalmıyor; dış darbe ve uzuvların göreli fizik hareketi korunuyor.

Kullanıcı aynı ileri dur-kalk testinde yeni davranışı görsel olarak çok iyi buldu. Unity MCP kontrolünde yeni mod `FullRigAcceleration=True`, telemetri aktif ve Console hatasızdı. Sorun prototype düzeyinde çözülmüş kabul edilebilir.

## Güncel diskteki temel değerler

2026-09-08 sahne/script okuması:

| Bileşen | Değer |
| --- | --- |
| Motor | Move Speed `4`, Acceleration `25`, Braking `35`, Full Rig Acceleration `true` |
| Kütle oranı | Player `12.00`, fizik proxy toplamı `2.58`, oran yaklaşık `4.65` |
| ChestPhysics Slerp | `200 / 30 / 100` |
| HeadPhysics Slerp | `100 / 15 / 60` |
| ThighPhysics L/R Slerp | `140 / 15 / 70` |
| ShinPhysics L/R Slerp | `70 / 8 / 50` |
| FootPhysics L/R Slerp | `35 / 4 / 60` |
| UpperArmPhysics L/R Slerp | `25 / 4 / 40` |
| ForearmPhysics L/R Slerp | `17 / 2.3 / 30` |

Eski devir notundaki Motor Braking `15` ve Chest `150 / 12 / 100` değerleri güncel sahne dosyasıyla uyuşmuyordu; yukarıdaki tablo diskteki güncel serialized durumu esas alır.

`accelerateConnectedBodies` yeni eklenen serialized alandır. Mevcut script varsayılanı `true` ve Play sırasında `true` olduğu MCP ile doğrulandı; sahne yeniden kaydedildiğinde alan açıkça serialize edilebilir.

`PhysicsRig`, `Player`ın child'ı değil; sahne kökünde ayrı bir GameObject'tir. Bu nedenle yeni bir controller içinde `Player.GetComponentsInChildren<ConfigurableJoint>()` kullanmak joint'leri bulmaz. Referanslar Inspector'dan verilmeli veya bağlantı grafiği sahne genelindeki joint'lerden kurulmalıdır.

## Telemetri notları

- Çıktı klasörü: `Diagnostics/`
- Dosya biçimi: `physics-telemetry-yyyyMMdd-HHmmss.csv`
- Logger `RuntimeInitializeOnLoadMethod(AfterSceneLoad)` ile Player'a geçici component olarak eklenir; sahneye kalıcı component eklemez.
- Dosya her 10 fizik örneğinde flush edilir ve Play kapanırken düzgün kapatılır.
- `PhysicsCharacterMotor` yalnız teşhis amacıyla son input/velocity/acceleration/step değerlerini read-only property'ler üzerinden açar.
- Telemetri regresyonlar bitene kadar tutulmalı; production temizliği sırasında kaldırılıp kaldırılmayacağı ayrıca kararlaştırılmalı.

## Gait cadence düzeltmesi ve açık görsel test

2026-09-08'de `ActiveRagdollPoseDriver` ileri gait fazı mesafe tabanlı hale getirildi:

```csharp
float cyclesPerSecond = forwardSpeed / strideLength;
```

`fullStrideSpeed` artık yalnız gait ağırlığının hızla ne kadar karışacağını belirliyor; cadence'i belirlemiyor. `strideLength = 1.6` ve `1.8 m/tam çevrim` denemeleri kullanıcıya hızlı geldi. Değer izole biçimde `2.0 m/tam çevrim` yapıldı; tam hız `4 m/s` olduğunda hesap `2.0 çevrim/sn` üretir. Eski, mesafe tabanlı olmayan hesap `1 çevrim/sn` üretiyordu.

Kod derlendi; `2.0` sahneye kaydedildi ve kullanıcı testinde cadence iyi bulundu. Bu değer cadence başlangıç ayarı olarak onaylandı. Ayak kaymasının ileri iyileştirmesi stance/swing ve ayak sabitleme aşamasında ele alınmalı.

## Sonraki önerilen sıra

1. Full-rig acceleration parkur regresyonu 2026-09-08 kullanıcı testinde geçti: rampa, 12/18/21 cm basamaklar, duvar ve dış kuvvet etkileşimleri düzgün çalıştı; eski yaw veya uzuv patlaması dönmedi.
2. A/D yan adım pozu ve W/S gait fazı geçişi 2026-09-08 kullanıcı testinde düzgün bulundu.
3. Proje sahibi mevcut yürüyüşte sorun görmediği için stance/swing, ayak sabitleme, yüzey normaline uyum ve ek gövde eğimi aşamaları şimdilik ertelendi.
4. Sonraki büyük mekanik seçimi eller/nesne tutma veya denge-düşme-toparlanma sistemidir; kapsam uygulanmadan önce proje sahibiyle netleştirilmelidir.

Son doğrulanan Unity durumu: aktif sahne `PhysicsCharacterDemo`, Play kapalı, sahne `Dirty=False`, `strideLength=2.0`, Console'da hata veya uyarı yok.
