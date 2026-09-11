# RagdollDemo — Claude Çalışma Kılavuzu

Devralma tarihi: 2026-09-12. Bu dosya, önceki `CLAUDE/` klasöründeki beş dokümanın (`PROJECT_DIRECTION`, `PHYSICS_CHARACTER_CHECKLIST`, `NEXT_CHAT_HANDOFF`, `BALANCE_FALL_RECOVERY_SYSTEM`, `CHARACTER_SETUP`) konsolide ve **koda/sahneye karşı doğrulanmış** halidir. Çelişki olduğunda kaynak sırası: **sahne dosyası > script > bu doküman**.

---

## 1. Proje ve hedef

Unity 6 demo sahnesinde **fizik tabanlı üçüncü şahıs karakter** geliştiriliyor. Proje sahibi mekaniği adım adım kendisi kuruyor.

- **Referans: R.E.P.O.** — kontrollü, oyuncuya öngörülebilir tepki veren fizik karakteri.
- **Referans DEĞİL: Human: Fall Flat** — sürekli gevşek/salaş his istenmiyor.
- Ana tasarım sorusu: *Fiziksel tepkiyi korurken karakteri ne ölçüde dengeli ve doğrudan kontrol edilebilir tutmalıyız?*

Referans yalnız **hareket ve fizik hissini** tanımlar; kamera stili, görsel yön, diğer mekanikler ve mimari hakkında tek başına karar oluşturmaz.

---

## 2. Çalışma biçimi — ÖNEMLİ

2026-09-08'de kararlaştırılan ve hâlâ geçerli olan iş bölümü:

| Kim | Ne yapar |
| --- | --- |
| **Proje sahibi** | Rigidbody, Collider, ConfigurableJoint, Physics Material, Layer matrisi ve **bütün Inspector/sahne ayarları**. Hareket hissi kararı. |
| **Claude (ben)** | Mekanizma ve gerekçe anlatımı, kod inceleme, bağlantı haritası, telemetri analizi, doğrulama, dokümantasyon. |
| **Birlikte** | Hareket hissi, kontrollülük seviyesi, mekanik kapsamı, kabul ölçütleri. |

**Kural: Proje sahibi açıkça "uygula / değiştir / yaz" demedikçe kod veya sahne ayarı değiştirilmez.** Varsayılan mod: analiz et, açıkla, öner.

Proje sahibi Unity biliyor ve **fizik/kod sistemlerini öğrenmek istiyor**. Dolayısıyla:
- Mekanizmayı ve *neden* öyle olduğunu anlat.
- "Şu menüye tıkla" seviyesinde temel tarif verme.
- Teşhisleri sahne, script, Unity Console ve `Diagnostics/` telemetrisi üzerinden yürüt — tahminle değil.

---

## 3. Ortam

- Unity **6000.3.8f1**, URP **17.3.0**, Input System **1.18.0**, Test Framework 1.6.0.
- Yerçekimi `-9.81`, fizik adımı `0.02 s` (varsayılan).
- Aktif sahne: `Assets/Scenes/PhysicsCharacterDemo.unity` (tek sahne).
- Input asset: `Assets/InputSystem_Actions.inputactions` → motor yalnız `Player/Move` (Vector2) eylemini okur. Mouse doğrudan `Mouse.current` üzerinden okunuyor.
- **Unity MCP mevcut** (`mcp__unity-mcp__*`): Console log okuma, komut çalıştırma, Scene/Game view capture. Canlı doğrulama için kullan; yazma işlemlerinde yine izin iste.

---

## 4. Sahne mimarisi

