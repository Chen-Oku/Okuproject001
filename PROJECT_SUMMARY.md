# Abyss Fall — Resumen del Proyecto

> Proyecto: `Okuproject001` · Producto: **Abyss Fall** · Versión: `0.1.4` · Unity `6000.3.6f1` (URP)

## 1. Concepto

Arcade vertical infinito: una esfera cae sin parar a través de una "hélice" de anillos de plataformas. El jugador desliza horizontalmente para **rotar el anillo actual** y alinear huecos seguros, evitando segmentos peligrosos mientras la puntuación crece con la velocidad y el riesgo asumido (sistema de combo).

## 2. Mecánicas principales

- **Caída + rotación táctil**: la bola cae a velocidad constante (con tope configurable); el jugador rota el anillo con drag horizontal ([HelixRotator.cs](Assets/Scripts/Gameplay/HelixRotator.cs)).
- **6 tipos de segmento** ([RingSegment.cs](Assets/Scripts/Gameplay/RingSegment.cs)): Safe, Dangerous, Crumbling, Bouncy, Checkpoint (escudo), FireLocked.
- **Generación procedural infinita** ([HelixGenerator.cs](Assets/Scripts/Gameplay/HelixGenerator.cs), 298 líneas — pieza central del juego): genera ~14 anillos por delante, libera 3 por detrás, con distribución anti-bias (Fisher–Yates) y soporte para forzar tipos/huecos/powerups (usado en el tutorial).
- **Powerups** (5s de duración): Fire (destruye/atraviesa bloqueos), Ghost (atraviesa peligro), Slow (reduce velocidad de caída).
- **Combo/Score**: combo sube si se "salta" un anillo sin tocar segmento alguno, hasta 10x; mejor puntuación persistida en `PlayerPrefs`.
- **4 zonas temáticas cíclicas** (Normal → Lava → Ice → Void), cada 15 anillos, cada una con su propia dificultad, física y estética ([ZoneManager.cs](Assets/Scripts/Core/ZoneManager.cs)). Ice añade inercia de rotación; Void aumenta velocidad de caída.
- **Tutorial guiado** en los primeros ~30 anillos, introduciendo cada powerup y mecánica de forma progresiva ([HelixGenerator.cs](Assets/Scripts/Gameplay/HelixGenerator.cs), [IntroPlatform.cs](Assets/Scripts/Gameplay/IntroPlatform.cs)).

## 3. Sistemas implementados

| Sistema | Archivo | Estado |
|---|---|---|
| Game state machine | [GameManager.cs](Assets/Scripts/Core/GameManager.cs) | ✅ |
| Audio (música/SFX, toggles) | [AudioManager.cs](Assets/Scripts/Core/AudioManager.cs) | ✅ |
| Score & combo | [ScoreManager.cs](Assets/Scripts/Core/ScoreManager.cs) | ✅ |
| Leaderboard (Top 10, local) | [LeaderboardManager.cs](Assets/Scripts/Core/LeaderboardManager.cs) | ✅ |
| Pausa (ESC / Start / two-finger-tap) | [PauseInputHandler.cs](Assets/Scripts/Core/PauseInputHandler.cs) | ✅ |
| HUD in-game, anuncios de zona | [UIManager.cs](Assets/Scripts/UI/UIManager.cs) | ✅ |
| Menú principal (Play/Scores/HowToPlay/Settings) | [MainMenuManager.cs](Assets/Scripts/UI/MainMenuManager.cs) | ✅ |
| Menú de pausa | [PauseMenuManager.cs](Assets/Scripts/UI/PauseMenuManager.cs) | ✅ |
| Cámara seguidora | [CameraFollow.cs](Assets/Scripts/Gameplay/CameraFollow.cs) | ✅ |

Input gestionado con **Unity Input System** (`InputSystem_Actions.inputactions`): touch/mouse drag, teclado, gamepad.

## 4. Tecnología

- **Unity 6000.3.6f1**, render pipeline **URP** (perfiles separados Mobile/PC).
- Paquetes relevantes: `com.unity.inputsystem` 1.18.0, `com.unity.render-pipelines.universal` 17.3.0, `com.unity.ugui` 2.0.0 (TextMeshPro), `com.unity.timeline`.
- Persistencia vía `PlayerPrefs` (scores, settings de audio) — sin backend remoto.
- Plataformas objetivo: PC y Android (build profiles configurados).

## 5. Estructura de carpetas

```
Assets/
├── Scripts/{Core, Gameplay, UI}/   — ~1730 líneas C# en 17 scripts
├── Scenes/                         — MainMenu.unity, Game.unity
├── Materials/{Helix, PowerUps, Sphere}/
├── Mesh/                           — modelo de plataforma rocosa (zona Lava)
├── Textures/, Art/ArtBrief.md      — referencia visual del proyecto
└── Settings/                       — perfiles URP Mobile/PC
```

## 6. Estado actual

**Funcional como prototipo jugable completo**: mecánica core, 4 zonas, powerups, combo, tutorial, menús, pausa, leaderboard y audio básico ya implementados.

**Pendiente / en progreso** (según [Assets/Art/ArtBrief.md](Assets/Art/ArtBrief.md)):
- Arte 3D final por zona (hoy: principalmente primitivas + un modelo de roca para Lava).
- VFX ambientales (chispas, nieve, fragmentos void) y trail de la bola.
- SFX de juego (colisión, powerup, muerte) — de momento solo hay click de botones.
- Pulido visual del escudo/checkpoint y eje central de la hélice.

## 7. Progresión reciente (git log)

```
13b9f19  plataforma inicial - logo
57efbc5  Pause Menu and leaderboard
223777c  Tutorial zone, some particles
36ccecc  Background menu - first build
f302f24  MainMenu
32a77fc  powerups
dc686cd  Segments, rotations, PP, zones
7917f91  Initial commit
```

Desarrollo concentrado en ~11 días, priorizando mecánica y UI sobre el arte final — consistente con el roadmap pendiente del ArtBrief.
