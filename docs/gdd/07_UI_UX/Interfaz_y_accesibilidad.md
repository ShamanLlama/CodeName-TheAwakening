# Interfaz, aprendizaje y accesibilidad

**Versión:** 1.0 · **Fecha:** 2026-11-22 · **Estado:** interfaz de 0.3.5–0.4.0 rehecha en Unity UI Toolkit (0.5.0), corregida en legibilidad (0.5.1), ampliada en onboarding, margen del rito y cierre anticipado (0.6.0), actualizada a `alignment-5` con árbol de talentos, `bury`/`restore`, amenaza acorralada, Mysticism y reglas de dificultad (0.7.0), y ampliada a `alignment-6` con remates diferenciados, invocación de *The world forgets*, rendimiento por talento, estado de rito detenido y avisos de detención del rito (0.8.0); usabilidad por validar con personas, en teléfono físico y en el ejecutable para Steam.

## Flujo implementado

Elegir dificultad y semilla opcional → consultar guía → seleccionar región y fundar
el culto → gestionar la red → revisar la alineación → iniciar y sostener el rito →
leer resultado y reiniciar. Al volver a abrir el juego, el guardado permite retomar
la partida. Véanse las diferencias de guardado en [Base técnica](../08_Programacion_y_Technical_Design/Base_tecnica.md).

El mapa ocupa la pantalla. El HUD muestra recursos y progreso; las amenazas se apilan
a la izquierda, el panel regional aparece a la derecha y la barra inferior da acceso
a la crónica. Las acciones regionales se agrupan en **CULT**, **RITES** y **OMENS**.
Los tooltips explican efectos, riesgos y motivos de bloqueo.

La pausa permite actuar. Espacio pausa, 1 y 2 cambian velocidad y Esc cierra el panel
o abre menú. Perder el foco pausa la partida. Al comenzar el rito, el juego reanuda a
velocidad normal. El menú incluye guía, sonido y exportación del reporte de playtest.

## Principios propuestos

1. Antes de pagar, entender coste, consecuencia y riesgo local.
2. Distinguir seguidores, exposición y awareness sin tener que memorizar sus fórmulas.
3. Entender qué falta para el rito y si el calendario todavía permite completarlo.

**Error que el motor ya previene:** financiar un sitio cuando no queda tiempo suficiente
para prepararlo y completar el rito, considerando las aceleraciones disponibles.
**Decisión permitida con riesgo:** sacrificar seguidores que sostienen un sitio.

## Teléfonos y pantallas táctiles (0.3.4, 2026-11-03)

El diseño base es 1600×900 y se reduce entero a cualquier pantalla, también en el teléfono. En el panel de región, las acciones y su explicación van antes de las estadísticas. Con ratón, pasar sobre una acción la explica y hacer clic la ejecuta. En pantallas táctiles (`Kit.Touch`) un toque solo abre la explicación bajo esa acción, con un botón para ejecutarla; otro toque la cierra. El panel se desplaza arrastrando, un arrastre no pulsa el botón bajo el dedo, y la barra de desplazamiento es más ancha y visible.

Intentos descartados tras probarlos en un teléfono físico (2026-11-03):

- **0.3.2:** reducir la altura base hasta 520 en pantallas cortas (una unidad ≥ 0,75 px CSS), con textos más grandes y barras más bajas. El texto se leía, pero el mapa quedaba muy pequeño.
- **0.3.3:** sobre lo anterior, recortar las latitudes polares y ampliar 1,7 veces la región abierta en vez de encoger el mapa. No convenció.

Se volvió a la escala original y solo se conservó el panel táctil con desplazamiento. En 0.3.5 el tamaño pasa a ser una opción del jugador (ver abajo); falta confirmarlo en un teléfono físico.

## Revisión de UX/UI (0.3.5, 2026-11-06)

Respuesta a una evaluación con nota 6,5/10. Lo que cambia para el jugador:

