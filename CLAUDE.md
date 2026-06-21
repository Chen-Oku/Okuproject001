<!-- Memoria de proyecto para Claude Code. Mantener < 200 líneas: cada línea consume contexto en CADA sesión. Lo que solo importe en parte del código, mover a .claude/rules/. -->

# Abyss Fall — Contexto de proyecto

Arcade vertical infinito en Unity. Una esfera cae a través de una hélice de
anillos de plataformas; el jugador rota el anillo actual (drag horizontal) para
alinear huecos seguros. La puntuación crece con velocidad y riesgo (combo).

- **Unity** `6000.3.6f1`, render **URP** (perfiles separados Mobile/PC).
- **Plataformas objetivo:** Android (principal) y PC.
- **Input:** Unity Input System (`InputSystem_Actions.inputactions`).
- **Persistencia:** `PlayerPrefs` (scores, settings). Sin backend remoto.

## Cómo construir / validar

- No hay test harness por CLI por defecto. La validación se hace en el Editor
  (escenas `Assets/Scenes/MainMenu.unity` y `Game.unity`).
- Si se agregan tests (EditMode/PlayMode), van en `Assets/Tests/` y se documenta
  aquí el comando de batchmode. Mientras no existan, NO inventar comandos de test.
- Para cualquier cambio de gameplay, dejar claro qué hay que verificar a mano en
  el Editor (escena, pasos, resultado esperado).

## Estructura

```
Assets/
├── Scripts/Core/      GameManager, ZoneManager, ScoreManager, AudioManager,
│                      LeaderboardManager, PauseInputHandler, AnalyticsManager,
│                      SpeedFeedbackManager
├── Scripts/Gameplay/  HelixGenerator (central), HelixRotator, RingSegment,
│                      BallController, ScoreTrigger, PowerupPickup,
│                      RingAutoRotator, CameraFollow, IntroPlatform, NearMissSystem
├── Scripts/UI/        UIManager, MainMenuManager, PauseMenuManager
├── Scenes/            MainMenu.unity, Game.unity
├── Materials/, Mesh/, Textures/, Art/ArtBrief.md
└── Settings/          perfiles URP Mobile/PC
```

## Mapa de sistemas (para no re-descubrir el código)

- **GameManager** — máquina de estados del juego.
- **HelixGenerator** — generación procedural infinita (~298 líneas, pieza
  central). Genera ~14 anillos por delante, libera 3 por detrás, distribución
  anti-bias (Fisher–Yates), soporte para forzar tipos/huecos/powerups (tutorial).
- **HelixRotator** — rotación táctil del anillo actual.
- **RingSegment** — 6 tipos: Safe, Dangerous, Crumbling, Bouncy, Checkpoint
  (escudo), FireLocked.
- **BallController** — estado y físicas de la esfera: clamp de velocidad de
  caída, escudo (Checkpoint), powerups activos (color/VFX), `Die()` →
  `GameManager.TriggerGameOver()`.
- **ScoreTrigger** — collider por anillo; al pasarlo decide si fue "saltado"
  (sin tocar segmento) y llama a `ScoreManager.AddScoreWithCombo`.
- **PowerupPickup / RingAutoRotator** — spawneados por HelixGenerator: pickup
  giratorio que activa powerup en `BallController`; rotación automática extra
  de un anillo (efecto de dificultad).
- **ZoneManager** — 4 zonas cíclicas (Normal → Lava → Ice → Void) cada 15
  anillos; Ice añade inercia de rotación, Void aumenta velocidad de caída.
- **ScoreManager** — score y combo (combo sube al "saltar" un anillo sin tocar
  segmento, hasta 10x; mejor score en PlayerPrefs).
- **Powerups** (5 s): Fire (atraviesa bloqueos), Ghost (atraviesa peligro),
  Slow (reduce velocidad de caída).
- **NearMissSystem** — sensor esférico en la esfera; detecta "casi-impacto" en
  segmentos riesgosos (Dangerous/FireLocked) sin powerup que los neutralice y
  alimenta el combo existente de `ScoreManager` (sin contador propio).
- **SpeedFeedbackManager** — traduce combo y Surge en FOV y camera shake
  (modelo de trauma de Eiserloh); solo escucha eventos, no conoce
  `HelixGenerator` ni la física. Soporta intensidad reducida (low-end) vía
  PlayerPrefs.
- **AnalyticsManager** — observador puro de eventos de Game/Score/ZoneManager;
  vuelca log + CSV por run en `Application.persistentDataPath`. Ningún
  manager lo referencia.

## Convenciones de arquitectura (IMPORTANTES)

- **Comunicación por eventos**: usar `C# events` / `UnityEvents`. Los sistemas
  nuevos publican/escuchan eventos.
- **Evitar**: referencias duras entre sistemas, abuso de singletons, dependencias
  fuertemente acopladas.
- Managers nuevos siguen el patrón de los existentes en `Scripts/Core`.
- Persistencia siempre vía `PlayerPrefs` mientras no haya backend.

## Restricción de producción (CLAVE)

- **No se crean modelos 3D nuevos.** Toda mejora visual debe lograrse con
  materiales, shaders, VFX (partículas/trails), post-processing, color grading,
  cámara y datos. Cosméticos = skins/trails/paletas, nunca geometría nueva.
- **Presupuesto de rendimiento Android gama baja**: partículas + post-processing
  + cambios de FOV pueden hundir el framerate. Todo efecto nuevo debe respetar el
  perfil URP Mobile y, idealmente, escalar por calidad del dispositivo.

## Notas de diseño / gotchas conocidos

- **Combo unificado**: el near-miss (`NearMissSystem`) y el "saltar sin tocar
  segmento" (`ScoreTrigger`) ya alimentan el mismo contador en `ScoreManager`
  (`AddScoreWithCombo`) — no crear un segundo combo paralelo.
- **Surge sin implementar**: `SpeedFeedbackManager` y `AnalyticsManager` ya
  exponen los hooks (`NotifySurgeStart/End`, `LogSurgeActivated`) para la
  mecánica de Surge del plan V1.5, pero ningún sistema los dispara todavía.
- **Velocidad casi constante**: como la caída es a velocidad ~constante, atar FOV
  y speed-lines a `CurrentSpeed/MaxSpeed` casi no se nota. Ya están atados al
  **tier de combo y al estado de Surge** en `SpeedFeedbackManager`.

## Cómo quiero que trabajes (Claude Code)

- Antes de refactors grandes o de tocar `HelixGenerator`, propón un plan corto y
  espera confirmación.
- Cambios pequeños y revisables; un sistema por commit.
- No leer `Library/`, `Temp/`, `obj/`, `Logs/`, `Build/` ni archivos `*.meta`
  salvo que se pida explícitamente (son ruido y gastan contexto).
- Responder y comentar el código en español; nombres de código en inglés.
