# Base técnica y límites

**Versión:** 0.4 · **Fecha:** 2026-11-25 · **Estado:** arquitectura vigente en Unity 6.3 LTS (`alignment-7`, juegos 0.9.0 / 0.9.1).

## Motor y módulos

Desde `0.5.0` el juego vive en `unity/`, con **Unity 6.3 LTS (`6000.3.25f1`)**, C# y UI
Toolkit (solo versiones LTS, nada experimental — D12). Las reglas vigentes son `alignment-7`
(`0.9.0` publicada como demo público WebGL en itch.io; `0.9.1` en progreso). La versión
Godot 4.7 (`0.2.0`–`0.4.0`) permanece en `godot/` y en la rama `godot` como referencia
histórica. El prototipo React/TypeScript de `src/` dejó de ser referencia en `alignment-2`.

| Módulo | Responsabilidad |
| --- | --- |
| `unity/Assets/Awakening/Core/Rules/` | Estado (`GameState`), constantes (`Balance`), catálogos (`GameData`), simulación y acciones (`GameEngine`), cálculos derivados (`Derived`), director (`Director`), amenazas (`Threats`), previsiones (`Forecast`) y RNG determinista (`Rng`). **Sin dependencias de Unity.** |
| `unity/Assets/Awakening/Core/` | Sesión de juego (`GameSession`), guardado (`SaveData`) y captura/telemetría de playtest (`PlaytestCapture`, `PlaytestOutbox`, `PlaytestStore`) |
| `unity/Assets/Awakening/Platform/` | Persistencia en IndexedDB (WebGL) o archivo con candado (escritorio) y envío de lotes a GTC Playtest Lab |
| `unity/Assets/Awakening/UI/` | Pantallas (`StartScreen`, `GameScreen`, `RegionPanel`, `Dialogs`), mapa procedural (`WorldMap`), textos/notas (`Notes`), estilos (`Palette`, `PaperGrain`) y audio (`Sfx`) |
| `core-tests/` | Pruebas unitarias y trazas grabadas (`dotnet test core-tests/Tests`: 310 pruebas en 0.9.1) y herramienta de medición/bot (`core-tests/Tools`: `measure`, `trace`, `regenerate-goldens`) |

Toda acción se previsualiza y valida mediante `GameEngine.PreviewAction` antes de ejecutarse
en `GameEngine.PerformAction`. Las reglas son deterministas y puras respecto al estado y la
semilla, independientes de cualquier panel de UI.

## Datos, guardado y telemetría

- **Conservación de población:** los cinco grupos regionales (`Unaware`, `Deniers`,
  `Believers`, `Followers`, `Losses`) suman siempre la capacidad fija de la región
  (`PopulationWeight × 100`). Modificar seguidores sin transferir desde otro grupo rompe
  el cálculo de apoyo de sitios.
- **Fuente única de parámetros:** [Balance.cs](../../../unity/Assets/Awakening/Core/Rules/Balance.cs)
  y [GameData.cs](../../../unity/Assets/Awakening/Core/Rules/GameData.cs). Con `alignment-7`,
  `dotnet run --project core-tests/Tools -- measure --seeds=48` reproduce 83/144 victorias
  del bot en Normal (142/144 en Easy, 47/144 en Hard).
- **Guardado (`save-v2`):** en WebGL usa IndexedDB (heredando ajustes, tutorial y
  consentimiento desde versiones previas); en escritorio escribe `save-v2.json` bajo
  `Application.persistentDataPath`. Al cambiar de ruleset se retiran las partidas en curso
  anteriores con aviso, conservando ajustes. Sin backend de cuentas ni nube por ahora.
- **Playtest Lab:** adaptador vigente `awakening-6`, con consentimiento explícito del jugador
  y contrato verificado en `GTC-page/gtc-web/contracts/playtests/awakening-6/`.

## Validación y pendientes técnicos aplazados

El [README de Unity](../../../unity/README.md) documenta pruebas (`dotnet test`), capturas
automatizadas (`unity/shots.sh --check-ui 1`), chequeo de layout (`unity/layout-check.sh`,
60/60 combinaciones) y exportación WebGL/Linux.

**Aplazado hasta acercarse al cierre (D07 / D26):**
- Exportación y pruebas en **Windows** para Steam.
- Integración de **Steamworks** (si entra en Camino 2).
- Soporte completo de **mando/teclado** y equivalencia de **lector de pantalla** (regresión
  conocida del port desde Godot).
- Arquitectura de localización/idiomas y soporte de datos para múltiples Dioses Antiguos
  (Camino 2) y eventos escritos con pausa (D13).

[Volver al índice](../README.md)
