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
│                      LeaderboardManager, PauseInputHandler
├── Scripts/Gameplay/  HelixGenerator (central), HelixRotator, RingSegment,
│                      CameraFollow, IntroPlatform
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
- **ZoneManager** — 4 zonas cíclicas (Normal → Lava → Ice → Void) cada 15
  anillos; Ice añade inercia de rotación, Void aumenta velocidad de caída.
- **ScoreManager** — score y combo (combo sube al "saltar" un anillo sin tocar
  segmento, hasta 10x; mejor score en PlayerPrefs).
- **Powerups** (5 s): Fire (atraviesa bloqueos), Ghost (atraviesa peligro),
  Slow (reduce velocidad de caída).

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

- **Conflicto de combo**: el plan V1.5 introduce un combo nuevo (near-miss,
  x5/x10/x20/x50) que NO debe duplicar el combo ya existente en ScoreManager.
  Unificar: el near-miss alimenta el combo existente, un solo contador.
- **Velocidad casi constante**: como la caída es a velocidad ~constante, atar FOV
  y speed-lines a `CurrentSpeed/MaxSpeed` casi no se nota. Atarlos al **tier de
  combo y al estado de Surge**, que sí varían.

## Cómo quiero que trabajes (Claude Code)

- Antes de refactors grandes o de tocar `HelixGenerator`, propón un plan corto y
  espera confirmación.
- Cambios pequeños y revisables; un sistema por commit.
- No leer `Library/`, `Temp/`, `obj/`, `Logs/`, `Build/` ni archivos `*.meta`
  salvo que se pida explícitamente (son ruido y gastan contexto).
- Responder y comentar el código en español; nombres de código en inglés.
