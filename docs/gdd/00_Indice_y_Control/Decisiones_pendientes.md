# Decisiones pendientes

**Versión:** 0.8 · **Fecha:** 2026-11-22 · **Estado:** D01–D05 y D08–D17 confirmadas; D05 y D15 implementadas en `alignment-5` (juego 0.7.0); remates de D05 y D18–D20 tomadas en `alignment-6` (juego 0.8.0) y pendientes de revisión de dirección; D07 con respuesta parcial; D06 pendiente.

Este registro distingue las decisiones confirmadas de las preguntas abiertas.
La primera etapa del GDD documenta la implementación; los objetivos futuros
se registran sin presentarlos como características ya disponibles.

## Decisiones confirmadas por dirección — 2026-10-28

1. **D01 — Documentar primero lo implementado.** Describir `alignment-1` y la interfaz
   Godot (la base de entonces) antes de proponer cambios. Documentar algo existente no obliga a conservarlo
   en una versión posterior.
2. **D02 — Partidas de 15–30 minutos.** Es el objetivo de experiencia, no la duración
   comprobada del prototipo. Hoy hay unos siete minutos de simulación a 1× más pausas.
   El 2026-11-21 dirección precisó que el rango **incluye la pausa** y la planificación.
   Falta medir sesiones reales.
3. **D03 — Mezcla de estrategia, narrativa y horror.** Las tres dimensiones forman
   parte de la experiencia buscada, sin prioridad relativa fijada. La forma narrativa,
   el tono y la profundidad de cada dimensión siguen pendientes. La respuesta no
   compromete nuevos sistemas, eventos escritos ni cinemáticas.

## Decisiones confirmadas por dirección — 2026-11-21

Respuestas a la revisión de gameplay del 2026-11-20 (síntesis de siete revisores). Fijan la
dirección de las builds 0.6.0–0.8.0; no describen funciones ya disponibles salvo donde se
indica.

