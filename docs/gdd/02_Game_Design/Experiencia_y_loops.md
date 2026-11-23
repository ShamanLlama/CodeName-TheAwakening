# Experiencia, loops y sistemas

**Versión:** 0.9 · **Fecha:** 2026-11-23 · **Estado:** síntesis de `alignment-7` (juego 0.9.0, Unity); pilares sin cambios.

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
| Sacrificio | Consumir seguidores y elegir dónde hacerlo | Dread inmediato y +10 de Madness en la región; menos ingreso y posible pérdida de apoyo |
| Contención | Ocultar evidencia o enterrar el registro; ante cada amenaza, obstruir, dispersar, silenciar o aceptar el golpe. Desde `alignment-7`, obstruir o silenciar deja la región 5 meses sin amenazas nuevas | Menor presión o daño evitado, a cambio de Influence, meses de la célula, Dread y awareness, o seguidores. Ocultar baja la awareness mundial solo una vez cada 6 meses (9 en Hard): entre medio, limpia solo la evidencia local. Enterrar el registro baja 5 de awareness al momento, por una Influence que sube con cada uso (35 más por uso en Normal desde `alignment-6`) |
| Talentos | Qué nodo tomar cuando se abre un sello y qué rama cerrar | Una regla que cambia a favor del plan elegido, a cambio de Influence creciente y de los nodos que ya no se podrán tomar. Cada rama termina en un remate que abre otra forma de ganar (`alignment-6`), y cada nodo muestra lo que rindió en la partida |
| Reparación | Recuperar meses que una amenaza oculta quitó a un sitio | Margen del rito a cambio de Influence y exposición |
| Creencia | Manifestar lo imposible | Más población reclutable y +20 de Madness; mayor atención y resistencia |
| Madness (`alignment-7`) | Dónde mantener alta la Madness regional | Dread cada mes y sitios que se preparan más rápido, a cambio de más amenazas (ocultistas desde 50) y awareness desde 50 |
| Oportunidades | Aprovechar un presagio o convertir una figura | Dread, aceleración o beneficio secreto; coste y plazo |
| Mantenimiento | Cuándo financiar cada sitio y cuánta Influence reservar | Margen de calendario frente a 1 Influence/mes por sitio durante toda la partida |
| Final | Conservar sitios listos, pagarlos y recuperar interrupciones; si el rito ya no cabe, cerrar la partida | Avance del rito frente al calendario y a la caza de sitios. El margen del rito (último inicio menos inicio más temprano) dice cuántos meses se pueden perder. Desde `alignment-6`, `Forecast.Rite` dice si el rito avanza este mes y qué sitio lo frena, y dispersar o sacrificar un sitio que lo sostiene lo avisa en el riesgo |

Sacrificar reduce el crecimiento futuro de awareness al reducir el culto; no elimina
el conocimiento ya acumulado. Ocultar evidencia sí puede reducirlo, a un precio que sube
con la awareness y como mucho una vez cada 6 meses en todo el mundo (`alignment-4`).
Desde `alignment-5` también lo reducen enterrar el registro y algunos nodos del árbol.
Con `alignment-6` son Scapegoats y The world forgets (−25 una vez por partida), y Open worship
deja de contar a los seguidores públicos.

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

## Cambios de `alignment-5` — 2026-11-21

**Problema.** En la primera partida humana con 0.6.0 (2026-11-21, Normal) el jugador perdió
por awareness en el mes 215, dos meses antes de terminar el rito. Una amenaza oculta ignorada
en el mes 185 le quitó 6 meses a su tercer sitio: solo podía dispersarla, porque obstruir
estaba en espera y no le alcanzaba el Dread para silenciar. Terminó con unos 700 de Influence
sin gastar (870 en el mes 168) y dijo «no tenía en qué gastar», «la awareness no hay cómo
bajarla con nada más» que ocultar y «un error me costó la partida». Además, la estrategia de
culto público del bot había caído de 21/48 a 5/48 con `alignment-4`.

**Objetivo.** Dar destino a la Influence, una segunda forma de bajar la awareness, una forma
de reparar un error y opciones de estrategia que cada partida obligue a elegir, sin tocar la
regla de ocultar. Decisiones del usuario del 2026-11-21: el árbol vive dentro de la partida y
no se completa en una; cada dificultad obliga a pensar; Mysticism tiene efecto; varias formas
de ganar y de perder.

