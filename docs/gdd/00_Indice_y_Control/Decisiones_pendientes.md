# Decisiones pendientes

**Versión:** 1.1 · **Fecha:** 2026-11-25 · **Estado:** D01–D06, D08–D26 confirmadas (todo lo implementado hasta `0.9.0` queda ratificado en el GDD); D07 parcial (cierre de tienda/idiomas aplazado al tramo final); mejoras y expansiones registradas como futuras features.

Este registro distingue las decisiones confirmadas de las preguntas abiertas y separa lo ya
implementado de lo planeado para etapas futuras.

## Decisiones confirmadas por dirección — 2026-11-25

1. **D06 — El arte procedural es temporal.** La dirección visual de archivo 1908–1926 se
   conserva como base, pero el dibujo procedural actual es temporal: el objetivo es contratar
   artistas para la UI y las artes del juego (y músico/diseñador de sonido con el grant).
2. **D13 — Eventos escritos quedan como característica futura.** Se mantienen en el diseño
   objetivo (pausan el juego, voz lovecraftiana y sucesos ficticios de 1908–1926), pero salen
   del hito vencido de 0.8.0 para diseñarse e implementarse más adelante.
3. **D12 / D07 (ampliación) — Beta pública abierta y preparación de Steam aplazada.** Desde el
   2026-11-24 el juego está abierto en itch.io para todo el público para hacer beta testing
   durante los próximos meses. Los trabajos específicos de publicación en Steam (build de
   Windows, Steamworks, soporte de mando, lector de pantalla e idiomas) se dejan para cuando
   el juego esté cerca de terminar.
4. **D25 — Filosofía de balance single-player.** El ajuste fino de balance se deja para una
   fase tardía. Al ser un juego para un jugador, no se busca simetría matemática perfecta
   («el balance es una ilusión»), sino que **el juego se sienta bien, dé opciones reales para
   elegir y haga que el jugador se sienta inteligente resolviéndolo**.
5. **D26 — Dos caminos de producción y lanzamiento.**
   - **Camino 1 (Sin grant / base propia):** completar el juego solo con **Cthulhu** (un solo
     Dios Antiguo), con la experiencia completa y probada, y publicarlo en itch.io
     (posiblemente sin precio / gratuito).
   - **Camino 2 (Con Unity Fellowship de 50.000 USD — en postulación, 12 meses desde marzo 2027, operando vía Shaman Llama SAS en Régimen General, Ecuador):**
     - contratar **1 diseñador gráfico / ilustrador** (UI + artes del juego + feedback visual,
       **$3.000 USD · 2 meses**);
     - contratar **1 ingeniero de audio** (banda sonora + SFX + feedback auditivo de horror,
       **$3.000 USD · 2 meses**);
     - incorporar **Nyarlathotep** (2.º Dios Antiguo) y **Shub-Niggurath** (3.er Dios Antiguo)
       como futuras features de alto nivel junto a **Cthulhu** (1.º, ya implementado);
     - realizar campaña en redes con contenido orgánico y asistir a **gamescom latam 2027**
       (22–25 abr 2027, ~$2.150 USD) y **gamescom Alemania 2027** (23–29 ago 2027, ~$3.950 USD
       como asistente Business, +$3.670 opcional si Indie Arena Booth);
     - registrar el juego en Ecuador (≤$1.000 USD) y Estados Unidos / LLC en EE. UU. (~$2.500
       USD) para cobrar en Steam y cubrir Steam Direct ($100).
   - **Pre-Grant (nov 2026 – feb 2027):** el estudio continúa trabajando y recogiendo feedback
     de la beta pública en itch.io; la contratación del equipo creativo y la producción a escala
     dependen de obtener el financiamiento.
   - El desglose vivo de presupuesto, alcance y cronograma de 12 meses se trabaja en
     [The_Awakening_Scope_Budget_Timeline.ods](../../../Grant-Unity/The_Awakening_Scope_Budget_Timeline.ods)
     (y su copia `.xlsx`).
