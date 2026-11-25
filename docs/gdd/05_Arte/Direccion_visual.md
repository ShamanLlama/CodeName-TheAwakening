# Dirección visual

**Versión:** 0.4 · **Fecha:** 2026-11-25 · **Estado:** dirección de época confirmada (D09); arte procedural actual marcado explícitamente como **temporal** (D06).

## Dirección estética confirmada (D09)

El archivo secreto de un culto sobre un escritorio iluminado por una lámpara entre **1908 y
1926**. El mapa es una carta de navegación antigua (mar oscuro, doble filete, pergamino,
rosa de los vientos y marca de R'lyeh en el Pacífico Sur) y cada panel funciona como un
documento físico: expediente (región), recortes de prensa (amenazas), telegramas (avisos),
libro mayor (barra inferior) y hojas impresas (diálogos y árbol de talentos).

El tono de época vive en el marco; los nombres de las mecánicas no cambian. La paleta asigna
un acento semántico por función (verdín para el culto, sangre de buey para peligro, violeta
para presagios/Dread/Madness, ocre para advertencias) en
[Palette.cs](../../../unity/Assets/Awakening/UI/Kit/Palette.cs). Tipografía actual:
IM Fell English, Old Standard TT y Courier Prime.

## Base temporal implementada (0.5.0–0.9.0)

Todo el prototipo actual se dibuja mediante código en Unity UI Toolkit (`Painter2D` para el
mapa, grano de papel y luz de lámpara generados en memoria al iniciar), sin ilustraciones ni
assets artísticos dedicados. Incluye indicadores legibles en mapa para seguidores, alertas,
regiones con `Madness` (`MAD N%`, velado violeta) y sitios acelerados (anillo y `>>`).

## Plan de arte final (D06 y D26 — futuro)

**Decisión confirmada el 2026-11-25:** el arte procedural actual es **temporal**. El objetivo
es contratar artistas profesionales para llevar la identidad visual a calidad de lanzamiento:

- **En Camino 2 (Unity Fellowship de 50.000 USD):** contratar **1 diseñador gráfico /
  ilustrador** (equipo en Ecuador: **$3.000 USD · 2 meses**) para cubrir con una sola línea
  visual coherente tanto la UI como las ilustraciones del juego, con alcance cerrado para no
  salirse del presupuesto:
  1. **UI del archivo 1908–1926:** piezas reutilizables 9-slice para expedientes, libro mayor,
     recortes de amenazas, marco del árbol de talentos, 16 iconos de acciones, 12+ iconos y
     sellos de talentos, estampas de amenazas/presagios y medidores, conservando el layout ya
     validado (60/60 combinaciones escritorio/táctil).
  2. **Arte e ilustraciones del juego:** carta cartográfica ilustrada, emblemas/presencias de
     los **3 Old Gods** (Cthulhu, Nyarlathotep y Shub-Niggurath), **feedback visual progresivo
     de horror** (capas/viñetas al subir `Awareness` y `Madness`), 4–6 ilustraciones de
     finales/despertar y viñetas para eventos escritos futuros (D13).
- **En Camino 1 (sin grant):** mantener la base visual actual pulida (o con mejoras puntuales
  de bajo coste) para cerrar una experiencia completa de Cthulhu en itch.io.

## Principios que el arte final debe preservar

- Reconocer de un vistazo la región seleccionada, sus amenazas, su `Madness` y los sitios
  detenidos o acelerados.
- No depender únicamente del color para estados críticos (etiquetas, símbolos y jerarquía).
- Mantener legibles todos los plazos a 1280×720, 1600×900 y pantallas compactas.

Fuentes: [WorldMap.cs](../../../unity/Assets/Awakening/UI/Map/WorldMap.cs),
[Palette.cs](../../../unity/Assets/Awakening/UI/Kit/Palette.cs),
[PaperGrain.cs](../../../unity/Assets/Awakening/UI/Kit/PaperGrain.cs) y licencias en
[Licenses](../../../unity/Assets/Awakening/Licenses/).

[Volver al índice](../README.md)
