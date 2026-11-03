# Interfaz, aprendizaje y accesibilidad

**Versión:** 0.3 · **Fecha:** 2026-11-03 · **Estado:** flujo Godot documentado; pantallas cortas y táctiles adaptadas en 0.3.2, mapa ampliado en 0.3.3; usabilidad por validar.

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

## Pantallas cortas y táctiles (0.3.2, 2026-11-02)

El diseño base es 1600×900. Antes se reducía entero a cualquier pantalla: en un teléfono horizontal (~844×390 px CSS) un texto de 15 quedaba en unos 6,5 px. Ahora `main._fit_window()` reduce la altura base hasta 520 para que una unidad nunca mida menos de 0,75 px CSS, usando la densidad del navegador. Con menos de 760 de altura base se activa el modo compacto (`Kit.compact`): los textos de lectura crecen (`Kit.fs`, mínimo 15), las barras son más bajas, el teletipo inferior queda en una línea, el título de inicio se reduce, y el formulario de inicio y los diálogos se desplazan dentro de su hoja con barra visible. En vertical, un aviso pide girar el teléfono. Una ventana de escritorio normal no cambia.

En el panel de región, las acciones y su explicación van antes de las estadísticas. Con ratón, pasar sobre una acción la explica y hacer clic la ejecuta. En pantallas táctiles (`Kit.touch`) un toque solo abre la explicación bajo esa acción, con un botón para ejecutarla; otro toque la cierra. El panel se desplaza arrastrando, y un arrastre no pulsa el botón bajo el dedo. Primera prueba en teléfono físico (0.3.2, 2026-11-03): el texto se lee, pero el mapa quedaba muy pequeño.

Mapa en pantallas cortas (0.3.3): la vista general recorta las latitudes polares (se ven de 50° S a 72° N) y llena la altura disponible; la barra superior baja a 46. Al abrir una región, el mapa ya no se encoge al lado del panel: se amplía 1,7 veces sobre esa región, centrada en la parte que el panel no tapa, con una transición corta; al cerrar el panel vuelve a la vista general. Así el texto mantiene el tamaño de 0.3.2 y el mapa gana espacio. En escritorio el mapa sigue completo al lado del panel.

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