6. **D04 (cierre de identidad) — El jugador juega como el Old God (Dios Antiguo).** El Dios
   Antiguo es quien decide qué hacer con sus cultistas, si aparecer/manifestarse y a quién
   controlar. Los eventos escritos (D13), los epílogos diferenciados y los ítems A–H de
   [09 — Open problems](../09-open-problems.md) quedan registrados como **futuras features**
   para analizar conforme avancen el diseño y la beta.
7. **Regla de trabajo del GDD y ratificación de D18–D24.** Todo lo que ya está implementado en
   `0.9.0` (incluidas D18–D24) queda ratificado como estado implementado en el GDD; las ideas
   de cambios o ampliaciones se manejan aparte como **futuras mejoras / features**.

## Decisiones confirmadas por dirección — 2026-10-28

1. **D01 — Documentar primero lo implementado.** Describir primero la base real antes de
   abrir rediseños. Documentar algo existente no obliga a conservarlo en versiones futuras.
2. **D02 — Partidas de 15–30 minutos.** Objetivo de experiencia que **incluye la pausa y la
   planificación** (precisado el 2026-11-21). Hoy hay unos 7 minutos de simulación a 1× más
   pausas; falta medir con jugadores externos de la beta abierta.
3. **D03 — Mezcla de estrategia, narrativa y horror.** Las tres dimensiones forman parte de la
   experiencia buscada.

## Decisiones confirmadas por dirección — 2026-11-21

Respuestas a la revisión de gameplay del 2026-11-20:

1. **D05 — El árbol de talentos vive dentro de cada partida.** Sin poder permanente entre
   partidas. **Implementada en `alignment-5` (0.7.0)** con 12 nodos en 3 ramas y remates
   actualizados en `alignment-6` (0.8.0).
2. **D04 — Voz lovecraftiana y sucesos ficticios.** El horror habla con registro lovecraftiano.
   Los eventos escritos usan sucesos ficticios con sabor de 1908–1926, no hechos históricos
   reales.
3. **D14 — Se puede cerrar la partida cuando el rito ya no cabe.** Termina como derrota
   (`conceded`). Implementada en 0.6.0 (`alignment-4`).
4. **D15 — Mysticism tiene efecto.** **Implementada en `alignment-5` (0.7.0):** modifica la
   awareness y conversiones de `manifest` según la región.
5. **D16 — Easy no tiene que ganarse a la primera, pero no debe abrumar.**
6. **D17 — Definición de «completo».** Árbol probado, gente externa jugando, balance sentido
   como justo y entretenido, varios caminos para ganar y varias formas de perder.

## Decisiones de `alignment-6` y `alignment-7` (ratificadas como implementadas — 2026-11-25)

Tomadas durante las builds `0.8.0` (2026-11-22) y `0.9.0` (2026-11-23) en respuesta a las
partidas internas; quedan ratificadas como implementación vigente. Cualquier ajuste posterior
se tratará como mejora futura (bajo D25):

1. **D18 (`0.8.0`) — Remates nuevos y retiro de los de 0.7.0.** Salen *A faith among faiths*,
   *Bought press* y *Offerings to the deep* (el efecto del primero pasa a *Open worship* solo
   para células públicas). Entran *The Great Procession* (rito de 8 meses con 5 regiones
   públicas), *The world forgets* (invocable una vez: −25 awareness y cancela amenazas a cambio
   de dispersar toda la red 6 meses) y *Dreams of R'lyeh* (sacrificios adelantan el rito).
2. **D19 (`0.8.0`) — Rendimiento visible por talento.** Cada talento tomado muestra una línea
   con lo que rindió en la partida.
3. **D20 (`0.8.0`) — Normal más exigente y Easy ajustado.** En Normal: `bury` sube +35 por uso,
   presión +0,05 desde el mes 100 y caza de sitios +0,6. En Easy: presión ×1,0 y amenaza oculta
   quita 5 meses.
