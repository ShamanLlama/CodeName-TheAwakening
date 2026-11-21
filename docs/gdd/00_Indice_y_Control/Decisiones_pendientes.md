# Decisiones pendientes

**Versión:** 0.5 · **Fecha:** 2026-11-18 · **Estado:** D01–D03 y D08–D12 confirmadas; D04, D05 y D07 con respuesta parcial; D06 pendiente.

Este registro distingue las decisiones confirmadas de las preguntas abiertas.
La primera etapa del GDD documenta la implementación; los objetivos futuros
se registran sin presentarlos como características ya disponibles.

## Decisiones confirmadas por dirección — 2026-10-28

1. **D01 — Documentar primero lo implementado.** Describir `alignment-1` y la interfaz
   Godot (la base de entonces) antes de proponer cambios. Documentar algo existente no obliga a conservarlo
   en una versión posterior.
2. **D02 — Partidas de 15–30 minutos.** Es el objetivo de experiencia, no la duración
   comprobada del prototipo. Hoy hay unos siete minutos de simulación a 1× más pausas.
   Falta precisar si el rango objetivo incluye las pausas y medir sesiones reales;
   todavía no se ha elegido cómo alcanzar ese rango.
3. **D03 — Mezcla de estrategia, narrativa y horror.** Las tres dimensiones forman
   parte de la experiencia buscada, sin prioridad relativa fijada. La forma narrativa,
   el tono y la profundidad de cada dimensión siguen pendientes. La respuesta no
   compromete nuevos sistemas, eventos escritos ni cinemáticas.

## Preguntas abiertas para las siguientes conversaciones

- **Identidad del jugador y tono.** ¿Quién dirige el culto? ¿Horror cósmico sobrio,
  humor oscuro u otro registro? ¿El nombre actual es definitivo?
- **Profundidad entre partidas.** ¿Campañas independientes, desbloqueos persistentes,
  escenarios o una campaña conectada? Hoy varían semilla, inicio, dificultad y doctrina;
  no hay un árbol de progresión entre partidas documentado como sistema actual.
- **Presentación.** ¿El mapa y el arte procedural son la dirección final o una base
  temporal? ¿Queremos música, ilustraciones o una representación del despertar?
- **Público e idiomas.** ¿A quién queremos llegar primero y en qué idioma? Hoy los
  textos están en inglés. Los dispositivos quedan respondidos en D12.
- **Objetivo de entrega.** Respondido en D08 (gratuito, corto y completo) y D12 (Steam
  como salida principal). Quedan equipo, disponibilidad y fechas.

## Registro de respuestas

| ID | Tema | Estado | Respuesta y fecha | Documentos afectados |
| --- | --- | --- | --- | --- |
| D01 | Relación con el prototipo | Confirmada | Documentar primero lo implementado — 2026-10-28 | Visión, loops y alcance |
| D02 | Duración objetivo | Confirmada; medición por precisar | 15–30 minutos por partida — 2026-10-28 | Mapa y ritmo, balance y playtesting |
| D03 | Experiencia buscada | Confirmada | Mezcla de estrategia, narrativa y horror — 2026-10-28 | Pilares, narrativa, arte y audio |
| D04 | Identidad y tono | Parcial | Se admiten sucesos escritos además de la crónica sistémica — 2026-11-12. Quién dirige el culto y el registro del horror siguen pendientes | Visión y narrativa |
| D05 | Progresión entre partidas | Parcial | Dirección quiere un árbol de talentos que dé opciones y estrategias distintas, con puntos que el jugador gasta — 2026-11-12. Falta decidir si vive dentro de la partida o persiste entre partidas | Game Design y alcance |
| D06 | Presentación final | Pendiente | — | Arte, audio y producción |
| D07 | Público, idiomas y dispositivos | Parcial | Dispositivos resueltos en D12: escritorio para Steam y web para la beta — 2026-11-17. Público e idiomas siguen pendientes | Público, UX y plataforma |
| D08 | Entrega y recursos | Confirmada | Juego gratuito, corto y completo; no una demo ni un producto de pago — 2026-11-12. Plataformas en D12; fechas por definir | Producción y negocio |
| D09 | Época y estética de interfaz | Confirmada | Archivo del culto en un escritorio de noche, 1908–1926, con calendario real; tono de época solo en el marco, sin renombrar mecánicas — 2026-10-30 | Arte, UI/UX y narrativa |
| D10 | Decisiones económicas y registro de versiones | Confirmada | Mejorar las decisiones sin tocar los pilares: mantenimiento de sitios, ocultamiento según awareness y caza de sitios (`alignment-2`, juego 0.3.0). Todo cambio se refleja en el sistema de playtest y en el [registro de versiones](../../playtests/versiones.md) — 2026-11-02 | Game Design, balance y playtesting |
| D11 | Reestructura del minuto a minuto | Confirmada | Se pueden reestructurar verbos y agregar mecánicas para mejorar el game feel, sin automatizar acciones del jugador: se descartan las posturas por célula y reclutar sigue siendo manual. La primera partida no tiene que poder ganarse; el juego debe oponer resistencia. Primer paso: `alignment-3` (juego 0.4.0) — 2026-11-12 | Game Design, balance, UI/UX y playtesting |
| D12 | Motor y plataformas | Confirmada | El juego pasa de Godot a Unity 6.3 LTS (solo versiones LTS, nada experimental) para salir principalmente en **Steam** con un ejecutable de escritorio. La build web de itch.io queda para pruebas y para compartir la beta de forma sencilla y controlada con GTC Playtest Lab — 2026-11-17, confirmada el 2026-11-18. Juego 0.5.0 | Visión, base técnica, producción, público y UI/UX |

No convertir preguntas pendientes en funcionalidades comprometidas. Los problemas
observados del prototipo siguen en [Open problems](../09-open-problems.md).

[Volver al índice](../README.md)
