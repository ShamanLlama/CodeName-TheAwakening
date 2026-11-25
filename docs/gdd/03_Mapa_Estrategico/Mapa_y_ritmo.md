# Mapa estratégico y ritmo de partida

**Versión:** 0.4 · **Fecha:** 2026-11-25 · **Estado:** mapa y calendario implementados (`alignment-7`); duración objetivo de 15–30 min con pausas en validación durante la beta abierta.

## Función del mapa

La partida se desarrolla en un mapa mundial de ocho regiones conectadas. Las conexiones
limitan la expansión mientras exista alguna célula activa. Si toda la red cae, `expand`
permite reconstruir en cualquier región elegible siempre que haya recursos y población
reclutable. No hay secuencia de niveles lineales.

Las capacidades son abstracciones de juego (fotografía de
[GameData.cs](../../../unity/Assets/Awakening/Core/Rules/GameData.cs) al 2026-11-25; no
describen demografía ni instituciones reales):

| Región visible | ID | Peso poblacional | Mysticism | Resistencia institucional | Seguidores para apoyar un sitio |
| --- | --- | ---: | ---: | ---: | ---: |
| North America | `north_america` | 14 | 35 | 75 | 350 |
| South America *(antes Latin America en 0.9.0)* | `latin_america` | 12 | 65 | 45 | 300 |
| Europe | `europe` | 13 | 30 | 80 | 325 |
| Africa | `africa` | 15 | 70 | 35 | 375 |
| West Asia | `middle_east` | 10 | 60 | 70 | 250 |
| South Asia | `south_asia` | 18 | 65 | 50 | 450 |
| East Asia | `east_asia` | 15 | 35 | 85 | 375 |
| Oceania | `oceania` | 3 | 55 | 45 | 75 |

La capacidad regional es `PopulationWeight × 100` (10.000 personas en todo el mundo) y el
requisito de apoyo de un sitio es `max(60, round(capacidad × 0,25))`. Las adyacencias y
etiquetas viven en [GameData.cs](../../../unity/Assets/Awakening/Core/Rules/GameData.cs).
La geometría usa [Natural Earth](../../../unity/Assets/Awakening/Licenses/NATURAL_EARTH.md).

Las regiones pobladas sirven mejor para reclutar, generar Influence y sacrificar; las
regiones pequeñas exigen menos seguidores para sostener un sitio ritual. Escepticismo,
resistencia institucional y `Mysticism` cambian el muro inicial de negadores, la presión de
amenazas y el coste/efecto de manifestar (`alignment-5`).

## Fases y cadencia actuales

1. **Colocación (`placing`):** elegir región inicial; el tiempo no avanza.
2. **Preparación (`playing`):** meses 0–199 (enero 1908 – agosto 1924). Expandir la red,
   tomar talentos al abrirse sellos, acumular Dread, gestionar `Madness` y preparar 3 sitios.
3. **Alineación (`alignment`):** mes 200 (septiembre 1924). Pausa automática para revisar si
   el rito puede empezar. La ventana dura 24 meses (hasta mes 224, septiembre 1926).
4. **Rito final (`awakening`):** sostener 3 sitios listos durante **12 meses acumulados**
   (u **8 meses** con *The Great Procession*, y adelantable con *Dreams of R'lyeh*), mientras
   sube la caza de sitios (`+0,6` de presión).
5. **Resultado (`won` / `lost`):** victoria (`awakened`) o derrota (`world_awareness`,
   `deadline`, o cierre voluntario `conceded` cuando el rito ya no cabe).

Fotografía vigente en [Balance.cs](../../../unity/Assets/Awakening/Core/Rules/Balance.cs) y
[GameData.cs](../../../unity/Assets/Awakening/Core/Rules/GameData.cs):

- **Preparación de cada sitio:** **24 meses** en Normal y Hard; **20 meses** en Easy
  (`alignment-7`). Se acelera con `Madness`, eclipses o figuras reveladas.
- **Velocidad:** a 1× cada mes dura 2 segundos reales (~7,5 minutos de simulación completa
  sin pausas); la velocidad 2× acorta los tramos de espera y las pausas permiten planificar
  sin presión de reflejos.
- **Cualquier cambio de calendario** debe mover junto con él las cadencias del director,
  los avisos, las derivas mensuales y los meses de apertura de sellos de talentos.

## Objetivo de duración y medición en beta abierta

**Confirmado por dirección (D02):** partidas de **15–30 minutos incluyendo la pausa y la
planificación**. En las pruebas internas de `0.6.0` y `0.7.0` las partidas duraron 14 y 22
minutos. Con la beta pública abierta en itch.io toca medir con jugadores externos: tiempo
real de sesión, tiempo en pausa, momentos sin decisiones claras y si el ritmo de las cuatro
fases se siente bien antes de cualquier ajuste fino tardío (D25).

La [referencia de rituales](../06-ritual-sites.md) detalla el margen, la aceleración y las
interrupciones; [08 — Design rationale](../08-rationale.md) conserva el historial de la
compresión del calendario.

[Volver al índice](../README.md)
