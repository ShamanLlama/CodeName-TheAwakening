# Experiencia, loops y sistemas

**Versión:** 0.6 · **Fecha:** 2026-11-21 · **Estado:** síntesis de `alignment-4` (juego 0.6.0, Unity); pilares sin cambios.

## Pilares de la base actual

1. **Crecer tiene consecuencias.** Los seguidores sostienen ingresos y rituales,
   pero elevan el conocimiento global del culto.
2. **Defender exige elegir.** Toda amenaza tiene tres respuestas que siempre funcionan
   si son legales, y cada una se paga en una moneda distinta: Influence y un año sin
   obstruir en esa región, meses de la célula, o Dread y awareness.
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
| Contención | Ocultar evidencia; ante cada amenaza, obstruir, dispersar, silenciar o aceptar el golpe | Menor presión o daño evitado, a cambio de Influence, meses de la célula, Dread y awareness, o seguidores. Ocultar baja la awareness mundial solo una vez cada 6 meses: entre medio, limpia solo la evidencia local |
| Creencia | Manifestar lo imposible | Más población reclutable; mayor atención y resistencia |
| Oportunidades | Aprovechar un presagio o convertir una figura | Dread, aceleración o beneficio secreto; coste y plazo |
| Mantenimiento | Cuándo financiar cada sitio y cuánta Influence reservar | Margen de calendario frente a 1 Influence/mes por sitio durante toda la partida |
| Final | Conservar sitios listos, pagarlos y recuperar interrupciones; si el rito ya no cabe, cerrar la partida | Avance del rito frente al calendario y a la caza de sitios. El margen del rito (último inicio menos inicio más temprano) dice cuántos meses se pueden perder |

Sacrificar reduce el crecimiento futuro de awareness al reducir el culto; no elimina
el conocimiento ya acumulado. La acción de ocultar evidencia sí puede reducirlo, a un
precio que sube con la awareness y como mucho una vez cada 6 meses en todo el mundo
(`alignment-4`).

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

## Cambios de `alignment-3` — 2026-11-12

**Problema.** En las partidas humanas de 0.3.x el medio juego era mantenimiento: en las dos
victorias registradas, 96 de 131 y 68 de 96 acciones fueron `recruit`, `counter` y `conceal`
al salir del cooldown. Una amenaza tenía una sola respuesta, así que obstruir (20–24 veces
por partida) no era una decisión. Además, tres de las cuatro derrotas de 0.3.4 fueron por
awareness, y entre 86 y 121 de esos puntos salieron de `manifest` y `reveal`: el coste estaba
en el texto de riesgo y no en el precio. Alternar `reveal`/`hide` cobraba +5 cada vez.
Son 2 testers externos; se tratan como hipótesis fuertes.

**Objetivo.** Más decisiones por minuto sin automatizar acciones del jugador ni alargar el
calendario. Dirección descartó las posturas por célula (reclutamiento automático) el
2026-11-12: quitan acciones al jugador. Reclutar sigue siendo manual.

| Cambio | Regla | Pilar que refuerza | Condición de legibilidad |
| --- | --- | --- | --- |
| Tres respuestas a una amenaza | **Obstruir** (`counter`): como antes, 30 de Influence por severidad y 12 meses sin obstruir ahí. **Dispersar** (`scatter`): gratis; la célula pasa 8 meses oculta, sin reclutar ni ingresar, su sitio no avanza ni cuenta para el rito, deja de ser pública, baja 10 de exposición y no atrae amenazas nuevas; 24 meses de cooldown. **Silenciar** (`silence`): 20 de Dread por severidad (13 con Harbinger), +3 de awareness por severidad, +6 de exposición; 12 meses de cooldown | 2: elegir entre monedas, no solo entre regiones | Los tres sellos aparecen en el recorte de la amenaza y en el expediente, cada uno con su precio y, debajo, lo que cuesta además |
| Awareness como precio | Toda acción informa la awareness que suma (`preview.awareness`) y se cobra exactamente eso | 4: el reloj que mata se ve antes de pulsar | Línea roja «+8 AWARENESS» bajo el precio de la acción y en su explicación |
| `reveal` sin trampa | Hacer público un culto suma +5 de awareness solo la primera vez en cada región; la exposición y el backlash se aplican siempre | 1: el coste global se paga por lo que el mundo aprende, no por cada clic | La línea de awareness desaparece del precio cuando la región ya fue pública |

Se probó que las amenazas se tiraran cada 3 meses en lugar de 4 para compensar las opciones
nuevas: el bot bajó de 103 a 59 victorias en 144 partidas. Se descartó; la cadencia no cambia.

**Medición (bot, no humanos).** `godot --headless --path godot -s res://dev/measure.gd`,
Normal, misma semilla por partida. En Unity se reproduce con
`dotnet run --project core-tests/Tools -- measure --seeds=48`, que da las mismas cifras
(comprobado el 2026-11-18). La política usa las respuestas nuevas así: obstruye en
regiones con sitio; si no puede, silencia cuando hay un sitio o la célula en peligro y sobra
Dread; dispersa células sin sitio cuando el golpe cuesta más que los meses ocultos.

