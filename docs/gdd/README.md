# The Awakening of Cthulhu — GDD

**Versión documental:** 0.2 · **Fecha:** 2026-10-28 · **Estado:** documentación de la implementación en curso.

Este es el GDD de trabajo del juego de Lucid Labworks Studio. Adapta las plantillas de
Game Time Continuum y aprovecha la documentación de reglas existente. La implementación
actual es el objeto de esta primera etapa: documentar lo que ya está implementado.
Los objetivos confirmados son partidas de **15–30 minutos** y una mezcla de
**estrategia, narrativa y horror**. Se distinguen del estado actual, con unos siete
minutos de simulación a 1× más pausas. Las propuestas y las decisiones pendientes se
identifican expresamente; esta etapa no compromete un rediseño ni funcionalidades nuevas.

El documento se trabaja en español. Los términos de interfaz y los identificadores
del código se conservan en inglés cuando ayudan a localizar una regla. El juego actual
está en Godot; React/TypeScript es el prototipo de referencia de sus reglas.

## Por dónde empezar

Lee [Resumen y visión](01_Informacion_General/Resumen_y_vision.md),
[Experiencia y loops](02_Game_Design/Experiencia_y_loops.md) y
[Decisiones pendientes](00_Indice_y_Control/Decisiones_pendientes.md).
Para modificar una regla, consulta después su referencia detallada.

| Área adaptada de GTC | Documento de trabajo | Contenido y estado |
| --- | --- | --- |
| 00 — Índice y control | [Control y origen](00_Indice_y_Control/Control_y_origen.md) · [Decisiones](00_Indice_y_Control/Decisiones_pendientes.md) · [Glosario](00_Indice_y_Control/Glosario.md) | Convenciones, procedencia, cambios y preguntas |
| 01 — Información general | [Resumen y visión](01_Informacion_General/Resumen_y_vision.md) | Concepto, plataformas actuales y objetivos confirmados |
| 02 — Game Design | [Experiencia y loops](02_Game_Design/Experiencia_y_loops.md) | Pilares actuales, decisiones, acciones y progresión |
| 03 — Mapa estratégico y ritmo | [Mapa y ritmo](03_Mapa_Estrategico/Mapa_y_ritmo.md) | Sustituye las fichas de niveles por regiones, conexiones y fases |
| 04 — Narrativa | [Mundo y narrativa sistémica](04_Narrativa/Mundo_y_narrativa.md) | Premisa actual y tono pendiente |
| 05 — Arte | [Dirección visual](05_Arte/Direccion_visual.md) | Lenguaje visual del prototipo; dirección final abierta |
| 06 — Audio | [Dirección de audio](06_Audio/Direccion_de_audio.md) | Efectos existentes y función sonora propuesta |
| 07 — UI/UX | [Interfaz y accesibilidad](07_UI_UX/Interfaz_y_accesibilidad.md) | Flujo actual, información crítica y validación pendiente |
| 08 — Programación y Technical Design | [Base técnica](08_Programacion_y_Technical_Design/Base_tecnica.md) | Motor, reglas, guardado y validación |
| 09 — Producción | [Alcance y validación](09_Produccion/Alcance_y_validacion.md) | Inventario actual, riesgos y siguientes comprobaciones |
| 10 — Público y negocio | [Público y distribución](10_Marketing_y_Negocio/Publico_y_distribucion.md) | Hipótesis de audiencia y decisiones comerciales abiertas |

## Referencia detallada de la implementación

Los capítulos previos se conservan como referencia de `alignment-1`; el nuevo índice
los integra sin duplicar sus reglas. Los cambios de `alignment-2` (2026-11-02) están en
[Experiencia y loops](02_Game_Design/Experiencia_y_loops.md#cambios-de-alignment-2--2026-11-02)
y en la fotografía de valores de [07 — Balance](07-balance.md); los capítulos afectados
llevan una nota al inicio. Están en inglés y describen la base implementada,
no el alcance comprometido de una futura versión.

| Documento | Pregunta que resuelve |
| --- | --- |
| [01 — Overview](01-overview.md) | Premisa, estados, resolución mensual y condiciones de victoria/derrota |
| [02 — Economy](02-economy.md) | Influence, Dread, seguidores y costes |
| [03 — Population](03-population.md) | Creencias, reclutamiento y conservación de población |
| [04 — Visibility and awareness](04-visibility.md) | Exposición regional y conocimiento global |
| [05 — Opposition](05-opposition.md) | Amenazas, oportunidades y respuesta del mundo |
| [06 — Ritual sites](06-ritual-sites.md) | Soporte, preparación e interrupción del rito |
| [07 — Balance values](07-balance.md) | Parámetros y fotografía fechada de valores |
| [08 — Design rationale](08-rationale.md) | Motivos de los cambios y evidencia registrada |
| [09 — Open problems](09-open-problems.md) | Problemas observados e hipótesis por volver a comprobar |

Los valores ejecutables viven en los datos y las reglas del código. Las fotografías
fechadas del GDD no los reemplazan. La relación entre las dos implementaciones está
explicada en [Base técnica](08_Programacion_y_Technical_Design/Base_tecnica.md).

## Historia y evidencia

Los diseños [MVP](../archive/GDD_mvp_proposal.md) y
[legacy](../archive/GDD_legacy_prototype.md) son antecedentes archivados. El
[plan de implementación](../implementation-plan.md), el
[registro de implementación](../implementation-checklist.md) y los
[playtests](../playtests/) aportan contexto y evidencia fechada. Algunas pruebas
corresponden a calendarios y economías anteriores: sus resultados no demuestran
el balance de la versión actual.