4. **D21 (`0.9.0`) — Madness como recurso regional con precio.** `manifest` suma +20 y
   `sacrifice` +10; baja 1/mes. Rinde `Madness/20` Dread/mes y acelera sitios en `Madness/100`
   meses extra/mes; cuesta presión (`Madness/400`) y, desde 50, atrae ocultistas y +0,05
   awareness/mes.
5. **D22 (`0.9.0`) — Dreams of R'lyeh activo antes del rito.** Duplica la Madness de
   sacrificios, contagia +2/mes desde regiones con 30+ a vecinas con célula y adelanta el rito
   2 meses (en vez de 1) si la región tiene 30+ Madness.
6. **D23 (`0.9.0`) — Respiro de 5 meses tras responder.** Obstruir o silenciar deja la región
   5 meses sin tiradas de amenaza; para compensar, Normal pasa a presión ×1,45 y Hard a ×1,15.
7. **D24 (`0.9.0`) — Easy con sitios de 20 meses y talentos más baratos.** Easy: sitios de 20
   meses y talentos `30 + 20n`. Normal y Hard: talentos `40 + 25n` (antes `+30n`).

## Preguntas abiertas para las siguientes etapas

- **Diseño de alto nivel de Nyarlathotep y Shub-Niggurath (Camino 2):** documentado en
  [Old_Gods.md](../02_Game_Design/Old_Gods.md) como futuras features; su diseño mecánico
  detallado entra durante el ciclo financiado.
- **Diseño de eventos escritos (D13) y epílogos:** estructura cerrada el 2026-11-25 en
  [Mundo_y_narrativa.md](../04_Narrativa/Mundo_y_narrativa.md) (5–8 eventos/partida, 3–5
  opciones con consecuencias visibles, pool compartido 1908–1926 + eventos por Old God);
  implementación durante el ciclo financiado.
- **Cierre de tienda e idiomas (D07):** aplazado hasta acercarse al final del desarrollo.

## Registro de respuestas