- **Por qué no puedo:** cada acción no disponible muestra en su fila el obstáculo real («NO CELL HERE», «NO THREAT», «NEED 30 INF», «IN 4 MO»). Si falta la célula, la amenaza o el omen, eso se dice antes que el precio. Los motivos se derivan en `UI/Text/Notes.cs` (antes `ui/notes.gd`) sin tocar el motor.
- **Aprender jugando:** la guía inicial queda en cinco frases. El resto llega como notas de una frase la primera vez que aparece cada situación. Ninguna pausa el reloj, cada una se ve una vez por dispositivo y se guarda en `settings.hints`.
- **Cerrar la partida:** la derrota explica la causa principal y da uno o dos consejos concretos. La victoria tiene su propia hoja.
- **Mapa:** contorno de región, etiquetas que no se tapan, zoom con rueda o pellizco y arrastre. R o «Whole world» vuelve a la vista completa.
- **Tamaño de interfaz:** 100–200 %, elegible en el menú y, en táctil, en el inicio. Por debajo de 1240×700 unidades se activa el layout compacto. El valor por defecto sigue en 100 % para respetar lo decidido en 0.3.4: a diferencia de 0.3.2, la escala la elige el jugador, y a diferencia de 0.3.3, el zoom del mapa lo controla él.
- **Accesibilidad:** movimiento reducido, foco de teclado en diálogos, atajos (Tab, ↑ ↓ Enter, ← →, C, D, H, R), contraste mínimo de 4,5:1 en avisos y de 3,5:1 en tinta tenue, y sitios estancados tachados además de en rojo.

## Barreras por comprobar

La orientación actual a ratón y tooltips necesita revisión para otras formas de
entrada. La paleta usa colores semánticos y el mapa contiene efectos animados;
no se ha comprobado equivalencia sin color, reducción de movimiento, lectura con
tecnologías de asistencia ni recorrido completo con teclado. La versión Unity no tiene
todavía equivalente al nombre accesible que Godot daba a los controles para el lector de
pantalla de escritorio (`accessibility_name`): es una regresión del port por resolver
antes de Steam.

El texto visible está en inglés. Idiomas de publicación, mandos (habituales en Steam) y
garantías de accesibilidad siguen abiertos; el escalado lo elige el jugador desde 0.3.5.
Los requisitos del antiguo prompt web no prueban que estas capacidades estén presentes en
el port.

## Prueba de comprensión propuesta

Pedir a una persona nueva que inicie, explique una alerta, encuentre un sitio sin
apoyo, use la pausa y diga por qué aumenta awareness. Después pedir que anticipe
si su plan llegará a tiempo. Registrar dudas y errores antes de explicarle la regla.
Repetir las tareas críticas con sonido desactivado y revisar dependencia del color.

Fuentes: [pantalla](../../../unity/Assets/Awakening/UI/Screens/GameScreen.cs), [panel regional](../../../unity/Assets/Awakening/UI/Screens/RegionPanel.cs),
[diálogos](../../../unity/Assets/Awakening/UI/Screens/Dialogs.cs), [notas](../../../unity/Assets/Awakening/UI/Text/Notes.cs) y [controles](../../../README.md).

## `alignment-3` — 2026-11-12

- **Respuestas a una amenaza.** El recorte del mapa y la ficha de amenaza del expediente
  muestran tres sellos: «OBSTRUCT · 30 INF», «SCATTER · 8 MO» y «SILENCE · 20 DREAD». En el
  expediente cada sello lleva debajo lo que cuesta además del precio, o el motivo por el que
  no está disponible. Las tres filas aparecen en la pestaña Cult solo mientras hay amenaza.
- **Awareness como precio.** La fila de una acción que suma awareness la muestra en rojo
  bajo el precio («+8 AWARENESS») y la explicación la repite en su propia línea.
- **Célula dispersa.** El expediente dice «SCATTERED · N MO»; reclutar y hacerse público
  muestran «SCATTERED N MO»; la ficha del sitio explica que está detenido por eso.

Comprobado con `dev/layout_check.gd` en los perfiles de 0.3.5; véase el
[registro de versiones](../../playtests/versiones.md).

## Port a Unity (0.5.0, 2026-11-18)

Misma composición, textos y comportamiento que 0.4.0, rehechos en UI Toolkit. Para el
jugador cambian tres cosas:

- **Tooltips propios.** UI Toolkit no muestra tooltips fuera del editor, así que el juego
  dibuja los suyos tras una breve pausa, como Godot. En táctil no hay tooltips: un toque
  abre la explicación, como antes.
- **Lector de pantalla.** Sin equivalente todavía (véase «Barreras por comprobar»).
- **Escritorio.** El ejecutable nativo usa el diseño de ratón aunque Linux declare
  pantalla táctil; el web conserva la detección táctil.

Comprobado con `unity/shots.sh --check-ui 1` y `unity/layout-check.sh` (60 combinaciones);
detalle en [verificación del port](../../unity/verificacion.md).

## Legibilidad y recortes (0.5.1, 2026-11-18)

Correcciones vistas en capturas de la build de desarrollo 0.5.0; sin cambios de reglas.

