# Interfaz, aprendizaje y accesibilidad

**Versión:** 0.7 · **Fecha:** 2026-11-21 · **Estado:** interfaz de 0.3.5–0.4.0 rehecha en Unity UI Toolkit (0.5.0), corregida en legibilidad (0.5.1) y ampliada en onboarding y reintento (0.6.0, ola 1); usabilidad por validar con personas, en teléfono físico y en el ejecutable para Steam.

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

[Volver al índice](../README.md)