```
Scene
├── Player                     (Rigidbody m=12, CapsuleCollider h≈2.0, 4 script)
│   ├── Mascot                 (görsel mesh — Design Mascot, LB3D Toon/URP)
│   └── Rig                    (görsel kemik hiyerarşisi: Spine, Head, Upper_Arm_*, Thigh_* ...)
├── PhysicsRig                 ★ Player'ın CHILD'I DEĞİL — sahne kökünde ayrı nesne
│   ├── ChestPhysics  HeadPhysics
│   ├── UpperArmPhysics_L/R    ForearmPhysics_L/R
│   ├── ThighPhysics_L/R       ShinPhysics_L/R       FootPhysics_L/R
├── Main Camera                (ShoulderCameraController)
├── Plane                      (zemin)
└── Cube (1..10)               (test parkuru: rampa, 12/18/21 cm basamaklar, itilebilir küpler)
```

**Bunu unutma:** `PhysicsRig` Player'ın altında olmadığı için `Player.GetComponentsInChildren<ConfigurableJoint>()` **hiçbir joint bulmaz**. Yeni bir controller yazarken ya Inspector referansı al, ya `physicsRig` transformunu tara, ya da motorun yaptığı gibi joint grafiğini sahne genelinden yürü.

### Fizik zinciri

12 `ConfigurableJoint` var; hiyerarşi **Transform'la değil `connectedBody` ile** kuruluyor. Bütün dinamik Rigidbody'ler aynı Transform seviyesinde duruyor (iç içe Rigidbody yok — bu kasıtlı).

| Joint sahibi | connectedBody | Mode | Angular motion (X/Y/Z) | Slerp (spring/damper/maxForce) | Limitler |
| --- | --- | --- | --- | --- | --- |
| ChestPhysics | Player | Slerp | Limited/Limited/Limited | `200 / 30 / 100` | X ±4°, Y 3°, Z 3° |
| HeadPhysics | ChestPhysics | Slerp | Lim/Lim/Lim | `100 / 15 / 60` | X ±4°, Y 4°, Z 4° |
| UpperArmPhysics_L/R | ChestPhysics | Slerp | Lim/Lim/Lim | `25 / 4 / 40` | X ±45°, Y 35°, Z 75° |
| ForearmPhysics_L | UpperArmPhysics_L | Slerp | Lim/Locked/Locked | `17 / 2.3 / 30` | X [-110, 10] |
| ForearmPhysics_R | UpperArmPhysics_R | Slerp | Lim/Locked/Locked | `17 / 2.3 / 30` | X [-10, 110] |
| ThighPhysics_L/R | Player | Slerp | Lim/Lim/Lim | `140 / 15 / 70` | X [-35, 45], Y 15°, Z 20° |
| ShinPhysics_L/R | ThighPhysics_* | Slerp | Lim/Locked/Locked | `70 / 8 / 50` | X [-5, 110] |
| FootPhysics_L/R | ShinPhysics_* | Slerp | Lim/Locked/Lim | `35 / 4 / 60` | X [-25, 70], Z 12° |

### Kütleler (sahne, doğrulanmış)

`Player 12.0` | Chest `0.5` | Head `0.2` | Thigh `0.3`×2 | Shin `0.22`×2 | Foot `0.12`×2 | UpperArm `0.18`×2 | Forearm `0.12`×2
→ **proxy toplamı `2.58`, kök/uzuv oranı ≈ `4.65`**. Bu oran kasıtlı: uzuv temasları kökü kolayca çeviremesin diye.

Fizik gövdeleri `BodyPhysics` layer'ında ve **kendi aralarında self-collision kapalı** (Layer Collision Matrix). Uzuvlarda düşük sürtünmeli Physics Material var (`Assets/Physics/`).

---

## 5. Script haritası

Hepsi `Assets/Scripts/Character/`. `PhysicsBoneFollower` dışında hepsi `RagdollDemo.Character` namespace'inde.

