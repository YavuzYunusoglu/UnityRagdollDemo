# RagdollDemo — Fizik Tabanlı Karakter Checklist

Son güncelleme: 2026-09-08  
Hedef: **R.E.P.O. yönünde kontrollü, fiziksel tepki veren; Human: Fall Flat kadar gevşek olmayan üçüncü şahıs karakter.**

## Durum işaretleri

- `[x]` Uygulandı ve temel testten geçti.
- `[ ]` Yapılacak veya hissiyat onayı bekliyor.

Bir sistemin çalışması, `game feel` açısından son haline ulaştığı anlamına gelmez. Özellikle yay, damper, kütle ve hareket açıları prototip boyunca tekrar ayarlanacaktır.

## 1. Tamamlanan proje temeli

- [x] Projenin fizik karakter yönü ve referansı belgelendi.
- [x] Aktif demo sahnesi oluşturuldu: `Assets/Scenes/PhysicsCharacterDemo.unity`.
- [x] Kemikli Design Mascot modeli `Player` yapısına yerleştirildi.
- [x] LB3D Toon shader'ları URP ile çalışacak şekilde düzeltildi; mevcut material bağlantıları korundu.
- [x] Pembe material sorunu giderildi ve model sahnede görünür durumda doğrulandı.

## 2. Kamera ve temel hareket

- [x] Rigidbody tabanlı `PhysicsCharacterMotor` eklendi.
- [x] WASD girdisi kamera yönüne göre yatay hedef hıza çevrildi.
- [x] Çapraz girdinin daha hızlı hareket üretmesi engellendi.
- [x] İvme ve frenleme `FixedUpdate` içinde kuvvetle uygulanıyor.
- [x] Üçüncü şahıs sağ omuz kamerası eklendi.
- [x] Mouse yatay hareketi kamera ve karakter yaw yönünü kontrol ediyor.
- [x] Mouse dikey hareketi yalnızca kamera pitch açısını değiştiriyor.
- [x] Mouse kilidi, `Esc` ile bırakma ve Game penceresine tıklayarak yeniden kilitleme çalışıyor.
- [x] Kamera engel kontrolü için SphereCast eklendi.
- [x] Rigidbody interpolation kullanılıyor.

## 3. Fizik iskeleti

- [x] `Player`, merkezi hareket gövdesi olarak korundu; ayrıca pelvis Rigidbody eklenmedi.
- [x] `BodyPhysics` layer'ı oluşturuldu.
- [x] Fizik gövdelerinin kendi aralarında çarpışması layer matrisiyle kapatıldı.
- [x] `PhysicsRig` isimli sabit organizasyon kökü oluşturuldu.
- [x] Bütün fizik Rigidbody'leri `PhysicsRig` altında aynı seviyeye taşındı.
- [x] Dinamik Rigidbody altında başka bir Rigidbody kalmadığı doğrulandı.
- [x] Sahne toplamı doğrulandı: `13 Rigidbody`, `12 ConfigurableJoint`.

### Gövde ve kafa

- [x] `ChestPhysics` oluşturuldu ve `Player` gövdesine bağlandı.
- [x] Göğüs açı limitleri ve Slerp Drive başlangıç değerleri ayarlandı.
- [x] `HeadPhysics` oluşturuldu ve `ChestPhysics` gövdesine bağlandı.
- [x] Kafa açı limitleri ve kontrollü yay davranışı ayarlandı.
- [x] Kafa ve göğüs görsel kemikleri fizik proxy'lerini takip ediyor.

### Kollar

- [x] Sol ve sağ üst kol fizik gövdeleri oluşturuldu.
- [x] Sol ve sağ ön kol fizik gövdeleri oluşturuldu.
- [x] Omuzlar üç eksende sınırlı Configurable Joint kullanıyor.
- [x] Dirsekler tek eksenli menteşe gibi sınırlandırıldı.
- [x] Sol ve sağ dirsek limit yönleri model için doğrulandı.
- [x] Kollar kontrollü dinlenme pozuna Slerp Drive ile çekiliyor.
- [x] Görsel üst kol ve ön kol kemikleri fizik proxy'lerini takip ediyor.

### Bacaklar ve ayaklar

