# Brief de Arte — Okuproject001

---

## Descripción del juego

Un juego de arcade vertical en el que una esfera cae en espiral a través de una torre infinita de plataformas circulares (hélice). El jugador rota los anillos para encontrar huecos o plataformas seguras mientras la esfera cae sin parar. Cada plataforma tiene un tipo diferente con un comportamiento y visual distintivo, y cada cierta cantidad de anillos el jugador entra a una nueva **zona temática** que cambia completamente la atmósfera.

**Tono general:** arcade moderno, estética neón/glow sobre fondos oscuros. Algo entre *Neon Abyss* y *Alto's Odyssey* — limpio, legible, pero con carácter visual fuerte.

---

## La Esfera (protagonista)

Objeto siempre en movimiento, centro de atención del jugador. Debe ser **inmediatamente reconocible y satisfactoria** de ver caer.

| Estado | Visual |
|---|---|
| Normal | Esfera translúcida con núcleo brillante, tipo cristal o energía |
| Shield activo | Pulso cian alrededor de la esfera — como un aura eléctrica |
| Fire powerup | Esfera envuelta en llamas naranjas/doradas |
| Ghost powerup | Esfera semitransparente, violeta/blanca, efecto etéreo |
| Slow powerup | Esfera rodeada de partículas azul hielo, movimiento más fluido |

---

## Plataformas de la Hélice

Actualmente son cubos primitivos. El objetivo es reemplazarlos por segmentos de plataforma con personalidad propia, adaptados a cada zona. La silueta base es un arco/cuña que encaja en el anillo circular. Todos deben ser **legibles de un vistazo** porque el jugador tiene fracciones de segundo para reaccionar.

| Tipo | Color base | Visual / Efecto |
|---|---|---|
| **Safe** | Verde esmeralda / azul eléctrico | Limpia, con borde luminoso sutil |
| **Dangerous** | Rojo magenta / negro | Bordes afilados, textura de advertencia, partículas de peligro |
| **Crumbling** | Ámbar / marrón agrietado | Grietas visibles, se fragmenta al contacto (VFX de escombros) |
| **Bouncy** | Amarillo neón / verde lima | Superficie tipo gelatina o resorte, compresión visual al impacto |
| **Checkpoint** | Cian / dorado | Brilla intensamente, efecto de escudo al recogerse |

---

## Zonas Temáticas

La hélice pasa por 4 zonas en ciclo. Cada zona cambia: color de fondo, estética de plataformas, partículas ambientales y sensación general.

---

### Zona 1 — NORMAL

**Mood:** tecnológico, espacio profundo, comienzo tranquilo.

- Fondo: negro-azul muy oscuro con estrellas lejanas o nebulosa suave
- Plataformas: geométricas y limpias, material metálico con bordes de neón azul/blanco
- Partículas: ninguna o polvo de luz muy sutil
- Referencia de mood: *interiores de nave espacial, minimal sci-fi*

---

### Zona 2 — LAVA

**Mood:** peligro, urgencia, calor extremo. Más segmentos peligrosos y rotación más rápida.

- Fondo: rojo oscuro con venas de magma, humo ascendente
- Plataformas: piedra volcánica negra con grietas de lava incandescente en los bordes
- Segmento Dangerous: espinas de roca ardiente, burbujas de magma
- Partículas: chispas y brasas flotando hacia arriba
- Referencia de mood: *caverna volcánica, inframundo*

---

### Zona 3 — HIELO

**Mood:** frío, cristalino, engañosamente bello pero resbaladizo.

- Fondo: azul-cyan profundo, efecto de aurora boreal en el fondo
- Plataformas: hielo translúcido con reflejos internos, bordes escarcha
- Segmento Crumbling: hielo agrietado que se astilla en cristales
- Segmento Bouncy: bloque de hielo comprimido que rebota
- Partículas: copos de nieve, cristales de hielo flotantes
- Referencia de mood: *cueva de hielo, tundra ártica*

---

### Zona 4 — VOID (El Vacío)

**Mood:** lo desconocido, gravedad rota, hiperaceleración. Más huecos, ball más rápida.

- Fondo: negro absoluto con distorsión espacial, efecto de portal/singularidad al centro
- Plataformas: casi transparentes, con bordes violeta/blanco, como materia oscura solidificada
- Efecto especial: las plataformas vibran o distorsionan levemente el espacio a su alrededor
- Partículas: fragmentos de luz que flotan en múltiples direcciones, sin gravedad aparente
- Referencia de mood: *dimensión alternativa, liminal space cósmico*

---

## Powerups (esferas coleccionables entre anillos)

Son esferas flotantes más pequeñas que la bola principal, con animación de rotación suave y un halo de luz.

| Powerup | Color | Visual |
|---|---|---|
| **Fire** | Naranja / Rojo | Llama interior, aura de calor distorsionada |
| **Ghost** | Violeta / Blanco | Translúcido, pulsante, efecto de fantasma |
| **Slow** | Azul celeste | Núcleo congelado, partículas heladas orbitando |

---

## Sugerencias adicionales

Ideas que potencian el atractivo visual sin cambiar la mecánica:

1. **Estela de la bola** — un trail de luz detrás de la esfera al caer reforzaría la velocidad y sería muy satisfactorio visualmente.
2. **Hélice como estructura** — en vez de anillos sueltos, un eje central visible (tubo de energía, columna de luz) que conecte todos los anillos y refuerce la forma de torre.
3. **Transición de zona animada** — cuando cambia la zona, el fondo y el color de las plataformas hacen fade gradual. Ya existe en código, solo necesita assets.
4. **Partículas en destrucción** — cuando Crumbling se rompe o Fire destruye una plataforma, una pequeña explosión de fragmentos sería muy satisfactoria.
5. **Segmento Dangerous con animación** — pulso o parpadeo de advertencia cuando la ball se acerca, como señal de peligro activo.

---

## Formato de entrega sugerido para el artista

- **Concept sheet por zona** — una lámina con el anillo completo + los 5 tipos de segmento en esa zona
- **Character sheet de la esfera** — los 5 estados en una sola imagen
- **Powerup sheet** — los 3 powerups con sus variaciones de brillo/estado
- **Mockup de pantalla** — una captura de cómo se vería el juego en pantalla con la zona activa