| Script | Exec order | Görev |
| --- | ---: | --- |
| `PhysicsCharacterMotor.cs` | 0 | Kamera yönünde WASD hareketi, kontrollü yaw, ground check, step solver, full-rig acceleration |
| `PhysicsCharacterBalanceController.cs` | 50 | Denge state machine (Balanced/Staggering/Fallen/Recovering), drive ölçekleme, fizik destekli kalkış |
| `ActiveRagdollPoseDriver.cs` | 60 | Joint `targetRotation` hedefleri: dinlenme pozu, procedural gait, yan adım, kalkış pozu |
| `PhysicsBoneFollower.cs` | 100 | `LateUpdate`'te fizik proxy rotasyonlarını görsel kemiklere aktarır (parent→child sırayla, başlangıç offset'i koruyarak) |
| `PhysicsCharacterTelemetry.cs` | 1000 | `Diagnostics/physics-telemetry-*.csv` yazar |
| `ShoulderCameraController.cs` | — | `LateUpdate`: mouse look, omuz kadrajı, cursor lock, SphereCast engel kontrolü, düşüşte göğüs takibi |
| `Assets/Tests/BalanceRecoveryProbe.cs` | — | Editor-only, Play sırasında elle eklenince kontrollü düşüş testi yapıp CSV/PNG üretir |

### Kare içi akış

```
Update        : kamera mouse look (Yaw/pitch) + moveInput okuma
FixedUpdate 0 : Motor  → yaw MoveRotation, ground check, step up, hareket ivmesi
FixedUpdate 50: Balance→ ölçümler, state machine, drive scale, recovery tork/destek
FixedUpdate 60: Pose   → joint targetRotation yazımı
   [PhysX çözümü]
FixedUpdate 1000: Telemetry CSV satırı
LateUpdate    : BoneFollower (kemikler) + Camera (pivot, boom, collision)
```

### Kritik mekanizmalar — neden böyle

**Full-rig acceleration** (`accelerateConnectedBodies = true`). Motor aynı `ForceMode.Acceleration` değerini Player'a **ve bütün bağlı dinamik Rigidbody'lere** uygular. Sadece köke uygulandığında joint'ler locomotion ivmesini uzuvlara gecikmeli taşıyor ve dur-kalkta kırbaç etkisi oluşuyordu. 2026-09-08 telemetri karşılaştırması (kök-only → full-rig, W bırakma tepe değerleri):

| Bölge | Hız farkı | Açı değişimi | Göreli açısal hız |
| --- | ---: | ---: | ---: |
| Chest | `0.441 → 0.053 m/s` | `8.83° → 3.98°` | `2.17 → 0.23 rad/s` |
| Thigh | `0.462 → 0.292 m/s` | `16.96° → 11.67°` | `2.34 → 1.48 rad/s` |
| Shin | `1.879 → 0.868 m/s` | `31.84° → 13.88°` | `3.87 → 1.74 rad/s` |
| Foot | `2.839 → 1.171 m/s` | `27.17° → 7.65°` | `4.33 → 1.28 rad/s` |

**Yaw sahipliği.** Motor her `FixedUpdate`'te `body.angularVelocity.y = 0` yapar, sonra `MoveRotation` ile `controlledYaw`'a döner (`turnSpeed 540°/s`). Uzuv temaslarının köke aktardığı yaw impulse'u böylece atılıyor. Bu düzeltmeyle Player yaw sapması `168° → 0°`, tepe dönüş hızı `1665°/s → 0.18°/s`, joint anchor ayrışması `4.2 cm → 1.15 cm` seviyesine indi.