| Estrategia | Victorias (24 seeds) | Victorias (48 seeds) | Influence final (mediana, 48) | Awareness final (48) | Respuestas por partida (48) |
| --- | --- | --- | --- | --- | --- |
| Early public | 9 → 9 | 17 → 21 | 35 → 36 | 47 → 59 | 6,5 obstruir · 3 dispersar · 2 silenciar |
| Late public | 22 → 24 | 43 → 48 | 184 → 52 | 49 → 64 | 16 · 3 · 2 |
| Hub | 18 → 15 | 35 → 34 | 69 → 41 | 57 → 90 | 13 · 3 · 4 |
| Total | 49/72 → 48/72 | 95/144 → 103/144 | | | |

Las derrotas de la estrategia hub cambian de causa: de 13 por calendario a 13 por awareness
y 1 por calendario (48 seeds). La política silencia sin pesar la awareness que suma.

**Riesgos.** La mejor línea del bot (late public con Hidden hand) ya no pierde: 48/48. Con
tres respuestas el juego es algo más fácil para quien ya lo domina; si los jugadores lo
confirman, subir la presión en `alignment-4` junto con el árbol. Dispersar puede resultar
gratis en una región que ya no crece; el coste real ahí es solo el ingreso de 8 meses.
Easy y Hard siguen sin medirse.

**Primera partida humana (2026-11-13, diseñador, Normal, Harbinger, local).** Ganó en el mes
212 con 0 seguidores perdidos por amenazas, 860 de Influence sin gastar, 229 de Dread y
awareness final 11. Respuestas: 14 obstruir, 12 silenciar, 3 dispersar; además 46 `conceal`.
Las tres respuestas se usaron, pero el precio en awareness de silenciar no pesó: cada
`conceal` quita 3 de awareness global y la Influence sobraba para pagarlo. Es una partida de
quien mejor conoce el juego; no dice nada de un jugador nuevo. Candidato para `alignment-4`:
enfriamiento mundial para el efecto global de `conceal`.

**Comprobación con jugadores.** Mirar en el Lab la mezcla de `counter`, `scatter` y `silence`
por partida: si una respuesta pasa del 80 % o no llega al 5 %, no hay decisión. Preguntar si
entendieron por qué subió la awareness antes de perder.

## Cambios de `alignment-4` — 2026-11-21

**Problema.** Ocultar (`conceal`) borraba la awareness mundial en ráfagas. Cada región tenía
su propio cooldown de 12 meses, así que con el juego en pausa se podía ocultar una vez por
célula en el mismo mes: todos los runs completos del Lab muestran meses con 5 a 8
ocultamientos, también con `alignment-2`. Con 8 células eran hasta 24 puntos de awareness
por año. El precio en awareness de silenciar dejaba de pesar, y el reloj que mata se
desactivaba expandiéndose, contra el pilar 1. Además, cuando el rito ya no cabía, el
jugador no lo sabía y seguía gastando hasta el mes 224 (run a5ac6dbf: 7 sacrificios y
8 ocultamientos en el mes 213).

**Objetivo.** Que la awareness se gestione en vez de borrarse, cambiando una sola regla de
balance para poder aislar su efecto. Y que una partida perdida pueda cerrarse en cuanto se
sabe que está perdida.

| Cambio | Regla | Pilar que refuerza | Condición de legibilidad |
| --- | --- | --- | --- |
| Efecto mundial de ocultar con plazo propio | Ocultar sigue siendo regional: −25 de exposición local, 12 meses de cooldown por región. Su −3 de awareness mundial tiene ahora un **cooldown mundial de 6 meses** (`WorldConcealReadyAt`). Mientras se recupera, ocultar cuesta solo las 20 de Influence base y no baja la awareness. Ocultar sin awareness no gasta el efecto mundial | 1: crecer vuelve a costar; 2: frente a una amenaza, silenciar ya no sale gratis | La vista previa dice cuándo vuelve el efecto mundial y, si está listo, que después descansa 6 meses. El sello muestra «GLOBAL EFFECT IN N MO» (`GameEngine.ConcealWorldCooldown`). Con la exposición en su piso y el efecto en reposo, la razón es «Nothing to conceal here. The world effect returns in N months.» |
| Margen del rito | `Forecast.Margin` calcula, con los sitios financiados, el inicio más temprano del rito, el último inicio posible (mes 212) y el margen entre ambos, además de los meses que le faltan a cada sitio, si tiene apoyo y si su célula está dispersa | 4: el jugador lee el plazo antes de perderlo | Dato derivado, sin regla nueva; la banda de margen llega con la interfaz de 0.6.0 |
| Cerrar la partida | Cuando el rito ya no puede completarse antes del mes 224 y no empezó, el jugador puede cerrar la partida como derrota (`GameEngine.Concede`, código `conceded`). Para decidirlo se cuentan los eclipses y figuras secretas presentes y los sitios que aún podrían financiarse ese mes, pero no presagios futuros. Todo final guarda su código: `awakened`, `world_awareness`, `deadline` o `conceded` | 4: una derrota con causa clara, sin minutos vacíos | El aviso «ya no cabe» y el botón de cierre llegan con la interfaz de 0.6.0 |

