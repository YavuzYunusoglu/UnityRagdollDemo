# RagdollDemo

Unity 6 üzerinde **fizik tabanlı üçüncü şahıs karakter** prototipi.

**Hedef:** R.E.P.O. yönünde — çevreye ve darbelere fiziksel tepki veren ama oyuncunun kontrolünde kalan bir karakter.
**Hedef değil:** Human: Fall Flat tarzı sürekli gevşek, salaş his.

---

## Hızlı başlangıç

1. Unity **6000.3.8f1** ile aç.
2. `Assets/Scenes/PhysicsCharacterDemo.unity` sahnesini aç.
3. Play'e bas, Game penceresine tıkla (imleç kilitlenir).

| Girdi | Sonuç |
| --- | --- |
| W / S | Bakış yönünde ileri / geri |
| A / D | Aynı yöne bakarken sola / sağa |
| Mouse X | Karakter + kamera yatay dönüşü |
| Mouse Y | Kameranın dikey bakışı (gövde dik kalır) |
| Esc | İmleci serbest bırakır, hareket girdisini keser |
| Game'e sol tık | İmleci tekrar kilitler |

Esc bir duraklatma menüsü değil: fizik devam eder, karakter frenler.

**Test etmek için sahnede:** rampa, 12/18/21 cm basamaklar ve itilebilir küpler var.
**Düşüş testi:** Play sırasında Player > `Physics Character Balance Controller` bileşen menüsünden `Debug/Force Fall` veya `Debug/Force Stagger`.

---

## Sahne yapısı

```
Player          → Rigidbody (m=12) + Capsule + 4 script    [ana hareket gövdesi]
  ├ Mascot      → görsel mesh
  └ Rig         → görsel kemikler
PhysicsRig      → 12 fizik proxy Rigidbody'si               ★ Player'ın child'ı DEĞİL
Main Camera     → sağ omuz kamerası
Plane + Cube×10 → zemin ve test parkuru
```

Fizik iskeleti Transform hiyerarşisiyle değil, `ConfigurableJoint.connectedBody` bağlantılarıyla kurulu: **13 Rigidbody, 12 ConfigurableJoint**. Görsel kemikler fizik proxy'lerini `LateUpdate`'te takip eder.

## Scriptler — `Assets/Scripts/Character/`

| Dosya | Ne yapar |
| --- | --- |
| `PhysicsCharacterMotor.cs` | Hareket, kontrollü dönüş, zemin kontrolü, basamak çıkma |
| `PhysicsCharacterBalanceController.cs` | Denge / sendeleme / düşme / kalkma state machine |
| `ActiveRagdollPoseDriver.cs` | Duruş pozu, yürüyüş salınımı, yan adım, kalkış pozu |
| `PhysicsBoneFollower.cs` | Fizik rotasyonlarını görsel iskelete aktarır |
| `ShoulderCameraController.cs` | Omuz kamerası, mouse look, engel kontrolü |
| `PhysicsCharacterTelemetry.cs` | Teşhis için CSV kaydı (`Diagnostics/`) |

---

## Nerede kaldık

**Çalışıyor ve testten geçti**
- Kamera yönünde WASD, kontrollü yaw, sağ omuz kamerası, imleç kilidi
- Tam vücut fizik proxy zinciri + görsel kemik takibi
- Mesafe tabanlı procedural yürüyüş (`2.0 m/tam çevrim`) ve A/D yan adımı
- Rampa ve 12/18/21 cm basamak çıkışı
- Dur-kalk savrulması çözüldü (hareket ivmesi tüm rig'e eşit uygulanıyor)
- Düşüş / kalkış / kamera sistemi — *teknik testler geçti, hissiyat onayı bekliyor*

**Sıradaki açık işler**
1. Yeni kalkış ve kamera hissinin oyun testiyle onayı ← **en öncelikli**
2. Bütün eklem drive değerlerini ortak bir "kontrollülük" seviyesine getirme
3. Denge eşiklerinin ve gait değerlerinin hissiyata göre kilitlenmesi
4. Eller ve nesne tutma sistemi (`HandPhysics_L/R` henüz yok)
5. Zıplama, çömelme, koşma, hareketli platform

**Not:** Sahnedeki bazı denge ayarları script varsayılanlarından farklı — özellikle `staggerDriveScale = 0` ve `fallenDriveScale = 0`. Bunlar sendelemede ve düşüşte eklemleri tamamen gevşetiyor. Deneysel ayar mıydı, kalıcı tercih mi, netleşmesi gerekiyor.

---

## Çalışma biçimi

- **Proje sahibi:** kodlama + bütün Inspector/sahne/fizik ayarları, hareket hissi kararı.
- **Claude:** mekanizma anlatımı, kod inceleme, telemetri analizi, doğrulama, dokümantasyon. Açıkça istenmedikçe kod veya sahne ayarı değiştirmez.

Teşhisler tahminle değil; sahne, script, Unity Console ve `Diagnostics/` telemetrisi üzerinden yürütülür.

---

## Dokümantasyon

- **`CLAUDE.md`** — tam teknik referans: mimari, joint tablosu, kütleler, bütün sayısal değerler, bilinen sınırlar, telemetri kullanımı. Karakterle ilgili işe başlamadan önce oku.
- **`RECOVERY_SMOOTHING_REVIEW.md`** — 2026-09-08 düşüş/kalkış/kamera çalışmasının ölçüm kayıtları ve doğrulama sınırları.
- **`Diagnostics/`** — fizik telemetrisi CSV'leri. `RecoveryReview/` altında kalkış kabul testleri, analiz scripti ve değişiklik öncesi yedekler.
