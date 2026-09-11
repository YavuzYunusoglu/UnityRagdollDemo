# RagdollDemo — Fizik Tabanlı Karakter Tasarım Yönü

Son güncelleme: 2026-09-08  
Durum: Tam vücut fizik proxy zinciri, kemik takibi, procedural gait, basamak desteği ve kararlı yaw kontrolü çalışıyor. Dur-kalk savrulması full-rig acceleration ile giderildi; mesafe tabanlı gait cadence'i `2.0 m/tam çevrim` değeriyle kullanıcı tarafından onaylandı. Rampa, 12/18/21 cm basamaklar ve çevre/dış kuvvet regresyonu geçti.  
Kaynak: Proje sahibinin bu projeye ilişkin başlangıç açıklaması.

## Amaç ve kesinleşen yön

Bu Unity projesi, fizik tabanlı bir karakterin geliştirileceği demo sahnesidir. Karakter, proje sahibiyle birlikte adım adım geliştirilecektir.

Temel referans **R.E.P.O.** oyunudur. **Human: Fall Flat tarzı aşırı salaş ve gevşek bir karakter hissi hedeflenmiyor.** Fizik tabanlı karakter yaklaşımı korunurken hareket ve kontrol hissi R.E.P.O. yönünde şekillendirilecektir.

Bu referans şimdilik karakterin hareket ve fizik hissini tanımlar. Kamera, görsel stil, diğer oyun mekanikleri ve teknik mimari hakkında tek başına karar oluşturmaz.

### Kamera ve kontrol kararı — 2026-09-03

Proje sahibi üçüncü şahıs omuz kamerası, kilitli mouse imleci ve mouse yönüne dönen karakter istedi. Yatay mouse hareketi karakterin Y eksenindeki dönüşünü, dikey hareket kameranın yukarı/aşağı bakışını kontrol eder. Gövde dik kalır. İlk prototipte sağ omuz, WASD hareketi, Esc ile imleci serbest bırakma ve Game görünümüne sol tıklayarak tekrar kilitleme kullanılır. Sağ omuz ve sayısal ayarlar başlangıç tercihleridir; hissiyata göre değiştirilebilir.

### Mevcut prototip — 2026-09-03

- Sahne: `Assets/Scenes/PhysicsCharacterDemo.unity`.
- Kullanıcının eklediği kemikli Design Mascot modeli, `Player` kökünde Rigidbody ve Capsule Collider ile bulunur. Altında `Mascot` ve `Rig` vardır.
- `PhysicsCharacterMotor`, mevcut Input System asset'indeki `Player/Move` eylemini okuyarak yatay hızı sınırlı ivmeyle hedefe yaklaştırır. Yerçekiminin dikey hızını değiştirmez. Varsayılan hava kontrolü sıfırdır.
- `ShoulderCameraController`, `Main Camera` üzerindedir; mouse bakışını, imleç kilidini ve temel kamera engel kontrolünü yönetir.
- Rigidbody'nin X/Z dönüşleri kilitli, Y dönüşü kontrollü `MoveRotation` hedefiyle yönetilir. Uzuv temaslarının köke aktardığı istenmeyen yaw torku motor tarafından temizlenir.
- `PhysicsRig` altında göğüs, kafa, kol, ön kol, uyluk, kaval ve ayak proxy'leri bulunur. Tüm dinamik Rigidbody'ler aynı hiyerarşi seviyesindedir ve ilişkileri `ConfigurableJoint` bağlantılarıyla tanımlanır.
- `PhysicsBoneFollower`, fizik proxy rotasyonlarını görsel kemiklere uygular. `ActiveRagdollPoseDriver`, dinlenme pozu ile hız tabanlı temel yürüyüş hedeflerini sürer.
- İlk motor ve kamera kurulumunun açıklaması: [CHARACTER_SETUP.md](CHARACTER_SETUP.md). Güncel fizik karakter durumu ve geliştirme sırası: [PHYSICS_CHARACTER_CHECKLIST.md](PHYSICS_CHARACTER_CHECKLIST.md).

### Kaldığımız yer — 2026-09-08

- Rampa ve 12/18/21 cm basamaklar çıkılabiliyor. Basamak yardımı, yükselirken bağlı fizik gövdelerini Player ile aynı miktarda taşıyor.
- A/D için küçük bir procedural yan adım pozu eklendi.
- Hızlı mouse dönüşündeki yaw patlaması çözüldü; önceki canlı ölçümde Player yaw sapması sıfıra, joint anchor ayrışması yaklaşık 1.15 cm seviyesine indi.
- Dur-kalk savrulmasının nedeni telemetriyle ayrıldı: locomotion ivmesi yalnız Player köküne uygulandığında fizik zinciri joint'ler üzerinden gecikmeli taşınıyordu.
- `PhysicsCharacterMotor`, aynı `ForceMode.Acceleration` değerini bütün bağlı dinamik Rigidbody'lere eşit uyguluyor. Kullanıcı 2026-09-08 testinde sert savrulma veya herhangi bir uzuv patlaması görmedi.
- Güncel değerler: Motor `Move Speed 4`, `Acceleration 25`, `Braking 35`, full-rig acceleration açık; Chest Slerp `200/30/100`; iki Thigh Slerp `140/15/70`; iki Shin Slerp `70/8/50`; iki Foot Slerp `35/4/60`; iki Forearm Slerp `17/2.3/30`.
- 2026-09-08 canlı sahne kütleleri: Player `12.00`, fizik proxy toplamı `2.58`, kök/uzuv oranı yaklaşık `4.65`.
- `ActiveRagdollPoseDriver` gait fazı artık `forwardSpeed / strideLength` kullanıyor. `1.6` ve `1.8 m/tam çevrim` kullanıcı denemelerinde hızlı kaldı; aktif sahnedeki `2.0 m/tam çevrim` cadence değeri iyi bulundu. A/D yan adım ve W/S faz geçişinde sorun görülmedi. Stance/swing, ayak sabitleme ve diğer ileri yürüyüş iyileştirmeleri şimdilik ertelendi.
- 2026-09-08 kullanıcı regresyonunda rampa, 12/18/21 cm basamaklar, duvar ve dış kuvvet etkileşimleri düzgün çalıştı; patlama veya step solver regresyonu görülmedi.