**Step solver.** Üç raycast: alt (dikey yüzey mi), üst (`maxStepHeight`'ta boş mu), tepe (yürünebilir mi). Geçerse `MoveCharacterBodiesUp` **bütün rig'i aynı miktarda** kaldırır — sadece kökü kaldırmak bacak joint'lerini geriyordu. 12/18/21 cm basamaklar kullanıcı testinde çıkıldı.

**Mesafe tabanlı cadence.** `cyclesPerSecond = forwardSpeed / strideLength`. `fullStrideSpeed` artık yalnız gait ağırlığını (blend) belirliyor, cadence'i değil. `strideLength = 2.0 m/tam çevrim` kullanıcı onaylı.

**Drive ölçekleme fiziği.** `ScaleDrive`: `spring *= scale`, `damper *= sqrt(scale)`, `maxForce *= scale`. Damper'ın `sqrt` ile ölçeklenmesi kasıtlı — kritik sönümleme `sqrt(k)` ile gider; ikisini de lineer ölçeklemek ara sertliklerde ciddi underdamped davranış üretiyordu. Her ölçekleme **başlangıç yedeğinden** hesaplanır, üstel kayıp olmaz.

**Recovery, rotasyon yazmaz.** `ApplyRecoveryTorque` açısal hız hatasından sınırlı ivme hesaplar, bunu gövdenin **dünya uzayı atalet tensörü** üzerinden gerçek torka çevirip `AddTorque` uygular. `ApplyGroundSupport` da sabit lift yerine PD benzeri destek ivmesini bütün rig'e verir. `recoveryLiftDistance (0.35)` artık "kaldırma mesafesi" değil, **destek alınabilecek maksimum zemin açıklığı**. Timeout artık zorla dikleştirmez — `Fallen`'a döner ve tekrar dener. Bu sayede havada/tavan altında karakter uçarak dikleşmiyor.

---

## 6. Diskteki güncel sayısal değerler

**⚠ DİKKAT: Sahnedeki bazı değerler script varsayılanlarından ve eski dokümanlardan farklı.** Proje sahibi son handoff'tan sonra Inspector'da ayar yapmış. Aşağıdaki tablo `PhysicsCharacterDemo.unity`'den okundu (2026-09-12).

### Motor
`moveSpeed 4` · `turnSpeed 540` · `acceleration 25` · `braking 35` · `airControl 0` · `accelerateConnectedBodies true` · `maxStepHeight 0.25` · `stepCheckDistance 0.12` · `stepUpSpeed 3` · `maxGroundAngle 55°`

### Kamera
`pivotHeight 1.55` · `shoulderOffset 0.55` (sağ omuz) · `distance 2.8` · `sensitivity 0.12 °/px` · `initialPitch 12°` · pitch `[-35, 70]` · `fallFollowSmoothTime 0.12` · `fallBlendTime 0.18` · `collisionReturnTime 0.16`

### Gait
`fullStrideSpeed 4` · `strideLength 2.0` · `thighSwing 18°` · `kneeBend 22°` · `armSwing 8°` · `gaitBlendSpeed 6` · `sideStepFrequency 1.35` · `sideStepThighAngle 7°` · `sideStepKneeBend 10°`

### Balance controller — sahne ≠ kod varsayılanı

| Alan | **Sahne (gerçek)** | Kod varsayılanı | Not |
| --- | ---: | ---: | --- |
| `staggerDriveScale` | **`0`** | `0.35` | ⚠ Sendelemede joint'ler **tamamen gevşek** |
| `fallenDriveScale` | **`0`** | `0.03` | ⚠ Düşüşte **tam ragdoll** |
| `fallChestDeviation` | **`60°`** | `30°` | ⚠ Poz sapmasından düşmek çok zorlaştı |
| `minimumFallenDuration` | **`2 s`** | `1 s` | |
| `recoveryForwardLean` | **`35°`** | `18°` | Maksimum değerde |
| `startupGraceTime` | `1` | `1` | |
| `staggerRelativeSpeed` / `fallRelativeSpeed` | `1` / `3` m/s | aynı | |
| `staggerRelativeAngularSpeed` / `fallRelativeAngularSpeed` | `3` / `7` rad/s | aynı | |
| `staggerChestDeviation` | `12°` | aynı | |
| `staggerDuration` | `0.35 s` | aynı | |
| `maximumFallenDuration` | `3 s` | aynı | |
| `settledLinearSpeed` / `settledAngularSpeed` | `0.8` / `2.5` | aynı | |
| `recoveryDuration` / `maximumRecoveryDuration` | `1.65` / `4 s` | aynı | |
| `recoveryRotationSpeed` | `220 °/s` | aynı | |
| `recoveryAngularDamping` | `12` | aynı | |
| `recoveryLiftDistance` | `0.35 m` | aynı | |
| `uprightTolerance` | `0.5°` | aynı | |
| `recoveryCrouchDepth` | `0.14 m` | aynı | |
| `recoveryAngularAcceleration` | `45 rad/s²` | aynı | |
| `supportResponseTime` / `maximumSupportAcceleration` | `0.2 s` / `35 m/s²` | aynı | |
| `recoverySettleTime` / `postRecoveryGraceTime` / `fallenSettleTime` | `0.18` / `0.45` / `0.18 s` | aynı | |

`staggerDriveScale = 0` ile hafif bir darbe karakteri `0.35 s` boyunca tamamen gevşetiyor, sonra `recoveryDuration 1.65 s` boyunca drive geri geliyor — yani **küçük bir temas ~2 saniyelik gevşeklik üretebilir.** Bu, "Human: Fall Flat gibi olmasın" hedefiyle doğrudan çelişebilir. Kullanıcıyla netleştirilmeli: deneysel ayar mı, kalıcı tercih mi?

---

## 7. Nerede kaldık

### Çalışıyor (kullanıcı testinden geçti)

- Rigidbody tabanlı locomotion, kamera yönünde WASD, çapraz hız avantajı yok.
- Sağ omuz kamerası, mouse kilidi, Esc/sol-tık akışı, SphereCast engel kontrolü.
- Kontrollü yaw; hızlı mouse dönüşünde yaw patlaması yok.
- 13 Rigidbody / 12 ConfigurableJoint fizik zinciri + görsel kemik takibi.
- İleri/geri procedural gait (mesafe tabanlı cadence) + A/D yan adım pozu.
- Rampa ve 12/18/21 cm basamak çıkışı; duvar sürtünmesi ve dış kuvvet etkileşimi.
- Full-rig acceleration; dur-kalk savrulması çözüldü.
- Denge/düşüş/kalkış state machine + düşüşte göğüs takipli kamera. **Teknik testler geçti, hareket hissi henüz kullanıcı onayı almadı.**

### Açık (checklist'ten devralınan)

**Yakın:**
- [ ] Kalkış ve düşüş kamerasının **hissiyat onayı** — en büyük açık kalem.
- [ ] Omuz/dirsek/kalça/diz/bilek drive değerlerini ortak bir "kontrollülük" seviyesine getir. Hiçbir uzuv diğerinden belirgin gevşek veya robotik görünmemeli.
- [ ] Kilitli açısal eksenli joint'lerde Slerp kullanımının PhysX davranışını yeniden değerlendir (Forearm, Shin, Foot bu durumda).
- [ ] Gait sayısal değerlerinin (stride 2.0 / swing 18° / knee 22° / arm 8° / blend 6) oyuncu hissine göre onayı.

**Ertelenmiş (2026-09-08 kararı — yürüyüşte sorun görülmedi):**
- [ ] Ayak başına zemin algılama · swing/stance ayrımı · stance ayağını sabitleme · bilek hedefini yüzey normaline yaklaştırma · hızlanma/frenlemede gövde eğimi.

**Büyük mekanikler (kapsam netleşmedi):**
- [ ] **Eller ve nesne tutma:** `HandPhysics_L/R` proxy'leri, bilek limitleri, follower bağlantısı, kamera merkezinden uzanma hedefi, Joint ile kavrama, tek/çift el, kütle-kuvvet sınırları, takılma güvenlik kuralı. *(Görsel `Hand_L/R` kemikleri sahnede var, fizik proxy'si yok.)*
- [ ] Zıplama, çömelme, koşma, hareketli platform, itme/çekme/ağır taşıma, fizik hasarı.
- [ ] Kamera son değerleri, sol/sağ omuz değiştirme, controller desteği, tuş remap, erişilebilirlik seçenekleri.