| Cambio | Regla | Pilar que refuerza | Condición de legibilidad |
| --- | --- | --- | --- |
| Árbol de talentos | Reemplaza la doctrina. Tres ramas (Congregation, Hidden hand, Harbinger) de 4 nodos: una raíz, dos nodos medios excluyentes y un remate que pide la raíz y uno de los medios. Los sellos se abren con el tiempo (Normal: meses 24, 64, 104, 144 y 176) y no se acumulan. Cada nodo cuesta 30 más que el anterior, desde 40 (40 + 70 + 100 + 130 + 160 = 500). Sin reespecialización. Con 5 sellos se toma como mucho una rama entera y dos nodos de otra. Las raíces conservan el efecto de la doctrina | 2: elegir qué cerrar; la Influence sobrante tiene un uso que compite con los demás | `TalentReason` da la razón exacta de cada nodo cerrado (sello, requisito, hermano elegido, Influence). El árbol en pantalla llega con la interfaz de 0.7.0 |
| Enterrar el registro (`bury`) | Acción regional con célula: −5 de awareness mundial por 50 de Influence + 25 por cada uso anterior (15 en Easy, 40 en Hard); +8 de exposición local y 6 meses de cooldown regional. No comparte el descanso mundial de ocultar | 1: la Influence compra tiempo frente al reloj que mata, cada vez más cara | La vista previa da el efecto y el precio del próximo entierro |
| Restaurar el sitio (`restore`) | Cuando una amenaza oculta ignorada quita meses a un sitio, el sitio guarda esa pérdida (`Site.Recoverable`; la siguiente pérdida la reemplaza). Restaurar devuelve la mitad, redondeando hacia arriba, a 30 de Influence por mes, con +10 de exposición. Una vez por pérdida | 4: un error tiene arreglo, con precio | Solo aparece cuando hay meses que recuperar; la vista previa dice cuántos |
| Respuestas al aparecer | `Threats.Options` dice, para obstruir, dispersar y silenciar, si se puede, por qué no y cuánto cuesta. `Threats.Cornered` es verdadero si no se puede obstruir ni silenciar | 2 y 4: el jugador sabe desde el primer mes qué podrá hacer | La interfaz lo muestra en la ficha de la amenaza cuando aparece |
| Mysticism | Manifestar en una región de Mysticism m suma `8 × (1 − (m − 50)/100)` de awareness y convierte `65 × (1 + (m − 50)/100)` negadores y `50 × (1 + (m − 50)/100)` desprevenidos, todo redondeado. África (70): 6 de awareness, 78 y 60. Europa (30): 10, 52 y 40 | 3: la ubicación cambia el plan también al manifestar | `Derived.MysticismNote` da la línea del panel |
| Dificultad con reglas | Easy: 6 sellos, una amenaza oculta quita 4 meses, enterrar sube 15 por uso. Normal: 5 sellos, 6 meses, +25. Hard: 4 sellos, 6 meses, +40 y el efecto mundial de ocultar descansa 9 meses. Los multiplicadores de reclutamiento, presión y aviso no cambian | D16 y «cada modo obliga a pensar» | `DifficultyDefinition.Rule` da la línea de la pantalla de inicio |
| Caza de sitios | Desde el mes 200, la presión sobre regiones con sitio sube +0,3 por tirada (antes +0,15) | 2: un final defendido; compensa el poder que da el árbol | Sin texto nuevo: el aviso del mes 176 ya existe |

