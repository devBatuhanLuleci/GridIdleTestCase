<div align="center">

# 🛡️ Grid Defence: Senior Case Study
### Robust Modular Architecture • High Performance • Scalable Design

![Banner](https://github.com/user-attachments/assets/87cb2f6d-d356-40a1-a2ea-f84ed756ca0c)

[![Unity](https://img.shields.io/badge/Unity-6000.0.60f1-blue.svg?style=for-the-badge&logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/URP-Universal%20Render%20Pipeline-lightgrey.svg?style=for-the-badge)](https://unity.com/render-pipelines/universal-render-pipeline)
[![Architecture](https://img.shields.io/badge/Architecture-Modular%20ASMDEF-red.svg?style=for-the-badge)](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html)

**Grid Defence** is a high-fidelity case study showcasing a modular, scalable, and performance-optimized tower defense framework. Built with a "Service-First" philosophy, it demonstrates advanced Unity engineering patterns suitable for production-scale mobile and desktop titles.

[Key Architecture](#-architectural-highlights) • [Technical Deep Dive](#-technical-features) • [Design Patterns](#-implemented-design-patterns) • [Module Breakdown](#-system-modules)

---

## 📸 Media & Visuals
*Dynamic lighting, custom shaders, and physics-based animations work in tandem to provide a premium feel.*

| Core Gameplay Loop | Combat UI & FX |
| :---: | :---: |
| <video src="https://github.com/user-attachments/assets/396df188-a3a7-4c51-9026-1f688ba3c632" width="300" /> | <video src="https://github.com/user-attachments/assets/c08e880e-81fd-4b0f-9ea4-ecfe2cab5bbf" width="300" /> |
| **Grid Placement** | **Dynamic Inventory** |
| <video src="https://github.com/user-attachments/assets/325b799f-5653-4a4f-8b8b-d1dd6e458771" width="300" /> | <video src="https://github.com/user-attachments/assets/910483dd-7e50-406e-8b5f-1079e12d49e5" width="300" /> |

</div>

---

## 🏗️ Architectural Highlights

As a Senior Developer, my focus was on creating a **foundation** that allows teams to scale content without increasing technical debt.

### 🧩 True Modularity (Assembly Definitions)
The project is strictly partitioned using **Assembly Definition Files (AsmDef)**. 
- **Benefits**: Decoupled compilation (faster iteration), enforced dependency rules (no spaghetti code), and clear boundaries between `GridSystem`, `Combat`, and `UI`.

### 💉 Dependency Management & Service Placement
Utilizes a high-performance **Service Locator** pattern integrated with a **Service Discovery** mechanism.
- Systems register themselves via interfaces (e.g., `IGridPlacementSystem`, `IEnemySpawner`).
- Components depend on **Abstractions**, not implementations, facilitating easy mocking for unit testing.

### 🚌 Event-Driven Communication
Cross-module interactions are handled through a central **EventBus** and `Action`-based observers.
- **Example**: The `UI` module updates the health bar by listening to `IEnemy.OnHealthChanged`, never directly querying the internal state of the `EnemyItem2D`.

---

## 🚀 Technical Features

### ⚔️ Advanced Combat & FX Systems
- **Material Instancing**: Projectiles and Units use per-renderer material instances. This allows individual hit-flashes (`_FlashAmount`) and outlines without breaking batching for static objects or affecting "atlas-mates."
- **Bezier Trajectories**: Projectiles use mathematical Bezier curves for organic motion, calculated efficiently with custom utility libraries.
- **Hit Feedback**: Integrated shader-driven flash effects combined with `DOTween` pulses for high-impact visual feedback.

### 🧩 Intelligent Grid System
- **Spatial Validation**: Supports multi-tile objects with real-time overlap checking and boundary validation.
- **Scalable Placement**: Logic is abstracted behind `IPlaceable`, allowing anything (Towers, Traps, Obstacles) to be integrated into the grid without code changes.

### 📦 Reactive Inventory
- **Replenishment Logic**: Automated inventory slot management that handles replenishment cycles and visual state synchronization between the backend data and the UI frontend.

---

## 🛠️ Implemented Design Patterns

- **Strategy Pattern**: Swap combat behaviors (Forward, Circular, Area-of-Effect) at runtime without modifying the unit class.
- **State Pattern**: Managed game states (`Placing`, `WaveActive`, `Result`) using a centralized `StateManager`.
- **Factory Pattern**: Centralized spawning logic for Enemies and Projectiles to manage object initialization and pooling hooks.
- **Observer Pattern**: Extensive use of `System.Action` and custom `EventBus` for loosely coupled systems.

---

## 📦 System Modules

| Module | Responsibility |
| :--- | :--- |
| **GridSystemModule** | Core grid math, tile management, and placement validation logic. |
| **GameplayModule** | Entity logic, combat stats, enemy AI, and unit-specific behaviors. |
| **UISystemModule** | Base UI frameworks, responsive panels, and drag-and-drop handlers. |
| **GameModule** | Higher-level managers (LevelManager, StateManager, Bootstrap). |
| **CORE** | Shared utilities, Service Locator, and Event Bus definitions. |

---

## 📁 Project Structure

```text
Assets/BoardGameTestCase/
├── Scripts/             # Modularized C# Code (AsmDef restricted)
├── DATA/                # ScriptableObject-based configurations (Units, Levels, Waves)
├── Prefabs/             # Atomic prefabs with pre-configured components
├── Shaders/             # Custom URP Shaders (Outline, Flash, Shine effects)
└── Sprites/             # Art assets optimized for sprite atlasing
```

---

## 👤 Developer Notes
Developed by **Batuhan Luleci**, Senior Game Developer. This case study reflects a professional approach to Unity development, prioritizing **performance, maintenance, and scalability**—the three pillars of a successful long-term project.

---

<div align="center">
Built for professional review.
</div>
