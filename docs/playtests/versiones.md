# Registro de versiones para playtest

Cada build que pueda llegar a un playtest, con o sin GTC Playtest Lab, tiene una entrada aquí. Sirve para saber qué reglas e interfaz vio cada jugador y no mezclar datos de builds distintas.

## Regla

Todo cambio al juego se refleja en el sistema de playtest en el mismo cambio:

1. **Versión del juego** (`bundleVersion` en `unity/ProjectSettings/ProjectSettings.asset`; hasta 0.4.0, `config/version` en `godot/project.godot`): sube en cada build distribuida. Un cambio de motor sin cambio de reglas también sube la versión menor (0.5.0). Cambio de reglas o balance → versión menor (0.3.0); solo UI, texto o correcciones → parche (0.2.1).
2. **Ruleset** (hoy `alignment-4`): cambia con cualquier modificación de reglas o valores de balance. Se envía en `run_started`/`capture_started` y se guarda en los saves.
3. **Adaptador** (hoy `awakening-3`): cambia si cambian acciones, regiones, doctrinas, eventos o sus campos. Requiere actualizar el contrato en `GTC-page/gtc-web/contracts/playtests/`.
4. **GTC:** una build nueva exige build y ronda nuevas en el Lab. La build lleva en **cambios de esta build** un resumen de su entrada aquí (versión, fecha, commit, reglas, qué cambió y por qué) y el enlace a este registro; quienes revisan la ronda lo ven en GTC. No se comparan rondas con reglas o política de reloj distintas sin decirlo.
5. **Entrada en este registro**, en la misma rama que el cambio: fecha, versión, commit, ruleset, adaptador, cambios, motivo y medición que lo respalda.

## Versiones

| Versión | Fecha | Commit | Ruleset | Adaptador | Ronda GTC | Estado |
| --- | --- | --- | --- | --- | --- | --- |
| 0.6.0 | 2026-11-21 | `675e2a7` | `alignment-4` | `awakening-3` | «0.6.0 playtest por invitación · ocultar con plazo y margen del rito» (pendiente) | Pendiente de publicación |
| 0.5.2 | 2026-11-18 | `880e836` | `alignment-3` | `awakening-2` | «0.5.2 playtest por invitación · interfaz legible», abierta del 2026-11-19 al 2026-12-03 (UTC) | En itch.io (Restricted con contraseña) |
| 0.5.1 | 2026-11-18 | `e1d5441` | `alignment-3` | `awakening-2` | «0.5.1 playtest por invitación · interfaz legible», cerrada el 2026-11-19 (UTC) con 1 partida | Reemplazada por 0.5.2 |
| 0.5.0 | 2026-11-18 | `a25f916` | `alignment-3` | `awakening-2` | «0.5.0 playtest por invitación · versión Unity», cerrada el 2026-11-19 (UTC) con 1 partida | Reemplazada por 0.5.1 |
| 0.4.0 | 2026-11-13 | `528c1f2` | `alignment-3` | `awakening-2` | «0.4.0 playtest por invitación · tres respuestas a cada amenaza», cerrada el 2026-11-18 con 1 partida | Reemplazada por 0.5.0 (último build Godot; rama `godot`) |
| 0.3.6 | 2026-11-06 | `fdb4576` | `alignment-2` | `awakening-1` | «0.3.6 playtest por invitación · decisiones económicas», cerrada el 2026-11-13 sin partidas | Reemplazada por 0.4.0 |
| 0.3.5 | 2026-11-06 | `88e10fc` | `alignment-2` | `awakening-1` | «0.3.5 playtest por invitación · decisiones económicas», cerrada el 2026-11-07 con 1 partida | Reemplazada por 0.3.6 |
| 0.3.4 | 2026-11-03 | `4c9cad6` | `alignment-2` | `awakening-1` | «0.3.4 playtest por invitación · decisiones económicas», cerrada el 2026-11-07 con 4 partidas | Reemplazada por 0.3.5 |
| 0.3.3 | 2026-11-03 | `0e54b73` | `alignment-2` | `awakening-1` | «0.3.3 playtest por invitación · decisiones económicas», cerrada el mismo día con 1 partida | Reemplazada por 0.3.4 |
| 0.3.2 | 2026-11-02 | `cf9a755` | `alignment-2` | `awakening-1` | «0.3.2 playtest por invitación · decisiones económicas», cerrada el 2026-11-03 con 1 partida | Reemplazada por 0.3.3 |
| 0.3.1 | 2026-11-02 | `2256c6c` | `alignment-2` | `awakening-1` | «0.3.1 playtest por invitación · decisiones económicas», cerrada el 2026-11-03 con 2 partidas | Reemplazada por 0.3.2 |
| 0.3.0 | 2026-11-02 | `85f5905`; exportada desde `dbfb07f` | `alignment-2` | `awakening-1` | «0.3.0 playtest por invitación · decisiones económicas», cerrada el mismo día sin partidas | Reemplazada por 0.3.1 |
| 0.2.0 | 2026-10-30 / 2026-10-31 | `ae38ec0`; adaptador E4–E6 en `6aa2419` | `alignment-1` | `awakening-1` | QA técnica E7 y «E7 piloto interno de gameplay» (abierta hasta 2026-11-09) | Reemplazada en itch.io por 0.3.0 el 2026-11-02 |

### 0.6.0 — ocultar con plazo, margen del rito y primera partida guiada