---

## 8. Bilinen sınırlar ve riskler

**Sistem sınırları (tasarım gereği, hata değil):**
- Kalkış procedural ve fizik destekli; el/ayak IK'sı veya zemine kilitli temas animasyonu yok.
- Dar alanda alternatif kalkış yeri aranmaz; çözüm yalnız tekrar denemedir. Alçak tavan testinde iki deneme de `Fallen`'a döndü (doğru davranış, ama karakter orada takılı kalır).
- Zemin yoksa (havada) recovery hiç başlamaz — bu kasıtlı güvenlik.
- Havada özel pose hedefi yok. `airControl 0`.
- Darbe kaynağı/temas noktası kaydedilmez; yalnız göğüs-kök göreli bozulması ölçülür.
- Eşiklerde hysteresis yok; state süreleri dolaylı koruma sağlıyor.
- `rootTilt` ölçülüyor ama düşme kararında **kullanılmıyor** (yalnız recovery bitiş kontrolünde diklik ayrıca bakılıyor).
- Ön/arka/yan düşüşe göre ayrı kalkış pozu yok.

**Kod/proje hijyeni (küçük, fırsat bulunca):**
- `PhysicsBoneFollower` global namespace'te; diğerleri `RagdollDemo.Character`.
- `PhysicsCharacterTelemetry` her Play'de `RuntimeInitializeOnLoadMethod` ile **koşulsuz** kurulur ve CSV yazar. `Diagnostics/` `.gitignore`'da değil → repo ve disk şişiyor (şu an 100+ CSV). Regresyonlar bitince açma/kapama anahtarı veya ignore kuralı konuşulmalı.
- Motor `Awake`'te `CacheCharacterBodies()` null kontrolünden önce çalışıyor.
- `BALANCE_FALL_RECOVERY_SYSTEM.md`'de bildirilen iki bug (**`angularYZ = joint.angularXDrive`** ve eksik `FinishRecovery()` kapanışı) **düzeltilmiş durumda** — doküman eskiydi, kod doğru.

