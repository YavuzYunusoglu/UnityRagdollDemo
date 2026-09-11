# Düşüş, kalkış ve kamera incelemesi — 2026-09-08

Bu çalışma, kullanıcının sistemi inceleme ve geliştirme yetkisiyle uygulanmıştır. Nihai hareket hissi kullanıcı tarafından henüz onaylanmadı.

## Teşhis

- Eski recovery, dinamik kökü her FixedUpdate'te `MoveRotation` ile çevirirken tüm gövdelere sabit `MovePosition` yükseltmesi veriyordu. Zeminin ve eklemlerin çözümü bu hedeflerle rekabet ediyordu.
- Süre dolunca `rootBody.rotation` doğrudan dik hedefe yazılıyordu. Karakter gerçekten hazır olmadan kontrol geri gelebiliyordu.
- Pose driver, yere düşmüş karakterin kayma hızını yürüyüş hızı sayıyordu.
- Kamera yalnız `root.position + up * 1.55` noktasına bakıyordu. Kök ayaklara yakın olduğu için karakter yatınca göğüs yüksekliğini temsil etmiyordu.
- Dizlerde kilitli eksenlerle Slerp birlikte kullanılıyordu. Göğüs X limiti yalnız ±4 dereceydi; kalkışa özel eklem kontrolü olmadan toplanma pozu belirginleşmiyordu.

## Yeni akış

1. Düşüşte normal motor kapanır, serbest fizik devam eder. Sakinleşme kısa bir süre boyunca doğrulanır. Maksimum bekleme süresi geçse bile yakın ve uygun eğimli zemin olmadan kalkış başlamaz.
2. Kalkış hedefi beşinci dereceden yumuşak geçişlerle hazırlanır: uzuvları toplama, hafif yana ağırlık aktarımı, öne eğilme ve doğrulma. Girdi kaynaklı gait bu süreçte susturulur.
3. Kök rotasyonu yazılmaz. Açısal hız hatasından hesaplanan sınırlı ivme, gövdenin dünya uzayındaki atalet tensörü üzerinden gerçek torka çevrilir.
4. Zemine uzaklık ve dikey hız, bütün dinamik rig gövdelerine aynı destek ivmesini verir. `recoveryLiftDistance` artık sabit kaldırma mesafesi değil, destek alınabilecek zemin açıklığıdır.
5. Kalkışta kapsül geçici ve kademeli kısalır; kalçanın alçalmasına yer açılır. Göğüs X limitleri geçici genişler. Kilitli eksenli Slerp eklemleri yalnız kalkış sırasında XYAndZ kullanır. Bitişte özgün şekil, limit ve drive modları geri gelir.
6. Kontrol ancak kök dikliği, kök/göğüs hızları ve göğüs sapması yeterince sakin olduğunda geri gelir. Son bölümde ortak yatay fren uygulanır. Kısa bir bitiş koruma süresi, kendi toparlanma hareketinin hemen yeni düşüş sayılmasını önler.
7. Süre dolması artık zorla dikleştirmez; fiziksel düşüş durumuna dönerek tekrar dener. Yaw düşüşte kilitli olduğundan bitiş koşulu yalnız gerçek dikliği değerlendirir; heading motor geri geldiğinde normal akışla yönetilir.

Yay kuvveti ve maksimum kuvvet `scale`, sönümleme `sqrt(scale)` ile ölçeklenir. Ara sertliklerde sönümün gereğinden fazla kaybolması azaltılır. Kalkış hazırlığında eklem güçleri erken toparlanır; Inspector'daki `currentDriveScale` temel eğriyi gösterir, eklem hazırlık tabanı bunu aşabilir.

## Kamera

Normal yürüyüşün mevcut pivotu korunur. Düşüş ve kalkış sırasında göğse göre başlangıçta kalibre edilen nokta, interpolate edilmiş Transform üzerinden LateUpdate'te takip edilir. Geçiş ve göğüs takibi ayrı SmoothDamp kullanır. Mouse yaw/pitch bağımsızdır; kamera karakterle birlikte takla atmaz.

Kamera çarpışmasında kardeş `PhysicsRig` gövdeleri de karakter olarak filtrelenir. Engele yaklaşırken mesafe hemen kısalır; engel açıldığında yavaşça geri uzar. Kamera ölçümü ayrı render-frame CSV'sindedir; FixedUpdate kamera sütunları görsel akıcılık ölçümü olarak kullanılmamalıdır.

## Ayarlar

- Sahne recovery hedef süresi: **1.65 s**; başarısız deneme sınırı: **4 s**. Sakinleşme koşulları nedeniyle gerçek bitiş daha geç olabilir.
- Kök öne eğilmesi: **18°**; gövde pozu: **24°**; kapsül çömelme payı: **0.14 m**.
- Kamera göğüs takibi: **0.12 s**; düşüş/normal takip geçişi: **0.18 s**; engelden uzaklaşma: **0.16 s**.
- Motorun `4 / 25 / 35`, tam-rig ivmesi ve sahnedeki `strideLength = 2.0` değerleri korunur.

## Tekrarlanabilir doğrulama

`Assets/Tests/BalanceRecoveryProbe.cs`, yalnız Unity Editor derlemesinde bulunur ve ancak Play sırasında açıkça eklendiğinde çalışır. İki saniye bekleyip kontrollü açısal hız darbesi verir, fizik ve render-frame CSV'lerini kaydeder; isteğe bağlı yan ve oyun kamerası PNG'leri üretir. Süre sonunda Play'den çıkar. Test bileşeni sahneye kaydedilmez.

