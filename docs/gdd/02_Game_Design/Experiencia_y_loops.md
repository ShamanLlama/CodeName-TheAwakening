# Experiencia, loops y sistemas

**Versión:** 0.3 · **Fecha:** 2026-11-02 · **Estado:** síntesis de `alignment-2` (juego 0.3.0); pilares sin cambios.

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
| Contención | Ocultar evidencia u obstruir una amenaza | Menor presión, awareness o daño evitado; gasto creciente con la awareness y cooldown |
| Creencia | Manifestar lo imposible | Más población reclutable; mayor atención y resistencia |
| Oportunidades | Aprovechar un presagio o convertir una figura | Dread, aceleración o beneficio secreto; coste y plazo |
| Mantenimiento | Cuándo financiar cada sitio y cuánta Influence reservar | Margen de calendario frente a 1 Influence/mes por sitio durante toda la partida |
| Final | Conservar sitios listos, pagarlos y recuperar interrupciones | Avance del rito frente al calendario y a la caza de sitios |

Sacrificar reduce el crecimiento futuro de awareness al reducir el culto; no elimina
el conocimiento ya acumulado. La acción de ocultar evidencia sí puede reducirlo, a un
precio que sube con la awareness.

## Cambios de `alignment-2` — 2026-11-02

**Problema.** Con `alignment-1` la victoria era una lista fija de compras: tres sitios y
100 Dread. Pagada hacia el mes 112–137, la Influence ya no tenía usos que compitieran
entre sí y se acumulaba. El bot del repositorio terminaba con 136–267 Influence de mediana
en las estrategias late/hub y pasaba 30–55 meses con 100 o más sin uso. El pilar 2
(defender exige elegir) también estaba débil: el bot obstruía 10–19 amenazas por partida
sin tener que elegir.

**Objetivo.** Que cada unidad de Influence tenga más de un uso razonable y que el jugador
evalúe cuál conviene, sin alargar el calendario ni agregar acciones.

| Cambio | Regla | Pilar que refuerza | Condición de legibilidad |
| --- | --- | --- | --- |
| Mantenimiento de sitios | Cada sitio financiado cuesta 1 Influence al mes. Si no se puede pagar, queda **inactivo** ese mes: no avanza ni sostiene el rito final | 1: comprometerse tiene un coste continuo; financiar pronto ya no es gratis | El ingreso de la barra inferior es neto e indica el coste de los sitios; el sitio inactivo se anuncia en la crónica y en su etiqueta |
| Ocultamiento contra la awareness | `conceal` quita 3 de awareness (antes 1) y cuesta 20 + la mitad de la awareness actual. Puede usarse aunque la exposición esté en su mínimo si hay awareness | 1: la Influence puede comprar tiempo frente al reloj que mata, cada vez más cara | El precio aparece en la acción y la guía explica que sube con la awareness |
| Caza de sitios en la alineación | Desde el mes 200, la presión de amenazas sobre regiones con sitio sube +0,15 por tirada | 2: el cooldown de obstrucción obliga a elegir qué sitio defender durante el rito | Aviso en la crónica en el mes 176 y en la previsión ritual durante los 24 meses previos |

Probamos primero un precio de `conceal` que subía 10 en cada uso. Lo descartamos porque
encarecía también el control de exposición local, del que depende la estrategia de culto
público: sin él el bot early-public no ganaba ninguna partida.

**Medición (bot, no humanos).** Política `dev/playthrough.gd` adaptada a las nuevas reglas
(reserva para obstrucciones y mantenimiento; oculta evidencia como antes y awareness solo
con Influence sobrante). Normal, 24 seeds por estrategia, base `alignment-1` con la misma
política frente a `alignment-2`:

| Estrategia | Victorias | Influence final (mediana) | Meses con ≥100 sin uso | Awareness final |
| --- | --- | --- | --- | --- |
| Early public | 10/24 → 9/24 | 43 → 48 | 0 → 1,5 | 77 → 48 |
| Late public | 24/24 → 22/24 | 267 → 116 | 54,5 → 29,5 | 61 → 51 |
| Hub | 24/24 → 18/24 | 136 → 69 | 30 → 3 | 63 → 55 |

Una variante con mantenimiento de 1,5 eliminó casi todo el excedente (33–44 de Influence
final, 0 meses sin uso), pero bajó las victorias del bot a 42/72. Queda como siguiente
ajuste si el piloto humano todavía muestra excedente.

**Riesgos.** La awareness final baja porque `conceal` ahora la contiene; vigilar que crecer
siga sintiéndose peligroso (pilar 1). La estrategia early public sigue siendo la más débil,
como en `alignment-1`. Easy y Hard no se han vuelto a medir.

**Comprobación con jugadores.** Preguntar si sintieron que tenían que elegir en qué gastar y
si la caza de sitios se anunció a tiempo. El Lab registra gastos por acción, región y
mes; con eso se ve si la Influence se gasta en ocultar, obstruir o crecer, y cuándo.

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

Fuentes: [balance](../../../godot/core/balance.gd), [acciones](../../../godot/core/game_data.gd),
[motor](../../../godot/core/game_engine.gd), [director](../../../godot/core/director.gd),
[registro de versiones](../../playtests/versiones.md), [razones del diseño](../08-rationale.md)
y [problemas abiertos](../09-open-problems.md).

[Volver al índice](../README.md)
