# Dirección de audio

**Versión:** 0.3 · **Fecha:** 2026-11-25 · **Estado:** SFX procedurales temporales implementados; banda sonora y diseño sonoro contratados en Camino 2 (D06, D26).

## Base temporal actual

El juego sintetiza efectos de sonido al iniciar en
[Sfx.cs](../../../unity/Assets/Awakening/UI/Audio/Sfx.cs): clic, acción, rechazo, amenaza,
presagio, sitio y final, sobre cuatro reproductores simultáneos. El menú permite silenciar y
ajustar volumen. No hay música, ambientes ni voces en el prototipo actual.

## Plan de audio final (D06 y D26 — futuro)

- **En Camino 2 (Unity Fellowship de 50.000 USD):** contratar **1 ingeniero de audio**
  (equipo en Ecuador: **$3.000 USD · 2 meses**) para banda sonora y efectos de sonido con
  paquete acotado de entregas:
  - **Banda sonora original (3–4 pistas adaptativas en bucle):** menú/inicio, preparación,
    tensión alta (`Awareness`/`Madness`) y alineación/rito final, acorde al tono lovecraftiano
    de 1908–1926 y pensada para no fatigar en sesiones de 15–30 minutos.
  - **Banco de SFX y capas ambientales (25–35 efectos con feedback progresivo de horror):**
    archivo/papel, sellos del árbol de talentos, amenazas, sacrificios, susurros/ambiente de
    `Madness` y `Awareness` crecientes, eventos escritos y desenlaces de los 3 Old Gods.
- **En Camino 1 (sin grant):** conservar y afinar el sistema ligero actual para no bloquear
  el cierre de la versión base de Cthulhu.

## Función sonora y comprobación

1. **Jerarquía de lectura:** advertencias de plazo o amenaza > hitos de fase (alineación,
   rito detenido/completado, sello de talento) > confirmaciones de acción > ambiente/música.
2. **Sin dependencia exclusiva del audio:** toda señal crítica debe seguir siendo legible en
   texto y gráficos con el juego silenciado.
3. **Pruebas futuras:** verificar varias amenazas simultáneas, repetición de acciones y
   transición al rito para evitar fatiga auditiva o pérdida de alertas.

[Volver al índice](../README.md)
