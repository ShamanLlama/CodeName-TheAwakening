# Base técnica y límites

**Versión:** 0.2 · **Fecha:** 2026-11-18 · **Estado:** referencia de la arquitectura desde 0.5.0 (port a Unity).

## Motor y módulos

Desde 0.5.0 el juego está en `unity/`, con Unity 6.3 LTS (6000.3.25f1), C# y UI Toolkit. Exporta WebGL para itch.io y un ejecutable de escritorio (Linux por ahora) pensado para Steam. Las reglas son las mismas de 0.4.0 (`alignment-3`); el port no cambió el diseño. La versión Godot 4.7 (0.2.0–0.4.0) sigue en `godot/` como referencia y en la rama `godot`. El prototipo React/TypeScript de `src/` dejó de ser referencia en `alignment-2`.

| Módulo | Responsabilidad |
| --- | --- |
| `unity/Assets/Awakening/Core/` | Estado, balance, acciones, simulación, director, previsiones, guardado, captura de playtest y sesión de juego. Sin dependencias de Unity |
| `unity/Assets/Awakening/Platform/` | Guardado en IndexedDB (web) o en archivo con candado (escritorio) y envío de lotes al Lab |
| `unity/Assets/Awakening/UI/` | Controlador, pantallas, mapa, diálogos, estilos y sonido |
| `core-tests/` | Pruebas de reglas, guardado y playtest con `dotnet`, incluidas las trazas grabadas |
| `godot/` | Versión Godot de referencia, con las trazas grabadas en `godot/tests/golden/` |

Una acción se previsualiza y valida en el motor antes de ejecutarse. El nuevo estado se refleja en la interfaz y se guarda. Las reglas no dependen de ningún panel. La semilla y la secuencia de comandos permiten reproducir resultados.

## Datos y guardado

La población se distribuye entre ajenos, negadores, creyentes, seguidores y pérdidas. Su suma conserva la capacidad regional. Modificar seguidores sin trasladar población rompe también el cálculo de apoyo de los sitios.

Los parámetros generales están en [Balance](../../../unity/Assets/Awakening/Core/Rules/Balance.cs); acciones, regiones y dificultades en [GameData](../../../unity/Assets/Awakening/Core/Rules/GameData.cs). Un cambio de reglas se hace en Core, se regeneran las trazas (`core-tests/Tools`, `regenerate-goldens`) y se registra un ruleset nuevo.

El guardado es el mismo sobre `save-v2` de Godot. En el navegador se guarda en la misma base IndexedDB y con la misma clave que usaba la build Godot, así que la build Unity hereda ajustes, tutorial, consentimiento y registros pendientes. En escritorio va a `save-v2.json` bajo `Application.persistentDataPath`. No hay backend, cuenta ni sincronización de partidas.

## Validación y dependencias

El [README de Unity](../../../unity/README.md) documenta ejecución, pruebas, capturas, chequeo de layout y exportaciones. Las diez trazas grabadas reproducen estado, vistas previas, resultados y textos idénticos a Godot, y el bot da las mismas victorias. Eso comprueba paridad de casos registrados; no demuestra calidad de experiencia ni cobertura de todos los escenarios. La verificación del port está en [docs/unity/verificacion.md](../../unity/verificacion.md).

La geometría usa Natural Earth y las fuentes tienen su licencia en `unity/Assets/Awakening/Licenses/`. Presupuestos de memoria, rendimiento y equipos mínimos de publicación están por medir; la carga web se midió en emulación (ver la verificación).

[Volver al índice](../README.md)
