# Base técnica y límites

**Versión:** 0.1 · **Fecha:** 2026-10-28 · **Estado:** referencia de la arquitectura existente.

## Motor y módulos

El juego está en `godot/`, con Godot 4.7, GDScript y renderer Compatibility. Exporta
para navegador, Linux y Windows. El prototipo React/TypeScript en `src/` conserva
el motor de reglas de referencia. No se introduce una migración tecnológica en este GDD.

| Módulo | Responsabilidad |
| --- | --- |
| `godot/core/` | Estado, balance, acciones, simulación, director, previsiones y guardado |
| `godot/map/` | Proyección, geometría e interacción con el mapa |
| `godot/ui/` | HUD, paneles, diálogos, estilos y sonido |
| `src/game/` y `src/data/` | Motor y datos TypeScript de referencia |
| `godot/tests/` y `src/tests/` | Pruebas de reglas y trazas para comparar motores |

Una acción se previsualiza y valida en el motor antes de ejecutarse. El nuevo estado
se refleja en la interfaz y se guarda. Las reglas no deberían depender de un panel
concreto. La semilla y la secuencia de comandos permiten reproducir resultados.

## Datos y guardado

La población se distribuye entre ajenos, negadores, creyentes, seguidores y pérdidas.
Su suma conserva la capacidad regional. Modificar seguidores sin trasladar población
rompe también el cálculo de apoyo de los sitios.

Los parámetros generales están en [balance TypeScript](../../../src/data/balance.ts)
y [Balance Godot](../../../godot/core/balance.gd); acciones, regiones y dificultades
tienen datos adicionales. El flujo de cambios documentado parte de TypeScript,
regenera las trazas y porta la regla a Godot. El GDD no sustituye esos datos.

Godot guarda en `user://save-v2.json`, con IndexedDB en navegador. Los guardados
LocalStorage del prototipo web no se importan. No se añade backend, cuenta ni
sincronización de partidas como requisito de esta adaptación.

## Validación y dependencias

El [README Godot](../../../godot/README.md) documenta ejecución, pruebas, exportaciones,
dependencias y diferencias frente a la versión web. Describe nueve trazas grabadas
en TypeScript que se reproducen en Godot y comparan estados, comandos, textos y guardado.
Eso comprueba paridad de casos registrados; no demuestra calidad de experiencia ni
cobertura de todos los escenarios.

La geometría usa Natural Earth y las fuentes tienen su licencia junto al proyecto.
Las exportaciones requieren las plantillas compatibles del motor. Presupuestos de
memoria, rendimiento y equipos mínimos de publicación están por medir.

Esta revisión modificó documentación; no ejecutó pruebas de motor ni generó builds.
Si se cambia una regla después de resolver el diseño, seguir las comprobaciones
del README, además de la validación con jugadores.

[Volver al índice](../README.md)
