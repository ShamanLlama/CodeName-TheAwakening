# Registro de versiones para playtest

Cada build que pueda llegar a un playtest, con o sin GTC Playtest Lab, tiene una entrada aquí. Sirve para saber qué reglas e interfaz vio cada jugador y no mezclar datos de builds distintas.

## Regla

Todo cambio al juego se refleja en el sistema de playtest en el mismo cambio:

1. **Versión del juego** (`config/version` en `godot/project.godot`): sube en cada build distribuida. Cambio de reglas o balance → versión menor (0.3.0); solo UI, texto o correcciones → parche (0.2.1).
2. **Ruleset** (hoy `alignment-1`): cambia con cualquier modificación de reglas o valores de balance. Se envía en `run_started`/`capture_started` y se guarda en los saves.
3. **Adaptador** (hoy `awakening-1`): cambia si cambian acciones, regiones, doctrinas, eventos o sus campos. Requiere actualizar el contrato en `GTC-page/gtc-web/contracts/playtests/`.
4. **GTC:** una build nueva exige build y ronda nuevas en el Lab. No se comparan rondas con reglas o política de reloj distintas sin decirlo.
5. **Entrada en este registro**, en la misma rama que el cambio: fecha, versión, commit, ruleset, adaptador, cambios, motivo y medición que lo respalda.

## Versiones

| Versión | Fecha | Commit | Ruleset | Adaptador | Ronda GTC | Estado |
| --- | --- | --- | --- | --- | --- | --- |
| 0.3.3 | 2026-11-03 | `0e54b73` | `alignment-2` | `awakening-1` | «0.3.3 playtest por invitación · decisiones económicas», abierta del 2026-11-03 al 2026-11-17 | En itch.io (Restricted con contraseña) |
| 0.3.2 | 2026-11-02 | `cf9a755` | `alignment-2` | `awakening-1` | «0.3.2 playtest por invitación · decisiones económicas», cerrada el 2026-11-03 con 1 partida | Reemplazada por 0.3.3 |
| 0.3.1 | 2026-11-02 | `2256c6c` | `alignment-2` | `awakening-1` | «0.3.1 playtest por invitación · decisiones económicas», cerrada el 2026-11-03 con 2 partidas | Reemplazada por 0.3.2 |
| 0.3.0 | 2026-11-02 | `85f5905`; exportada desde `dbfb07f` | `alignment-2` | `awakening-1` | «0.3.0 playtest por invitación · decisiones económicas», cerrada el mismo día sin partidas | Reemplazada por 0.3.1 |
| 0.2.0 | 2026-10-30 / 2026-10-31 | `ae38ec0`; adaptador E4–E6 en `6aa2419` | `alignment-1` | `awakening-1` | QA técnica E7 y «E7 piloto interno de gameplay» (abierta hasta 2026-11-09) | Reemplazada en itch.io por 0.3.0 el 2026-11-02 |

### 0.3.4 — escala original con panel táctil

- **Motivo:** en el teléfono físico no convencieron ni la escala de 0.3.2 (mapa muy pequeño) ni el zoom de 0.3.3 (2026-11-03). Se vuelve a la interfaz de 0.3.1 y se conserva solo la lectura de los poderes.
- **Interfaz:** se quitan la escala adaptable, el modo compacto, los textos agrandados, las barras más bajas, el aviso para girar el teléfono y el zoom del mapa. Se mantienen del panel de región: acciones antes de las estadísticas, explicación bajo la acción con botón para ejecutarla en táctil, desplazamiento arrastrando y una barra de desplazamiento visible en táctil. Escritorio igual que en 0.3.1 salvo el orden del panel.
- **Reglas y playtest:** sin cambios (`alignment-2`, `awakening-1`).
- **Tests:** 39 tests Godot, incluido el flujo táctil de acciones. Capturas a 844×390 CSS con densidad 2 y a 1600×900.

### 0.3.3 — mapa más grande en teléfonos