- [x] Sol ve sağ uyluk fizik gövdeleri oluşturuldu.
- [x] Sol ve sağ kaval fizik gövdeleri oluşturuldu.
- [x] Kalça eklemleri kontrollü üç eksenli limitler kullanıyor.
- [x] Dizler tek eksenli menteşe gibi sınırlandırıldı.
- [x] Dizlerde hafif doğal kırılma hedefi verildi.
- [x] Sol ve sağ ayak fizik gövdeleri oluşturuldu.
- [x] Ayak bileklerine öne/arkaya ve sınırlı yana esneme verildi.
- [x] Görsel uyluk, kaval ve ayak kemikleri fizik proxy'lerini takip ediyor.

## 4. Pose ve takip sistemi

- [x] `PhysicsBoneFollower`, parent kemikten child kemiğe doğru sıralı güncelleniyor.
- [x] Kemik ile fizik proxy'si arasındaki başlangıç rotasyon farkı offset olarak korunuyor.
- [x] Follower interpolasyon uygulanmış Rigidbody transformlarını kullanıyor.
- [x] Eksik bırakılan opsiyonel gövde/kemik çiftleri için doğrulama eklendi.
- [x] `ActiveRagdollPoseDriver`, bütün joint hedeflerini `FixedUpdate` içinde uyguluyor.
- [x] Kol, bacak ve ayakların sabit dinlenme pozları oluşturuldu.

## 5. Procedural yürüyüş ve kararlılık

- [x] Fiziksel yatay hızdan beslenen procedural gait eklendi.
- [x] Uyluklar karşılıklı salınıyor.
- [x] Öne gelen bacağın dizi daha fazla kırılıyor.
- [x] Kollar bacakların ters yönünde salınıyor.
- [x] Dururken gait ağırlığı yumuşak biçimde sıfıra dönüyor.
- [x] Geri harekette yürüyüş fazı ters yönde ilerliyor.
- [x] Yanal harekette ileri yürüyüş salınımı kapatıldı; A/D sırasında ayakların yaw torku üretmesi azaltıldı.
- [x] Hızlı mouse hareketi için karakter dönüşü `540°/sn` ile sınırlandı.
- [x] Kontrollü yaw hedefi fizik gövdesinin dış torkundan ayrıldı.
- [x] Uzuv temaslarının `Player` üzerinde biriktirdiği yaw açısal hızı temizleniyor.
- [x] Art arda WASD sorunu canlı telemetriyle tekrar üretildi ve düzeltme karşılaştırıldı.

### Son kararlılık ölçümü

- [x] Player yaw sapması `168°` seviyesinden `0°` seviyesine indirildi.
- [x] Tepe Player dönüş hızı `1665°/sn` seviyesinden yaklaşık `0.18°/sn` seviyesine indirildi.
- [x] En yüksek uzuv açısal hızı `20.7 rad/sn` seviyesinden `7.1 rad/sn` seviyesine indirildi.
- [x] En yüksek joint anchor ayrışması `4.2 cm` seviyesinden `1.15 cm` seviyesine indirildi.
- [x] Test sonunda Console'da hata veya exception bulunmadı.

## 6. Sıradaki aşama — temel hareket hissini kilitle

Bu aşama tamamlanmadan yeni fizik uzuvları veya taşıma sistemi eklenmemeli.

- [x] Art arda WASD, hızlı mouse dönüşü ve ani duruşu normal oynanışta tekrar değerlendir.
  - Hızlı mouse dönüşündeki yaw patlaması çözüldü.
  - Dur-kalk savrulmasının nedeni, hareket ivmesinin yalnız Player Rigidbody'sine uygulanıp uzuvlara joint'ler üzerinden gecikmeli aktarılmasıydı.
  - Motor ivmesi bütün bağlı dinamik Rigidbody'lere eşit dağıtıldı. 2026-09-08 kullanıcı testinde sert savrulma veya uzuv patlaması görülmedi; full-rig telemetride Player yaw açısal hızı `0°/sn` kaldı.
  - Bitti ölçütü: İstenmeyen yaw dönüşü, sürekli titreşim veya uzuv patlaması olmamalı.
- [x] Fizik uzuvları için düşük sürtünmeli bir Physics Material oluştur ve temas davranışını karşılaştır.
  - Amaç: Player kapsülü hareketi taşırken ayak ve uzuvların zemine takılıp köke tork aktarmasını azaltmak.
  - Bitti ölçütü: Yanal harekette takılma azalmalı; dışarıdan vurulan uzuvlar hâlâ okunabilir fizik tepkisi vermeli.