- **Mapa.** Nombres y cuentas atrás con una copia de papel detrás de la tinta; seguidores
  en fichas de papel opaco. La región seleccionada o bajo el puntero recupera tinta plena y
  un subrayado. Las etiquetas esquivan los avisos y quedan dentro de la vista; si no caben
  cerca de su región, esperan en lugar de alejarse de ella.
- **Amenazas.** La columna muestra hasta dos amenazas completas en escritorio y una en
  compacto, con «+N more threats · review» para las restantes. Si falta espacio pasa a una
  tarjeta breve que remite al expediente, y recupera el detalle cuando vuelve el espacio.
- **Pronóstico.** Ocupa una franja reservada sobre el libro mayor, fuera del centro del
  mapa, y se oculta al abrir una hoja modal.
- **Bloqueos.** Las compras y respuestas no disponibles pierden el relleno activo, pero
  conservan tinta legible y el motivo del bloqueo.
- **Expediente y libro mayor.** Estadísticas y People quedan en un pie fijo en escritorio;
  en compacto se desplazan con una indicación de que quedan notas. El ticker de la crónica
  se limita a dos líneas, con el texto completo en el tooltip y en la hoja Chronicle;
  durante la alineación cede su espacio al botón del rito.

Comprobado con `unity/layout-check.sh` (60/60) y capturas a 1600×900 y 844×390 táctil;
sin prueba en teléfono físico. Detalle en el [registro de versiones](../../playtests/versiones.md).

## Onboarding y reintento (0.6.0, ola 1 — 2026-11-21)

Respuesta a la revisión de gameplay (F1 y F2), sin automatizar decisiones ni conceder recompensas por perder:

- **Colocación inicial.** El subtítulo ya no empuja a empezar en una región pequeña; explica la regla real: las regiones pobladas reclutan bien y las pequeñas sostienen sitios con menor coste de seguidores.
- **Primera decisión económica.** Al fundar la primera célula, la nota contextual `first_cell` lee los valores reales del estado y de `GameData` (Influence disponible, coste de reunir seguidores y coste de abrir una célula vecina) para plantear la primera disyuntiva sin decidir por el jugador.
- **Pestañas del expediente.** La primera vez que se abre un expediente de región en partida activa, una nota breve bajo las pestañas explica qué agrupa **CULT**, **RITES** y **OMENS**; se registra en `settings.hints` (`file_tabs`) y cada pestaña mantiene además su tooltip descriptivo.
- **Metas con fecha para las primeras partidas.** Una ficha discreta sobre el mapa guía cuatro metas encadenadas calculadas con el calendario de `Format`: tres células antes de enero de 1911, primer sacrificio, primer sitio antes de enero de 1917 (con la pista de que las regiones pequeñas sostienen sitios con facilidad) y tres sitios antes de enero de 1923. El jugador puede ocultarlas desde la propia ficha o alternarlas en el menú; el ajuste `hideGoals` se guarda en `SaveSettings` con compatibilidad hacia atrás para guardados anteriores. La ficha cede espacio automáticamente a amenazas, notas contextuales y etiquetas del mapa.
- **Reintentar y hecho fechado.** La pantalla final añade **«Try this world again»** junto a **«New World»** para reiniciar con la misma semilla y la misma dificultad, e incluye un hecho fechado derivado del estado o de la crónica que explica el desenlace sin otorgar bonificaciones tras perder.

## Margen del rito, cierre anticipado y ocultar (0.6.0, ola 2 — 2026-11-21)

Respuesta a la revisión de gameplay (R1 y L1 en interfaz), derivada directamente de `Forecast.Margin`, `GameEngine.CanConcede` y `GameEngine.ConcealWorldCooldown`:

- **Banda del margen del rito.** Vive integrada en la franja reservada del pronóstico sobre el libro mayor, sin cubrir el mapa ni romper el diseño compacto. Muestra los tres sitios requeridos como fichas compactas (seguidores/requisito cuando falta apoyo, estado de dispersión o letargo y meses pendientes), el Dread reservado sobre 100 (`DREAD X/100`) y la línea de calendario con `Format` (`Earliest rite · latest · margin`, o indicación de aceleración necesaria cuando `Margin < 0` con `Fits` verdadero). Al pasar el cursor (escritorio) o al tocar una ficha (escritorio y táctil) se despliega el detalle completo del sitio con seguidores sobre el requisito, apoyo/dispersión, meses restantes y acceso directo al expediente (`OPEN FILE`). Su presencia visual escala según el estado: discreta con margen holgado, resaltada en ámbar cuando el margen baja a 6 meses o menos (o hay un sitio detenido/urgencia), y crítica en rojo cuando el rito ya no cabe.
- **Aviso cuando el rito ya no cabe.** Cuando `!Fits` y el rito no ha comenzado, la franja muestra el titular **«The stars will pass without you»** junto con la causa concreta (plazo superado, sitios insuficientes o retraso del tercer sitio sin aceleración disponible). El aviso también aparece en el diálogo de alineación.
- **Cerrar la partida (`CanConcede`).** Solo cuando `GameEngine.CanConcede` es verdadero aparece el sello **«LET THE STARS PASS»** (en la franja y en el diálogo de alineación). Abre un diálogo de confirmación (**«Let the Stars Pass»** / **«Keep Watching»**) que pausa el reloj y, al confirmar, llama a `GameSession.Concede()`. La pantalla final distingue el cierre (**«The Stars Passed Unanswered»**) y deriva su propio hecho fechado en `Notes.DatedFact` para `ResultCode == "conceded"`.
- **Sello de ocultar (`ConcealWorldCooldown`).** Mientras el efecto mundial de `Conceal the evidence` descansa, tanto su fila en el expediente como el panel de detalle muestran **«GLOBAL EFFECT IN N MO»** además del texto de la vista previa del motor.

