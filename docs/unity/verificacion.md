# Verificación del port a Unity

> Estado: en curso · Fecha: 2026-11-18 · Rama `unity/main` · Unity 6000.3.25f1 (6.3 LTS)

Qué se comprobó del port de Godot 0.4.0 a Unity, cómo repetirlo y qué no se comprobó. Plan y fases en [plan-migracion.md](plan-migracion.md).

## Reglas y guardado

| Comprobación | Resultado |
| --- | --- |
| Diez trazas grabadas de `godot/tests/golden/`: estado tras cada paso, vistas previas de las 14 acciones en las 8 regiones, resultados de comandos, valores derivados y texto del log | Idénticos (tolerancia numérica de Godot, texto exacto) |
| Las mismas trazas con la cultura del sistema en `es-CL` | Idénticas |
| Finales del bot (`policy-endings`) | Idénticos |
| `measure --seeds=24` en Godot y en C# | Iguales: early 9/24, late 24/24, hub 15/24, total 48/72, con las mismas medianas |
| Guardado de Godot real (mes 60) decodificado y continuado | Mismo estado |
| Total de pruebas `dotnet test core-tests/Tests` | 154 aprobadas |

Repetir: `dotnet test core-tests/Tests`; `dotnet run --project core-tests/Tools -- measure --seeds=24` frente a `godot --headless --path godot -s res://dev/measure.gd -- --seeds=24`.

## Playtest

| Comprobación | Resultado |
| --- | --- |
| Pruebas de captura, bandeja de salida, almacén y sesión (consentimiento, IDs, reloj, reintentos, división 413, recibos, comentarios, caducidad) | Aprobadas dentro de las 154 |
| Trazas grabadas jugadas por los comandos reales de la sesión, con el gasto de cada evento igual a la diferencia de los contadores del motor | Iguales a Godot |
| Lotes sellados de esas partidas validados con `contracts/playtests/validate.py` de GTC | 33/33 válidos con `awakening-2`; el mismo lote declarado `awakening-1` con `scatter` se rechaza |
| Registro de playtest guardado por Godot cargado en Unity | Se acepta sin cambios y se reanuda |
| Prueba de escritor único nativo (dos procesos, mismo guardado) | Aprobada (EditMode) |

### De punta a punta contra un receptor GTC local (2026-11-18)

Receptor `gtc-web` en `config.settings.ci` con PostgreSQL 17 desechable en Docker, proyecto, build `awakening-2`/`alignment-3` y ronda abierta sintéticos, origen `https://127.0.0.1:58485`. Build web de desarrollo de Unity apuntada a `http://127.0.0.1:58483/api/playtests/v1/batches/`, servida por HTTPS local, Chromium headless.

- **Sin consentimiento no se envía nada:** cero peticiones al cargar y con la hoja «Join the playtest?» abierta.
- **Al aceptar**, el primer lote llega con `201`. Partida jugada con clics y teclado: colocación en África, `recruit` y velocidad doble.
- **Lo recibido:** `run_started`, `session_started`, `checkpoint_reached` (tutorial), `starting_region_selected`, `action_performed` con `spent` de 18 Influence, y puntos de control de tiempo. Secuencia contigua, cobertura `from_start`. Reparto del reloj: 43 s de simulación y 9 s de planificación.
- **Recarga a mitad de partida:** la pantalla de inicio muestra la partida en curso y el consentimiento recordado («Receipt confirmed and saved locally»). Continue abre una segunda sesión (`session_started: resume`) con un `capture_gap` de tiempo desconocido, como en Godot. La partida sigue hasta el mes 67 con la secuencia contigua.
- **Sin errores** de consola en ninguna fase.

### Herencia del guardado de Godot en el navegador

Un guardado escrito por la build Godot, sembrado en IndexedDB (`lucid-awakening-save-v1` / `snapshots` / `user://save-v2.json`) en el mismo origen, abre en la build Unity como «Run in progress · January 1913 · Normal», con sus ajustes de interfaz. Sin errores de consola.

## Interfaz

| Comprobación | Resultado |
| --- | --- |
| Capturas Godot frente a Unity: inicio, consentimiento, continuar, ruleset retirado, guía, menú, doctrina, crónica, Lab, alineación, ganada, perdida, comentario final, partida temprana, media, tardía y rito, a 1600×900 y a 844×390 táctil al 160 % | 23 pares sin excepciones; diferencias menores de métrica de fuentes y grano del papel |
| `--check-ui 1` (inicio y Continue, todas las hojas, reloj retenido por diálogos, foco de teclado, participación, ajustes, doctrina, abandonar, finales con comentario) | `UI_CHECKS_PASSED` |
| Chequeo de layout (60 combinaciones de 0.4.0) | Pendiente |

Repetir: `unity/shots.sh <carpeta> "mid --select africa" ...` y `unity/shots.sh <carpeta> "start --check-ui 1"`.

## Carga web

`scripts/measure-web-load.mjs`: teléfono emulado 844×390 a 3×, CPU 4× más lenta, latencia 40 ms, primera visita, gzip local como itch.io; «listo» = desaparece la pantalla de carga. Dos corridas.

| Build | Descarga | 12 Mbps | 4 Mbps |
| --- | --- | --- | --- |
| Godot 0.4.0 publicada | 6,94 MB | 8,1–8,7 s | 17,1 s |
| Unity, juego completo | 9,03 MB | 7,1–7,3 s | 19,1 s |

## Qué no se comprobó

- Un teléfono físico, Safari y el host real de itch.io (se verifica en la publicación con el smoke alojado).
- Lector de pantalla: Godot exponía `accessibility_name` en escritorio; el port no tiene equivalente todavía.
- Windows y macOS: solo se construyó y probó Linux.
- Personas reales: toda la evidencia es automatizada o sintética.