- [x] Player kütlesi ile toplam uzuv kütlesi arasındaki oranı değerlendir.
  - 2026-09-08 canlı sahne ölçümü: `Player 12.00`, fizik proxy'leri toplam `2.58`, oran yaklaşık `4.65`. Eski `Player 3.00 / oran 1.16` kaydı güncel sahneyle uyuşmuyordu.
  - Bitti ölçütü: Uzuv temasları karakteri kolayca çevirmemeli; büyük dış kuvvetler hâlâ karakteri etkileyebilmeli.
- [ ] Omuz, dirsek, kalça, diz ve bilek Drive değerlerini ortak bir kontrollülük seviyesine getir.
  - Sağ/sol simetrisi var ancak 2026-09-06 itibarıyla değerler teşhis sırasında deneysel olarak değiştirildi; son kontrollülük seviyesi onaylanmadı.
  - Kilitli açısal eksen bulunan joint'lerde Slerp kullanımının PhysX davranışı yeniden ele alınmalı.
  - Bitti ölçütü: Hiçbir uzuv diğerlerinden belirgin biçimde daha gevşek veya daha robotik görünmemeli.
- [x] Küçük bir test parkuru oluştur: düz zemin, rampa, alçak basamaklar ve itilebilir küpler sahnede bulunuyor.
- [x] Parkurda başlangıç hareket regresyonunu tamamla.
  - Testler: ileri, geri, çapraz, strafe, ani yön değiştirme, hızlı dönüş, duvara sürtünme ve dışarıdan itilme.
  - 2026-09-08 kullanıcı doğrulaması: rampa, 12/18/21 cm basamaklar ve çevre/dış kuvvet etkileşimleri düzgün çalışıyor; full-rig acceleration sonrası patlama veya step solver regresyonu görülmedi.

## 7. Sonraki aşama — yürüyüş kalitesi

- [x] A/D için ayrı ve küçük bir yan adım hedef pozu ekle.
  - Kod eklendi (`1.35` frekans, `7°` uyluk yana açısı, `10°` diz kırılması); 2026-09-08 kullanıcı testinde sorun görülmedi.
- [x] Hareket yönü değişirken gait fazının bacakları çaprazlamadığını doğrula.
  - 2026-09-08 kullanıcı testinde W/S geçişi düzgün bulundu.
- [x] Stride frequency ile gerçek hareket hızını eşleştir.
  - 2026-09-08: gait fazı `forwardSpeed / strideLength` ile mesafe tabanlı hale getirildi. `1.6` ve `1.8 m/tam çevrim` denemeleri hızlı bulundu; `2.0 m/tam çevrim` kullanıcı testinde iyi bulundu ve cadence başlangıç değeri olarak onaylandı.
  - Ayak kaymasını daha fazla azaltmak için stance/swing ve ayak sabitleme çalışmaları aşağıda ayrı maddeler olarak açık kalıyor.
- [ ] Her ayak için basit zemin algılama ekle.
- [ ] Yürüyüşü `swing` ve `stance` fazlarına ayır.
- [ ] Stance ayağını zeminde daha kararlı tutacak kontrollü ayak hedefi ekle.
- [ ] Rampa ve alçak basamaklarda ayak bileği hedefini yüzey normaline yaklaştır.
- [ ] Hızlanma ve frenleme sırasında hafif gövde eğimi ekle.
  - Bitti ölçütü: Fizik hissedilmeli fakat oyuncu yürümek için karakterle mücadele etmemeli.
- 2026-09-08 karar notu: Mevcut yürüyüşte sorun görülmediği için zemin algılama, stance/swing, ayak sabitleme, yüzey normaline uyum ve ek gövde eğimi şimdilik ertelendi.
- [ ] Mevcut gait sayısal değerlerini oyuncu hissine göre onayla:
  - Stride Length: `2.0 m/tam çevrim`
  - Thigh Swing: `18°`
  - Knee Bend: `22°`
  - Arm Swing: `8°`
  - Gait Blend Speed: `6`

## 8. Eller, uzanma ve nesne tutma