## Árbol de talentos, contención, amenaza acorralada, Mysticism y dificultad (0.7.0 — 2026-11-21)

Interfaz de `alignment-5` (U1), alimentada directamente por `GameData`, `GameEngine`, `Threats` y `Derived`:

- **Hoja del árbol de talentos (`Dialogs.Talents`).** Reemplaza el antiguo diálogo de doctrina y el botón del libro mayor (`Talents`, atajo `T`, con `D` como alias). Presenta las tres ramas (**Congregation**, **Hidden hand** y **Harbinger**) con sus cuatro nodos cada una (raíz de nivel I, dos nodos medios excluyentes de nivel II y remate de nivel III), unidos con conectores de tinta. La cabecera muestra los sellos de la dificultad como indicadores visuales, el conteo de sellos abiertos y sin usar (`TalentSlotsOpen`, `TalentSlotsUnused`), la fecha y meses restantes del próximo sello (`NextTalentSlotMonth`) y el precio en Influence del siguiente nodo (`TalentCost`). Cada nodo distingue su estado real: tomado (`◆ TAKEN`), disponible con botón de compra (`Take · X Inf`), cerrado por exclusión (`× CLOSED` con la razón exacta de `TalentReason`), bloqueado por requisito o sello, o corto de Influence. En escritorio se muestran las tres columnas a la vez; en compacto/teléfono horizontal las ramas usan pestañas superiores y los dos nodos medios aparecen lado a lado con objetivos táctiles amplios. Al tomar un talento la hoja se actualiza en el sitio para mostrar de inmediato la exclusión del nodo hermano.
- **Aviso de sello en el libro mayor y nota inicial.** Cuando existe al menos un sello abierto sin usar, el libro mayor destaca el aviso **«1 SEAL UNBROKEN»** (o **«N SEALS UNBROKEN»**); cuando no quedan sellos sin usar muestra **«SEAL IN N MO»** o **«ALL SEALS SPENT»**. La nota contextual `talents` sustituye a la antigua nota de doctrina la primera vez que se abre un sello y explica que el árbol no se completa en una partida y que cada par intermedio cierra a su hermano.
- **Acciones `Bury the record` y `Restore the site` en el expediente.** `Bury the record` se suma a **CULT** con precio creciente, beneficio de reducción de awareness (`−5 AWARENESS` / `−8` con *Bought press*) y coste/efecto/riesgo desde `PreviewAction`. `Restore the site` se suma a **RITES**: sin meses perdidos aparece atenuada (`NO LOST PREP`), y cuando `Site.Recoverable > 0` destaca con borde y lavado verdigris, etiqueta `+N MO LOST` y un sello directo **«RESTORE · +N MO · X INF»** dentro de la tarjeta del sitio ritual.
- **Amenaza acorralada (`Threats.Options` y `Threats.Cornered`).** Tanto el recorte de prensa sobre el mapa como la ficha de amenaza del expediente muestran desde que aparece la amenaza las tres respuestas con su precio (incluido el plazo de dispersión con *Sleeper cells*) y el motivo concreto por el que las no disponibles están bloqueadas (p. ej. `counter rests 9 more months`, `need 40 Dread`). Cuando `Threats.Cornered` es verdadero (no se puede obstruir ni silenciar), ambos muestran la marca destacada **«CORNERED · ONLY SCATTER OR ENDURE»** (o **«CORNERED · NO ANSWER READY»** si tampoco se puede dispersar).
- **Mysticism en el expediente.** La barra de Mysticism y la línea descriptiva `Derived.MysticismNote(regionId)` se muestran tanto en el perfil de colocación inicial como en el expediente de toda partida activa.
- **Reglas de dificultad en el inicio.** Al seleccionar **Easy**, **Normal** o **Hard** en `StartScreen`, un recuadro bajo el selector muestra `DifficultyDefinition.Rule` con las reglas que cambian las decisiones (sellos, pérdida por sociedad oculta, incremento de entierros y descanso mundial de ocultar), además de su tooltip en cada botón.