**Los 12 nodos de `alignment-5`.** Los remates piden la raíz y cualquiera de los dos medios.
Los tres remates de esta tabla salieron en `alignment-6`; la tabla vigente está en
[Cambios de `alignment-6`](#cambios-de-alignment-6--2026-11-22).

| Rama | Raíz (tier 1) | Medio A (tier 2) | Medio B (tier 2) | Remate (tier 3) |
| --- | --- | --- | --- | --- |
| Congregation | **Swelling flock**: +35 % de reclutamiento automático | **Open worship**: las células públicas reclutan ×2,5 (no ×2), la awareness no las frena, no suman awareness mensual y hacerse público no alarma al mundo | **Martyrs**: los seguidores perdidos por una amenaza dan 1 de Dread por cada 3 | **A faith among faiths**: el tamaño del culto suma la mitad de awareness cada mes |
| Hidden hand | **Quiet friends**: las amenazas llegan 2 meses más tarde y obstruir cuesta un tercio menos | **Sleeper cells**: la célula dispersa vuelve en 5 meses, no en 8 | **Contingency**: restaurar devuelve todos los meses y no suma exposición | **Bought press**: enterrar baja 8 (no 5) y su precio sube la mitad de rápido |
| Harbinger | **Red altars**: el sacrificio da 35 de Dread, manifestar cuesta 15 y silenciar un tercio menos | **Mute witnesses**: silenciar suma 2 de awareness por severidad, no 3 | **Scapegoats**: una amenaza que cae baja la awareness 1 por severidad | **Offerings to the deep**: un sacrificio en una región amenazada termina la amenaza, con +1 de awareness |

Bajan la awareness tres nodos de dos ramas: Bought press, Scapegoats y A faith among faiths.
El margen contra el error está en Contingency y Sleeper cells.

**Descartes medidos (bot, Normal, 48 seeds).**
- Offerings con +3 de awareness: la política Harbinger bajó a 10/48 (41/48 con 0). Queda en +1.
- Scapegoats con −2 por severidad: 47/48 para Harbinger. Queda en −1 (31/48).
- La palanca del culto público no era el lastre de awareness sino la awareness que suma
  hacerse público (+5 por región) y el goteo de las células públicas. Sin Open worship, un
  culto que se hace público en el mes 64 gana 6/48; escondido, 32/48; público con Open
  worship, 41/48. Open worship hace que el culto público sea la mejor opción de su rama, no
  una trampa.
- Precios del árbol más bajos (30, 50, 70…) no ayudaron al bot: el cuello está en cuándo
  llega cada nodo, no en cuánto cuesta.
- Caza de sitios: con +0,15 el total fue 111/144 (38/42/31); con +0,25, 103/144; con +0,3,
  95/144.

**Medición (bot, no humanos).** `dotnet run --project core-tests/Tools -- measure --seeds=48
--difficulty=normal|easy|hard`. Una política por rama:
- **early** toma Congregation (Swelling flock, Open worship, A faith among faiths) y se hace
  pública cuando tiene Open worship (o desde el mes 120);
- **late** toma Hidden hand (Quiet friends, Contingency, Bought press) y se hace pública en el
  mes 160;
- **terror** (antes «hub») toma Harbinger (Red altars, Scapegoats, Offerings) y nunca se hace
  pública.

Las tres compran nodos en cuanto se abre un sello y ahorran para el siguiente mientras la
awareness está por debajo de 50. Entierran con Influence sobrante desde 60 de awareness y
restauran cuando sobra Influence. No silencian si la awareness pasaría de 75.

| Estrategia | Normal | Easy | Hard |
| --- | --- | --- | --- |
| early (Congregation, culto público) | 5 → 29 | 17 → 48 | 1 → 0 |
| late (Hidden hand) | 45 → 38 | 48 → 48 | 21 → 10 |
| terror (Harbinger) | 30 → 28 | 46 → 47 | 5 → 7 |
| Total | 80/144 → 95/144 | 111/144 → 143/144 | 27/144 → 17/144 |

Criterios del plan: total en Normal entre 80 y 105, cada política al menos 12/48 y ninguna
más de 15 por encima de otra (máximo 38, mínimo 28), y Easy por encima de Normal y Hard por
debajo. Se cumplen. En Normal cada política toma 3 nodos de mediana; entierran en 2, 21 y 11
de 48 partidas, y restauran en 20, 26 y 10.

**Riesgos.**
- Easy queda en 143/144 para el bot. Es una política que conoce el juego, no un jugador
  nuevo, pero si los jugadores también ganan siempre, Easy dejaría de obligar a pensar.
- Hard baja de 27 a 17 y el culto público no gana ninguna (0/48): en Hard hay una sola
  estrategia fuerte (late) y otra débil.
- La caza de sitios a +0,3 es un cambio de la regla base, no del árbol: el final se vuelve
  más duro para todos.
- Ninguna política del bot sigue Mute witnesses, Martyrs ni Sleeper cells (late tiene Martyrs
  como quinto nodo y casi nunca llega a él): su balance solo está cubierto por las pruebas
  unitarias.
- Offerings puede volver a ser una trampa si un jugador sacrifica en cada amenaza: cada
  sacrificio suma +12 de exposición.

**Comprobación con jugadores.** Con `awakening-4`: reparto de ramas, meses con sellos sin
usar (mediana menor a 12), usos de enterrar y restaurar, amenazas acorraladas al aparecer,
Influence sin gastar al final (hoy ~700) y si alguna rama gana mucho más que otra. Preguntar
«¿qué nodo descartaste y por qué?».

## Cambios de `alignment-6` — 2026-11-22

**Problema.** Primera partida humana con 0.7.0 (2026-11-22, Normal, inicio en África). Ganó en
el mes 220 con el rito iniciado en el 200. Tomó Congregation completa (Swelling flock, Martyrs,
A faith among faiths) y después Quiet friends y Sleeper cells. Gastó la Influence en obstruir
(1170), enterrar (675, 6 usos), talentos (500) y ocultar (264), y nunca juntó más de 300. La
awareness terminó en 84 y no ignoró ninguna amenaza. Martyrs no dio nada porque no perdió
seguidores ante amenazas. Dispersó dos sitios en pleno rito (meses 204 y 211), lo que lo detuvo
unos 8 meses, y no entendió que dispersar lo frenaba. Dijo: «para mí fácil, pero se sintió
mucho mejor» y «el último nodo del árbol debería ser más game changer».

**Objetivo.** Que cada remate abra otra forma de ganar (K1), que cada talento muestre lo que dio
(K2), que el rito detenido se vea y se avise antes de causarlo (K3) y que Normal exija más (K4),
sin cambios que castiguen un solo error.

| Cambio | Regla | Pilar que refuerza | Condición de legibilidad |
| --- | --- | --- | --- |
| The Great Procession (`great_procession`) | Remate de Congregation. Si al iniciar el rito hay 5 o más regiones con culto público, el rito pide 8 meses y no 12 (`GameState.RiteMonthsRequired`, fijado al iniciar). Mientras dura, cada región pública suma +0,3 de awareness por mes y ninguna puede volver a la clandestinidad | 1 y 4: un final más corto a la vista de todos, pagado en awareness y en no poder esconderse | El texto del talento dice cuántas regiones públicas hay y cuántos meses pediría el rito si empezara ahora |
| The world forgets (`world_forgets`) | Remate de Hidden hand que se invoca una vez por partida (`GameEngine.InvokeCapstone`). Baja 25 de awareness mundial y cancela todas las amenazas activas. A cambio, todas las células quedan dispersas 6 meses: no reclutan, no rinden, sus sitios se detienen y termina el culto público. Si el rito está en curso, también se detiene | 2: la salida de emergencia, pagada en meses de toda la red | `InvokeReason` y `InvokeRisk` dan la razón para no poder invocarlo y el costo; el registro dice cuántas amenazas se disolvieron |
| Dreams of R'lyeh (`dreams_of_rlyeh`) | Remate de Harbinger. Durante el rito, cada sacrificio que deja en pie tres sitios con apoyo adelanta el rito un mes y suma +2 de awareness mundial | 1: el final pasa a ser una carrera entre los seguidores que se sacrifican y la awareness | La vista previa del sacrificio dice si el sueño adelanta el rito (n/N) o por qué no |
| Open worship absorbe A faith among faiths | Además de lo que ya hacía, el mundo deja de contar a los seguidores de las células públicas: no suman awareness por tamaño del culto | 1: el culto público crece sin que el tamaño lo delate, pero sigue pagando en amenazas (+0,1 de presión y redadas) | Lo dice la descripción del nodo |
| Rendimiento por talento | `GameState.TalentYield[id]` acumula lo que dio cada talento tomado y `GameEngine.TalentYieldText` lo dice en una línea | 4: el jugador ve si su elección rindió | Devuelve "" si el talento no se tomó |
| Rito como dato | `Forecast.Rite` da `Started`, `Progress`, `Required`, `Advancing` y los `Blockers` por sitio (`destroyed`, `dormant`, `scattered`, `unsupported`, `unprepared`) mientras el rito está detenido. Dispersar un sitio que el rito necesita avisa «The final rite halts while this site hides: N months.», y sacrificar uno que queda sin apoyo avisa «The final rite halts until this site again holds N followers.» | 4: el rito detenido se ve y se anticipa | Los textos de riesgo aparecen solo durante el rito y solo si no hay un cuarto sitio que lo sostenga |
| Normal más exigente | Enterrar sube 35 por uso (antes 25). Desde el mes 100, cada tirada de amenaza suma +0,05 de presión. La caza de sitios desde el mes 200 sube de +0,3 a +0,6 | 2: más amenazas que atender, ninguna regla nueva castiga un solo error | El aviso del mes 176 ya anuncia la caza; `Director.ThreatPressure` expone la presión |
| Easy | La presión de amenazas pasa de ×0,75 a ×1,0 y una amenaza oculta ignorada quita 5 meses (antes 4). Conserva 6 sellos, +2 meses de aviso, reclutamiento ×1,2 y enterrar +15 | D16: no se gana siempre, pero no abruma | `DifficultyDefinition.Rule` cambia con los valores |

**Los 12 nodos (`alignment-6`).** Los remates piden la raíz y cualquiera de los dos medios.

| Rama | Raíz (tier 1) | Medio A (tier 2) | Medio B (tier 2) | Remate (tier 3) |
| --- | --- | --- | --- | --- |
| Congregation | **Swelling flock**: +35 % de reclutamiento automático | **Open worship**: las células públicas reclutan ×2,5 (no ×2), la awareness no las frena, no suman awareness mensual ni por tamaño, y hacerse público no alarma al mundo | **Martyrs**: los seguidores perdidos por una amenaza dan 1 de Dread por cada 3 | **The Great Procession**: con 5 o más regiones públicas al iniciar, el rito pide 8 meses y no 12; mientras dura, +0,3 de awareness por región pública y por mes, y nadie vuelve a la clandestinidad |
| Hidden hand | **Quiet friends**: las amenazas llegan 2 meses más tarde y obstruir cuesta un tercio menos | **Sleeper cells**: la célula dispersa vuelve en 5 meses, no en 8 | **Contingency**: restaurar devuelve todos los meses y no suma exposición | **The world forgets** (se invoca una vez): −25 de awareness y se cancelan todas las amenazas; todas las células quedan dispersas 6 meses y el rito en curso se detiene |
| Harbinger | **Red altars**: el sacrificio da 35 de Dread, manifestar cuesta 15 y silenciar un tercio menos | **Mute witnesses**: silenciar suma 2 de awareness por severidad, no 3 | **Scapegoats**: una amenaza que cae baja la awareness 1 por severidad | **Dreams of R'lyeh**: durante el rito, cada sacrificio que deja tres sitios con apoyo adelanta el rito un mes, con +2 de awareness |

**Qué mide el rendimiento de cada talento** (`TalentYieldText`):

| Talento | Unidad | Línea |
| --- | --- | --- |
| Swelling flock | Seguidores que no habrían llegado sin el +35 % | «+N followers so far» |
| Open worship | Awareness que no se sumó: al hacerse público, el goteo mensual y el tamaño de las células públicas | «N world awareness spared so far» |
| Martyrs | Dread ganado | «N Dread so far» |
| Quiet friends | Influence ahorrada al obstruir | «N Influence saved so far» |
| Sleeper cells | Meses de dispersión ahorrados (3 por dispersión) | «N months of hiding saved so far» |
| Contingency | Meses restaurados por encima de la mitad | «N more preparation months restored so far» |
| Red altars | Dread de más al sacrificar y Dread ahorrado al manifestar y al silenciar | «N Dread gained or saved so far» |
| Mute witnesses | Awareness que no sumó silenciar | «N world awareness spared so far» |
| Scapegoats | Awareness que bajó | «N world awareness lifted so far» |
| The Great Procession | Meses de rito ahorrados (4 o 0) | Antes del rito, cuántas regiones públicas hay y cuántos meses pediría; después, «The rite needs 8 months, not 12» o «Too few walked…» |
| The world forgets | Awareness quitada al invocarlo | «Not yet invoked» o «Invoked: −N world awareness» |
| Dreams of R'lyeh | Meses de rito adelantados | «N rite months dreamed forward so far» |

**Qué pasó con los remates viejos.** Los tres salen del juego. Ninguno pasa a un nodo medio
porque los seis medios son nodos que el plan de 0.7.0 hizo obligatorios: Open worship, Martyrs,
Sleeper cells, Contingency, Mute witnesses y Scapegoats. Para poner un remate viejo en el medio
habría que sacar uno de ellos.
- **A faith among faiths:** su efecto pasa a Open worship, solo para las células públicas. Sin
  ese efecto y con las primeras palancas de K4, el culto público del bot cayó de 29/48 a 9/48.
  Con los valores finales gana 11/48 si el mundo deja de contar la mitad de los seguidores
  públicos, 19/48 si deja de contar tres cuartos y 22/48 si no cuenta ninguno.
- **Bought press:** sale. Enterrar conserva su efecto base. La rama ya tiene un nodo que baja la
  awareness más que Bought press, The world forgets, y el paso de enterrar es la palanca de
  dificultad K4.
- **Offerings to the deep:** sale. Terminar una amenaza con un sacrificio competía con Dreams of
  R'lyeh por el mismo recurso, y la rama ya responde a las amenazas con Red altars y Mute
  witnesses. Siguen bajando la awareness dos nodos de ramas distintas: Scapegoats y The world
  forgets.

**Cifras cambiadas frente al plan.**
- **La procesión** no duplica la awareness mensual de las regiones públicas. Ese goteo es 0,075
  por mes, y 0 con Open worship, así que duplicarlo no costaba nada. Se reemplazó por +0,3 por
  región pública y por mes: unos 12 de awareness en 8 meses con 5 regiones. Con +0,5, el culto
  público perdió 16 de 48 partidas por awareness. Además se prohíbe volver a la clandestinidad
  durante la procesión, porque si no bastaba con ocultar todo al día siguiente de empezar.
- **The world forgets** dispersa 6 meses, no 4. Con 4, Hidden hand ganó 38/48 y se separó en 16
  de early. Con 6 bajó a 31. Ponerle precio en Influence lo dejó sin uso: el bot nunca tenía
  la Influence cuando hacía falta.

**Descartes medidos (bot, Normal, 48 seeds salvo que se diga).**
- Presión desde el mes 100 de +0,08 a +0,1: el culto público cae a 20–24/48 y Hidden hand queda
  en 35–38. Más presión favorece a quien se oculta.
- Más pérdida por investigación y amenaza oculta (8 % → 10 %): early 8/48 y terror 19/48;
  Hidden hand sigue en 41. Obstruir más caro (30 → 35 por severidad): castiga sobre todo al
  culto público.
- Presión extra solo sobre células ocultas: hunde a Harbinger, que nunca es público.
- Easy: con presión ×0,85 o ×0,9 queda en 139–143/144. Con ×1,0 y la pérdida oculta en 5
  queda en 135, y las tres políticas se parecen más (44, 47 y 44).
- Las corridas de 48 seeds tienen ruido de ±4 por política. Por eso la elección se confirmó con
  144 seeds.

**Medición (bot, no humanos).** `dotnet run --project core-tests/Tools -- measure --seeds=48
--difficulty=normal|easy|hard`. Una política por rama y remate:
- **early** toma Swelling flock, Open worship y The Great Procession. Abre una quinta célula
  (Asia del Sur) y desde el mes 184 hace públicas sus cinco regiones. Espera a lo sumo hasta
  el mes 201 para tener las cinco;
- **late** toma Quiet friends, Sleeper cells y The world forgets. Lo invoca si la awareness
  llega a 85 o si dos amenazas que dañan un sitio o destruyen una célula caen en 2 meses o
  menos sin respuesta;
- **terror** toma Red altars, Scapegoats y Dreams of R'lyeh. Durante el rito sacrifica donde
  el sueño adelanta, mientras la awareness no pase de 94 y la célula conserve su apoyo.

| Estrategia | Normal (`alignment-5` → `alignment-6`) | Easy | Hard |
| --- | --- | --- | --- |
| early (Congregation, procesión) | 29 → 22 | 48 → 44 | 0 → 0 |
| late (Hidden hand, olvido) | 38 → 31 | 48 → 47 | 10 → 14 |
| terror (Harbinger, sueños) | 28 → 27 | 47 → 44 | 7 → 3 |
| Total | 95/144 → 80/144 | 143/144 → 135/144 | 17/144 → 17/144 |

**Uso de los remates, en partidas de 48.**

| Dificultad | Procesión (8 meses) | The world forgets | Dreams of R'lyeh |
| --- | --- | --- | --- |
| Normal | 36 partidas, 22 ganadas | 39 partidas, 26 ganadas | 32 partidas, 157 meses adelantados, 27 ganadas |
| Easy | 48 partidas, 44 ganadas | 19 partidas, 19 ganadas | 48 partidas, 238 meses adelantados, 44 ganadas |
| Hard | 0 partidas | 42 partidas, 14 ganadas | 4 partidas, 14 meses adelantados, 3 ganadas |

En Normal todas las victorias de early son procesiones. Ninguna política pierde por awareness;
las derrotas son por plazo.

**Criterios de K4.**
- **Normal:** 80/144, dentro de 75–85. Cada política gana al menos 12/48 y la mayor diferencia
  es 9. **Se cumple.**
- **Con 144 seeds:** 70, 102 y 74 de 144. El total equivale a 82/144 y la diferencia, a unos 11
  sobre 48.
- **Easy:** 135, entre Normal y 140. **Se cumple.**
- **Hard:** 17, por debajo de Normal. **Se cumple.**
- **Hard, culto público:** 0/48. No llega a 3: el culto público nunca reúne la Influence para su
  tercer nodo (mes 120) y sus sitios caen antes del rito.

**Riesgos.**
- Hidden hand sigue siendo la rama más fuerte para el bot, y la presión general la favorece.
  Con 144 seeds le saca unos 11 de 48 a early.
- En Hard solo gana Hidden hand de forma regular (14/48). Ni el culto público ni Harbinger
  llegan a su remate a tiempo.
- La procesión cuesta poca awareness al bot (ninguna derrota por awareness). Su precio real es
  exponer cinco regiones a redadas desde el mes 184.
- La caza de sitios a +0,6 endurece el final para todos. Los tres remates responden a ella:
  acortan el rito, lo adelantan o cancelan las amenazas.
- Martyrs, Mute witnesses y Contingency siguen fuera de las políticas del bot. Solo los cubren
  pruebas unitarias.

**Comprobación con jugadores.** Con `awakening-5`: qué remates se usan y cuándo, los meses de
rito detenido y sus causas, lo que rindió cada talento (¿Martyrs sigue en 0?) y si Normal se
siente exigente sin que un error decida la partida. Preguntar «¿tu remate cambió cómo
terminaste?».

## Cambios de `alignment-7` — 2026-11-23

**Problema.** Dos partidas humanas con 0.8.0 (2026-11-22), una en Normal y otra en Easy, ambas con
Harbinger. El jugador se rindió en los meses 199 y 200 sin empezar el rito, con el primer sitio
listo en los meses 157 y 149. Cinco amenazas ocultas cayeron sobre regiones con sitio 2–12 meses
después de obstruir o silenciar ahí: las dos respuestas seguían en espera de 12 meses, así que solo
quedaba dispersar, y cada dispersión frenó el sitio 8 meses. En Easy tomó un solo talento porque no
le alcanzó la Influence. Dreams of R'lyeh rindió 0 porque solo actuaba durante el rito. El jugador
dijo: «Tenemos Madness como variable pero no se usa en nada».

**Objetivo.** Que la Madness sea un recurso con precio (M1), que Dreams of R'lyeh cambie la partida
antes del rito (M2), que responder una amenaza deje respirar a la región (G1) y que Easy dé margen
de calendario y talentos (E1), con Normal entre 75 y 85 de 144 para el bot.

| Cambio | Regla | Pilar que refuerza | Condición de legibilidad |
| --- | --- | --- | --- |
| Madness: fuentes | Manifestar suma +20 a la Madness regional (0–100) y sacrificar +10. Baja 1 por mes (antes 0,3) | 1: el terror deja huella y mantenerla exige seguir pagando | Las vistas previas de `manifest` y `sacrifice` dicen cuánta queda («Madness here +20 (40)») |
| Madness: Dread | Cada región con célula rinde Madness/20 de Dread por mes, dentro de `DreadCap` | 1 y 3: el sacrificio y la manifestación rinden después | `GameEngine.MadnessDread`, `MadnessDreadTotal` y `MadnessText` («+2 Dread/month · …») |
| Madness: sitios | Un sitio que avanza (financiado, activo y con apoyo) gana Madness/100 meses extra por mes; con 50, uno cada 2 meses. La fracción se guarda en `Site.Quickening` | 3: la Madness es otra forma de acelerar un sitio tarde | `MadnessQuickens` y `MadnessText` («site prepares 60% faster»); `Forecast.Margin` la cuenta como aceleración al decidir si el rito todavía cabe |
| Madness: precio | La presión de amenaza suma Madness/400 (antes /2000). Con 50 o más, una célula oculta sin sitio atrae una sociedad ocultista en vez de investigadores, y cada región así suma +0,05 de awareness por mes | 1 y 2: la Madness alta atrae a quien la persigue y se nota en el mundo | `MadnessText` («draws occultists · +0.05 world awareness/month») y el riesgo de manifestar («Madness draws occultists.») |
| Dreams of R'lyeh | Los sacrificios dejan el doble de Madness. Cada mes, después de bajar, cada región con 30 o más da +2 a cada región vecina con célula (+1 neto). En el rito, el sacrificio que deja tres sitios con apoyo adelanta un mes, o dos si la región tiene 30 o más de Madness, con +2 de awareness | 1: el remate de Harbinger arma la partida antes del rito, con el riesgo de la Madness | `TalentYield` cuenta la Madness que puso el sueño y `GameState.DreamRiteMonths` los meses de rito; `TalentYieldText`: «+N Madness spread so far», y en el rito «… · M rite months dreamed forward so far» |
| Respiro tras responder | Obstruir o silenciar deja la región fuera de las tiradas de amenaza durante 5 meses (`Balance.ThreatGraceMonths`, `RegionState.ThreatGraceUntil`). Dispersar no da respiro: la región ya queda fuera de la tirada mientras dura | 2: responder sigue costando, pero no lleva a una segunda amenaza sin respuesta | `GameEngine.ThreatGraceLeft`; obstruir y silenciar lo dicen en la vista previa («No new threat here for 5 months.») |
| Presión de Normal y Hard | `DifficultyDefinition.Pressure`: Normal ×1,45 (antes 1,0) y Hard ×1,15 (antes 1,25). Easy queda en ×1,0 | 2: el respiro quita tiradas; la presión devuelve las amenazas por partida al nivel de `alignment-6` | `Director.ThreatPressure` |
| Easy | Sitios de 20 meses (`DifficultyDefinition.SiteMonths`) y talentos de 30 + 20 por talento tomado | D16: Easy da margen a quien no sabe el calendario de memoria | `GameEngine.SiteMonths`, `LatestSiteStart`, la descripción de `prepare`, su vista previa, `Derived.SiteLabel(state, region)`, `Forecast` y `DifficultyDefinition.Rule` usan el valor de la dificultad |
| Talentos en Normal y Hard | Cada talento cuesta 25 más que el anterior, desde 40 (antes 30) | 2: los talentos compiten menos con obstruir | `TalentCost` |
| Saves | El ruleset pasa a `alignment-7`. Los saves de `alignment-6` se retiran con aviso y los ajustes se conservan | — | Aviso de `SaveData.TakeRetiredNotice` |

**Cifras cambiadas frente al plan.**
- **Aceleración proporcional.** El plan pedía un mes extra cada 2 meses con 50 o más. Con un umbral y
  una baja lineal, la Madness llegaba a 50 solo con unos 120 seguidores sacrificados en la región del
  sitio, y por debajo no daba nada. Ahora cada punto acelera, y con 50 da lo mismo que el plan.
- **Baja de 1 por mes.** Con 0,5 la Madness de la región donde se sacrifica crecía sola: el bot
  sumaba más Dread pasivo, más presión y más awareness sin decidirlo. Con 1, mantenerla alta pide
  seguir manifestando o sacrificando.
- **Dreams of R'lyeh no exige Madness para adelantar el rito.** Con la condición del plan (Madness ≥ 50
  para adelantar) y sin respiro, terror ganó 10 de 48; sin la condición, 29. Los meses de rito
  adelantados cayeron de unos 5 a unos 2 por partida, y juntar Madness en el final obligaba a
  manifestar, que suma unos 8 de awareness cada vez. La Madness alta da un mes de más; no es la condición.