1. **D05 — El árbol de talentos vive dentro de cada partida.** No hay poder que persista
   entre partidas. **Cerrada en 0.7.0 (`alignment-5`):** reemplaza la doctrina; tres ramas
   (Congregation, Hidden hand, Harbinger) de raíz, dos nodos medios excluyentes y remate, 12
   nodos; sellos que se abren con el tiempo (6, 5 y 4 según la dificultad) y precio creciente
   en Influence, así que una partida no lo completa. Detalle en
   [Experiencia y loops](../02_Game_Design/Experiencia_y_loops.md#cambios-de-alignment-5--2026-11-21).
2. **D04 — Voz lovecraftiana y sucesos ficticios.** El horror habla con registro
   lovecraftiano. Los eventos escritos usan sucesos ficticios con sabor de 1908–1926, no
   hechos históricos reales.
3. **D02 — La duración incluye la pausa.** Los 15–30 minutos cuentan la pausa y la
   planificación.
4. **D13 — Los eventos escritos pausan el juego.** Llegan en 0.8.0.
5. **D14 — Se puede cerrar la partida cuando el rito ya no cabe.** Termina como derrota.
   Entra en 0.6.0 con `alignment-4`.
6. **D15 — Mysticism tiene efecto.** **Cerrada en 0.7.0 (`alignment-5`):** es un rasgo
   regional que actúa sobre manifestar. En una región de Mysticism m, manifestar suma
   `8 × (1 − (m − 50)/100)` de awareness y convierte un `(m − 50)` % más (o menos) de negadores
   y desprevenidos. Va de 6 de awareness en África a 10 en Europa. Se eligió manifestar porque
   es la acción que describe el rasgo («apertura a lo imposible») y porque no toca el
   reclutamiento, ya cargado por el árbol. El panel lo explica en una línea
   (`Derived.MysticismNote`).
7. **D16 — Easy no tiene que ganarse a la primera, pero no debe abrumar.**
8. **D17 — Definición de «completo».** Árbol probado, gente externa jugando, balance
   sentido, varios caminos para ganar y varias formas de perder.

## Decisiones de diseño de `alignment-6` — 2026-11-22

Las tomó el agente de reglas de 0.8.0 dentro del plan de la build. Responden a la primera
partida humana con 0.7.0: «el último nodo del árbol debería ser más game changer». Dirección
puede revisarlas.

1. **D05 (ampliación) — Cada remate abre otra forma de ganar.** The Great Procession (rito de 8
   meses con 5 regiones públicas), The world forgets (una vez: −25 de awareness y sin amenazas, a
   cambio de 6 meses de red dispersa) y Dreams of R'lyeh (cada sacrificio adelanta el rito). El
   bot gana con cada uno. Detalle en
   [Experiencia y loops](../02_Game_Design/Experiencia_y_loops.md#cambios-de-alignment-6--2026-11-22).
2. **D18 — Los remates de 0.7.0 salen del juego.** Los seis nodos medios ya los ocupan nodos
   obligatorios, así que ninguno de los remates viejos puede bajar de nivel. El efecto de A faith
   among faiths pasa a Open worship, solo para las células públicas, porque sin él el culto
   público dejaba de ser viable. Bought press y Offerings to the deep salen.
3. **D19 — Cada talento dice lo que rindió.** Es una línea por talento tomado, en la unidad del
   talento. Responde a «Martyrs no dio nada» sin cambiar Martyrs: primero se mide si el jugador
   lo nota.
4. **D20 — Normal más exigente sin castigar un error.** Enterrar sube 35 por uso, cada tirada
   de amenaza pesa +0,05 desde el mes 100 y la caza de sitios sube a +0,6. Easy pasa a la
   presión de Normal y una amenaza oculta le quita 5 meses. Ninguna regla nueva convierte un
   error en derrota: las tres suman amenazas que se pueden responder.

## Preguntas abiertas para las siguientes conversaciones

- **Identidad del jugador y tono.** ¿Quién dirige el culto? ¿El nombre actual es
  definitivo? El registro quedó en D04: voz lovecraftiana.
- **Profundidad entre partidas.** Respondido en D05: el árbol vive dentro de cada
  partida. Queda por decidir si hay escenarios o variantes entre partidas además de
  semilla, inicio, dificultad y talentos.
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
| D02 | Duración objetivo | Confirmada; falta medir | 15–30 minutos por partida — 2026-10-28. El rango incluye la pausa y la planificación — 2026-11-21 | Mapa y ritmo, balance y playtesting |
| D03 | Experiencia buscada | Confirmada | Mezcla de estrategia, narrativa y horror — 2026-10-28 | Pilares, narrativa, arte y audio |
| D04 | Identidad y tono | Confirmada | Se admiten sucesos escritos además de la crónica sistémica — 2026-11-12. Voz lovecraftiana; los sucesos son ficticios, con sabor de época, no históricos — 2026-11-21. Quién dirige el culto sigue abierto | Visión y narrativa |
| D05 | Progresión entre partidas | Confirmada e implementada | Dirección quiere un árbol de talentos que dé opciones y estrategias distintas, con puntos que el jugador gasta — 2026-11-12. El árbol vive dentro de cada partida, sin poder que persista entre partidas — 2026-11-21. Implementado en `alignment-5` (juego 0.7.0): 12 nodos en tres ramas, sellos por tiempo según la dificultad, reemplaza la doctrina — 2026-11-21. Remates nuevos en `alignment-6` (juego 0.8.0), véase D18 — 2026-11-22 | Game Design, balance y alcance |
| D06 | Presentación final | Pendiente | — | Arte, audio y producción |
| D07 | Público, idiomas y dispositivos | Parcial | Dispositivos resueltos en D12: escritorio para Steam y web para la beta — 2026-11-17. Público e idiomas siguen pendientes | Público, UX y plataforma |
| D08 | Entrega y recursos | Confirmada | Juego gratuito, corto y completo; no una demo ni un producto de pago — 2026-11-12. Plataformas en D12; fechas por definir | Producción y negocio |
| D09 | Época y estética de interfaz | Confirmada | Archivo del culto en un escritorio de noche, 1908–1926, con calendario real; tono de época solo en el marco, sin renombrar mecánicas — 2026-10-30 | Arte, UI/UX y narrativa |
| D10 | Decisiones económicas y registro de versiones | Confirmada | Mejorar las decisiones sin tocar los pilares: mantenimiento de sitios, ocultamiento según awareness y caza de sitios (`alignment-2`, juego 0.3.0). Todo cambio se refleja en el sistema de playtest y en el [registro de versiones](../../playtests/versiones.md) — 2026-11-02 | Game Design, balance y playtesting |
| D11 | Reestructura del minuto a minuto | Confirmada | Se pueden reestructurar verbos y agregar mecánicas para mejorar el game feel, sin automatizar acciones del jugador: se descartan las posturas por célula y reclutar sigue siendo manual. La primera partida no tiene que poder ganarse; el juego debe oponer resistencia. Primer paso: `alignment-3` (juego 0.4.0) — 2026-11-12 | Game Design, balance, UI/UX y playtesting |
| D12 | Motor y plataformas | Confirmada | El juego pasa de Godot a Unity 6.3 LTS (solo versiones LTS, nada experimental) para salir principalmente en **Steam** con un ejecutable de escritorio. La build web de itch.io queda para pruebas y para compartir la beta de forma sencilla y controlada con GTC Playtest Lab — 2026-11-17, confirmada el 2026-11-18. Juego 0.5.0 | Visión, base técnica, producción, público y UI/UX |
| D13 | Eventos escritos y tiempo | Confirmada | Los eventos escritos pausan el juego; llegan en 0.8.0 — 2026-11-21 | Narrativa, UI/UX y ritmo |
| D14 | Cierre voluntario | Confirmada | El jugador puede cerrar la partida, como derrota, cuando el rito ya no cabe — 2026-11-21. `alignment-4`, juego 0.6.0 | Game Design, rituales, UI/UX y playtesting |
| D15 | Mysticism | Confirmada e implementada | Tendrá efecto; se diseña con el árbol en 0.7.0 — 2026-11-21. En `alignment-5` (juego 0.7.0) cambia la awareness y las conversiones de manifestar: `8 × (1 − (m − 50)/100)` de awareness y `(m − 50)` % más o menos de conversiones — 2026-11-21 | Población, regiones, balance y Game Design |
| D16 | Dificultad Easy | Confirmada | Easy no tiene que ganarse en la primera partida, pero no debe abrumar — 2026-11-21 | Balance y playtesting |
| D17 | Definición de «completo» | Confirmada | Árbol probado, gente externa jugando, balance sentido, varios caminos para ganar y varias formas de perder — 2026-11-21 | Producción, alcance y playtesting |
| D18 | Remates de 0.7.0 | Tomada en `alignment-6`; falta revisión de dirección | Salen A faith among faiths, Bought press y Offerings to the deep; el efecto de A faith among faiths pasa a Open worship para las células públicas — 2026-11-22 | Game Design y balance |
| D19 | Rendimiento por talento | Tomada en `alignment-6`; falta revisión de dirección | Cada talento tomado dice en una línea lo que rindió en la partida — 2026-11-22 | Game Design, UI/UX y playtesting |
| D20 | Exigencia de Normal y Easy | Tomada en `alignment-6`; falta revisión de dirección | Normal: enterrar +35, presión +0,05 desde el mes 100, caza +0,6. Easy: presión ×1,0 y amenaza oculta de 5 meses. Bot: Normal 80/144, Easy 135, Hard 17 — 2026-11-22 | Balance y playtesting |

No convertir preguntas pendientes en funcionalidades comprometidas. Los problemas
observados del prototipo siguen en [Open problems](../09-open-problems.md).

[Volver al índice](../README.md)
