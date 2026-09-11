# İlk karakter hareketi ve omuz kamerası

Bu aşamanın amacı: mouse ile yönlenen, WASD ile fizik üzerinden hareket eden bir karakteri sağ omuz kamerasından kontrol etmek. Uzuv fiziği ve yürüme animasyonu sonraki aşamadır.

## Sahnedeki bağlantılar

- `Player`: Rigidbody, Capsule Collider ve `PhysicsCharacterMotor`.
- `PhysicsCharacterMotor > Input Actions`: `Assets/InputSystem_Actions.inputactions`.
- `PhysicsCharacterMotor > View`: Main Camera üzerindeki `ShoulderCameraController`.
- `Main Camera`: Camera, Audio Listener, mevcut URP kamera bileşeni ve `ShoulderCameraController`.
- `ShoulderCameraController > Target`: `Player` kökü. Kamera Player'ın alt objesi değildir.
- Rigidbody: Use Gravity açık, Is Kinematic kapalı, Interpolate açık, Continuous Dynamic; Freeze Position tamamen kapalı, Freeze Rotation yalnızca X/Z açık.

## Nasıl çalışıyor?

1. **Update:** Input System'den hareket ve mouse girdileri okunur. Mouse X bakışın yatay açısını değiştirir. Mouse Y, kameranın dikey açısını sınırlar içinde değiştirir; karakteri öne/arkaya yatırmaz.
2. **FixedUpdate:** Karakterin yönü `Rigidbody.MoveRotation` ile güncellenir. Hedef yatay hız ile mevcut yatay hız arasındaki fark hesaplanır ve sınırlandırılmış ivme `AddForce` ile uygulanır. Böylece çarpışma etkileri tek karede silinmez. Yatay düzeltme, yerçekiminin yönettiği dikey hıza dokunmaz.
3. **LateUpdate:** Kamera, karakterin interpolasyon uygulanmış konumunu omuz ofsetinden takip eder. Araya engel girerse küre taramasıyla kamera mesafesi kısalır. Fizik güncellemesini beklemeden bakış açısı güncellenir.

Mouse delta zaten o karedeki yer değiştirmedir; tekrar `deltaTime` ile çarpılmaz. Hareket ivmesindeki zaman hesabını `ForceMode.Acceleration` için Unity yapar. Hedef hız farkını ivmeye çevirirken ise `fixedDeltaTime` ile bölüyoruz.

## Kontroller

| Girdi | Sonuç |
| --- | --- |
| W / S | Bakışın yatay yönünde ileri / geri |
| A / D | Aynı yöne bakarken sola / sağa hareket |
| Mouse X | Karakter ve kameranın yatay dönüşü |
| Mouse Y | Kameranın dikey bakışı |
| Esc | İmleci serbest bırakır ve hareket girdisini keser |
| Game görünümüne sol tık | İmleci tekrar kilitler |

Pencere odağı kaybolursa imleç serbest bırakılır; geri dönünce Game görünümüne tıklanır. Esc bir duraklatma menüsü değildir: fizik devam eder, karakter frenler.

## İlk ayarlar

Değerler prototip başlangıçlarıdır; R.E.P.O. oyunundan alınmış değerler değildir.

| Bileşen / alan | Başlangıç | Etki |
| --- | --- | --- |
| Motor / Move Speed | 4 m/s | Hedef hareket hızı |
| Motor / Acceleration | 25 m/s² | Hareket girdisi varken hız düzeltme sınırı |
| Motor / Braking | 35 m/s² | Girdi bırakıldığında frenleme sınırı |
| Motor / Air Control | 0 | Havada hareket motorunun etkisi |
| Kamera / Sensitivity | 0.12 derece/piksel | Mouse hassasiyeti |
| Kamera / Pivot Height | 1.55 m | Kök konumundan kamera pivot yüksekliği |
| Kamera / Shoulder Offset | 0.55 m | Pozitif: sağ omuz; negatif: sol omuz |
| Kamera / Distance | 2.8 m | Geriye uzaklık |
| Kamera / Initial Pitch | 12° | Başlangıçta aşağı bakış |
| Kamera / Minimum–Maximum Pitch | -35° / 70° | Yukarı/aşağı bakış sınırları |

## İlk deneme

1. Play'e bas ve gerekirse Game görünümüne tıkla. Mouse ile karakterin dönmesini ve dik durmasını kontrol et.
2. W ile ilerle, tuşu bırakıp frenlemeyi gözle. A/D sırasında karakter aynı bakış yönünü korumalı. Çapraz yürüyüş daha hızlı olmamalı.
3. Dikey mouse hareketinde gövde dik kalmalı; kamera aşırı dönüp ters yüz olmamalı.
4. Esc'ye basıp yeniden tıkla. Kilit geri gelirken kamera aniden sıçramamalı.
5. Ayarları kalıcı değiştirmek için Play modundan çıkıp Inspector'da düzenle.

Mevcut model başlangıç pozunda hareket eder. Bu aşama tam ragdoll, adım atma, merdiven çıkma, zıplama veya hareketli platform desteği içermez. Kamera engel kontrolü temel prototiptir; pivotun başlangıçta bir engelin içinde olduğu durum ayrıca ele alınmalıdır.

## Doğrulama — 2026-09-03

Unity 6000.3.8f1 içinde scriptler derlendi ve sahne referansları bağlandı. Kamera kadrajı görüntüyle kontrol edildi. Play modunda zemine oturma ve geçici Input System cihazlarıyla şu kontroller doğrulandı:

- İleri ve çapraz hareket hızları yaklaşık 3.88 m/s; çapraz hareket hız avantajı yaratmadı. Fizik temasları nedeniyle hız, 4 m/s hedefinin biraz altında kaldı.
- Girdi bırakıldıktan sonra yatay hız sıfıra yaklaştı.
- 100 piksel yatay mouse girdisi 12° dönüş üretti; kamera hedef açısı ile Rigidbody açısı eşleşti.
- Yukarı bakış -35° sınırında kaldı; gövde dikliğini korudu.
- Esc kilidi bıraktı, sol tık yeniden kilitledi. Pencere odağı yokken kontrol kesildi.
- Kontrol sonunda Console'da hata veya exception yoktu. Geçici giriş cihazları kaldırıldı, Play modu sonlandırıldı.

Hareket hissinin kullanıcı değerlendirmesi ve engelli bir parkurda kamera çarpışmalarının kapsamlı denemesi henüz yapılmadı.

## Kaynaklar

- [Unity: Rigidbody.MoveRotation](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody.MoveRotation.html)
- [Unity: Cursor.lockState](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Cursor-lockState.html)
- [Unity Input System: Mouse](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/manual/Mouse.html)