| ID | Tema | Estado | Respuesta y fecha | Documentos afectados |
| --- | --- | --- | --- | --- |
| D01 | Relación con el prototipo | Confirmada | Documentar primero lo implementado — 2026-10-28 | Visión, loops y alcance |
| D02 | Duración objetivo | Confirmada; en medición | 15–30 minutos por partida — 2026-10-28; incluye pausa y planificación — 2026-11-21 | Mapa y ritmo, balance y playtesting |
| D03 | Experiencia buscada | Confirmada | Mezcla de estrategia, narrativa y horror — 2026-10-28 | Pilares, narrativa, arte y audio |
| D04 | Identidad y tono | Confirmada | Voz lovecraftiana y sucesos ficticios 1908–1926 — 2026-11-21. El jugador juega como el **Old God** (decide sobre cultistas, apariciones y control) — 2026-11-25 | Visión y narrativa |
| D05 | Árbol de talentos | Confirmada e implementada | Vive dentro de cada partida, sin metaprogresión; 12 nodos en 3 ramas — 2026-11-21 (`alignment-5`) y remates en `alignment-6` | Game Design, balance y alcance |
| D06 | Presentación final (arte/audio) | Confirmada | Arte procedural es temporal; contratar artistas (UI y juego) y músico/sonidista para calidad final y feedback visual/auditivo — 2026-11-25 | Arte, audio y producción |
| D07 | Público, idiomas y dispositivos | Parcial (aplazado cierre) | Beta pública en itch.io desde 2026-11-24; trabajo de tienda Steam, Windows, mando, lector de pantalla e idiomas se deja para el tramo final — 2026-11-25 | Público, UX y base técnica |
| D08 | Entrega y modelo | Actualizada por D26 | Gratuito en Camino 1 (itch.io); comercial/Steam en Camino 2 con Unity Grant $50k — 2026-11-25 | Producción y negocio |
| D09 | Época y estética | Confirmada | Archivo del culto en escritorio de noche, 1908–1926, calendario real; sin renombrar mecánicas — 2026-10-30 | Arte, UI/UX y narrativa |
| D10 | Economía y registro de versiones | Confirmada | Mantenimiento de sitios, ocultar según awareness y caza de sitios (`alignment-2`); registro en `versiones.md` — 2026-11-02 | Game Design, balance y playtesting |
| D11 | Minuto a minuto | Confirmada | Sin automatizar acciones del jugador; reclutar manual; el juego opone resistencia (`alignment-3`) — 2026-11-12 | Game Design, balance y UI/UX |
| D12 | Motor y plataformas | Confirmada | Unity 6.3 LTS; beta pública web en itch.io; Steam como salida de escritorio en Camino 2 — 2026-11-18 / 2026-11-25 | Visión, base técnica y público |
| D13 | Eventos escritos y pausa | Confirmada (futura feature) | Los eventos escritos pausan el juego; quedan como futura feature a diseñar e implementar — 2026-11-21 / 2026-11-25 | Narrativa, UI/UX y alcance |
| D14 | Cierre voluntario | Confirmada e implementada | Cerrar como derrota (`conceded`) cuando el rito ya no cabe (`alignment-4`, 0.6.0) — 2026-11-21 | Game Design, rituales y UI/UX |
| D15 | Mysticism | Confirmada e implementada | Cambia awareness y conversiones de `manifest` según región (`alignment-5`, 0.7.0) — 2026-11-21 | Población, regiones y balance |
| D16 | Dificultad Easy | Confirmada | No debe ganarse a la primera necesariamente, pero no debe abrumar — 2026-11-21 | Balance y playtesting |
| D17 | Definición de «completo» | Confirmada | Árbol probado, beta con externos, sensación de opciones/balance, varios caminos para ganar y perder — 2026-11-21 | Producción y alcance |
| D18 | Remates de `alignment-6` | Implementada y ratificada | Procesión, The world forgets y Dreams of R'lyeh reemplazan a los remates de 0.7.0 — 2026-11-22 / 2026-11-25 | Game Design y balance |
| D19 | Rendimiento por talento | Implementada y ratificada | Cada talento tomado muestra en una línea lo que rindió — 2026-11-22 / 2026-11-25 | Game Design y UI/UX |
| D20 | Exigencia de Normal y Easy (`0.8.0`) | Implementada y ratificada | Normal: `bury` +35, presión +0,05 desde mes 100, caza +0,6; Easy: presión ×1,0 y pérdida oculta 5 meses — 2026-11-22 / 2026-11-25 | Balance |
| D21 | Madness como recurso (`0.9.0`) | Implementada y ratificada | Dread pasivo, aceleración de sitios y precio en ocultistas/awareness — 2026-11-23 / 2026-11-25 | Población, economía y oposición |
| D22 | Dreams of R'lyeh antes del rito (`0.9.0`) | Implementada y ratificada | Doble Madness en sacrificios, contagio entre vecinas y +2 meses de rito con 30+ Madness — 2026-11-23 / 2026-11-25 | Game Design y rituales |
| D23 | Respiro tras responder (`0.9.0`) | Implementada y ratificada | 5 meses sin amenazas tras obstruir/silenciar; presión Normal ×1,45 y Hard ×1,15 — 2026-11-23 / 2026-11-25 | Oposición y balance |
| D24 | Easy y precio de talentos (`0.9.0`) | Implementada y ratificada | Easy: sitios de 20 meses y talentos `30 + 20n`; Normal/Hard: paso +25 — 2026-11-23 / 2026-11-25 | Balance y rituales |
| D25 | Filosofía de balance | Confirmada | Balance fino en fase tardía; en single-player importa que se sienta bien, dé opciones y haga sentir inteligente al jugador — 2026-11-25 | Game Design, balance y alcance |
| D26 | Dos caminos de producción | Confirmada | Camino 1: solo Cthulhu, completo en itch.io (posible gratis). Camino 2: con Unity Grant $50k y equipo en Ecuador → arte/UI, audio/música, +1–2 Old Gods, eventos/redes, registro EC/US, LLC y Steam — 2026-11-25 | Visión, arte, audio, producción y negocio |

Los problemas observados y los ítems diferidos siguen en [09 — Open problems](../09-open-problems.md).

[Volver al índice](../README.md)