- **Motivo:** en la prueba en teléfono físico de 0.3.2 (2026-11-03) el texto ya se leía, pero el mapa quedaba muy pequeño, sobre todo con el panel de región abierto. Se buscó un equilibrio sin volver a achicar el texto.
- **Interfaz:** en pantallas cortas la vista general recorta las latitudes polares (50° S–72° N) y la barra superior baja de 50 a 46. Al abrir una región, el mapa se amplía 1,7 veces sobre ella en la parte libre en lugar de encogerse al lado del panel (antes quedaba a unos dos tercios del ancho de la vista general). Escritorio sin cambios. Detalle en [Interfaz y accesibilidad](../gdd/07_UI_UX/Interfaz_y_accesibilidad.md).
- **Reglas y playtest:** sin cambios (`alignment-2`, `awakening-1`).
- **Tests:** 41 tests Godot, incluido uno que comprueba que cada región ampliada queda fuera del panel y dentro de la vista recortada. Capturas a 844×340 CSS con densidad 2 (teléfono con barras del navegador) y 1600×900.
- **GTC (2026-11-03 UTC):** build «The Awakening 0.3.3 · alignment-2» (`0e54b73`) y ronda nueva, abierta en pausa, probada en el host y habilitada; cierra el 2026-11-17. La ronda 0.3.2 se cerró con 1 partida recibida, que se conserva aparte.
- **Publicación:** upload `20247015` en itch.io como único archivo jugable; texto de página igual al de 0.3.2 con la versión actualizada. La build alojada coincide con el export (`index.pck` idéntico), arranca sin errores y no envía nada antes de responder. Sin probar todavía en teléfono físico.

### 0.3.2 — legible en teléfonos y pantallas cortas

- **Motivo:** en el teléfono la interfaz era ilegible y las descripciones de los poderes quedaban fuera de la pantalla sin forma de leerlas (reporte del 2026-11-02).
- **Interfaz:** escala adaptable según la pantalla (mínimo 0,75 px CSS por unidad), modo compacto con textos más grandes, barras más bajas, inicio y diálogos con desplazamiento, aviso para girar el teléfono. En el panel de región las acciones van primero; en táctil, un toque explica la acción bajo ella con un botón para ejecutarla, y el panel se desplaza arrastrando. Detalle en [Interfaz y accesibilidad](../gdd/07_UI_UX/Interfaz_y_accesibilidad.md).
- **Reglas y playtest:** sin cambios (`alignment-2`, `awakening-1`). Los eventos no registran qué explicaciones se abren.
- **Tests:** 40 tests Godot, incluidos escala de texto compacta y flujo táctil de acciones. Capturas a 844×390 CSS con densidad 2 y a 1600×900; sin teléfono físico todavía.
- **GTC (2026-11-03 UTC):** build «The Awakening 0.3.2 · alignment-2» (`cf9a755`) y ronda nueva con perfil «computadora o teléfono en horizontal», abierta en pausa, probada en el host y habilitada; cierra el 2026-11-17. La ronda 0.3.1 se cerró con 2 partidas recibidas, que se conservan aparte.
- **Publicación:** upload `20246652` en itch.io como único archivo jugable; la página ahora recomienda computadora o teléfono en horizontal y explica el toque en los poderes ([itch-page.md](itch-page.md)). La build alojada coincide con el export (`index.pck` idéntico), arranca sin errores y no envía nada antes de responder. Sin probar todavía en teléfono físico.

### 0.3.1 — participación preguntada y recordada

- **Motivo:** aumentar la participación en el playtest sin casillas premarcadas, que no constituyen consentimiento.
- **Interfaz:** al pulsar Begin sin respuesta para la ronda en ese dispositivo, una hoja «Join the playtest?» muestra el aviso completo y dos botones del mismo tamaño: «Join the playtest & play» y «Play without sharing». Escape la cierra sin empezar.
- **Playtest:** la respuesta se guarda por ronda en los metadatos del guardado. Un sí recordado se confirma de nuevo al abrir el juego (solo con guardado escribible); un no, o Detener en el menú, se recuerda como no y no crea IDs. Una ronda nueva vuelve a preguntar. Antes se renovaba en cada apertura. Reglas y adaptador sin cambios.
- **GTC:** build «The Awakening 0.3.1 · alignment-2» (`2256c6c`) y ronda nueva, abierta en pausa, probada en el host y habilitada; cierra el 2026-11-16. La ronda 0.3.0 se cerró antes de tiempo sin partidas recibidas. Notas de consentimiento actualizadas en el contrato (`cdaecbc`).
- **Publicación:** upload `20241172` en itch.io como único archivo jugable; texto de página actualizado en [itch-page.md](itch-page.md). La build alojada coincide con el export, arranca sin errores y no envía nada antes de responder.
- **Tests:** 38 tests Godot, incluidos respuesta recordada por ronda y dispositivo, «no» sin IDs, Detener recordado como no y ventana de solo lectura sin pregunta. La prueba web E4 se ajustó a la nueva política pero no se volvió a ejecutar.

### 0.3.0 — `alignment-2`: decisiones económicas

