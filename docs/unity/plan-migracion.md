# Plan de migración a Unity

> Estado: **borrador para revisión**, sin ejecución · Fecha: 2026-11-17 · Base: `main` en `7d67097` (0.4.0, `alignment-3`, adaptador `awakening-2`) · GTC `c7bb677`

## 1. Resumen

Portar The Awakening 0.4.0 de Godot 4.7 (GDScript) a **Unity 6000.3.25f1** (C#) sin cambiar reglas, con el mismo contrato de playtest y el mismo formato de guardado. La build Godot sigue publicada en itch.io hasta que la build Unity la iguale y pase sus pruebas; recién entonces se reemplaza.

Lo que hace viable un port fiel:

- Las reglas (`godot/core/`, ~60 KB) son funciones puras sobre un estado serializable, con **diez trazas golden** que fijan estado, vistas previas, resultados, valores derivados, texto del log y guardado en cada paso. Son el oráculo del port.
- El contrato GTC es HTTP + JSON y no depende del motor. **El servidor no necesita cambios** para aceptar la build Unity si los lotes son iguales.
- La máquina ya tiene Unity 6000.3.25f1 con los módulos WebGL y Linux, licencia activa y un proyecto (Nut) que compila WebGL en batchmode.

Lo que cuesta de verdad es la interfaz: todo está dibujado en código (mapa Natural Earth, documentos, sellos, medidores, shaders de papel y lámpara, sonido sintetizado), con escala de interfaz, modo compacto, táctil y un chequeo de layout automático.

## 2. Qué hay hoy (inventario)

### Reglas (`godot/core/`) — se portan 1:1

| Archivo | Contenido | Notas para C# |
| --- | --- | --- |
| `balance.gd` | ~60 constantes, `RULESET = alignment-3` | `static class Balance` con `const`/`static readonly` |
| `game_data.gd` | 8 regiones (población, escepticismo, resistencia, misticismo, adyacencia, tags), 14 acciones, 3 doctrinas, 3 dificultades, 3 amenazas, 4 oportunidades, nombres | Orden de `REGION_IDS` explícito: las sumas en punto flotante dependen de él |
| `game_engine.gd` | Estado inicial, colocación, vista previa y ejecución de acciones, costos (obstruir, silenciar, awareness, ocultar, sitios), mantenimiento, dispersión, doctrina, rito, simulación mensual, victoria/derrota | Comandos inmutables: devuelven copia profunda o el estado original |
| `derived.gd` | Seguidores, reclutamiento, ingresos, apoyo de sitios, arrastre de awareness, calendario, log (máx. 100) | |
| `director.gd` | Amenazas cada 4 meses, oportunidades cada 12, resolución, caza de sitios desde el mes 200 | Consume el RNG en el mismo orden |
| `threats.gd`, `forecast.gd` | Consecuencias y textos de amenazas; calendario ritual | Texto exacto: lo comparan las golden |
| `rng.gd` | xorshift32 + FNV-1a sobre UTF-16 | `uint` en C#; trivial |
| `js.gd` | `Math.round`, `toFixed`, `Number.toString` | **Riesgo**: ver §6 |
| `save_data.gd` | Validación y guardado `save-v2`, rulesets retirados, ajustes opcionales | Mismo JSON, mismas claves |

### Playtest (`godot/core/playtest_*`, `godot/core/playtest_browser.js`)

- `PlaytestCapture` (~20 KB): consentimiento por ronda, IDs UUIDv4, secuencia, reloj en cuatro particiones, outbox durable, lotes de ≤32 eventos y ≤64 KiB, división ante 413, recibos, TTL de 7 días, límites 5.000 eventos / 8 MiB, comentarios, validación del almacén. Lógica pura: se porta y se prueba fuera de Unity.
- `PlaytestClient`: transporte; solo envía lo que el almacenamiento confirmó como durable; backoff con jitter y `Retry-After`; un envío cada 5 s.
- `PlaytestPlatform` + `playtest_browser.js`: IndexedDB (`lucid-awakening-save-v1`, store `snapshots`) con el sobre completo en una transacción, Web Locks para un solo escritor, `fetch` sin credenciales ni redirecciones con lectura acotada a 16 KB, foco y visibilidad con `performance.now()`. En nativo: archivo con reemplazo atómico y directorio de lock.
- Puntos de emisión en `main.gd`: `run_started`/`capture_started`, `session_started`, `starting_region_selected`, `action_performed` (gasto por diferencia de contadores del motor), `doctrine_selected`, `awakening_started`, `phase_changed`, `checkpoint_reached` (`tutorial_completed`, `first_site_ready`), `time_checkpoint` (periódico 20 s, foco, visibilidad, menú), `capture_gap`, `run_finished`, `run_abandoned`.
- `data/playtest-config.json`: `enabled`, `endpoint`, `collection_id`, `build_id`, `round_id`; vacío en el repo, lo completa `export-build.py`.

### Interfaz y presentación (`godot/ui/`, `godot/map/`, `main.gd`, ~190 KB)

- **Pantallas:** inicio (dificultad, seed, tamaño de interfaz, hoja «Join the playtest?»), juego (barra superior con lecturas, barra inferior con velocidad/rito/doctrina, recortes de amenazas, pronóstico ritual plegable, notas de una frase, toasts, «Whole world»), expediente de región (pestañas, filas de acción con precio, awareness en rojo, motivo de bloqueo, tres sellos por amenaza, tarjetas, leyenda), diálogos (menú, doctrina, crónica, guía, alineación, final ganado/perdido con consejos, panel GTC con comentarios y export, atajos).
- **Mapa:** proyección Natural Earth, 177 países en 8 regiones (`data/earth_regions.json`, 128 KB), anillos y contorno por región, rutas de adyacencia, etiquetas con halo que se esquivan, sitios (tachados si están estancados), omens, amenazas con cuenta atrás, R'lyeh, rosa de los vientos, meridianos, zoom con rueda/pellizco, arrastre, selección por punto.
- **Estilo:** archivo de culto 1908–1926 sobre escritorio con lámpara; calendario real (mes 0 = enero 1908). Tres familias de fuentes OFL (IM Fell English, Old Standard TT, Courier Prime; 7 archivos). Shaders `paper` (grano, fibras, manchas) y `lamp`. Paleta y tema en código.
- **Sonido:** 7 efectos sintetizados al arrancar (click, act, refuse, threat, omen, site, end), sin archivos.
- **Accesibilidad y entrada:** tamaño de interfaz 100/130/160/200 % con layout compacto, movimiento reducido, foco de teclado en diálogos, atajos (Espacio, 1, 2, Tab, ↑↓, Enter, ←→, C, D, H, R, N, Esc), `accessibility_name` en escritorio, aviso de girar el teléfono.
- **Web:** plantilla propia más chica y `web/shell.html` con portada, barra de latón y aviso de carga lenta.

### Herramientas de desarrollo y pruebas

- 57 tests Godot: motor (20), playtest (17), UI (13), guardado (5), golden (2 que recorren 10 trazas).
- `dev/playthrough.gd` (bot con estrategias early/late/hub), `dev/measure.gd` (tasas de victoria por seed), `dev/regenerate_goldens.gd`, `dev/layout_check.gd` (84 combinaciones de escenario × perfil), arnés de capturas con escenarios `start … lost`.
- `tests/playtest-web.mjs`: sonda HTTPS contra un receptor GTC local (dos ventanas, respuesta perdida, offline, fallo de almacenamiento).
- Flujo de publicación en `GTC-page/gtc-web/output/playtests-alignment-3/v040/` (no versionado): `prepare-round.py`, `export-build.py`, `artifact-smoke.mjs`, `hosted-smoke.mjs`, `activate-round.py`.

### Fuera del alcance

`src/` (prototipo React/TS) ya no es referencia desde `alignment-2`; no se porta.

## 3. Conexión con GTC Playtest Lab

| Pieza | ¿Cambia con Unity? |
| --- | --- |
| Endpoint `POST /api/playtests/v1/batches/`, esquema `awakening-2`, límites v1 | No. Los lotes deben ser idénticos en forma y semántica |
| Modelo `PlaytestBuild` (adapter, ruleset, `source_revision`, notas) | No. La build se registra como «The Awakening 0.5.0 · alignment-3 · Unity» |
| CORS (`browser_origin` de la ronda, `PLAYTESTS_ALLOWED_ORIGINS`, `Expose-Headers: Retry-After`) | No, si la build Unity se sirve desde el mismo host de itch.io. Se verifica en la ronda pausada |
| `apps/playtests/test_awakening_adapter.py` | Se agregan los streams emitidos por la build Unity como fixture adicional (commit en `gtc-web`) |
| Scripts de publicación | Sí: `export-build.py` pasa a batchmode de Unity y los smokes comparan `Build/*.data` en lugar de `index.pck` |

Condiciones para no romper la comparación entre rondas:

1. **Misma política de reloj.** Simulación, planificación (pausa, menú, guía), segundo plano y excluido se atribuyen igual que en Godot, incluido el gap por pausa de más de 30 s y el checkpoint cada 20 s. Si algo cambia, se dice en las notas de la build.
2. **Mismos puntos de emisión y mismo gasto** (diferencia de `influenceSpent`, `dreadSpent`, `sacrificed`).
3. **Cada build, una ronda nueva**, como siempre. La ronda Unity no se mezcla con la de 0.4.0 Godot aunque compartan reglas.

Verificación sin ensuciar datos, como en 0.4.0: con la ronda abierta en pausa, un lote real de la build Unity debe responder `503 collection_paused` (pasó el esquema) y no `422`.

## 4. Arquitectura propuesta en Unity

```
unity/                                  proyecto Unity 6000.3.25f1, junto a godot/
  Assets/Awakening/Core/                Awakening.Core.asmdef  (noEngineReferences)
    Rules/  Balance, GameData, GameState, Engine, Derived, Director, Threats, Forecast, Rng, JsNumber
    Save/   SaveData (save-v2)
    Playtest/ PlaytestCapture, PlaytestContract (validación, IDs)
    Dev/    Playthrough (bot), Measure
  Assets/Awakening/Platform/            Awakening.Platform.asmdef
    Storage (IndexedDB vía .jslib en WebGL, archivo atómico + lock en nativo)
    Transport (fetch vía .jslib en WebGL, UnityWebRequest en nativo)
    Clock y visibilidad
    Plugins/WebGL/AwakeningLab.jslib    port de playtest_browser.js
  Assets/Awakening/Game/                GameController (= main.gd): ciclo, comandos, emisión de eventos
  Assets/Awakening/UI/                  UI Toolkit construido desde C# + USS
    Map/  WorldMap (Painter2D), Geo (proyección, picking)
    Screens/  Start, Game, RegionPanel, Dialogs, Notes, Format, Palette, Theme, Gauges, IconButton
    Audio/  Sfx (AudioClip.Create con PCM)
  Assets/Awakening/Tests/               EditMode + PlayMode
  Assets/WebGLTemplates/Awakening/      port de web/shell.html
core-tests/                             proyecto dotnet (netstandard2.1 + xUnit) que compila Core/ y corre las golden sin abrir Unity
```

Decisiones y motivos:

- **Core sin `UnityEngine`.** Se compila igual en Unity y en un `dotnet test` de segundos. Permite que Codex porte y pruebe reglas sin el editor, y que dos agentes no se peleen por un proyecto Unity abierto.
- **Estado tipado** (`GameState`, `RegionState`, `Population`, `Site`, `Threat`, `Opportunity`, `Figure`, `Telemetry`) con `Clone()` profundo, en vez de diccionarios. Serialización a las mismas claves JSON (`elapsedMonths`, `rngState`, `scatteredUntil`…) para leer los guardados de Godot.
- **JSON con `com.unity.nuget.newtonsoft-json`** (paquete oficial). `JsonUtility` no maneja diccionarios ni `null`. Requiere `link.xml` para IL2CPP.
- **UI Toolkit** para pantallas y diálogos, construido en C# como hoy en Godot, con USS para tema y escala. **`Painter2D`** (`generateVisualContent`) para mapa, medidores, sellos e iconos: es el equivalente directo de `_draw()` y rellena polígonos arbitrarios. Alternativa descartada: uGUI + meshes propios, más código para lo mismo.
- **Shaders de papel y lámpara:** UI Toolkit no acepta materiales propios en todos los elementos; plan A, generar al arrancar una textura de ruido de papel con el mismo hash y usarla como fondo teñido; plan B, `RenderTexture` con shader para el fondo. Se decide en el spike.
- **Texto:** UI Toolkit con fuentes SDF generadas desde los TTF. Hay que verificar que las mayúsculas de época no se encimen (el mismo problema hizo descartar el servidor de texto simple en Godot).
- **Render:** URP 2D como en Nut, salvo que el spike muestre que el pipeline integrado da una build web claramente más chica.

## 5. Formato de guardado y continuidad

- Mismo sobre `save-v2` y misma clave de IndexedDB (`lucid-awakening-save-v1` / `snapshots` / `user://save-v2.json`). Así la build Unity hereda ajustes, tutorial, consentimiento recordado y registros pendientes de quien ya jugó la build Godot en ese navegador. Los pendientes conservan su build/ronda original, como hoy.
- Unity no usa `Application.persistentDataPath` (IDBFS) en WebGL: el `.jslib` sigue siendo el único escritor, con la misma garantía de «guardado completo antes del POST».
- En nativo, ruta equivalente a `user://save-v2.json` bajo `persistentDataPath`. No se migran saves nativos de Godot salvo que lo pidas.

## 6. Riesgos técnicos

| Riesgo | Mitigación |
| --- | --- |
| **Tamaño y carga web.** Godot 0.4.0 pesa 7,2 MB en itch.io y queda listo en 9,2 s a 12 Mbps en teléfono emulado. Unity WebGL puede ser más pesado | **Gate en la fase 1**: build mínima con UI Toolkit, fuentes, Newtonsoft y el mapa; IL2CPP con tamaño optimizado, stripping alto, módulos de motor sin usar desactivados, compresión con *decompression fallback* (itch.io no siempre envía `Content-Encoding`). Se mide igual que 0.3.6. Si queda muy por encima, decides antes de seguir |
| **Formato de números.** El log y las previsiones dependen de `toFixed` y `Number.toString`. Mono/IL2CPP no garantiza el *shortest round-trip*, y una máquina en `es-CL` escribe comas | `JsNumber` propio con `CultureInfo.InvariantCulture` en todo Core; las golden lo prueban en `dotnet`, en el editor y **en una build WebGL IL2CPP** que reproduce una traza en el navegador |
| **Determinismo.** Orden de iteración y tipos numéricos | Todo cálculo en `double`; iterar siempre por `REGION_IDS`; nada de `Dictionary` para regiones en reglas; `float` solo en el mapa |
| **Reloj del playtest.** En WebGL el bucle se detiene con la pestaña oculta | Tiempo desde `performance.now()` vía `.jslib`, visibilidad y foco registrados en JS como hoy, conciliación al volver |
| **Durabilidad antes de enviar** | Port literal de `playtest_browser.js` y de la máquina de estados de `PlaytestClient`; se repite la sonda HTTPS de `playtest-web.mjs` sobre la build Unity |
| **Accesibilidad.** Godot expone `accessibility_name` en escritorio | Revisar el soporte de lector de pantalla de Unity 6.3 en el spike; si no cubre escritorio, se registra como diferencia conocida |
| **Dos motores a la vez** | Congelar reglas durante el port (decisión 2). El bot y `measure` se portan para que el balance siga medible |

## 7. Fases y criterios de salida

Cada fase termina con algo comprobable. Las fases 2 y 3 corren en paralelo con la 4.

| Fase | Entrega | Sale cuando |
| --- | --- | --- |
| **0. Decisiones** | Respuestas a §9 | Este documento aprobado |
| **1. Andamiaje y spike web** | `unity/` con asmdefs, paquetes, plantilla WebGL, build en batchmode (`-executeMethod`), CI local con `dotnet test`; escena mínima con el mapa dibujado y texto en las tres fuentes | Build WebGL medida contra 0.4.0 (tamaño servido, tiempo hasta listo a 12 y 4 Mbps en emulación de teléfono) y decisión de seguir |
| **2. Reglas** | Core completo + bot + `measure` + regenerador de golden | Las 10 trazas golden pasan en `dotnet test` y en EditMode; `measure` con 48 seeds da las mismas victorias que Godot (103/144) |
| **3. Guardado y playtest (lógica)** | `SaveData`, `PlaytestCapture`, máquina de estados del cliente, port de los 17 tests de playtest y 5 de guardado | Tests pasan; un save de Godot 0.4.0 carga en Unity; los 13 lotes del test de costos validan con `contracts/playtests/validate.py` contra `awakening-2` |
| **4. Plataforma** | `.jslib` (IndexedDB, Web Locks, fetch, visibilidad), almacenamiento y lock nativo, `GameController` con todos los puntos de emisión | Sonda HTTPS con receptor GTC local y Postgres desechable (mismo procedimiento E4) pasa en la build Unity |
| **5. Mapa e interfaz** | Todas las pantallas, diálogos, expediente, mapa, notas, sonido, atajos, escala, compacto, táctil, movimiento reducido, pantalla de carga | Capturas lado a lado Godot/Unity de los 10 escenarios; checklist de §2 completo |
| **6. Arneses de QA** | Capturas por escenario y `layout_check` en Unity (con `xvfb-run` en Linux) | Las 84 combinaciones pasan |
| **7. Prueba integrada** | Build release con config de prueba | Partida completa en WebGL y Linux; save heredado desde Godot en el mismo navegador; revisión cruzada Claude/Codex sin hallazgos abiertos; prueba en teléfono físico |
| **8. Publicación 0.5.0** | Entrada en `versiones.md`; build y ronda GTC nuevas (pausada → probada → habilitada); upload en itch.io; ronda 0.4.0 cerrada | Lote real responde `collection_paused` con la ronda pausada; build alojada idéntica al export; arranca sin enviar nada antes del consentimiento |
| **9. Retiro de Godot** | Rama/tag `godot-final`; decisión sobre `godot/` y `src/` | Solo después de una ronda Unity con partidas reales |

## 8. Reparto de trabajo con Codex

Cada agente trabaja en su propio worktree y rama; nadie abre el mismo proyecto Unity a la vez. Cada entrega pasa por una revisión del otro agente antes de integrarse a `unity/main` (rama de integración), y esa rama solo llega a `main` en la fase 8.

| Trabajo | Quién | Por qué |
| --- | --- | --- |
| Fase 1: andamiaje, spike de tamaño, pipeline de build | Claude | Decide la arquitectura y el gate |
| Fase 2: reglas, golden, bot, `measure` | **Codex** | Port mecánico con oráculo exacto; no necesita el editor |
| Fase 3: guardado y `PlaytestCapture` con sus tests | **Codex** | Lógica pura, contrato escrito |
| Fase 4: `.jslib`, plataforma, `GameController`, sonda HTTPS | Claude | Requiere el navegador, el receptor GTC local y criterio sobre el reloj |
| Fase 5: mapa y pantallas | Claude (mapa, juego, expediente) · **Codex** (`Notes`, `Format`, textos de guía/final, `Sfx`, diálogos simples) | Codex toma piezas con poca dependencia visual |
| Fase 6: arneses de QA | **Codex** | Port directo de `layout_check.gd` y del arnés de capturas |
| Revisión cruzada | Ambos | Claude revisa reglas/playtest de Codex; Codex revisa UI/plataforma de Claude |
| Fase 8: GTC, itch.io, `versiones.md` | Claude | Usa producción y requiere tu consentimiento en cada paso |

## 9. Decisiones (respondidas el 2026-11-17)

1. **Objetivo:** mantener la build web para itch.io y hacer un ejecutable para Steam. El gate de tamaño web de la fase 1 sigue vigente; la integración con Steamworks queda para después de la paridad.
2. **Desarrollo congelado** durante la migración: se porta `alignment-3` tal cual. El árbol de talentos y los eventos escritos se hacen ya en Unity.
3. **Codex:** `gpt-6.1-sol` con esfuerzo `high`.
4. **Guardado:** la build Unity hereda el de Godot en el navegador (mismo formato y misma clave).
5. **Versión:** 0.5.0, con `alignment-3` y `awakening-2` sin cambios. La regla de `CLAUDE.md` pasa a `PlayerSettings.bundleVersion` de Unity; se actualizan `CLAUDE.md` y `versiones.md` en la fase 8.
6. **Escritorio:** por ahora solo Linux. Windows se decide más adelante (requiere instalar el módulo).

## 10. Ramas y carpetas

- La rama `godot` (creada el 2026-11-17 desde `7d67097`, 0.4.0) conserva la versión Godot. Se usa solo para correcciones urgentes de la build publicada mientras dure la migración.
- La migración se hace desde `main`: `unity/` vive junto a `godot/`, porque las golden, el bot y las capturas de Godot son la referencia del port. `godot/` sale de `main` recién en la fase 9.
- Para trabajar en la rama `godot` sin cambiar de rama en este checkout, se abre como `git worktree` en otra carpeta (por ejemplo `../cd-the_awekening-godot`): mismo repositorio e historial, sin copiar archivos.
- Codex y Claude trabajan cada uno en su propio worktree de la migración, sobre la rama de integración `unity/main`.
