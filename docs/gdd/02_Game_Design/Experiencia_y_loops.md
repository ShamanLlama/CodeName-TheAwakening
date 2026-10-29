# Experiencia, loops y sistemas

**Versión:** 0.2 · **Fecha:** 2026-10-28 · **Estado:** síntesis de `alignment-1`; estrategia, narrativa y horror como objetivo confirmado.

## Pilares de la base actual

1. **Crecer tiene consecuencias.** Los seguidores sostienen ingresos y rituales,
   pero elevan el conocimiento global del culto.
2. **Defender exige elegir.** Obstruir una amenaza siempre funciona si la acción es
   legal, pero bloquea temporalmente otra obstrucción en esa región.
3. **La ubicación cambia el plan.** La población y las instituciones locales modifican
   reclutamiento, oposición y coste de apoyo ritual.
4. **El jugador necesita leer los plazos.** Alineación, preparación y amenazas deben
   permitir anticipar una pérdida. Es una intención que todavía requiere validar UX.

Estos pilares se recuperan del GDD existente y describen la base estratégica actual.
Dirección confirmó una mezcla de estrategia, narrativa y horror, sin jerarquía entre
las tres dimensiones. La crónica, las amenazas y los rituales aportan la ficción
actual; queda por evaluar si producen el efecto narrativo y de horror buscado.
Primero se documenta lo implementado, según D01–D03 del
[registro de decisiones](../00_Indice_y_Control/Decisiones_pendientes.md).

## Core loop

Leer el estado regional y los plazos → elegir dónde invertir, crecer o contenerse →
obtener seguidores e Influence → sacrificar parte de la red para obtener Dread →
preparar y sostener sitios → responder a las consecuencias y ajustar el plan.

El motor avanza por meses. Las acciones inmediatas no consumen meses por sí mismas;
recursos, condiciones y cooldowns limitan su uso. El jugador puede planificar y actuar
en pausa.

## Loops de apoyo y decisiones

| Loop | Decisión recurrente | Ganancia y coste |
| --- | --- | --- |
| Crecimiento | Reclutar en secreto o hacer público el culto | Más seguidores frente a evidencia y oposición |
| Expansión | Abrir una célula en una región conectada | Nuevo potencial económico y ritual; más frentes |
| Sacrificio | Consumir seguidores y elegir dónde hacerlo | Dread inmediato; menos ingreso y posible pérdida de apoyo |
| Contención | Ocultar evidencia u obstruir una amenaza | Menor presión o daño evitado; gasto y cooldown |
| Creencia | Manifestar lo imposible | Más población reclutable; mayor atención y resistencia |
| Oportunidades | Aprovechar un presagio o convertir una figura | Dread, aceleración o beneficio secreto; coste y plazo |
| Final | Conservar sitios listos y recuperar interrupciones | Avance del rito frente al calendario y amenazas |

Sacrificar reduce el crecimiento futuro de awareness al reducir el culto; no elimina
el conocimiento ya acumulado. La acción de ocultar evidencia sí puede reducirlo.

## Acciones y progresión actuales

El conjunto actual tiene doce acciones: `recruit`, `expand`, `sacrifice`, `reveal`,
`hide`, `conceal`, `manifest`, `prepare`, `counter`, `exploit`, `convert` y
`reveal_figure`. Sus condiciones y costes están en los datos y en `previewAction`;
las referencias detalladas del índice explican cada sistema.

Una doctrina por partida especializa reclutamiento (**Congregation**), infiltración
(**Hidden hand**) o terror ritual (**Harbinger**). La progresión consiste en ampliar
la red, financiar sitios y alcanzar el final; no se documenta un sistema actual de
desbloqueos permanentes entre partidas.

Se gana completando el rito dentro de la ventana con suficientes sitios listos y
apoyados. Se pierde por conocimiento global máximo o por agotar el calendario.
El progreso del rito se conserva durante interrupciones; un sitio puede perder
preparación por amenazas y se destruye si su célula pierde todos los seguidores.

## Comprobación propuesta

Observar si una persona cambia su plan después de un sacrificio o amenaza y puede
explicar la relación entre seguidores, ingreso y apoyo ritual. Registrar qué acciones
ignora y por qué. Las simulaciones anteriores no demuestran comprensión humana.

Fuentes: [acciones](../../../src/data/abilities.ts), [motor](../../../src/game/engine.ts),
[razones del diseño](../08-rationale.md) y [problemas abiertos](../09-open-problems.md).

[Volver al índice](../README.md)