- **El sueño duplica la Madness del sacrificio.** Solo con el contagio, ninguna región del bot
  llegaba a 30 y el remate seguía rindiendo 0 antes del rito. Con el doble, el contagio rinde una
  mediana de 58 de Madness por partida en Normal.
- **Contagio de +2 después de bajar.** Con +1 antes de bajar, la región vecina perdía en el mismo mes
  lo que recibía y su Madness no se movía.
- **Respiro de 5 meses, no 6, con más presión.** Las tiradas son cada 4 meses, así que el respiro
  salta de golpe: con 4 meses Normal da 50/144 y con 6, 93/144, aun con la presión ×1,4. Con 5 y
  ×1,45 las amenazas por partida vuelven al nivel previo (early 38,6, late 24,2, terror 27,7, frente a
  35,2, 24,6 y 24,0 sin respiro) y Normal queda en 83.
- **Hard ×1,15.** Con ×1,25 y el respiro, el culto público gana 2/48 (7/144). Con ×1,15 gana 4/48 y Hard
  sigue muy por debajo de Normal. Hard conserva su aviso más corto, menos reclutamiento y menos sellos.
  early y terror reciben más amenazas por partida que en Normal (46,8 y 30,4, frente a 38,6 y 27,7);
  late, algo menos (21,8 frente a 24,2).