**Son telemetri gözlemi (`Diagnostics/physics-telemetry-20260911-235204.csv`, 245 s):**
Oturum boyunca **hiç hareket input'u yok** (Play açık bırakılmış, Game penceresi odakta değil). Buna rağmen `t ≈ 2.2 s`'de karakter kendiliğinden ivmelenip `1.55 m/s`'ye ulaşmış ve ~2.9 s sürüklenip durmuş. Grounded `1`, step solver hiç tetiklenmemiş. Muhtemelen spawn yerleşmesi/kayma. **Doğrulanmadı — kullanıcıyla teyit edilmeli, gerekirse tekrar üretilmeli.**

---

## 9. Telemetri nasıl kullanılır

- Çıktı: `Diagnostics/physics-telemetry-yyyyMMdd-HHmmss.csv`, her fizik adımında bir satır, 10 örnekte bir flush.
- Sütunlar: input, desired/target/applied acceleration, grounded, step, `full_rig_acceleration`, Player + Chest + Thigh/Shin/Foot (L/R) dünya hızları, köke göre açılar ve göreli açısal hızlar, kamera offset/mesafe.
- **FixedUpdate'teki kamera sütunlarını görsel akıcılık ölçümü olarak kullanma** — kamera `LateUpdate`'te çalışır. Render-frame ölçümü ayrı CSV'de.
- Motor teşhis değerlerini read-only property'lerle açıyor (`LastMoveInput`, `LastAppliedAcceleration`, `SteppedThisFixedUpdate` ...). Yeni bir ölçüm gerekiyorsa aynı deseni izle.
- `Diagnostics/RecoveryReview/`: `acceptance-*` = son sürümün doğrulama kayıtları. `baseline-forward.csv` = eski kod. `v1`, `v2`, `final-*`, `articulated-*`, `verified-*` = **ara deneyler, kanıt olarak kullanma.** Yeniden hesap: `Analyze-Recovery.ps1`. Özet: `acceptance-summary.json`.
- Değişiklik öncesi script/sahne yedekleri: `Diagnostics/RecoveryReview/*.before.txt`, `BeforeRecovery.unity`.