Kayıtlar: `Diagnostics/RecoveryReview/`. `baseline-forward.csv` özgün kodun ölçümüdür. `acceptance-*` son uygulamanın doğrulama kayıtlarıdır; `v1`, `v2`, `final-*`, `articulated-*`, `verified-*` ara deneylerdir ve son sürüm kanıtı olarak kullanılmamalıdır.

Değişiklik öncesi üç scriptin tam kopyası ve kaydedilmemiş sahnenin kopyası aynı klasördedir: `*.before.txt`, `BeforeRecovery.unity`. Bu yedek, inceleme başındaki kullanıcı düzenlemelerini de içerir.

## Ölçülen sonuçlar

Unity Play modunda aynı sahne ve 0.02 s fizik adımı kullanıldı. Son durumda ileri, geri ve yan düşüşler birer kalkış denemesiyle `Balanced`a döndü. İleri düşüşte kamera pivotu yaklaşık 1.56 m'den 0.33 m'ye indi ve kalkışla geri yükseldi. Hem yan hem oyun kamerasının kayıtları incelendi.

| Senaryo | Recovery süresi | Tepe dönüş / fizik adımı | Tepe kök ivmesi | Tepe joint anchor ayrımı |
| --- | ---: | ---: | ---: | ---: |
| Eski ileri düşüş | 1.12 s | 4.40° | 16.15 m/s² | 1.18 cm |
| Yeni ileri düşüş | 2.42 s | 1.48° | 6.49 m/s² | 4.37 cm |
| Yeni geri düşüş | 2.26 s | 1.90° | 5.10 m/s² | 0.51 cm |
| Yeni yan düşüş | 2.44 s | 1.72° | 11.89 m/s² | 2.47 cm |

Kalkış daha uzun ve daha düşük tepe ivmeli hale geldi. Yeni toplanma pozunda ileri düşüşün geçici joint anchor ayrımı arttı; bunu akıcılık kazancı olarak yorumlamamak gerekir. Son durumda anchor ayrımı tekrar yaklaşık 1 mm'nin altına indi. Bu ölçümler tek sahnedeki kontrollü teknik testlerdir; genel oynanış veya tüm yüzeyler için garanti değildir. JSON özeti `Diagnostics/RecoveryReview/acceptance-summary.json`, yeniden hesaplama komutu `Diagnostics/RecoveryReview/Analyze-Recovery.ps1` içindedir.

Ek durum testleri:

- Sendeleme: `Staggering → Recovering → Balanced`, tam düşüşe geçmedi.
- Zemin yok: rig yalnız test oturumunda yerçekimi kapatılıp 25 m yükseltildi. 9 saniyelik kayıtta timeout sonrasında da `Fallen` kaldı; kalkış başlamadı.
- Alçak tavan: düşüş sonrasında 1.05 m açıklıklı geçici tavan eklendi. İki kalkış denemesi de 4 saniyede `Fallen`a döndü; zorla dikleşme ve motoru geri verme olmadı. Tavanın altında recovery sırasındaki tepe ivme 30.53 m/s² idi. Engel altında çözüm hâlâ tekrar denemedir; alternatif yer arama sistemi yoktur.
- Edit moduna dönüşte yerçekimi, başlangıç constraint'leri, 2.00386 m kapsül yüksekliği ve göğüs ±4° limitleri doğrulandı. Geçici test nesneleri sahneye kaydedilmedi.
- Sahne kaydı doğrulandı: `Saved=True`, `Dirty=False`, test probe sayısı `0`. Unity Console sorgusu `0 error / 0 warning` döndürdü.
- Kaydedilen son sahne yeniden Play'e alınarak ileri düşüş tekrarlandı (`acceptance-saved-scene.csv`): 2.42 s recovery, 1.48° tepe dönüş adımı, 6.49 m/s² tepe ivme ve sonunda `Balanced`. Son test sonrası sahne tekrar kaydedildi; Play kapalı bırakıldı.

İlk birkaç fizik örneğinin zamanı render/FixedUpdate başlangıç farkıyla negatif olabilir; karşılaştırma yalnız `Recovering` örneklerinden yapılır. Farklı yönlerdeki kayıtlar ayrı senaryolardır; yönler arasında A/B karşılaştırması yapılmaz.

## Kalan sınırlar

Bu hâlâ procedural, fizik destekli bir kalkıştır; el/ayak IK'sı ve zemine kilitlenmiş temas animasyonu değildir. Çevre teması poz hedeflerine göre öncelikli olduğundan eklemlerin gerçek bükülmesi hedef açıdan küçük olabilir. Dar alanlarda otomatik başka bir kalkış yeri seçmez. Yürüyüş hissi, eğimli/karmaşık yüzeyler ve kamera mouse kullanımı ayrıca kullanıcı oyun testiyle değerlendirilmelidir.

Graphify'nin incelemede kullanılan başlangıç haritası `C:/Users/aeria/Desktop/Notes/Vilya/Vilya/Graphify/RagdollDemo/graphify-out` altına aktarılmıştır. Bu harita değişiklik öncesi ilişkileri temsil eder; yeni kodun güncel kaynağı scriptlerdir.