- **Talentos a +25.** En Normal el bot gasta en talentos cerca de un tercio de lo que gasta en obstruir:
  255/220/194 de Influence frente a 872/752/650 por partida (media). Sin respiro ni Madness la proporción
  era parecida: 315/286/247 frente a 860/647/698. En la configuración de prueba, con +25 las victorias no
  se mueven (77 frente a 79 con +30), pero más partidas llegan al remate: Dreams de 20 a 27 y la
  procesión de 34 a 37. Con +20, Normal sube a 105.

**Descartes medidos (bot, Normal, 48 seeds salvo que se diga).**
- Que Harbinger manifieste siempre donde se prepara un sitio: 0/48. Cada manifestación suma unos 8 de
  awareness, 15 de exposición y creyentes que suben la presión, y las amenazas ocultas le quitan el
  sitio. Por eso la política solo manifiesta donde un sitio no estaría listo en el mes 200.
- Más presión de Normal sin respiro más corto (×1,5 con 6 meses): Hidden hand se aleja 16 de Harbinger.
  Más presión favorece a quien se oculta, como en `alignment-6`.
- Presión desde el mes 100 de +0,1 o caza de sitios de +1,0: no compensan el respiro y castigan sobre
  todo al culto público.
- The world forgets de 8 meses: Hidden hand no cambia (107/144 con 6 y con 8).

