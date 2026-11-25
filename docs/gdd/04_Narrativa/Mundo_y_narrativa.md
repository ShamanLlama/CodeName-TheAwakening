# Mundo y narrativa sistémica

**Versión:** 0.7 · **Fecha:** 2026-11-25 · **Estado:** narrativa sistémica implementada; identidad del jugador (Old God) y estructura de eventos escritos (D13) cerradas a nivel de diseño; implementación de eventos escritos, epílogos y feedback audiovisual planeada como futura feature.

## Premisa e identidad del jugador (D04)

Juegas como un Old God (Dios Antiguo). Hoy encarnas a Cthulhu (`0.9.1`) y, con el
financiamiento del grant, podrás elegir también a Nyarlathotep o Shub-Niggurath
([Old_Gods.md](../02_Game_Design/Old_Gods.md)). Entre enero de 1908 y septiembre de 1926
decides qué hacer con tus cultistas, cuándo manifestarte ante los mortales y a quién controlar
para despertar durante la alineación astral (alrededor del 23 de marzo de 1925, fecha del
surgimiento de R'lyeh en *The Call of Cthulhu*).

La humanidad responde a las señales, al tamaño del culto, a la locura regional y a los
rituales; si te descubre por completo, la red cae. Las figuras convertibles son arquetipos
genéricos (*celebrated artist* y *minister*).

## Qué está implementado hoy (`0.9.1`)

- **Crónica sistémica:** la historia de cada partida sale de tus acciones, manifestaciones,
  amenazas, respuestas, oportunidades, sitios interrumpidos, talentos y desenlace.
- **Marco de época (D09):** la interfaz presenta el archivo secreto de 1908–1926 (expedientes,
  recortes de prensa, telegramas, libro mayor y fechas reales), sin renombrar mecánicas para
  que todo siga siendo legible.
- **Hecho fechado al cierre:** la pantalla final resume la causa de victoria o derrota con un
  dato fechado sacado de la propia partida (`Notes.DatedFact`).

## Diseño confirmado de eventos escritos y epílogos (D13, futura feature)

Definido por dirección el 2026-11-25 para implementarse en el ciclo financiado:

- **Frecuencia:** 5 a 8 eventos por partida (uno cada 25 a 35 meses) para sumar narrativa y
  horror sin estirar la partida fuera de los 15–30 minutos.
- **Interactividad y claridad mecánica:** cada evento pausa el reloj y muestra una hoja o
  telegrama en voz lovecraftiana sobre un suceso ficticio de 1908–1926 (sin hechos históricos
  reales), con entre 3 y 5 opciones. Cada opción muestra su coste, efecto y riesgo exactos
  antes de hacer clic.
- **Dos pools de contenido:**
  1. **Pool compartido de época (1908–1926):** sucesos disparados por hitos globales o
     regionales válidos para cualquier dios (umbrales de `Awareness` 25/50/75, primera región
     con `Madness ≥ 50`, financiación del primer sitio, apertura de la alineación).
  2. **Eventos propios de cada Old God:** sucesos exclusivos que refuerzan la identidad de
     cada dios (Cthulhu primero; luego Nyarlathotep y Shub-Niggurath).
- **Epílogos diferenciados:** una hoja final escrita según el Old God, el remate con el que
  ganaste (*The Great Procession*, *The world forgets*, *Dreams of R'lyeh* o rito estándar) o
  la causa de derrota (`world_awareness`, `deadline`, `conceded`).
- **Feedback visual y auditivo progresivo (D06):** al incorporar al diseñador gráfico/ilustrador
  y al ingeniero de audio, el aspecto del escritorio, las viñetas de eventos, la música
  adaptativa y el ambiente sonoro reaccionarán al avance de `Awareness`, `Madness` y la
  cercanía de 1925.

### Ejemplo de referencia (no implementado aún)

> **TELEGRAMA · MARZO 1914: «Un vapor ballenero regresa sin tripulación al Pacífico Sur»**
> *(Disparador: primera región con `Madness ≥ 50`, pool de Cthulhu)*
> Un casco a la deriva aparece frente a las costas de Oceanía con diarios llenos de geometría
> imposible. Los periódicos locales ya preguntan qué ocurrió cerca de `47°9′S`.
> - **1. Reivindicar el presagio:** +15 `Madness` en Oceanía, +40 `Believers`, +6 `Awareness`.
> - **2. Hundir el expediente en la capitanía:** −35 `Influence`, −10 `Exposure`, 0 `Awareness`.
> - **3. Alimentar los sueños de los estibadores:** −20 `Followers` locales, +20 `Dread`.
> - **4. Dejar que la historia se enfríe:** sin coste; +3 `Awareness` y aparece una oportunidad de `nightmare` en la región.

## Comprobación en beta abierta

Durante la beta pública (dic. 2026 – feb. 2027) revisaremos partidas reales y comentarios para
ver en qué tramos se aplana el ritmo y ubicar ahí los disparadores de eventos escritos.

Fuentes: [01. Overview](../01-overview.md), [Old_Gods.md](../02_Game_Design/Old_Gods.md),
[GameData.cs](../../../unity/Assets/Awakening/Core/Rules/GameData.cs),
[Director.cs](../../../unity/Assets/Awakening/Core/Rules/Director.cs) y
[Dialogs.cs](../../../unity/Assets/Awakening/UI/Screens/Dialogs.cs).

[Volver al índice](../README.md)
