<div align="center">

# 🛡️ Board Defence - Grid Based Strategy Game

![Banner](https://github.com/user-attachments/assets/your-banner-id-here) 
*(Not: Az önce senin için ürettiğim banner görselini buraya ekleyebilirsin!)*

[![Unity](https://img.shields.io/badge/Unity-6000.2.6f2-blue.svg?style=for-the-badge&logo=unity)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Mobile%20%7C%20Web-orange.svg?style=for-the-badge)](https://unity.com/)

**Board Defence**, Unico Studio için geliştirilmiş, yüksek performanslı ve modüler mimariye sahip grid tabanlı bir savunma oyunudur.

[Özellikler](#-özellikler) • [Mimari](#-mimari) • [Modüller](#-modüller) • [Kurulum](#-kurulum) • [Teknik Detaylar](#-teknik-detaylar)

</div>

---

## 📸 Media & Demo

Aşağıdaki bölümlere oyun içinden aldığın video ve GIF'leri ekleyebilirsin.

| Gameplay GIF | Combat System | Placement Mechanics |
| :---: | :---: | :---: |
| ![Gameplay](https://via.placeholder.com/300x500?text=Gameplay+GIF) | ![Combat](https://via.placeholder.com/300x500?text=Combat+GIF) | ![Placement](https://via.placeholder.com/300x500?text=Placement+GIF) |

---

## 🎮 Özellikler

- **🧩 Gelişmiş Grid Sistemi**: Dinamik boyutlandırma ve tile tabanlı yerleştirme validasyonu.
- **⚔️ Akıllı Savaş Mekaniği**: Strateji örüntüleri (Strategy Pattern) ile yönetilen farklı saldırı tipleri.
- **🌊 Dalga Yönetimi**: Esnek düşman spawn ve dalga kontrol mekanizması.
- **📦 Dinamik Envanter**: Seviye bazlı çalışan ve UI ile entegre envanter sistemi.
- **🎨 Üstün Görsel Geri Bildirim**: Sprite Outline Shader ve akıcı animasyonlar ile zenginleştirilmiş kullanıcı deneyimi.

---

## 🏗️ Mimari Tasarım

Proje, **SOLID** prensiplerine sadık kalınarak, tamamen modüler ve test edilebilir bir yapıda inşa edilmiştir.

### 🌟 Temel Prensipler
- **Service Locator & DI**: Bağımlılıklar arayüzler üzerinden yönetilir, runtime servis erişimi merkezidir.
- **State Machine**: Oyun akışı (Placing, Fight, Win/Lose) state pattern ile kontrol edilir.
- **Modular Assembly (AsmDef)**: Her modül kendi assembly'sine sahiptir, compilation süreleri minimize edilmiştir.
- **Clean Code**: Kendini açıklayan metod isimleri ve yapısal bütünlük (No magic strings, no reflection).

---

## 📦 Modül Yapısı

### 🔵 Core Modules
- **GridSystem**: Grid mantığı, tile validasyonu ve yerleştirme kontrolleri.
- **GameModule**: Oyunun ana kalbi; StateManager ve FlowController.
- **CombatSystem**: Düşman takibi, hasar mekaniği ve saldırı stratejileri.

### 🟢 Gameplay Modules
- **Placement**: Birim yerleştirme akışı ve görsel feedback.
- **Inventory**: Oyuncunun sahip olduğu birimlerin yönetimi.
- **Strategies**: `ForwardAttackChallenge` ve `AllDirectionsAttack` gibi genişletilebilir stratejiler.

### 🔴 UI & Debug
- **UISystem**: Modern ve responsive arayüz bileşenleri.
- **DebugModule**: Geliştirme sürecini hızlandıran araçlar ve loglama.

---

## 🚀 Kurulum

1. **Unity Versiyonu**: Proje `6000.2.6f2` sürümü ile uyumludur.
2. **Klonlama**:
   ```bash
   git clone https://github.com/batuhanluleci/GridIdleTestCase.git
   ```
3. **Sahne**: `Assets/BoardGameTestCase/Scenes/Gameplayscene.unity` sahnesini açın.
4. **Başlat**: Play butonuna basarak savunmaya başlayın!

---

## 🛠️ Teknik Detaylar

### Yazılım Stack'i
- **Engine**: Unity 2023+ (Unity 6 ready)
- **Rendering**: Universal Render Pipeline (URP)
- **Logic**: C# (Async/Await, Interfaces, Generics)
- **Tweening**: DOTween (Yüksek performanslı animasyonlar)
- **Pattern**: Service Locator, Strategy, State, Observer

---

## 📁 Dosya Organizasyonu

```text
Assets/BoardGameTestCase/
├── Scripts/             # Tüm operasyonel kodlar (AsmDef bazlı)
├── DATA/                # ScriptableObject verileri ve ayarlar
├── Prefabs/             # Birimler, Düşmanlar ve UI elementleri
├── Shaders/             # Özel URP shader'lar
└── Sprites/             # Görsel varlıklar
```

---

## 👤 Geliştirici

**Batuhan Luleci**  
*Game Developer & Software Architect*

---

<div align="center">
Made with ❤️ for Unico Studio Test Case
</div>