**Política del bot.** `dotnet run --project core-tests/Tools -- measure --seeds=48 --difficulty=normal|easy|hard`.
Cambia en `alignment-7`:
- Antes del mes 200, todas las políticas silencian solo hasta 50 de awareness (antes 75). Con el Dread
  de la Madness, 75 gastaba la awareness que el final necesita.
- **terror** sacrifica en la región de un sitio que se prepara mientras le sobren 160 seguidores sobre
  el apoyo, hasta 50 de Madness. Manifiesta además donde el sitio no estaría listo en el mes 200.
  Responde primero con Dread mientras le alcance para los sitios. En el rito silencia hasta 85 de awareness.
- La herramienta informa el Dread de la Madness, los meses de sitio acelerados, la Madness del sueño,
  las amenazas por partida y la Influence gastada por uso. `trace <estrategia> <seed>` imprime una partida.

| Estrategia | Normal (`alignment-6` → `alignment-7`) | Easy | Hard |
| --- | --- | --- | --- |
| early (Congregation, procesión) | 22 → 27 | 44 → 48 | 0 → 4 |
| late (Hidden hand, olvido) | 31 → 35 | 47 → 48 | 14 → 31 |
| terror (Harbinger, sueños y Madness) | 27 → 21 | 44 → 46 | 3 → 12 |
| Total | 80/144 → 83/144 | 135/144 → 142/144 | 17/144 → 47/144 |

