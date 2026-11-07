# Interfaz, aprendizaje y accesibilidad

**Versión:** 0.5 · **Fecha:** 2026-11-06 · **Estado:** revisión de UX/UI en 0.3.5 (tamaño de interfaz, layout compacto, notas de primera vez, motivos de bloqueo, final con causa, mapa con zoom); usabilidad por validar con personas y en teléfono físico.

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
o abre menú. Perder el foco pausa la partida. Al comenzar el rito, Godot reanuda a
velocidad normal. El menú incluye guía, sonido y exportación del reporte de playtest.

## Principios propuestos

1. Antes de pagar, entender coste, consecuencia y riesgo local.
2. Distinguir seguidores, exposición y awareness sin tener que memorizar sus fórmulas.
3. Entender qué falta para el rito y si el calendario todavía permite completarlo.

**Error que el motor ya previene:** financiar un sitio cuando no queda tiempo suficiente
para prepararlo y completar el rito, considerando las aceleraciones disponibles.
**Decisión permitida con riesgo:** sacrificar seguidores que sostienen un sitio.

## Teléfonos y pantallas táctiles (0.3.4, 2026-11-03)

El diseño base es 1600×900 y se reduce entero a cualquier pantalla, también en el teléfono. En el panel de región, las acciones y su explicación van antes de las estadísticas. Con ratón, pasar sobre una acción la explica y hacer clic la ejecuta. En pantallas táctiles (`Kit.touch`) un toque solo abre la explicación bajo esa acción, con un botón para ejecutarla; otro toque la cierra. El panel se desplaza arrastrando, un arrastre no pulsa el botón bajo el dedo, y la barra de desplazamiento es más ancha y visible.

Intentos descartados tras probarlos en un teléfono físico (2026-11-03):

- **0.3.2:** reducir la altura base hasta 520 en pantallas cortas (una unidad ≥ 0,75 px CSS), con textos más grandes y barras más bajas. El texto se leía, pero el mapa quedaba muy pequeño.
- **0.3.3:** sobre lo anterior, recortar las latitudes polares y ampliar 1,7 veces la región abierta en vez de encoger el mapa. No convenció.

Se volvió a la escala original y solo se conservó el panel táctil con desplazamiento. En 0.3.5 el tamaño pasa a ser una opción del jugador (ver abajo); falta confirmarlo en un teléfono físico.

## Revisión de UX/UI (0.3.5, 2026-11-06)

Respuesta a una evaluación con nota 6,5/10. Lo que cambia para el jugador:

- **Por qué no puedo:** cada acción no disponible muestra en su fila el obstáculo real («NO CELL HERE», «NO THREAT», «NEED 30 INF», «IN 4 MO»). Si falta la célula, la amenaza o el omen, eso se dice antes que el precio. Los motivos se derivan en `ui/notes.gd` sin tocar el motor.
- **Aprender jugando:** la guía inicial queda en cinco frases. El resto llega como notas de una frase la primera vez que aparece cada situación. Ninguna pausa el reloj, cada una se ve una vez por dispositivo y se guarda en `settings.hints`.
- **Cerrar la partida:** la derrota explica la causa principal y da uno o dos consejos concretos. La victoria tiene su propia hoja.
- **Mapa:** contorno de región, etiquetas que no se tapan, zoom con rueda o pellizco y arrastre. R o «Whole world» vuelve a la vista completa.
- **Tamaño de interfaz:** 100–200 %, elegible en el menú y, en táctil, en el inicio. Por debajo de 1240×700 unidades se activa el layout compacto. El valor por defecto sigue en 100 % para respetar lo decidido en 0.3.4: a diferencia de 0.3.2, la escala la elige el jugador, y a diferencia de 0.3.3, el zoom del mapa lo controla él.
- **Accesibilidad:** movimiento reducido, foco de teclado en diálogos, atajos (Tab, ↑ ↓ Enter, ← →, C, D, H, R), contraste mínimo de 4,5:1 en avisos y de 3,5:1 en tinta tenue, y sitios estancados tachados además de en rojo.

## Barreras por comprobar

La orientación actual a ratón y tooltips necesita revisión para otras formas de
entrada. La paleta usa colores semánticos y el mapa contiene efectos animados;
no se ha comprobado equivalencia sin color, reducción de movimiento, lectura con
tecnologías de asistencia ni recorrido completo con teclado en Godot.

El texto visible está en inglés. Idiomas de publicación, escalado de texto elegido por
el jugador, mandos y garantías de accesibilidad siguen abiertos. Los requisitos del
antiguo prompt web no prueban que estas capacidades estén presentes en el port.

## Prueba de comprensión propuesta

Pedir a una persona nueva que inicie, explique una alerta, encuentre un sitio sin
apoyo, use la pausa y diga por qué aumenta awareness. Después pedir que anticipe
si su plan llegará a tiempo. Registrar dudas y errores antes de explicarle la regla.
Repetir las tareas críticas con sonido desactivado y revisar dependencia del color.

Fuentes: [pantalla](../../../godot/ui/game_screen.gd), [panel regional](../../../godot/ui/region_panel.gd),
[diálogos](../../../godot/ui/dialogs.gd) y [controles](../../../README.md).

[Volver al índice](../README.md)
