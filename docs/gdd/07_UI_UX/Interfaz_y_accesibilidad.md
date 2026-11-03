# Interfaz, aprendizaje y accesibilidad

**Versión:** 0.4 · **Fecha:** 2026-11-03 · **Estado:** flujo Godot documentado; panel táctil con desplazamiento desde 0.3.2; escala de teléfono original restaurada en 0.3.4; usabilidad por validar.

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

Se volvió a la escala original y solo se conservó el panel táctil con desplazamiento. El texto pequeño en teléfonos sigue pendiente.

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
