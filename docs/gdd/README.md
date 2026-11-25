# The Awakening of Cthulhu — GDD

**Versión documental:** 0.6 · **Fecha:** 2026-11-25 · **Estado:** consolidado sobre el juego 0.9.0 / 0.9.1 (Unity 6.3 LTS, `alignment-7`), con beta pública abierta en itch.io y dos caminos de producción definidos.

Este es el GDD de trabajo del juego de **Shaman Llama** (*Shaman Llama Games / Shaman Llama SAS*). Adapta las plantillas de
Game Time Continuum y separa con claridad **lo ya implementado** de **lo planeado para el futuro**.
Los objetivos confirmados son partidas de **15–30 minutos** (incluyendo pausa y planificación),
una mezcla de **estrategia, narrativa y horror** con registro lovecraftiano de 1908–1926, y una
filosofía de diseño centrada en que el jugador tenga opciones reales y se sienta inteligente al
resolver el juego (D25).

Desde 0.5.0 el juego vive en **Unity 6.3 LTS**. Desde el 2026-11-24 la build web `0.9.0` está
abierta al público en **itch.io** como demo para una etapa de beta testing de varios meses con
GTC Playtest Lab. La producción futura contempla **dos caminos** (D26): cerrar una versión
completa con Cthulhu para itch.io, o escalar hacia Steam con arte/audio contratados y nuevos
Dioses Antiguos si se obtiene el Unity Grant de 50.000 USD.

El documento se trabaja en español. Los términos visibles de la interfaz y los identificadores
del código se conservan en inglés cuando ayudan a localizar una regla. Las reglas ejecutables
viven en `unity/Assets/Awakening/Core/Rules/`. La versión Godot (0.2.0–0.4.0) queda como
referencia en la rama `godot`, y el prototipo React/TypeScript de `src/` es solo antecedente
histórico.

## Por dónde empezar

Si quieres leer todo el GDD de corrido en un solo documento, abre
[docs/GDD_consolidado.md](../GDD_consolidado.md). Para trabajar por área, empieza por
[Resumen y visión](01_Informacion_General/Resumen_y_vision.md),
[Experiencia y loops](02_Game_Design/Experiencia_y_loops.md),
[Old Gods](02_Game_Design/Old_Gods.md) y
[Decisiones pendientes](00_Indice_y_Control/Decisiones_pendientes.md).
Para consultar o modificar una regla vigente, ve a su capítulo técnico (`01`–`07`).

| Área adaptada de GTC | Documento de trabajo | Contenido y estado |
| --- | --- | --- |
| 00 — Índice y control | [Control y origen](00_Indice_y_Control/Control_y_origen.md) · [Decisiones](00_Indice_y_Control/Decisiones_pendientes.md) · [Glosario](00_Indice_y_Control/Glosario.md) | Convenciones, decisiones D01–D26 y términos vigentes |
| 01 — Información general | [Resumen y visión](01_Informacion_General/Resumen_y_vision.md) | Concepto, beta pública, objetivos y caminos de entrega |
| 02 — Game Design | [Experiencia y loops](02_Game_Design/Experiencia_y_loops.md) · [Old Gods](02_Game_Design/Old_Gods.md) | Pilares, loops, árbol de talentos, filosofía de balance y visión de los 3 Old Gods (Cthulhu implementado; Nyarlathotep y Shub-Niggurath futuros) |
| 03 — Mapa estratégico y ritmo | [Mapa y ritmo](03_Mapa_Estrategico/Mapa_y_ritmo.md) | Regiones, adyacencias, fases y objetivo de 15–30 min |
| 04 — Narrativa | [Mundo y narrativa sistémica](04_Narrativa/Mundo_y_narrativa.md) | Crónica actual, tono lovecraftiano (D04) y eventos escritos futuros (D13) |
| 05 — Arte | [Dirección visual](05_Arte/Direccion_visual.md) | Prototipo procedural temporal y plan de contratación de artistas (D06) |
| 06 — Audio | [Dirección de audio](06_Audio/Direccion_de_audio.md) | SFX procedurales actuales y plan de música/sonido con grant |
| 07 — UI/UX | [Interfaz y accesibilidad](07_UI_UX/Interfaz_y_accesibilidad.md) | Flujo implementado hasta 0.9.0, soporte táctil y pendientes hacia lanzamiento |
| 08 — Programación y Technical Design | [Base técnica](08_Programacion_y_Technical_Design/Base_tecnica.md) | Motor Unity 6.3 LTS, arquitectura Core/Platform/UI, guardado y pruebas |
| 09 — Producción | [Alcance y validación](09_Produccion/Alcance_y_validacion.md) | Inventario actual, beta abierta, Camino 1 vs. Camino 2 (Unity Grant) y riesgos |
| 10 — Público y negocio | [Público y distribución](10_Marketing_y_Negocio/Publico_y_distribucion.md) | Demo público en itch.io, ruta a Steam, LLC/registros y eventos |

## Referencia detallada de las reglas vigentes (`alignment-7`)

Los capítulos `01` a `06` están consolidados en español sobre las reglas vigentes de
`alignment-7` (Unity `0.9.0`–`0.9.1`) y enlazan directamente a
`unity/Assets/Awakening/Core/Rules/`. El historial detallado de cada iteración de balance y
sus mediciones de bot sigue en [Experiencia y loops](02_Game_Design/Experiencia_y_loops.md),
[08 — Design rationale](08-rationale.md) y [versiones.md](../playtests/versiones.md).

| Documento | Pregunta que resuelve |
| --- | --- |
| [01 — Overview](01-overview.md) | Premisa, fases, ciclo mensual, determinismo y condiciones de victoria/derrota |
| [02 — Economy](02-economy.md) | Influence, Dread, seguidores, costes, mantenimiento y vista previa |
| [03 — Population](03-population.md) | Creencias, reclutamiento, Mysticism, Madness y conservación de población |
| [04 — Visibility and awareness](04-visibility.md) | Exposición regional, awareness mundial, culto público y frenos (`conceal`, `bury`, talentos) |
| [05 — Opposition](05-opposition.md) | Presión de amenazas, respuestas (`counter`, `scatter`, `silence`), respiro y oportunidades |
| [06 — Ritual sites](06-ritual-sites.md) | Apoyo, preparación, restauración, aceleración, remates y previsión del rito final |
| [07 — Balance values](07-balance.md) | Política de fuente única y fotografías fechadas de valores hasta `alignment-7` |
| [08 — Design rationale](08-rationale.md) | Motivos históricos de los cambios de reglas y evidencia registrada |
| [09 — Open problems](09-open-problems.md) | Problemas observados, riesgos de `alignment-7` e ítems diferidos por decidir |

Los valores ejecutables viven en `Balance.cs` y `GameData.cs`. Las fotografías fechadas del
GDD no los reemplazan.

## Historia y evidencia

Los diseños [MVP](../archive/GDD_mvp_proposal.md) y
[legacy](../archive/GDD_legacy_prototype.md) son antecedentes archivados. El
[plan de implementación](../implementation-plan.md), el
[registro de implementación](../implementation-checklist.md) y los
[playtests](../playtests/) aportan contexto y evidencia fechada. Algunas pruebas anteriores
corresponden a calendarios y economías previas: sus resultados no demuestran el balance de
la versión actual.