Kalkış ölçüm sonuçları (2026-09-08, aynı sahne, 0.02 s adım):

| Senaryo | Recovery süresi | Tepe dönüş/adım | Tepe kök ivmesi | Tepe anchor ayrımı |
| --- | ---: | ---: | ---: | ---: |
| Eski ileri düşüş | 1.12 s | 4.40° | 16.15 m/s² | 1.18 cm |
| Yeni ileri düşüş | 2.42 s | 1.48° | 6.49 m/s² | 4.37 cm |
| Yeni geri düşüş | 2.26 s | 1.90° | 5.10 m/s² | 0.51 cm |
| Yeni yan düşüş | 2.44 s | 1.72° | 11.89 m/s² | 2.47 cm |

Kalkış daha uzun ve daha düşük tepe ivmeli hale geldi. İleri düşüşteki geçici anchor ayrımı artışını "akıcılık kazancı" diye yorumlama.

### Test raporu formatı

```text
Test:
State sırası:
Beklenen davranış:
Görülen davranış:
Live Readout tepe değerleri:
Console hata/uyarı:
Varsa video / Diagnostics CSV adı:
```

---

## 10. Debug araçları

- Balance controller Inspector context menu (yalnız Play sırasında):
  - `Debug/Force Stagger` → `Staggering` + göğse `0.75 m/s` VelocityChange.
  - `Debug/Force Fall` → `Fallen` + köke `3.5 rad/s` VelocityChange tork.
  - `VelocityChange` kütleden bağımsız olduğu için farklı kütle ayarlarında tekrarlanabilir.
- `Live Readout` bölümü (state, chestDeviation, relativeLinear/AngularSpeed, rootTilt, currentDriveScale) yalnız izleme amaçlı, controller yazar.
- `Assets/Tests/BalanceRecoveryProbe.cs`: Editor-only, sahneye kaydedilmez. Play'de elle eklenince 2 s bekler, kontrollü açısal darbe verir, fizik + render CSV'si ve isteğe bağlı PNG üretir, sonra Play'den çıkar.
- Bir eşik aşıldığı fizik adımında motor (order 0) balance'tan (order 50) önce çalışmış olabilir → o adımda son bir motor ivmesi uygulanmış olabilir. Sonraki adımlarda motor kapalıdır.

---

## 11. Henüz karara bağlanmayanlar

Bunlar **iş listesi değil**, proje sahibiyle netleştirilecek açık sorular:

- Kameranın nihai mesafesi, omuz ofseti, hassasiyeti; sol/sağ omuz seçeneği.
- Nihai karakter görünümü (mevcut Design Mascot prototip amaçlı).
- Denge eşiklerinin ve drive scale değerlerinin son hali (özellikle `staggerDriveScale`/`fallenDriveScale = 0` tercihi).
- Yürüyüş dışı eylemlerin kapsamı: koşma, zıplama, çömelme, tutma/taşıma.
- Tek/çok oyunculu kapsam, hedef platform, giriş cihazları.
- Telemetrinin production'da kalıp kalmayacağı.

**Bir özelliğin uygulanmış olması, hareket hissinin onaylandığı anlamına gelmez.** "Kesinleşen yön" ile "doğrulanmayı bekleyen hipotez"i ayrı tut.

---

## 12. Bu dosyayı güncelleme kuralı

- Yeni karar alındığında ilgili bölümü **tarih ekleyerek** güncelle.
- Sayısal değer yazarken kaynağını belirt (sahne mi, script varsayılanı mı).
- Sahnedeki değer değiştiğinde §6 tablosunu yenile — bu tablo kolayca eskiyor.
- Uzun teşhis anlatılarını buraya yığma; ölçüm CSV'sine + kısa özete işaret et.
