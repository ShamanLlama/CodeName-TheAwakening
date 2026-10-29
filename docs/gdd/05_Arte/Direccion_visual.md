# Dirección visual

**Versión:** 0.1 · **Fecha:** 2026-10-28 · **Estado:** descripción del prototipo; identidad final en revisión.

## Lenguaje visual actual

Mapa mundial oscuro, paneles superpuestos, marcadores de culto, rutas, sitios,
amenazas y presagios. Godot dibuja los elementos gráficos mediante código sobre
geometría geográfica; la tipografía utiliza Barlow y Barlow Condensed.
El README de Godot cita Plague Inc. como referencia de presentación estratégica,
sin que eso defina todavía una dirección artística final.

La paleta implementada asigna un acento por significado: verde ácido para culto y
crecimiento, rojo para peligro, violeta para presagios y Dread, ámbar para advertencias.
Los fondos oscuros y bordes discretos separan mapa, información y controles.
Los valores exactos pertenecen a [Palette](../../../godot/ui/palette.gd).

## Función visual propuesta

- Hacer reconocible la región seleccionada, su situación y sus amenazas.
- Separar evidencia local, conocimiento global y preparación ritual mediante
  etiquetas y señales además del color.
- Mantener visibles los plazos mientras los efectos del mapa aportan atmósfera.

Estas son prioridades propuestas para revisión; no constituyen una auditoría de
legibilidad ni de accesibilidad ya superada.

## Producción y decisiones abiertas

Confirmar si el dibujo procedural es una elección final o temporal, qué identidad
debe tener el culto y cómo representar el despertar. Ilustraciones, retratos,
animaciones y cinemáticas requieren una decisión de alcance antes de abrir fichas.
No se hereda como requisito artístico la dirección de la marca GTC: GTC aporta
la plantilla documental y este juego pertenece a Lucid Labworks Studio.

**Propuesta de revisión:** comparar inicio, desarrollo y rito a 1280×720 y 1600×900.
Pedir a una persona que localice el peligro más urgente y un sitio interrumpido.
Revisar color, tamaño y animación según el resultado.

Fuentes: [mapa](../../../godot/map/world_map.gd), [paleta](../../../godot/ui/palette.gd),
[README Godot](../../../godot/README.md) y [licencia tipográfica](../../../godot/fonts/OFL-Barlow.txt).

[Volver al índice](../README.md)