- **Commit:** `675e2a7`, rama `t3/release-0.6.0` integrada en `main`. La hicieron tres agentes en paralelo: Claude Opus 5.5 (reglas), GPT‑6.1 Sol (telemetría y Lab) y Gemini 4 Neon (interfaz, en dos pasadas). Claude integró y revisó.
- **Motivo:** la revisión de gameplay del 2026-11-20 (siete revisores, síntesis fuera del repo) coincidió en tres problemas:
  - ocultar en ráfagas de 5 a 8 regiones en un mismo mes borraba la awareness;
  - el jugador no veía cuándo el rito dejaba de caber;
  - el Lab no registraba amenazas ni recursos a lo largo de la partida.

  Se suman las decisiones del usuario del 2026-11-21: cerrar la partida cuando el rito ya no cabe, no abrumar en Easy, y explicar las pestañas, que es el único comentario recibido de un jugador.
- **Reglas (`alignment-4`):**
  - Ocultar sigue siendo regional: −25 de exposición local y 12 meses de cooldown por región.
  - Su −3 de awareness mundial ahora descansa 6 meses en todas las regiones (`Balance.ConcealWorldMonths`). Mientras descansa, ocultar cuesta 20 de Influence y solo limpia la exposición local.
  - Con awareness en 0 no gasta el efecto mundial.
  - El jugador puede cerrar la partida como derrota cuando el rito ya no puede completarse y aún no empezó (`GameEngine.CanConcede`/`Concede`, `ResultCode = "conceded"`).
  - Cada final guarda su causa en `ResultCode`.
  - Las partidas sin terminar de `alignment-3` se retiran con aviso; los ajustes se conservan.
- **Playtest (`awakening-3`):** eventos nuevos:
  - `threat_spawned`;
  - `threat_resolved`, con la respuesta (incluida `ignored`), las respuestas legales en ese momento y las pérdidas;
  - `month_snapshot` cada 12 meses (awareness, Influence, Dread, seguidores, células y sitios financiados y listos);
  - `speed_changed`.

  `run_finished` informa la causa exacta (`awakened`, `world_awareness`, `deadline`, `conceded`). Contrato en `GTC-page/gtc-web/contracts/playtests/awakening-3/`; el Lab resume amenazas por respuesta, curva de awareness, velocidad y causa de cierre (migración `0010`, sin transformar datos).
- **Interfaz:**
  - **Margen del rito:** una banda en la franja del pronóstico muestra la fecha más temprana y la última para iniciar el rito y el margen en meses, con fichas por sitio (seguidores, meses, dispersión, apoyo) y el Dread sobre 100. Se vuelve más visible cuando el margen se achica. Cuando el rito ya no cabe, avisa «The stars will pass without you» con la causa y ofrece «LET THE STARS PASS», con confirmación.
  - **Ocultar:** su fila muestra «GLOBAL EFFECT IN N MO» mientras el efecto mundial descansa.
  - **Primera partida:**
    - Nota con las cifras reales de la primera decisión económica.
    - Explicación de CULT, RITES y OMENS la primera vez que se abre un expediente.
    - Cuatro metas con fecha que se pueden ocultar desde el menú.
    - El subtítulo de colocación ya no empuja a empezar en una región chica.
  - **Final:** un hecho fechado que explica el resultado y «Try this world again», con la misma semilla y dificultad.
- **Medición:**
  - `dotnet test core-tests/Tests`: 184/184, antes 154.
  - Bot, Normal, 48 semillas por política (`dotnet run --project core-tests/Tools -- measure --seeds=48`): victorias de 103/144 con `alignment-3` a 80/144 con `alignment-4`.
    - Por política: early de 21 a 5, late de 48 a 45, hub de 34 a 30.
    - Con 4 meses serían 86/144; se quedó en 6 porque 80 cumple el umbral acordado.
    - **Riesgo:** la estrategia de culto público (early) casi deja de ganar, y bajar el plazo no la arregla (8/48 con 4 meses). Revisar con datos de jugadores antes de 0.7.0.
  - Las goldens de Godot (`alignment-3`) siguen intactas. Hay goldens nuevas para `alignment-4` y una prueba exige que coincidan con las viejas hasta el primer ocultamiento con el efecto mundial en reposo.
  - `UiChecks`: `UI_CHECKS_PASSED`. `unity/layout-check.sh`: 60/60.
  - GTC: 121 pruebas aprobadas y 9 omitidas por requerir PostgreSQL. Validador del contrato: 23 lotes válidos y 42 rechazos comprobados.
- **Qué mirar en esta ronda:**
  - Meses con 4 o más ocultamientos (objetivo 0).
  - Ocultar por debajo del 15 % de las acciones.
  - Awareness final de las victorias entre 35 y 75.
  - Órdenes después del mes 212 sin rito iniciado (objetivo 0).
  - Cuántas partidas se cierran con «LET THE STARS PASS».
  - Abandonos antes del mes 24.
- **Límites:** sin prueba con personas ni en teléfono físico todavía; las capturas son del ejecutable de desarrollo Linux. Mysticism sigue sin efecto: se diseña con el árbol en 0.7.0.

### 0.5.2 — botones con su propia tinta al tocarlos

