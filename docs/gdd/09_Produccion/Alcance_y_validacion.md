# Alcance, roadmap y validación

**Versión:** 0.7 · **Fecha:** 2026-11-25 · **Estado:** beta pública abierta en itch.io (`0.9.1`, `alignment-7`); roadmap de 12 meses (marzo 2027 a marzo 2028) alineado al Unity Fellowship.

## Dónde estamos hoy (completado en `0.9.1`)

Existe un ciclo completo y jugable de Cthulhu en Unity 6.3 LTS (`alignment-7`):
- Mapa mundial de ocho regiones, economía, conservación de población, 16 acciones, amenazas
  con tres respuestas y respiro de 5 meses, y oportunidades.
- Árbol de talentos intra-partida (3 ramas × 4 nodos = 12 nodos, 3 remates diferenciados y
  rendimiento visible por talento), `Mysticism` y `Madness` como recurso regional activo.
- Previsión y margen del rito, aviso de rito detenido, cierre voluntario (`conceded`), 4
  finales, guardado `save-v2`, modo táctil/compacto, 310 pruebas automatizadas de reglas y
  60/60 comprobaciones de layout.
- Desde el 2026-11-24 la build web está abierta al público en itch.io (`v0.9.1` desde el
  2026-11-25) como demo gratuito con telemetría opcional en GTC Playtest Lab (`awakening-6`).

## Dos caminos de producción (D26)

| Dimensión | Camino 1 (base propia, sin grant) | Camino 2 (Unity Fellowship, $50.000 USD en 12 meses) |
| --- | --- | --- |
| Contenido jugable | Completar y pulir Cthulhu (1 Old God) con mejoras de la beta abierta | Todo lo del Camino 1 + Nyarlathotep y Shub-Niggurath ([Old_Gods.md](../02_Game_Design/Old_Gods.md)) + eventos escritos (D13) y epílogos |
| Arte y UI | Base procedural actual pulida | Contratar 1 diseñador gráfico/ilustrador ($3.000 por 2 meses en Ecuador) para UI, mapa, ilustraciones y feedback visual de horror |
| Audio | SFX procedurales actuales | Contratar 1 ingeniero de audio ($3.000 por 2 meses en Ecuador) para OST adaptativa, SFX y ambiente de horror |
| Plataforma | itch.io (web/descargable), posiblemente gratis | Steam (escritorio Windows/Linux, Q1 2028) + itch.io como demo |
| Legal y tienda | Sin estructura societaria adicional | Recibido en Shaman Llama SAS (Régimen General, Ecuador); registro IP en Ecuador y EE. UU., LLC en EE. UU. y Steam Direct |
| Difusión | Orgánica en comunidad y beta abierta | Campaña orgánica en redes, gamescom latam 2027 (abr. 2027), gamescom Alemania 2027 (ago. 2027) y Steam Next Fest |

## Roadmap y cronograma (Pre-Grant + 12 meses de Fellowship)

| Etapa | Ventana | Estado | Qué ocurre y qué se entrega |
| --- | --- | --- | --- |
| Base entregada | Sep – nov 2026 | Completado | Migración a Unity 6.3 LTS, vertical slice completo de Cthulhu (`0.5.0` a `0.9.1`), demo público en itch.io, telemetría `awakening-6`, 310 tests y 60/60 layout checks. |
| Pre-Grant (beta abierta) | Dic 2026 – feb 2027 | En curso | Seguir iterando el juego con feedback real de la beta pública (decisiones, dificultades elegidas, ramas de talentos y victorias/derrotas) y dejar cerrados los briefs de contratación. |
| Meses 1–3 (producción creativa) | Mar – may 2027 | Con grant | Arranque del Fellowship (GDC mar. 2027). Contratos de 2 meses: diseñador gráfico/ilustrador ($3k) e ingeniero de audio ($3k). Presencia en gamescom latam 2027 (22–25 abr. 2027). Arquitectura multi-dios y motor de eventos escritos (D13). |
| Meses 4–6 (integración y Dios #2) | Jun – ago 2027 | Con grant | Integración de UI final, mapa ilustrado, OST y SFX con feedback progresivo de horror. Implementación de Nyarlathotep (2.º Old God) y primera tanda de eventos escritos. Presencia en gamescom Alemania 2027 (23–29 ago. 2027). |
| Meses 7–9 (Dios #3, narrativa y tienda) | Sep – nov 2027 | Con grant | Implementación de Shub-Niggurath (3.er Old God), pool completo de eventos y epílogos diferenciados. Registro legal EC/US, LLC en EE. UU., página *Coming Soon* en Steam y campaña orgánica. |
| Meses 10–12 (polish, Steam y 1.0) | Dic 2027 – mar 2028 | Con grant | Pase tardío de sensación y balance (D25), build de Windows, Steamworks, accesibilidad (mando, teclado y lector de pantalla), localización, Steam Next Fest y lanzamiento 1.0 en Q1 2028. |

Hoja editable con fórmulas: [The_Awakening_Scope_Budget_Timeline.ods](../../../Grant-Unity/The_Awakening_Scope_Budget_Timeline.ods).

## Filosofía de balance y criterios de la beta abierta (D17, D25)

- **Definición de «completo» (D17):** árbol probado, gente externa jugando, balance que se
  sienta satisfactorio, varios caminos para ganar y varias formas de perder.
- **Filosofía de balance (D25):** el ajuste fino va al final. Como juego single-player, la
  meta es que se sienta bien, dé opciones reales y haga sentir inteligente al jugador cuando
  resuelve la partida.
- **Qué medimos en la beta abierta (`awakening-6`):** decisiones de los jugadores,
  modalidades/dificultades elegidas, nodos del árbol de talentos y tasa de victorias/derrotas
  (junto con duración real frente al objetivo de 15–30 minutos con pausas).

## Riesgos principales y mitigación

| Riesgo | Contexto | Mitigación |
| --- | --- | --- |
| Pocos datos externos todavía | Partidas humanas previas son internas; el demo público abrió el 2026-11-24 | Tres meses de beta abierta (dic. 2026 – feb. 2027) antes de arrancar el Fellowship |
| Retención fiscal US sobre el grant | Ecuador no tiene tratado con EE. UU. y el FAQ de Unity advierte posible *withholding tax* | Consulta enviada a `fellowship@unity3d.com` y reserva bancaria/IVA/ISD (~$1.500) en Shaman Llama SAS |
| Crecimiento de alcance con 3 Old Gods | Nyarlathotep y Shub-Niggurath deben dar variedad sin duplicar el motor | Ambos corren sobre el mismo núcleo `Core/Rules` y mapa de 8 regiones, y entran solo con financiamiento |
| Sensibilidad del respiro y `Madness` nueva | Medido solo con bot en `0.9.0` | Observar en beta abierta y ajustar en el pase tardío de balance (meses 10–12) |

Los ítems diferidos y el backlog de futuras features están en [09. Open problems](../09-open-problems.md).

[Volver al índice](../README.md)
