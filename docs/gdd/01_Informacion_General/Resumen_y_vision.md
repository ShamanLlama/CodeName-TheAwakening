# Resumen y visión

**Versión:** 0.6 · **Fecha:** 2026-11-25 · **Estado:** base implementada en Unity (`0.9.1` abierta como demo público en itch.io) y dos caminos de producción definidos.

## Concepto en una frase

Encarna a un Dios Antiguo (Cthulhu) entre 1908 y 1926: decide qué hacer con tus cultistas,
cuándo manifestarte y a quién controlar para preparar tu despertar antes de que pase la
alineación astral o la humanidad descubra la red.

## Identidad actual

- **Título de trabajo:** The Awakening of Cthulhu.
- **Estudio:** Shaman Llama (Shaman Llama Games / Shaman Llama SAS, Ecuador).
- **Género:** estrategia para un jugador sobre mapa mundial, con simulación en tiempo real,
  pausa activa y gestión de crecimiento y secreto.
- **Cámara y escala:** vista global de ocho regiones y gestión abstracta de población y
  células, sin unidades individuales.
- **Base jugable:** Unity 6.3 LTS desde 0.5.0. La versión vigente (`0.9.1`, publicada en
  itch.io el 2026-11-25) usa las reglas `alignment-7`, con árbol de talentos intra-partida,
  remates diferenciados y `Madness` como recurso regional. Godot (`0.2.0`–`0.4.0`) queda en
  la rama `godot` como referencia, y React/TypeScript (`src/`) es antecedente histórico.
- **Distribución actual:** demo público gratuito en web en
  `https://shamanllamagames.itch.io/the-awakening-of-cthulhu` (abierto desde 2026-11-24)
  para beta testing durante los próximos meses con GTC Playtest Lab (participación opcional).
- **Duración actual y objetivo:** unos 7,5 minutos de simulación a 1× sin pausas; el objetivo
  confirmado es de 15 a 30 minutos por partida contando pausas y planificación (D02). En
  partidas internas duró entre 14 y 22 minutos; falta medir sesiones externas en la beta.

## Propuesta jugable

Eliges dónde iniciar, cómo crecer, dónde sacrificar, cuándo ocultarte, cómo usar la locura
regional, qué ramas del árbol de talentos abrir y dónde preparar tres sitios rituales. Cada
seguidor produce Influence y sostiene rituales, pero también sube el conocimiento global. Las
amenazas te obligan a elegir entre monedas distintas (Influence, meses de inactividad o
Dread + awareness), y el cierre cambia según el remate que elijas.

## Objetivos confirmados y filosofía de diseño

- **Experiencia buscada (D03, D04, D09):** mezcla de estrategia, narrativa y horror en un
  archivo nocturno de culto entre 1908 y 1926, con voz lovecraftiana y sucesos ficticios. Los
  eventos escritos que pausan el juego (D13) quedan como futura feature ya diseñada a alto
  nivel.
- **Filosofía de balance (D25):** el ajuste fino de balance va al final del proyecto. Al ser
  un juego single-player, la prioridad es que se sienta bien, ofrezca opciones reales y haga
  sentir inteligente al jugador cuando resuelve la partida.
- **Presentación (D06):** el arte procedural y los SFX sintetizados actuales son temporales.
  El plan con financiamiento es contratar un diseñador gráfico/ilustrador y un ingeniero de
  audio en Ecuador.

## Dos caminos de producción y entrega (D26)

1. **Camino 1 (base propia, sin grant):** completar y pulir la experiencia de Cthulhu (un solo
   Dios Antiguo) y publicarla en itch.io, posiblemente gratuita.
2. **Camino 2 (con Unity Fellowship de 50.000 USD, ventana de 12 meses de marzo 2027 a marzo
   2028):** contratar 1 diseñador gráfico/ilustrador ($3.000 por 2 meses) y 1 ingeniero de
   audio ($3.000 por 2 meses); sumar a Nyarlathotep y Shub-Niggurath como futuros Old Gods;
   añadir eventos escritos y epílogos; hacer campaña orgánica en redes y asistir a gamescom
   latam (abr. 2027) y gamescom Alemania (ago. 2027); registrar el juego en Ecuador y EE. UU.,
   abrir LLC en EE. UU. y publicar en Steam en el Q1 2028.

El trabajo específico de cierre para Steam (Windows, Steamworks, mando, lector de pantalla e
idiomas) se deja para el tramo final del desarrollo (D07). Véanse
[Decisiones pendientes](../00_Indice_y_Control/Decisiones_pendientes.md) y
[Alcance, roadmap y validación](../09_Produccion/Alcance_y_validacion.md).

## Comprobación en beta abierta

Observar en las partidas públicas si una persona nueva entiende qué intenta lograr, por qué
crecer puede perjudicarla y si siente que sus elecciones de región, talentos y respuestas
cambian el curso de la partida.

Fuentes: [README](../../../README.md), [01. Overview](../01-overview.md),
[README de Unity](../../../unity/README.md) y [registro de versiones](../../playtests/versiones.md).

[Volver al índice](../README.md)
