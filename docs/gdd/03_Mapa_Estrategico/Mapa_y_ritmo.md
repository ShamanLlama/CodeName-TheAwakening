# Mapa estratégico y ritmo de partida

**Versión:** 0.2 · **Fecha:** 2026-10-28 · **Estado:** mapa y calendario implementados; objetivo de 15–30 minutos confirmado.

## Función del mapa

La partida se desarrolla en un mapa mundial de ocho regiones conectadas. Las
conexiones limitan la expansión mientras existe alguna célula. Si toda la red cae,
la acción de expansión permite reconstruir sin esa exigencia de vecindad, siempre
que queden recursos y población elegible. No hay una secuencia de niveles.

Las capacidades son abstracciones de juego. La tabla es una fotografía de los datos
al 2026-10-28; no describe población ni instituciones reales.

| Región visible | ID | Peso poblacional | Seguidores para apoyar un sitio hoy |
| --- | --- | ---: | ---: |
| North America | `north_america` | 14 | 350 |
| Latin America | `latin_america` | 12 | 300 |
| Europe | `europe` | 13 | 325 |
| Africa | `africa` | 15 | 375 |
| West Asia | `middle_east` | 10 | 250 |
| South Asia | `south_asia` | 18 | 450 |
| East Asia | `east_asia` | 15 | 375 |
| Oceania | `oceania` | 3 | 75 |

La capacidad parte del peso regional y el requisito ritual se calcula sobre ella.
Las adyacencias y los rasgos se mantienen en [regiones](../../../src/data/regions.ts)
y su equivalente [GameData](../../../godot/core/game_data.gd), evitando duplicar el
grafo completo en prosa. La geometría usa [Natural Earth](../../../godot/data/NATURAL_EARTH.md).

Las regiones de mayor población ofrecen reservas de reclutamiento y exigen conservar
más seguidores para un sitio. Escepticismo e instituciones cambian la población
disponible y la oposición. No hay campañas regionales ni sucesos exclusivos por región
comprometidos para una futura entrega.

## Fases y cadencia actuales

1. **Colocación:** elegir región inicial; el tiempo no avanza.
2. **Preparación:** desarrollar la red, conseguir Dread y preparar sitios.
3. **Alineación:** pausa automática para revisar si se puede iniciar el rito.
4. **Rito:** sostener suficientes sitios listos, responder a golpes y recuperar apoyo.
5. **Resultado:** explicar victoria o causa de derrota y permitir otra partida.

Fotografía actual: 200 meses de preparación, 24 de alineación y 12 meses acumulados
para completar el rito. Cada sitio requiere 24 meses apoyados. A 1× un mes dura dos
segundos reales, de modo que el límite completo supone 448 segundos de simulación.
Las pausas aumentan el tiempo real de sesión. Los valores se consultan en
[balance](../../../src/data/balance.ts).

La alineación impide ganar antes de su apertura, aunque los sitios estén listos.
Interrumpir el rito conserva su progreso mientras el calendario sigue corriendo.
Cambiar la duración exige revisar también frecuencias de amenazas, advertencias,
derivas y cooldowns; no basta con alargar el contador.

## Objetivo de duración y prueba de ritmo

**Confirmado por dirección:** partidas de **15–30 minutos**. El prototipo tiene unos
siete minutos de simulación a 1×, más pausas; no hay una medición actual de sesión
real que permita compararlo directamente con el rango objetivo.

**Por precisar:** si el objetivo incluye pausas, cuánto tiempo corresponde a
planificación y lectura, y cuántas decisiones debe sostener cada fase. No se ha
elegido un calendario nuevo ni cambiado la velocidad de simulación.

**Propuesta de comprobación posterior a documentar la base:** medir tiempo total,
pausas, lectura de eventos, periodos sin decisiones, comprensión del plan ritual
y recuperación. Evaluar juntos ritmo estratégico, narrativa y horror antes de
decidir cómo alcanzar el rango objetivo. Alargar el contador por sí solo no
demuestra que la partida tenga más decisiones ni mejor experiencia.

La [referencia de rituales](../06-ritual-sites.md) explica interrupciones y plazos;
el [rationale](../08-rationale.md) conserva el motivo de la compresión del calendario.

[Volver al índice](../README.md)