Se descartó sacar el efecto mundial a una acción nueva («Burn the archives»): era más clara,
pero sumaba un verbo y un adaptador sin aislar mejor el cambio.

**Medición (bot, no humanos).** `dotnet run --project core-tests/Tools -- measure --seeds=48`,
Normal, 48 seeds por estrategia. La política solo oculta por awareness cuando el efecto
mundial está listo; con la política anterior, `alignment-4` da 79/144 (early 5, late 45,
hub 29).

| Estrategia | Victorias | Derrotas (awareness · calendario) | Awareness final (mediana) | Ocultamientos por partida | Mes de victoria (mediana) |
| --- | --- | --- | --- | --- | --- |
| Early public | 21 → 5 | 0 · 27 → 4 · 39 | 59 → 82 | 20 → 17 | 212 → 214 |
| Late public | 48 → 45 | 0 · 0 → 2 · 1 | 64 → 73 | 13 → 13 | 212 → 212 |
| Hub | 34 → 30 | 13 · 1 → 16 · 2 | 90 → 90 | 9 → 10 | 214 → 212 |
| Total | 103/144 → 80/144 | | | | |

El criterio previo era bajar el cooldown a 4 meses si las victorias caían por debajo de
80/144. Quedaron en 80, en el límite, así que se mantienen 6 meses. Con 4 meses serían 86/144
(early 8, late 46, hub 32), y con 3 meses, 91/144 (11, 47, 33). El bot tiene como mucho 4
células y nunca ocultó en 4 regiones el mismo mes, ni antes ni después (máximo 2), así que
la meta de 0 meses con 4 o más ocultamientos solo puede comprobarse con jugadores.

**Riesgos.** La estrategia early public se hunde (21 → 5). Pierde sobre todo por calendario:
con la awareness más alta, el lastre de reclutamiento deja sitios sin apoyo al llegar la
alineación. Ni siquiera 4 meses la recupera (8/48), así que el plazo de ocultar no es la
palanca para el culto público. Si los jugadores lo confirman, revisar el backlash o el
lastre antes de aflojar ocultar. La diferencia entre 79 y 80 victorias está dentro del ruido
de 48 seeds.

**Comprobación con jugadores.** Con la telemetría de `awakening-3`: meses con 4 o más
ocultamientos (meta 0), ocultar por debajo del 15 % de las acciones, awareness final de las
victorias entre 35 y 75, y órdenes después del mes 212 sin rito iniciado (meta 0). Preguntar
si entendieron por qué perdieron y si cerrar la partida les pareció justo.

## Acciones y progresión actuales

El conjunto actual tiene catorce acciones: `recruit`, `expand`, `sacrifice`, `reveal`,
`hide`, `conceal`, `manifest`, `prepare`, `counter`, `scatter`, `silence`, `exploit`,
`convert` y `reveal_figure`. Sus condiciones y costes están en los datos y en `previewAction`;
las referencias detalladas del índice explican cada sistema.

Una doctrina por partida especializa reclutamiento (**Congregation**, +35 % de
reclutamiento automático), infiltración (**Hidden hand**, las amenazas llegan 2 meses
después y obstruir cuesta un tercio menos) o terror ritual (**Harbinger**, el sacrificio
da 35 de Dread, manifestar cuesta 15 y silenciar un tercio menos). Se compra una vez,
por 40 de Influence. La progresión consiste en ampliar
la red, financiar sitios y alcanzar el final; no se documenta un sistema actual de
desbloqueos permanentes entre partidas. El árbol de talentos vivirá dentro de cada partida
(D05) y llega en 0.7.0.

Se gana completando el rito dentro de la ventana con suficientes sitios listos y
apoyados. Se pierde por conocimiento global máximo o por agotar el calendario. Desde
`alignment-4`, el jugador también puede cerrar la partida como derrota cuando el rito ya
no cabe y no empezó.
El progreso del rito se conserva durante interrupciones; un sitio puede perder
preparación por amenazas y se destruye si su célula pierde todos los seguidores.

## Comprobación propuesta

Observar si una persona cambia su plan después de un sacrificio o amenaza y puede
explicar la relación entre seguidores, ingreso y apoyo ritual. Registrar qué acciones
ignora y por qué. Las simulaciones anteriores no demuestran comprensión humana.

Fuentes: [balance](../../../unity/Assets/Awakening/Core/Rules/Balance.cs), [acciones](../../../unity/Assets/Awakening/Core/Rules/GameData.cs),
[motor](../../../unity/Assets/Awakening/Core/Rules/GameEngine.cs), [director](../../../unity/Assets/Awakening/Core/Rules/Director.cs),
[registro de versiones](../../playtests/versiones.md), [razones del diseño](../08-rationale.md)
y [problemas abiertos](../09-open-problems.md).

[Volver al índice](../README.md)
