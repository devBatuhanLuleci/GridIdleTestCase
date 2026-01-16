# Board Defence Game - Test Case

Bu proje, Unico Studio için geliştirilmiş bir grid tabanlı savunma oyunu test case'idir. Oyuncular grid üzerine savunma birimleri yerleştirerek düşman dalgalarına karşı mücadele ederler.

## 🎮 Oyun Özellikleri

- **Grid Tabanlı Yerleştirme Sistemi**: Savunma birimlerini grid üzerine yerleştirme
- **Savaş Sistemi**: Otomatik atış mekanizması ile düşmanlara saldırma
- **Stratejik Saldırı Yönleri**: İleriye doğru veya her yöne saldırı stratejileri
- **Düşman Dalga Sistemi**: Farklı tipte düşmanlar ve dalga yönetimi
- **Envanter Yönetimi**: Seviye bazlı envanter sistemi
- **Durum Yönetimi**: Yerleştirme, Savaş, Kazanma ve Kaybetme durumları

## 🏗️ Mimari

Proje, modüler bir mimari ile SOLID prensiplerine uygun olarak geliştirilmiştir. Her modül kendi assembly tanımına sahiptir ve bağımlılıklar arayüzler üzerinden yönetilir.

### Temel Mimari Prensipler

- **Service Locator Pattern**: Runtime servis erişimi için ServiceLocator kullanımı
- **Dependency Injection**: [SerializeField] referansları ve arayüz tabanlı bağımlılık yönetimi
- **Modüler Assembly Yapısı**: Her modül kendi assembly tanımına sahip
- **State Machine**: Oyun durumu yönetimi için state pattern kullanımı
- **Interface-Based Design**: Tüm bağımlılıklar arayüzler üzerinden yönetilir

### Kod Standartları

- Yorum satırları kullanılmaz (self-explanatory code)
- Reflection tabanlı Unity metodları kullanılmaz (FindObjectOfType, GetComponent vb.)
- String tabanlı kontroller yapılmaz
- Runtime reflection kullanılmaz
- Tüm bağımlılıklar ServiceLocator veya [SerializeField] ile enjekte edilir

## 📦 Modüller

### Core Modules

- **BoardGameTestCase.Core**: Temel ScriptableObject'ler ve ortak yapılar
- **GridSystemModule**: Grid yönetimi, tile sistemi ve yerleştirme validasyonu
  - Core: Grid arayüzleri ve temel yapılar
  - Managers: Grid yönetim mantığı
  - Services: Grid servisleri
  - Tiles: Tile implementasyonları

### Gameplay Modules

- **GameModule**: Oyun akışı kontrolü ve durum yönetimi
  - Core: Arayüzler ve enum'lar
  - Managers: StateManager, GameManager
  - Services: GameFlowController

- **GameplayModule**: Oyun içi mekanikler
  - Combat: Savunma birimleri savaş sistemi
  - Strategies: Saldırı stratejileri (Forward, AllDirections)

- **CombatModule**: Genel savaş yönetimi
  - CombatManager: Savaş durumu yönetimi
  - Enemy tracking ve combat lifecycle

- **PlacementModule**: Yerleştirme sistemi
  - Grid üzerine birim yerleştirme mekanizması
  - Geçerli/geçersiz yerleştirme kontrolleri

- **InventoryModule**: Envanter yönetimi
  - Seviye bazlı envanter sistemi

### UI Modules

- **UISystemModule**: Kullanıcı arayüzü sistemi
  - Core: UI arayüzleri
  - Managers: UI yönetimi
  - UIElements: UI bileşenleri
  - Combat, Gameplay, Inventory, Settings alt modülleri

### Utility Modules

- **DebugModule**: Geliştirme ve debug araçları
- **Editor**: Editor araçları ve ScriptableObject oluşturucular

## 🚀 Kurulum

### Gereksinimler

- Unity Editor 6000.2.6f2 veya üzeri
- Universal Render Pipeline desteği

### Adımlar

1. Projeyi klonlayın:
```bash
git clone [repository-url]
```

2. Unity Hub üzerinden projeyi açın

3. Unity Editor'de projeyi açtıktan sonra, tüm assembly referanslarının doğru yüklendiğinden emin olun

4. `Assets/BoardGameTestCase/Scenes/Gameplayscene.unity` sahnesini açın

5. Play butonuna basarak oyunu başlatın

## 🎯 Oyun Akışı

1. **Placing State**: Oyuncu grid üzerine savunma birimleri yerleştirir
2. **Fight State**: Yerleştirme tamamlandıktan sonra savaş başlar
   - Düşmanlar spawn olur
   - Savunma birimleri otomatik olarak atış yapar
   - Düşmanlar hedefe ulaşmaya çalışır
3. **Win/Lose State**: Tüm düşmanlar yenildiğinde kazanma, hedefe ulaşan düşman olduğunda kaybetme

## 🛠️ Teknik Detaylar

### Servis Lokasyonu

Tüm servisler `ServiceLocator` pattern ile yönetilir:

```csharp
ServiceLocator.Instance.Register<IService>(service);
ServiceLocator.Instance.Get<IService>();
```

### Durum Yönetimi

Oyun durumları `StateManager` üzerinden yönetilir:
- `Placing`: Yerleştirme aşaması
- `Fight`: Savaş aşaması
- `Win`: Kazanma durumu
- `Lose`: Kaybetme durumu

### Saldırı Stratejileri

Savunma birimleri farklı saldırı stratejilerine sahiptir:
- `ForwardAttackStrategy`: Sadece ileri yönde saldırı
- `AllDirectionsAttackStrategy`: Her yönde saldırı

### Grid Sistemi

- Tile tabanlı grid yapısı
- Yerleştirme validasyonu
- Geçerli/geçersiz yerleştirme görsel feedback'i

## 📁 Proje Yapısı

```
Assets/BoardGameTestCase/
├── Scripts/
│   ├── CORE/                    # Temel yapılar
│   ├── GridSystemModule/        # Grid sistemi
│   ├── GameModule/              # Oyun akışı
│   ├── GameplayModule/          # Oyun mekanikleri
│   ├── CombatModule/            # Savaş yönetimi
│   ├── PlacementModule/         # Yerleştirme sistemi
│   ├── InventoryModule/         # Envanter
│   ├── UISystemModule/          # UI sistemi
│   └── DebugModule/             # Debug araçları
├── DATA/
│   ├── GridSettings/            # Grid ayarları
│   ├── LEVELS/                  # Seviye verileri
│   ├── UnitsSettings/           # Birim ayarları
│   └── PlacementSettings/       # Yerleştirme ayarları
├── Prefabs/                     # Oyun prefab'ları
├── Scenes/                      # Oyun sahneleri
└── Settings/                    # Proje ayarları
```

## 🧪 Test Case Kapsamı

Bu test case aşağıdaki özellikleri göstermektedir:

- ✅ Modüler mimari tasarımı
- ✅ SOLID prensiplerine uyum
- ✅ Service Locator pattern kullanımı
- ✅ Interface-based dependency injection
- ✅ State machine implementasyonu
- ✅ Grid tabanlı yerleştirme sistemi
- ✅ Combat sistemi ve strateji pattern
- ✅ Modular assembly yapısı
- ✅ Clean code prensipleri

## 📝 Lisans

Bu proje Unico Studio için geliştirilmiş bir test case'dir.

## 👤 Geliştirici

Batuhan Luleci

---

**Not**: Bu proje Unity 6000.2.6f2 sürümü ile geliştirilmiştir.