Con 144 seeds, Normal da 76, 107 y 65 de 144, unos 83 de 144. La diferencia mayor es de unos 14 sobre 48.

**Uso de los remates y de la Madness, en partidas de 48.**

| Dificultad | Procesión | The world forgets | Dreams of R'lyeh | Meses de sitio acelerados por Madness (early / late / terror) | Dread de Madness por partida (mediana) |
| --- | --- | --- | --- | --- | --- |
| Normal | 32 partidas, 27 ganadas | 22, 17 ganadas | 25 partidas, 110 meses de rito, 21 ganadas | 112 / 1 / 87 | 71 / 60 / 88 |
| Easy | 48, 48 ganadas | 1, 1 ganada | 44 partidas, 284 meses, 43 ganadas | 92 / 0 / 448 | 84 / 72 / 443 |
| Hard | 9, 4 ganadas | 21, 15 ganadas | 14 partidas, 67 meses, 12 ganadas | 94 / 2 / 84 | 65 / 62 / 81 |

early también acelera sitios: sacrifica en las regiones de sus sitios cuando le falta Dread. En las
partidas de terror que aceleran, la aceleración casi siempre rescata un sitio atrasado. Por eso esas
partidas ganan menos que el promedio.

**Criterios.**
- **Normal:** 83/144, dentro de 75–85. Cada política gana al menos 12/48 y la mayor diferencia es 14. **Se cumple.**
- **Política con Madness** (terror): 21/48, al menos 12. **Se cumple.**
- **Easy:** 142/144, al menos 138. **Se cumple.**
- **Hard:** 47/144, por debajo de Normal; culto público 4/48, al menos 3. **Se cumple.**