## Tasarım yorumu — prototiple doğrulanacak

Aşağıdaki maddeler, kullanıcının tarifinden çıkarılan çalışma hipotezleridir; ayrıca onaylanmış özellikler veya referans oyunların teknik uygulamasına ilişkin tespitler değildir.

- **Kontrol edilebilir hareket:** Karakter, oyuncunun yönlendirmesine anlaşılır ve tutarlı tepki vermeli. Gevşeklik, temel hareketi sürekli zorlaştırmamalı.
- **Hissedilen fizik:** Çevreyle temas, çarpışma ve dış kuvvetler karakter üzerinde okunabilir bir etki oluşturmalı.
- **Ölçülü esneklik:** Gövde veya uzuvlarda fiziksel salınım kullanılabilir; miktarı, hedeflenen daha kontrollü hissi desteklemeli.
- **Denge ve toparlanma:** Karakterin sendelemesi veya devrilmesi eklenirse, kontrol kaybının nedeni ve toparlanması oyuncu açısından anlaşılır olmalı. Bu davranışların eklenmesi henüz kesinleşmedi.

Ana tasarım sorusu: Fiziksel tepkiyi korurken karakteri ne ölçüde dengeli ve doğrudan kontrol edilebilir tutmalıyız?

## Henüz kararlaştırılmayanlar

- Kameranın kesin mesafesi, omuz ofseti ve mouse hassasiyeti.
- Nihai karakter görünümü ve uzuvların son fizik ayarları; mevcut model prototip için kullanılıyor.
- Yürüyüşün son gait değerleri, yan adım pozu ve ayak sabitleme yaklaşımı.
- Yürüme dışındaki eylemler: koşma, zıplama, çömelme, tutma ve taşıma.
- Ragdoll durumuna geçiş, devrilme ve ayağa kalkma kuralları.
- Tek oyunculu veya çok oyunculu kapsam; hedef platform ve giriş cihazları.
- Hareket hızı, kütle, sönümleme ve eklem sertliği gibi ayarlar.

Bu maddeler uygulanacak işler listesi değildir. Gerektikçe proje sahibiyle netleştirilecek açık kararlardır.

## Önerilen ilk değerlendirme

Mevcut sahne ve temel bileşenler incelendi; omuz kamerası ve hareket motoru bağlandı. İlk prototipte şu davranışlar değerlendirilmeli:

1. Düz zeminde hareket, duruş ve yön değiştirme: Oyuncu karakteri öngörülebilir biçimde yönlendirebiliyor mu?
2. Bir engele temas ve dışarıdan itilme: Fiziksel tepki hissediliyor mu, kontrol üzerindeki etkisi uygun mu?
3. Salınım ve denge: Hareket, kullanıcının kaçınmak istediği aşırı salaş hisse kayıyor mu?

Başarı ölçütü, proje sahibinin prototipte R.E.P.O. yönündeki kontrollü fizik hissini doğrulamasıdır. Sayısal eşikler ve ayrıntılı kabul ölçütleri henüz belirlenmedi.

## Sonraki çalışmalar için bağlam

- 2026-09-08: Kullanıcının açık geliştirme isteğiyle denge/düşüş/kalkış ve kamera akışı güncellendi. Mevcut uygulama ve doğrulama sınırları `RECOVERY_SMOOTHING_REVIEW.md` içinde; son hissiyat henüz kullanıcı onayı almadı.

- Güncel tamamlanan işler ve önerilen geliştirme sırası: [PHYSICS_CHARACTER_CHECKLIST.md](PHYSICS_CHARACTER_CHECKLIST.md).
- Yeni sohbet için son teşhis ve devir özeti: [NEXT_CHAT_HANDOFF.md](NEXT_CHAT_HANDOFF.md).
- Karakterle ilgili yeni çalışmaya başlamadan önce bu dosyayı oku.
- Kesinleşen yön ile tasarım yorumlarını birbirinden ayır; açık kararları verilmiş gibi uygulama.
- Yeni kararlar alındığında ilgili bölümü güncelle ve tarih ekle.
- "Mevcut prototip" bölümü uygulanan yapıyı, "Tasarım yorumu" bölümü doğrulanmayı bekleyen hedefleri anlatır. Bir özelliğin uygulanmış olması, hareket hissinin proje sahibi tarafından onaylandığı anlamına gelmez.