- [ ] `HandPhysics_L` ve `HandPhysics_R` proxy'lerini oluştur.
- [ ] El bileği joint limitlerini ayarla.
- [ ] El görsel kemiklerini follower sistemine bağla.
- [ ] Kamera merkezinden fiziksel uzanma hedefi üret.
- [ ] Kolları hedefe çeken kontrollü fizik pose sistemi ekle.
- [ ] Yakındaki fizik nesnesini Joint ile kavrama prototipi oluştur.
- [ ] Tek elle ve çift elle tutma davranışlarını ayır.
- [ ] Taşınabilecek nesne kütlesi ve kavrama kuvveti sınırlarını belirle.
- [ ] Nesne duvara takıldığında kolun veya karakterin patlamadan bırakmasını sağlayan güvenlik kuralı ekle.
  - Bitti ölçütü: Oyuncu nesneyi kolayca hedefleyebilmeli; ağırlık fiziksel hissedilmeli; kontrol rastgele kopmamalı.

## 9. Denge, düşme ve toparlanma

- [ ] Denge durumlarını tanımla: dengeli, sendeleyen, düşmüş ve toparlanan.
- [ ] Dış kuvvet ve gövde açısına göre pose drive gücünü geçici azalt.
- [ ] Kontrollü sendeleme davranışı ekle.
- [ ] Tam ragdoll'a geçiş koşullarını belirle.
- [ ] Havada kullanılacak pose hedefini oluştur.
- [ ] Yere çarpma sonrası kısa toparlanma gecikmesi ekle.
- [ ] Ayağa kalkma veya otomatik toparlanma prototipi oluştur.
  - Bitti ölçütü: Oyuncu neden düştüğünü anlayabilmeli ve makul sürede tekrar kontrol kazanmalı.

## 10. Daha sonraki hareket özellikleri

- [ ] Zıplama.
- [ ] Çömelme.
- [ ] Koşma veya hızlanma modu gerekiyorsa tasarla.
- [ ] Hareketli platform desteği.
- [x] Merdiven ve farklı basamak yükseklikleri için temel step solver; 12/18/21 cm basamaklar kullanıcı testinde çıkıldı.
- [ ] İtme, çekme ve ağır nesne taşıma.
- [ ] Fizik hasarı veya çarpma tepkisi gerekiyorsa eşiklerini belirle.

## 11. Kamera, giriş ve erişilebilirlik

- [x] 2026-09-08: Düşüş/kalkışta göğse göre yumuşak kamera takibi, aşamalı fizik recovery ve kontrollü ileri/geri/yan düşüş testleri. Ayrıntılar: `RECOVERY_SMOOTHING_REVIEW.md`.
- [ ] Yeni kalkış ve kamera hissinin kullanıcı oyun testinde onayı.

- [ ] Kamera mesafesi, omuz ofseti ve hassasiyeti için son değerleri belirle.
- [ ] Sağ/sol omuz değiştirme seçeneğini değerlendir.
- [ ] Controller desteği ve stick deadzone ayarlarını ekle.
- [ ] Tuş yeniden eşleme ihtiyacını belirle.
- [ ] Kamera sarsıntısı kullanılacaksa azaltma/kapatma seçeneği ekle.
- [ ] Düşük hareket toleransı için fizik salınımı azaltma seçeneğini değerlendir.

## 12. Son doğrulama hedefi

- [x] Karakter düz zeminde güvenilir biçimde kontrol ediliyor.
- [x] Ani yön ve kamera değişimleri fizik zincirini kararsızlaştırmıyor.
- [x] Çevre ve nesne temasları görsel olarak okunuyor.
- [x] Karakter dış kuvvetlerden etkileniyor fakat sıradan hareket sırasında kontrolünü kaybetmiyor.
- [ ] Yürüme ve tutma sistemleri R.E.P.O. yönündeki kontrollü fizik hissini destekliyor.
- [ ] Human: Fall Flat düzeyinde sürekli gevşeklik oluşmuyor.
- [ ] Console temiz, sahne kaydedilmiş ve kritik testler tekrarlanabilir durumda.

## Çalışma paylaşımı

- **Proje sahibi:** Kodlama ile Rigidbody, Collider, Configurable Joint, Physics Material ve diğer Inspector/sahne ayarlarını kendisi yapar.
- **Codex:** Mekanizmayı ve gerekçeyi açıklar; proje inceleme, bağlantı haritası çıkarma, veri toplama, telemetri analizi, doğrulama ve dokümantasyon gibi angarya işleri yapar. Proje sahibi açıkça istemedikçe kod veya sahne ayarı değiştirmez.
- **Birlikte:** Hareket hissi, kontrollülük seviyesi, mekanik kapsam ve kabul testleri kararlaştırılır.
