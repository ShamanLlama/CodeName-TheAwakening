# Dirección visual

**Versión:** 0.2 · **Fecha:** 2026-10-30 · **Estado:** dirección de época confirmada; legibilidad por validar.

## Lenguaje visual actual

Dirección confirmada el 2026-10-30: el archivo secreto de un culto sobre un escritorio
iluminado por una lámpara, entre 1908 y 1926. El mapa es una carta de navegación antigua:
mar oscuro dentro de un marco con doble filete, tierra en pergamino manchado, rosa de los
vientos y la marca de R'lyeh en el Pacífico Sur. Cada panel es un documento: expediente
(región), recortes de prensa (amenazas), telegramas (avisos), libro mayor (barra inferior)
y hojas impresas con marco (diálogos). Godot dibuja todo mediante código; el grano del
papel es un shader sin texturas.

El calendario muestra fechas reales: la partida empieza en enero de 1908, la alineación
se abre en septiembre de 1924 y se cierra en septiembre de 1926, alrededor del 23 de marzo
de 1925, la noche en que R'lyeh emerge en *The Call of Cthulhu*. Solo cambia la
presentación; las reglas cuentan meses como antes.

El tono de época se limita al marco: los nombres de las mecánicas (Influence, Dread,
Followers, Obstruct) no cambian. Tipografía: IM Fell English para títulos, Old Standard TT
para el texto y Courier Prime para los datos mecanografiados.

La paleta asigna un acento por significado, con una tinta para el papel y un tono claro
para lo oscuro: verdín para el culto, sangre de buey para el peligro, violeta para
presagios y Dread, ocre para advertencias. Los valores exactos pertenecen a
[Palette](../../../godot/ui/palette.gd).

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
[README Godot](../../../godot/README.md) y licencias tipográficas en [fonts](../../../godot/fonts/).

[Volver al índice](../README.md)