**Riesgos.**
- El respiro es la palanca más fuerte de la build y, por la cadencia de 4 meses, salta por escalones. No
  conviene tocarlo sin volver a medir la presión.
- En Easy, Harbinger dispara la Madness: el contagio forma un racimo, el Dread llega al tope y la
  awareness termina en 93 de mediana (una derrota por awareness en 48). Hay que ver si a un humano le
  pasa lo mismo.
- Hidden hand sigue siendo la rama más fuerte para el bot.
- Manifestar en la región de un sitio sigue siendo mala idea para el bot salvo para rescatarlo. Hay que
  comprobar con jugadores si la Madness se siente como estrategia o solo como un extra del sacrificio.

**Comprobación con jugadores.** Con `awakening-6`:
- ¿Dónde y cuándo sube la Madness? ¿Cuánto Dread rindió y cuántos meses de sitio aceleró?
- ¿Cuántos respiros se usaron y cuántas amenazas cayeron sin respuesta posible?
- En Easy, ¿cuándo se financia cada sitio y cuántos talentos se toman?
- Preguntar «¿usaste la Madness a propósito?» y «¿alguna amenaza te dejó sin respuesta?».

## Acciones y progresión actuales

El conjunto actual tiene dieciséis acciones: `recruit`, `expand`, `sacrifice`, `reveal`,
`hide`, `conceal`, `manifest`, `prepare`, `counter`, `scatter`, `silence`, `exploit`,
`convert`, `reveal_figure`, `bury` y `restore`. Sus condiciones y costes están en los datos y
en `previewAction`; las referencias detalladas del índice explican cada sistema.

Desde `alignment-5` la doctrina es la raíz de cada rama del árbol de talentos, que vive
dentro de la partida (D05): Swelling flock (Congregation), Quiet friends (Hidden hand) y Red
altars (Harbinger) conservan el efecto de las tres doctrinas. El árbol se describe en
[Cambios de `alignment-5`](#cambios-de-alignment-5--2026-11-21), con los remates de
[Cambios de `alignment-6`](#cambios-de-alignment-6--2026-11-22). La progresión consiste en
ampliar la red, tomar talentos, financiar sitios y alcanzar el final; no hay desbloqueos
permanentes entre partidas.

Se gana completando el rito dentro de la ventana con suficientes sitios listos y
apoyados: 12 meses, u 8 con The Great Procession, que Dreams of R'lyeh puede adelantar
(dos meses por sacrificio donde la Madness llega a 30, desde `alignment-7`). Los sitios se
preparan en 24 meses, o en 20 en Easy, y la Madness de su región los acelera. Se pierde por conocimiento global máximo o por agotar el calendario. Desde
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