## Remates, rendimiento por talento y rito detenido (0.8.0 — 2026-11-22)

Interfaz de `alignment-6` (U1), alimentada por `GameData.InvokableTalentIds`, `GameEngine` (`InvokeReason`, `InvokeRisk`, `InvokeCapstone`, `TalentYieldText`, `ProcessionRegions`, `RiteMonthsFor`, `InProcession`, `DreamAdvances`) y `Forecast.Rite`:

- **Remates diferenciados en el árbol (`Dialogs.Talents`).** Cada nodo de nivel III (**The Great Procession**, **The world forgets** y **Dreams of R'lyeh**) recibe marco reforzado en tinta de presagio/culto y un recuadro estructurado con **WHEN** (condición), **EFFECT** (efecto) y **COST** (precio), además de la descripción del motor.
  - **The Great Procession:** muestra en vivo cuántas regiones tienen culto público respecto a las 5 requeridas (`ProcessionRegions`) y si la condición del rito de 8 meses se cumple en ese momento; la franja del margen del rito añade la chapa `PROCESSION N/5`, y tanto el botón del libro mayor como el diálogo de alineación muestran el chequeo de procesión y el rito de 8 meses al iniciarlo.
  - **The world forgets:** una vez tomado, muestra el botón **«INVOKE»** dentro de su tarjeta del árbol y un acceso rápido (**«CAPSTONE · INVOKE»**) en el libro mayor mientras esté disponible (`InvokeReason == ""`). Ambos abren un diálogo de confirmación (`Dialogs.InvokeConfirm`) que resume el efecto inmediato (bajada de awareness y amenazas disueltas) y destaca el precio completo de `InvokeRisk` (dispersión de 6 meses y detención del rito si está en curso). Tras invocarse, el acceso rápido se oculta y la tarjeta en el árbol pasa a estado **«× SPENT»** con su rendimiento acumulado.
  - **Dreams of R'lyeh:** durante el rito final, la fila de `Sacrifice followers` en la pestaña **RITES** y su panel de detalle indican si sacrificar en esa región adelanta el rito (`DreamAdvances` → **«+1 RITE MONTH»** / recuadro **«DREAMS OF R'LYEH · ADVANCES RITE +1 MO»** con el progreso resultante y `+2` de awareness) o si no lo adelanta (**«NO RITE ADVANCE»** / **«DOES NOT ADVANCE»** cuando quedarían menos de tres sitios con apoyo).
- **Rendimiento por talento (`TalentYieldText`).** Cada talento tomado muestra en su tarjeta del árbol una banda con su rendimiento real en la partida (por ejemplo, `0 Dread so far`, `60 Influence saved so far`, `Invoked: −25 world awareness` o `1 rite months dreamed forward so far`).
- **Rito detenido (`Forecast.Rite`) y riesgo al dispersar o sacrificar.** La barra superior, el libro mayor, la barra de progreso y la franja del rito usan la duración real (`Progress / Required`, 8 o 12 meses). Cuando `Forecast.Rite` indica que el rito no avanza, pasan a rojo con **«RITE HALTED»** y un recuadro que enumera cada región bloqueadora y su causa (`scattered`, `unsupported`, `dormant`, `destroyed`, `unprepared`), con acceso directo al expediente si un sitio fue destruido. Al dispersar (`scatter`) o sacrificar (`sacrifice`) en una región cuyo sitio sostiene el rito activo, el aviso de detención del rito proveniente de `PreviewAction.Risk` se destaca en rojo negrita tanto en el recorte de amenaza del mapa (`HALTS RITE · N MO`) como en la tarjeta de amenaza, en la fila de acción (`HALTS FINAL RITE`) y en un recuadro propio dentro del detalle del expediente (`HALTS THE FINAL RITE`).
- **Consistencia de notas y cierre (`Notes.cs`).** Las causas de cierre, la línea de margen, la pantalla final y el hecho fechado derivan los meses requeridos de `RiteMonthsFor` / `Forecast.Rite` y reconocen el inicio de *The Great Procession*. Se retiró la referencia obsoleta a *Offerings to the deep* en el expediente.

[Volver al índice](../README.md)