- **Motivo:** con `alignment-1` la Influence sobraba tras financiar los tres sitios y defender casi nunca obligaba a elegir. Diseño y medición completos en [Experiencia y loops](../gdd/02_Game_Design/Experiencia_y_loops.md#cambios-de-alignment-2--2026-11-02).
- **Reglas:**
  - Mantenimiento de 1 Influence al mes por sitio financiado; sin pago, el sitio queda inactivo (no avanza ni cuenta para el rito).
  - `conceal` quita 3 de awareness y cuesta 20 + la mitad de la awareness; se puede usar en el mínimo de exposición si hay awareness.
  - Desde el mes 200, +0,15 de presión de amenazas en regiones con sitio; aviso en el mes 176 y en la previsión ritual.
- **Interfaz:** ingreso neto con coste de sitios, etiqueta de sitio inactivo, textos de guía, tooltip de Influence y descripción de acciones.
- **Guardados:** una partida `alignment-1` sin terminar no continúa; se conservan ajustes, tutorial y registros de playtest, y el jugador ve un aviso. En nativo se guarda una copia `.retired.json`.
- **Playtest:** adaptador sin cambios (`awakening-1`); `run_started`/`capture_started` envían `alignment-2`. GTC acepta `alignment-2` en el esquema y en la build (`0005_playtestbuild_ruleset_alignment_2`). Requiere build y ronda nuevas en el Lab.
- **Tests:** las trazas de `godot/tests/golden/` ya no comparan con el motor TypeScript; se regeneran con `dev/regenerate_goldens.gd` y fijan el comportamiento de `alignment-2`. 36 tests Godot y 102 tests del Lab de GTC (SQLite, 8 omitidos que requieren PostgreSQL).
- **GTC (2026-11-02):** desplegado `d5cfc40` en Railway (web y mantenimiento, migración `0005`). El receptor rechaza con `422 unsupported_version` un inicio de partida cuyo ruleset no coincide con el de su build. Build «The Awakening 0.3.0 · alignment-2» y su ronda registradas en el Lab. Los IDs están en `GTC-page/gtc-web/output/playtests-alignment-2/round-runtime.json`, no en este repositorio.
- **Artefacto:** `the-awakening-0.3.0-pilot.zip` (SHA-256 `848b4d4e…1e66`), exportado desde un checkout limpio de `dbfb07f` con la configuración de esa ronda; arranca en Chromium local sin errores y sin enviar nada antes del consentimiento.
- **Publicación (2026-11-02):** subido a itch.io como upload `20236476`, único archivo jugable en el navegador. La página pasó de Draft a Restricted con contraseña para compartirla con personas externas a Lucid Labworks y GTC; texto y mensaje de invitación en [itch-page.md](itch-page.md). La build alojada en `html-classic.itch.zone` coincide con el export (`index.pck` idéntico), arranca sin errores y no envía nada antes del consentimiento. La ronda, con perfil «personas invitadas por enlace», se abrió en pausa y se habilitó tras esa prueba; cierra el 2026-11-16. Sin probar todavía en teléfono físico con esta build.
- **Medición (2026-11-02, bot, Normal, 24 seeds × 3 estrategias):** victorias 58/72 → 49/72; Influence final mediana late 267 → 116 y hub 136 → 69; meses con ≥100 sin uso late 54,5 → 29,5 y hub 30 → 3. Easy y Hard no se midieron.

### 0.2.0 — base `alignment-1`

- **Reglas:** port de las reglas del prototipo TypeScript a Godot 4.7, verificado por trazas de paridad (`godot/tests/test_golden.gd`). Calendario de 200 meses de preparación, 24 de alineación y 12 de rito final.
- **Interfaz:** archivo del culto 1908–1926 (`ae38ec0`).
- **Playtest:** adaptador E4 de GTC Playtest Lab con consentimiento, cola durable y reloj por sesión ([guía](../playtest-lab.md)). La configuración del repositorio mantiene la recolección apagada.
- **Medición de referencia (2026-11-02, bot scripteado, no humanos):** 36 partidas Normal con `dev/playthrough.gd` (early/late/hub × 12 seeds). Tres sitios financiados hacia el mes 112–137 y listos hacia el 139–167; después quedan 50–60 meses con ≥100 Influence sin uso en las estrategias late/hub y 145–869 Influence al final. Motiva la revisión de decisiones económicas. Con la política adaptada a 0.3.0 (reserva y ocultamiento selectivo) la misma base da 58/72 victorias sobre 24 seeds.
