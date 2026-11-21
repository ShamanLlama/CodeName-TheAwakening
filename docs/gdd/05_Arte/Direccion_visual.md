# Dirección visual

**Versión:** 0.3 · **Fecha:** 2026-11-18 · **Estado:** dirección de época confirmada; legibilidad por validar.

## Lenguaje visual actual

Dirección confirmada el 2026-10-30: el archivo secreto de un culto sobre un escritorio
iluminado por una lámpara, entre 1908 y 1926. El mapa es una carta de navegación antigua:
mar oscuro dentro de un marco con doble filete, tierra en pergamino manchado, rosa de los
vientos y la marca de R'lyeh en el Pacífico Sur. Cada panel es un documento: expediente
(región), recortes de prensa (amenazas), telegramas (avisos), libro mayor (barra inferior)
y hojas impresas con marco (diálogos). Todo se dibuja mediante código, sin texturas
incluidas en la build: en Unity el mapa se traza con `Painter2D`, y el grano del papel y la
luz de la lámpara se generan al iniciar como texturas en memoria. En Godot (0.2.0–0.4.0)
eran shaders; el aspecto es el mismo, con métrica de fuentes y textura del papel algo
distintas.

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
[Palette](../../../unity/Assets/Awakening/UI/Kit/Palette.cs).

Legibilidad (0.5.1): los nombres y cuentas atrás del mapa llevan una copia de papel
detrás de la tinta, y los seguidores, fichas de papel opaco; las manchas grandes del papel
se atenuaron. Los botones y las cifras usan Old Standard o Courier Prime.

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

Fuentes: [mapa](../../../unity/Assets/Awakening/UI/Map/WorldMap.cs), [paleta](../../../unity/Assets/Awakening/UI/Kit/Palette.cs),
[papel](../../../unity/Assets/Awakening/UI/Kit/PaperGrain.cs), [README de Unity](../../../unity/README.md) y licencias
tipográficas en [Licenses](../../../unity/Assets/Awakening/Licenses/).

[Volver al índice](../README.md)
