# Setup BossPattern_Slam3x

## ✓ Checklist Setup

### 1. **Boss GameObject Structure** (REQUIRED)
Boss harus punya child transform dengan nama yang mengandung "Right" dan "Hand":
```
Boss (Main GameObject)
├── RightHand (atau "Right Hand" atau "right_hand")
├── LeftHand
└── [Other parts]
```

**Solusi Auto-Find:**
- Script akan otomatis mencari child dengan nama mengandung "right" + "hand"
- Jika tidak ketemu, pastikan nama child benar atau assign manual di Inspector

### 2. **Player Setup**
- ✓ Player GameObject harus bertag "Player"
- ✓ Player harus punya Collider2D (untuk damage detection)

### 3. **Inspector Assignment (BossPattern_Slam3x Component)**

#### **REQUIRED:**
- **Right Hand**: Transform of boss's right hand (MOST IMPORTANT!)
- **Player Transform**: Leave empty (auto-find via "Player" tag)

#### **RECOMMENDED:**
- **Slam Count**: 3 (sudah default)
- **Slam Damage**: 30 (sesuai game balance)
- **Damage Radius**: 1.5 (area damage di sekitar tangan)
- **Player Layer**: Set ke layer player (opsional jika pakai tag)

#### **TIMING:**
- **Telegraph Duration**: 2.0s (waktu alert muncul, player bisa dodge)
- **Alert Fade Duration**: 0.5s (fade out sebelum slam)
- **Delay Between Slams**: 1.0s (jeda antar slam)
- **End Delay**: 0.8s (jeda setelah semua slam)

#### **PERGERAKAN:**
- **Raise Height**: 3.0 (tinggi tangan terangkat saat casting)
- **Slam Down Speed**: 22.0 (kecepatan tangan saat menghantam)
- **Retract Speed**: 6.0 (kecepatan tangan saat kembali)

#### **ALERT VISUAL:**
- **Alert Color**: Red (1, 0.15, 0.15, 0.9)
- **Alert Size**: 2.5
- **Alert YOffset**: -1.5 (posisi alert di bawah, sesuai dengan ground level)

---

## 🎮 CARA PAKAI

### Dari BossController:

```csharp
// Attach component ini ke Boss GameObject
public BossPattern_Slam3x slamPattern;

// Dalam coroutine:
yield return StartCoroutine(slamPattern.ExecutePattern());

// Atau dengan callback:
yield return StartCoroutine(slamPattern.ExecutePattern(() => {
    Debug.Log("Slam pattern selesai!");
    // Lanjut ke pattern berikutnya
}));
```

---

## 🔧 TROUBLESHOOTING

### ❌ "Right Hand belum di-assign di Inspector!"
**Solusi:**
1. Di scene, cari Boss GameObject
2. Expand hierarchy, cari child transform bernama "RightHand" atau "Right Hand"
3. Di BossPattern_Slam3x Inspector, drag transform tersebut ke field "Right Hand"

### ❌ "Player tidak ditemukan!"
**Solusi:**
1. Pastikan Player GameObject punya tag "Player"
   - Select Player → Inspector → Tag dropdown → "Player"
2. Atau assign playerTransform manual di Inspector

### ❌ "Tangan tidak bergerak sama sekali"
**Solusi:**
1. Check Console untuk error message
2. Pastikan rightHand assignment benar
3. Cek Rigidbody2D pada hand — jika pakai physics:
   - Set Body Type: Dynamic
   - Set Gravity Scale: 0
   - Set Is Kinematic: true (agar bisa di-script)

### ❌ "Alert tidak muncul"
**Solusi:**
1. Cek Console untuk debug log
2. Pastikan Camera bisa melihat Z position
3. Pastikan Sorting Order tidak ketutup object lain
4. Check SpriteRenderer color opacity (alpha harus > 0)

### ❌ "Damage tidak terdeteksi"
**Solusi:**
1. Pastikan Player punya Collider2D
2. Pastikan "Player Layer" di Inspector cocok dengan layer player
3. Cek debug log untuk "Found X colliders in slam area"
4. Pastikan Player punya component PlayerHealth atau HealthManager

---

## 📊 DEBUG INFO

Script akan print banyak log ke Console. Cari log berikut untuk tracking:

```
[BossPattern_Slam3x] Auto-found rightHand: RightHand
[BossPattern_Slam3x] Hand origin position: (X, Y, Z)

[BossPattern_Slam3x] ═══════════════════════════════════
[BossPattern_Slam3x] Slam 1 / 3
[BossPattern_Slam3x] DoSingleSlam: Target pos recorded = ...
[BossPattern_Slam3x] Raising hand to ...
[BossPattern_Slam3x] Telegraph wait: 1.5s before fade
[BossPattern_Slam3x] SLAMMING DOWN to ...
[BossPattern_Slam3x] Found X colliders in slam area
[BossPattern_Slam3x] Hit: PlayerName, Tag: Player
[Slam Damage] PlayerHealth -30 HP
```

---

## 📝 FLOW PATTERN

Setiap slam:
1. **Record** posisi player saat ini
2. **Telegraph** (1.5s) — tangan naik, alert muncul, player lihat warning
3. **Fade** (0.5s) — alert fade out smooth
4. **SLAM!** — tangan turun cepat ke posisi player
5. **Damage** — cek apakah player ada dalam radius
6. **Retract** — tangan kembali ke posisi asal
7. **Delay** (1.0s) — jeda sebelum slam berikutnya

**Total waktu per slam:** ~4.6 detik

---

## 🎨 CUSTOMIZATION

### Custom Alert Prefab:
Jika ingin pakai prefab custom (bukan auto-generate):
1. Buat GameObject dengan SpriteRenderer
2. Assign ke field "Alert Prefab"
3. Optional: Tambahkan SlamAlertIndicator component untuk pulse animation

### Adjust Timing:
- Kurangi `telegraphDuration` → less time for player to dodge
- Naikkan `slamDownSpeed` → faster slam
- Naikkan `damageRadius` → lebih mudah kena player

---

## ✅ TESTING

Gunakan **BossPatternTester.cs** untuk test:
- Tekan `2` untuk test Pattern 2 (Slam 3x)
- Tekan `R` untuk reset
- Check Console untuk debug log detail