- **Commit:** `880e836`.
- **Motivo:** al probar 0.5.1 en un teléfono, el botón «Perform» quedó gris azulado con el texto casi invisible después de tocarlo. El tema por defecto de Unity colorea los botones con `:hover:enabled` y `:active:enabled`, reglas más específicas que las del juego (`:hover`, `:active`), así que al pasar el cursor o el dedo se veía el gris de Unity (#D1D1D1 al pasar, #959595 al pulsar). Venía desde 0.5.0; en teléfono se nota más porque el toque deja el estado de hover puesto.
- **Reglas y playtest:** sin cambios (`alignment-3`, `awakening-2`).
- **Interfaz:**
  - Todas las reglas de hover y pulsación de botones, sellos, filas de acciones, pestañas y enlaces repiten `:enabled` (y `:hover:active:enabled`), y conservan las tintas de papel del juego. La pestaña abierta y el botón activo mantienen su color aunque se toquen.
  - Courier Prime e IM Fell no tienen flechas: «Scroll ↓/↑» se veía como un hueco. Ahora dice «Scroll down · more case notes» y «Scroll up · actions and case notes», y la tarjeta plegada de amenaza dice «Tap for responses» / «Inspect responses».
- **Medición:**
  - `UiChecks` simula hover y pulsación sobre botón, botón principal, sello, enlace, fila y pestaña, y falla si el color no es el del juego. Antes del arreglo falló en el primer botón (hover #D1D1D1); después pasa: `UI_CHECKS_PASSED`.
  - `unity/layout-check.sh` con `LAYOUT_JOBS=4`: 60/60 combinaciones aprobadas, todas a la resolución pedida. Pruebas de reglas sin tocar (`Core/` no cambia).
- **GTC (2026-11-19 UTC):** build «The Awakening 0.5.2 · alignment-3 · Unity» (`880e836`, adaptador `awakening-2`) con su resumen en «cambios de esta build». Ronda nueva, abierta en pausa, probada en el host y habilitada; cierra el 2026-12-03. La ronda 0.5.1 se cerró con 1 partida recibida, que se conserva aparte.
- **Publicación:** upload `20654890` en itch.io como único archivo jugable (`the-awakening-0.5.2-pilot.zip`, 8,9 MB, exportado desde un checkout limpio de `880e836`). Los archivos alojados son idénticos al export, la configuración descomprimida tiene los IDs de la ronda nueva y la build arranca sin errores de consola, sin enviar nada antes de responder y con el guardado listo. La página pasa a «Novedad de 0.5.2 / New in 0.5.2».
- **Límites:** la prueba usa eventos de puntero sintéticos en el ejecutable de desarrollo Linux; el toque real en teléfono queda por confirmar en un teléfono con esta build.

### 0.5.1 — tinta legible y expedientes sin recortes

- **Commit:** `e1d5441` (rama `t3/ui-glitches-051`, integrada en `main`). Corrección hecha por Codex (`gpt-6.1-sol`) en dos pasadas, con revisión de Claude entre ambas.
- **Motivo:** corregir los fallos de legibilidad y superposición vistos en las capturas de la build de desarrollo 0.5.0. El papel de época debe dejar claros los precios, plazos y amenazas, también en teléfono.
- **Motor:** Unity 6000.3.25f1 (6.3 LTS), UI Toolkit; sin paquetes ni generadores de texto experimentales.
- **Reglas y playtest:** sin cambios (`alignment-3`, `awakening-2`); no se modifica `Core/`.
- **Interfaz:**
  - Nombres y cuentas atrás del mapa con una copia de papel detrás de la tinta; solo la copia posterior lleva contorno SDF, por lo que el trazo de las letras conserva su grosor. Los seguidores usan fichas de papel opaco y verde oscuro, legibles sobre tierra, territorios del culto y mar. La región seleccionada o bajo el puntero recupera tinta plena y un subrayado; los demás nombres conservan opacidad 0,86. Los plazos se separan de su marcador. Las etiquetas esquivan los avisos visibles y quedan dentro de la vista del mapa; al ampliar, no se muestran nombres de regiones cuyo ancla ya está fuera de la vista. Si no hay espacio cercano, la etiqueta espera hasta mover el mapa o cerrar el aviso, para conservar su asociación geográfica.
  - La columna muestra hasta dos amenazas completas en escritorio y una en el diseño compacto, con «+N more threats · review» para recorrer las restantes. En cada pasada de layout mide tres alternativas independientes (dos completas, una completa y una breve), consulta el modo compacto vigente y elige la más detallada que cabe. Recupera el detalle cuando vuelve el espacio, sin conservar la degradación anterior ni alternar entre cuadros. La tarjeta breve remite a las respuestas del expediente. Los bloqueos de sus respuestas se leen junto al sello.
  - El pronóstico ocupa una franja reservada sobre el libro mayor, fuera del centro del mapa, y se oculta por completo al abrir una hoja modal. «site/sites» concuerda con la cantidad en la presentación, sin cambiar el texto del motor.
  - Los botones y los titulares con cifras usan Old Standard o Courier Prime. Las compras y respuestas no disponibles pierden el relleno activo y conservan tinta legible y el motivo del bloqueo; el botón de doctrina sigue permitiendo inspeccionar las opciones.
  - El expediente conserva estadísticas y People en un pie estable en escritorio; en compacto se desplaza con una barra ancha y una indicación explícita de que quedan notas. La leyenda usa menos espacio y cabe en una fila cuando hay ancho suficiente.
  - Influence, Dread y Followers comparten las mismas alturas de línea. Durante la alineación, la crónica cede su espacio al botón del rito; su texto completo sigue disponible en la hoja Chronicle. El ticker se limita a dos líneas (42 unidades): conserva la entrada completa si cabe, o muestra su primera frase y, si hace falta, una forma más corta cortada entre palabras. El texto completo queda en el tooltip y en Chronicle; no se agrega una elipsis automática ni se permite que crezca fuera del libro mayor.
  - Se atenúan las manchas grandes del papel y se declara un favicon SVG propio en la plantilla WebGL.
- **Medición:**
  - `dotnet test core-tests/Tests`: 154 pruebas aprobadas, 0 fallidas y 0 omitidas; reglas sin cambios.
  - `unity/shots.sh` con `--check-ui 1`: `UI_CHECKS_PASSED` (inicio, Continue, hojas, reloj, foco, participación, ajustes, doctrina, abandonar y finales).
  - `unity/layout-check.sh`, reutilizando la build final de desarrollo y con `LAYOUT_JOBS=4`: 60/60 combinaciones aprobadas. Cinco escenarios (early, mid, late, alignment y awakening), normales y cargados, en los seis perfiles de 1600×900, 1280×720 al 130 %, 1600×900 al 160 % táctil y 844×390 al 100, 160 y 200 % táctil. El arnés comprueba etiquetas contra avisos y límites del mapa, tarjetas recortadas y pronóstico visible con una hoja modal abierta. Comprueba el propio libro mayor contra su franja reservada y cada contenido contra los límites de la barra, usando la entrada real más larga de cada escenario; las amenazas adicionales de crowd se generan con Director.ScheduleThreat para producir también su crónica real.
  - Segunda revisión: capturas nuevas de mid/Africa, late/crowd, explicación de recruit/Africa y alignment, tanto a 1600×900 como a 844×390 con táctil y escala 160 %, más alineación sin modal en teléfono y una captura de late/crowd con geometría registrada en escritorio. Las cuentas 152 (West Asia) y 143 (Oceania) vuelven a leerse; South Asia conserva contraste sobre el mar en late/crowd. Sin excepciones en las capturas.
  - Regresión de recuperación: cambia la altura real del lienzo y verifica dos completas → una completa → breve → una completa → dos completas, con 12 cuadros de estabilización y 12 cuadros de comprobación por paso. En late/crowd de escritorio, alturas de 540/287/190 unidades; pasa con espacios disponibles de 560/414/239/414/560 unidades. La entrada real más larga de ese escenario tiene 221 caracteres; el máximo observado en la matriz es 222. El ticker conserva el texto completo en el tooltip. Una combinación de teléfono se repitió sola porque el gestor de ventanas había reducido su altura a 353 píxeles; se exige 844×390 en la verificación final de perfiles.
- **GTC (2026-11-19 UTC):** build «The Awakening 0.5.1 · alignment-3 · Unity» (`e1d5441`, adaptador `awakening-2`) con su resumen en «cambios de esta build». Ronda nueva, abierta en pausa, probada en el host y habilitada; cierra el 2026-12-03. La ronda 0.5.0 se cerró con 1 partida recibida, que se conserva aparte: misma regla, interfaz distinta.
- **Publicación:** upload `20654281` en itch.io como único archivo jugable (`the-awakening-0.5.1-pilot.zip`, 8,9 MB, exportado desde un checkout limpio de `e1d5441`). Los archivos alojados, incluido `favicon.svg`, son idénticos al export; en `index.html` solo se suma el script que inyecta itch.io. La build arranca sin errores de consola (ya no hay 404 de `favicon.ico`), sin enviar nada antes de responder y con el guardado listo. La página suma «Novedad de 0.5.1 / New in 0.5.1» y pide avisar si algo se ve tapado, cortado o difícil de leer.
- **Comprobación de IDs:** Unity comprime el `.data` con LZ4, así que buscar los IDs como texto puede fallar aunque estén (el `build_id` de 0.5.1 quedó partido por una referencia de LZ4). Desde 0.5.1 los scripts de exportación y de prueba descomprimen la configuración y la comparan entera.
- **Límites:** sin prueba en teléfono físico ni con personas todavía; las capturas de revisión son del ejecutable de desarrollo Linux.

### 0.5.0 — el mismo juego en Unity 6.3 LTS

- **Motivo:** mantener la build web para itch.io y preparar un ejecutable de escritorio para Steam (decisión del 2026-11-17). El desarrollo de reglas quedó congelado durante el port. Plan en [docs/unity/plan-migracion.md](../unity/plan-migracion.md).
- **Motor:** Unity 6000.3.25f1 (6.3 LTS), C#, UI Toolkit; WebGL con IL2CPP y WebAssembly 2023, sin compresión propia (itch.io comprime al servir). El juego vive en `unity/`; `godot/` queda como referencia y la rama `godot` conserva la 0.4.0. Reglas en `unity/Assets/Awakening/Core/`, sin dependencias de Unity, probadas con `dotnet`.
- **Reglas:** sin cambios (`alignment-3`). Las diez trazas grabadas reproducen estado, vistas previas, resultados, valores derivados y textos idénticos a Godot, también con la cultura del sistema en `es-CL`. El bot da las mismas victorias: con 24 seeds, 48/72 (early 9, late 24, hub 15) y las mismas medianas.
- **Interfaz:** misma composición, textos y comportamiento que 0.4.0, rehechos en UI Toolkit:
  - El mapa se dibuja con `Painter2D`, y el grano del papel y la luz de la lámpara se generan en código.
  - Los tooltips son propios, porque UI Toolkit no los muestra fuera del editor.
  - Diferencias conocidas: métrica de fuentes y textura del papel ligeramente distintas. El lector de pantalla de escritorio (`accessibility_name` en Godot) no tiene equivalente todavía.
  - El navegador pide `favicon.ico` y recibe 404, porque la plantilla no declara ícono. Es inofensivo; se corrige en la próxima build.
- **Guardados:** mismo sobre `save-v2`, en la misma base IndexedDB y clave que la build Godot. Quien jugó 0.4.0 en el navegador conserva partida, ajustes, tutorial, consentimiento y registros pendientes. Comprobado sembrando un guardado real de Godot.
- **Playtest:** mismo adaptador `awakening-2`, mismos eventos, misma política de reloj y mismo flujo de consentimiento. La lógica de emisión de `main.gd` vive ahora en `GameSession` (Core) con pruebas propias:
  - Las trazas jugadas por los comandos reales emiten el gasto exacto del motor.
  - 33 lotes sellados validan contra el esquema.
  - De punta a punta contra un receptor GTC local (PostgreSQL desechable, HTTPS), los lotes llegan con `201`, la secuencia es contigua y la reanudación tras recargar abre una segunda sesión con su `capture_gap`, como en Godot.
  - El port corrigió tres diferencias del primer controlador frente a Godot: elegir la velocidad actual ya no cuenta como cambio, el recorte de la semilla es igual a `strip_edges` y el tiempo se acumula en `double`.
- **Medición de carga (emulación de teléfono, gzip local, primera visita):**

  | Build | Descarga | 12 Mbps | 4 Mbps |
  | --- | --- | --- | --- |
  | Godot 0.4.0 | 6,94 MB | 8,1–8,7 s | 17,1 s |
  | Unity 0.5.0 | 9,03 MB | 7,1–7,3 s | 19,1 s |

  En itch.io la 0.5.0 transfiere 9,7 MB frente a 7,2 MB de la 0.4.0. Sin teléfono físico.
- **Tests y chequeos:**
  - 154 pruebas `dotnet` y 1 EditMode.
  - `--check-ui` aprobado: inicio, Continue, todas las hojas, reloj, foco, participación, ajustes, doctrina, abandonar y finales.
  - Chequeo de layout 60/60 en los perfiles de 0.4.0; corrigió tres desbordes del libro mayor.
  - 23 pares de capturas Godot/Unity.
  - Detalle en [docs/unity/verificacion.md](../unity/verificacion.md).
- **GTC (2026-11-18 UTC):** build «The Awakening 0.5.0 · alignment-3 · Unity» (`a25f916`, adaptador `awakening-2`) con su resumen en «cambios de esta build». Ronda nueva, abierta en pausa, probada en el host y habilitada; cierra el 2026-12-02. Con la ronda en pausa, el Lab de producción aceptó el contrato de un lote generado por la sesión de Unity (`collection_paused`, sin guardar nada) y rechazó el mismo lote declarado `awakening-1` con `scatter` (`schema_invalid`). La ronda 0.4.0 se cerró con 1 partida recibida, que se conserva aparte.
- **Publicación:** upload `20638224` en itch.io como único archivo jugable (`the-awakening-0.5.0-pilot.zip`, 8,9 MB, exportado desde un checkout limpio de `a25f916`). Los archivos alojados son idénticos al export; en `index.html` solo se suma el script que inyecta itch.io. La build arranca sin enviar nada antes de responder y con el guardado listo. El texto de la página dice que es el mismo juego en otro motor y pide avisar si algo cambió o falla.

### 0.4.0 — `alignment-3`: tres respuestas a cada amenaza

- **Motivo:** revisión de diseño del 2026-11-12 sobre las partidas humanas de 0.3.x (foto del Lab del 2026-11-12: 2 testers externos, 7 partidas jugadas, 2 victorias). El medio juego era mantenimiento: 96 de 131 y 68 de 96 acciones de las dos victorias fueron `recruit`, `counter` y `conceal`. Tres de las cuatro derrotas de 0.3.4 fueron por awareness que salió sobre todo de `manifest` y `reveal`, cuyo coste solo estaba en el texto de riesgo. Diseño y medición completos en [Experiencia y loops](../gdd/02_Game_Design/Experiencia_y_loops.md#cambios-de-alignment-3--2026-11-12); decisión D11.
- **Reglas:**
  - Una amenaza tiene tres respuestas. `counter` (obstruir) no cambia. `scatter` (dispersar) es gratis: la célula pasa 8 meses oculta, sin reclutar ni ingresar; su sitio no avanza ni cuenta para el rito; deja de ser pública, baja 10 de exposición y no atrae amenazas nuevas; 24 meses de cooldown. `silence` (silenciar) cuesta 20 de Dread por severidad (13 con Harbinger), suma 3 de awareness por severidad y 6 de exposición; 12 meses de cooldown.
  - `reveal` suma +5 de awareness solo la primera vez que una región se hace pública.
  - Toda acción informa en su vista previa la awareness que suma, y se cobra exactamente eso.
  - Descartado: posturas por célula con reclutamiento automático (quitan acciones al jugador) y amenazas cada 3 meses (el bot bajó de 103 a 59 victorias en 144).
- **Interfaz:** tres sellos por amenaza en el recorte del mapa y en el expediente, cada uno con su precio y lo que cuesta además; línea roja «+N AWARENESS» bajo el precio de las acciones que la suman; estado «SCATTERED · N MO» en el expediente y en la ficha del sitio; guía, notas y consejos de derrota actualizados. La pantalla final cuenta «Threats answered».
- **Guardados:** una partida `alignment-2` sin terminar no continúa; se conservan ajustes, tutorial y registros de playtest, y el jugador ve el aviso. El estado de cada región agrega `revealed`, `scattered` y `scatteredUntil`.
- **Playtest:** adaptador nuevo `awakening-2`, porque cambia la lista de acciones (14, con `scatter` y `silence`). Los registros en cola de builds anteriores conservan `awakening-1`. En GTC (`gtc-web` `c7bb677`): contrato y fixture en `contracts/playtests/awakening-2/`, registro de adaptadores, etiquetas del informe y migración `0009_awakening_2_alignment_3`. `awakening-1` sigue aceptando solo sus 12 acciones.
- **Medición (2026-11-12, bot, Normal, `dev/measure.gd`):** con 24 seeds por estrategia, 49/72 → 48/72 victorias; con 48 seeds, 95/144 → 103/144 (early 17 → 21, late 43 → 48, hub 35 → 34). Influence final mediana (48 seeds) late 184 → 52 y hub 69 → 41. Las derrotas de hub pasan de 13 por calendario a 13 por awareness. Respuestas por partida: 6,5–16 obstrucciones, 3 dispersiones, 2–4 silencios. La mejor línea del bot (late) ya no pierde; Easy y Hard no se midieron. No hay partidas humanas con estas reglas.
- **Tests:** 57 tests Godot (8 nuevos: dispersar, sitio disperso, célula dispersa sin amenazas, silenciar, `reveal` una vez, awareness como precio, tres sellos, awareness junto al precio); trazas de `tests/golden/` regeneradas para `alignment-3`. 121 tests del Lab de GTC (SQLite, 9 omitidos que requieren PostgreSQL), con uno nuevo para `awakening-2`. Los 13 lotes que emite el juego en `test_game_commands_emit_actual_costs…` validan contra el esquema `awakening-2`.
- **Chequeo de layout:** `dev/layout_check.gd` pasa en 60 combinaciones: 5 escenarios (early, mid, late, alignment, awakening), normales y cargados, en los 6 perfiles de 0.3.5. Una corrida falló al arrancar mientras otro proceso de Godot reimportaba el proyecto y pasó dos veces al repetirla sola. No se probó en un teléfono físico ni en el export web.
- **Prueba local (2026-11-13, diseñador, escritorio, sin Lab):** victoria en el mes 212 en Normal con Harbinger; 14 obstruir, 12 silenciar, 3 dispersar y 46 `conceal`; 0 seguidores perdidos por amenazas, 860 de Influence sin gastar y awareness final 11. Sin errores. El precio en awareness de silenciar no pesó porque `conceal` lo borra; queda como candidato para `alignment-4` (véase [07 — Balance](../gdd/07-balance.md)).
- **GTC (2026-11-13 UTC):** commit `c7bb677` de `gtc-web` desplegado en Railway (migración `0009`). Build «The Awakening 0.4.0 · alignment-3» (`528c1f2`, adaptador `awakening-2`) con su resumen en «cambios de esta build», y ronda nueva, abierta en pausa, probada en el host y habilitada; cierra el 2026-11-27. Con la ronda en pausa, el Lab de producción aceptó el contrato de un lote `awakening-2` con `scatter` y `silence` (respondió `collection_paused`, sin guardar nada) y rechazó el mismo lote declarado como `awakening-1` (`schema_invalid`). La ronda 0.3.6 se cerró sin partidas recibidas.
- **Publicación:** upload `20497493` en itch.io como único archivo jugable (`the-awakening-0.4.0-pilot.zip`, exportado desde un checkout limpio de `528c1f2` con la plantilla propia). La build alojada coincide con el export (`index.pck` idéntico), arranca sin errores y no envía nada antes de responder. El texto de la página explica las tres respuestas y la awareness en rojo, y pregunta si una respuesta domina. Sin probar todavía en teléfono físico.

### 0.3.6 — carga más rápida

- **Motivo:** en el teléfono la 0.3.5 tardaba mucho en abrir (reporte del 2026-11-06). La medición mostró que no era una regresión: la 0.3.4 tardaba lo mismo. Casi toda la descarga era el motor de Godot (10,6 MB comprimidos de 11,8), con 3D, física, navegación, XR, controles avanzados, SVG e importadores que el juego no usa.
- **Motor:** plantilla web propia de Godot 4.7.2 (`scripts/build-web-template.sh`, Emscripten 4.0.11, `optimize=size`, LTO). Solo incluye GDScript, FreeType, RegEx, mbedtls y el servidor de texto avanzado sin Graphite. Se descartó el servidor de texto simplificado porque encimaba mayúsculas en las fuentes de época, y `optimize=size_extra` porque arrancaba más lento. Las casillas y el deslizador se dibujan en `UiTheme`, porque el motor ya no trae SVG. Escritorio nativo (Linux y Windows) sin cambios.
- **Pantalla de carga:** `godot/web/shell.html` reemplaza el logo de Godot por la portada del juego, con una barra de latón y el porcentaje; al terminar la descarga dice «Unsealing the chart». Si tarda más de 8 s, explica que la primera visita descarga todo y las siguientes usan la caché. El fondo de arranque del motor es el del escritorio, sin imagen.
- **Arranque:** la geometría del mapa (anillos, puntos y contornos) se calcula una vez por sesión y la comparten la portada y el juego.
- **Medición (Chromium con GPU, teléfono emulado 844×390, CPU 4× más lenta, servidor con gzip como itch.io):** descarga de 11,8 a 6,6 MB. Hasta el juego listo, a 12 Mbps de 12,7 a 9,0 s y a 4 Mbps de 27,7 a 18,3 s (dos corridas por caso). No se midió en un teléfono físico.
- **Medición en itch.io (misma emulación, builds alojadas):** itch.io sirve 7,2 MB en lugar de 11,8 (su gzip comprime menos que el local). Juego listo a 12 Mbps en 9,2 s frente a 11,8 s, y a 4 Mbps en 18,8 s frente a 28,6 s.
- **Reglas y playtest:** sin cambios (`alignment-2`, `awakening-1`).
- **Tests:** 49 tests Godot. En el navegador, con la plantilla nueva, el juego arranca, coloca el culto, abre el expediente y el menú sin errores en consola; los textos se dibujan igual que con la plantilla oficial.

- **GTC (2026-11-07 UTC):** build «The Awakening 0.3.6 · alignment-2» (`fdb4576`), con su resumen en «cambios de esta build», y ronda nueva, abierta en pausa, probada en el host y habilitada; cierra el 2026-11-21. La ronda 0.3.5 se cerró con 1 partida recibida, que se conserva aparte.
- **Publicación:** upload `20335887` en itch.io como único archivo jugable (`the-awakening-0.3.6-pilot.zip`, exportado desde un checkout limpio de `fdb4576` con la plantilla propia). La build alojada coincide con el export (`index.pck` idéntico), arranca sin errores y no envía nada antes de responder.

### 0.3.5 — revisión de UX/UI

- **Motivo:** evaluación de UX/UI del 2026-11-06 (6,5/10 en conjunto). Lo más bajo: teléfono (3), accesibilidad (4), claridad de acciones (6), onboarding (6) y momentos clave (6). Las acciones no disponibles se leían a 2,4:1 y su motivo solo aparecía al pasar el ratón.
- **Acciones:** las filas no disponibles van en cursiva con tinta legible (5,2:1). En lugar del precio muestran el obstáculo real en pocas palabras («NO CELL HERE», «NO THREAT», «NEED 30 INF», «IN 4 MO»); un obstáculo estructural se antepone al precio. Se oculta «Establish a cell» donde ya hay célula. Los sellos (Obstruct, omens) llevan el motivo debajo. ↑ ↓ eligen acción, Enter la ejecuta y ← → cambian de pestaña.
- **Doctrina y rito:** el botón de doctrina indica cuánta Influence falta y el diálogo lo repite. Durante la alineación el botón del rito dice «not ready» si aún no se puede.
- **Final de partida:** la derrota nombra la causa, muestra la lista de sitios y el rito, y da uno o dos consejos para la próxima partida. La victoria tiene hoja propia con fondo violeta, el dístico del Necronomicon y entrada animada. El estado de GTC y el export pasan a enlaces secundarios.
- **Mapa:** se entinta el contorno de cada región en vez de todas sus fronteras internas, las etiquetas llevan halo de papel y se apartan para no taparse, y los sitios estancados se tachan además de ir en rojo. Rueda o pellizco para acercar, arrastre para desplazar, «Whole world» o R para volver.
- **Pronóstico y amenazas:** el aviso del calendario ritual tiene un titular («Sites will finish 10 months too late», «1 site lost support»). Los recortes de amenaza dicen sus efectos en palabras («Kills 19 followers», «Site loses 6 months»).
- **Onboarding:** guía más corta y notas de una frase la primera vez que aparece cada situación (célula, amenaza, omen, sitios, awareness, sitio sin apoyo, doctrina). Se guardan por dispositivo.
- **Teléfono y tamaño:** tamaño de interfaz de 100, 130, 160 o 200 % en el menú y, en pantallas táctiles, en el inicio. Por defecto sigue en 100 %, como en 0.3.4. Con tamaños grandes se activa un layout compacto: barras más bajas, sin crónica en la barra, recortes de amenaza reducidos, pronóstico plegable y hojas con desplazamiento. En vertical, un aviso pide girar el teléfono.
- **Accesibilidad:** opción de movimiento reducido, foco de teclado en los diálogos, tabla de atajos (Tab, C, D, H, R), WARN y FAINT con más contraste, estado de los sitios distinguible sin color y tasas con un decimal.
- **Reglas y playtest:** sin cambios (`alignment-2`, `awakening-1`). Los ajustes nuevos (`uiScale`, `reducedMotion`, `hints`) son opcionales en el guardado y los saves de 0.3.4 cargan igual. El arnés de capturas ya no invierte `won` y `lost`.
- **Segunda pasada (revisión independiente de Codex, 6,5/10):** el panel ya no se ensancha con explicaciones largas: el botón táctil dice «Perform · coste». La barra inferior compacta cabe al 200 % y el botón del rito queda en «Final Rite». El pronóstico se centra en la parte visible del mapa y, durante el rito, muestra una barra de progreso que no se oculta. En táctil, una fila bloqueada abre su explicación con el botón desactivado. Un sitio con mantenimiento impago se ve igual en tarjeta, mapa, sellos y listas: tachado y «Dormant». Las etiquetas del mapa esquivan sitios, omens y cuentas atrás. Foco de teclado en el inicio y en la hoja de participación, y N cierra las notas. Una nota abierta más de 12 s cede su lugar a la siguiente. En pantallas cortas las tarjetas del panel van después de las acciones y las filas táctiles miden 52. El plazo cambia a ámbar en los últimos 36 meses. FAINT pasa a 4,7:1. El escenario `awakening` del arnés ya muestra el rito en curso.
- **Tercera pasada (segunda revisión de Codex, 7,5/10):**
  - Los avisos se reparten el mapa visible sin taparse: el pronóstico se aparta a la derecha de las amenazas, la columna de amenazas se desplaza y se detiene encima del pronóstico, «Whole world» va arriba a la derecha y una nota sin hueco libre espera sin darse por leída.
  - Las notas ya no se apilan: se registran como vistas solo cuando se muestran, sin reentrar en el refresco.
  - El detalle de cada acción muestra el precio completo («COST 40 INF · 60 DREAD») aunque otra cosa la bloquee.
  - La hoja de participación encierra el foco y lo devuelve al formulario al cerrarse; los enlaces muestran foco.
  - Botones de icono y filas de acción tienen `accessibility_name` para lectores de pantalla (escritorio nativo).
  - En el layout compacto, el texto pequeño crece dos unidades y los iconos táctiles miden 52×50.
- **Chequeo de layout:** `godot/dev/layout_check.gd` recorre cada región, pestaña, explicación de acción y diálogo, y falla si algo sale del lienzo o queda bajo el panel. Con `--crowd=1` agrega amenazas en todas las regiones con célula, una nota abierta y el mapa ampliado, y comprueba que ningún aviso tape a otro. Pasa en 84 combinaciones: 7 escenarios, normales y cargados, a 1600×900, 1280×720 al 130 %, 1600×900 al 160 % táctil y 844×390 CSS al 100, 160 y 200 %. Sobre la copia anterior a la segunda pasada detecta los desbordes que reportó Codex.
- **Tests:** 49 tests Godot (10 nuevos: motivos de bloqueo, toque en fila bloqueada, final de partida, titular del pronóstico, notas, ajustes guardados, sitio inactivo, escenario del rito, tasas y contorno de regiones). Capturas a 1600×900, 1280×720, 1600×900 al 160 % y teléfono a 844×390 CSS con densidad 2 al 160 y 200 %, y en vertical. Export web probado en Chromium con emulación táctil a 844×390 ×3: arranca sin errores y el toque en 160 % reconstruye la interfaz. El pellizco y el arrastre no se probaron en un teléfono físico.

- **GTC (2026-11-07 UTC):** build «The Awakening 0.3.5 · alignment-2» (`88e10fc`), con su resumen en «cambios de esta build», y ronda nueva, abierta en pausa, probada en el host y habilitada; cierra el 2026-11-21. La ronda 0.3.4 se cerró con 4 partidas recibidas, que se conservan aparte.
- **Publicación:** upload `20335119` en itch.io como único archivo jugable (`the-awakening-0.3.5-pilot.zip`, exportado desde un checkout limpio de `88e10fc`). La página recomienda en el teléfono jugar en horizontal con un tamaño de interfaz de 160 % o 200 % y explica el pellizco ([itch-page.md](itch-page.md)). La build alojada coincide con el export (`index.pck` idéntico), arranca sin errores y no envía nada antes de responder.

### 0.3.4 — escala original con panel táctil

- **Motivo:** en el teléfono físico no convencieron ni la escala de 0.3.2 (mapa muy pequeño) ni el zoom de 0.3.3 (2026-11-03). Se vuelve a la interfaz de 0.3.1 y se conserva solo la lectura de los poderes.
- **Interfaz:** se quitan la escala adaptable, el modo compacto, los textos agrandados, las barras más bajas, el aviso para girar el teléfono y el zoom del mapa. Se mantienen del panel de región: acciones antes de las estadísticas, explicación bajo la acción con botón para ejecutarla en táctil, desplazamiento arrastrando y una barra de desplazamiento visible en táctil. Escritorio igual que en 0.3.1 salvo el orden del panel.
- **Reglas y playtest:** sin cambios (`alignment-2`, `awakening-1`).
- **Tests:** 39 tests Godot, incluido el flujo táctil de acciones. Capturas a 844×390 CSS con densidad 2 y a 1600×900.
- **GTC (2026-11-03 UTC):** build «The Awakening 0.3.4 · alignment-2» (`4c9cad6`) y ronda nueva, abierta en pausa, probada en el host y habilitada; cierra el 2026-11-17. La ronda 0.3.3 se cerró con 1 partida recibida, que se conserva aparte.
- **Notas en GTC (2026-11-03):** desde `6c53954` cada build del Lab tiene el campo «cambios de esta build», visible en la ronda, en la lista del proyecto y en el export. Se completó para las siete builds de The Awakening (E7 QA y piloto, 0.3.0–0.3.4).
- **Publicación:** upload `20247332` en itch.io como único archivo jugable. La página vuelve a recomendar computadora y avisa que en el teléfono el texto es pequeño ([itch-page.md](itch-page.md)). La build alojada coincide con el export (`index.pck` idéntico), arranca sin errores y no envía nada antes de responder.

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
