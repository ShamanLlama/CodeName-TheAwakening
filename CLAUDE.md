# The Awakening of Cthulhu — instrucciones del proyecto

## Cambios y playtest

Todo cambio al juego se refleja en el sistema de playtest en el mismo cambio, según [docs/playtests/versiones.md](docs/playtests/versiones.md):

- Subir `bundleVersion` en `unity/ProjectSettings/ProjectSettings.asset` (menor para reglas/balance, parche para UI/texto). Desde 0.5.0 el juego es el proyecto Unity en `unity/`; `godot/` queda como referencia hasta retirarlo y la rama `godot` conserva la versión Godot.
- Cambio de reglas o balance → nuevo ruleset; cambio de acciones, regiones, doctrinas o eventos → nuevo adaptador y contrato GTC (`../../GTC-page/gtc-web/contracts/playtests/`).
- Agregar la entrada con fecha, versión, commit, ruleset, adaptador, cambios, motivo y medición.
- Al publicar, la build nueva en GTC Playtest Lab lleva un resumen de esa entrada en «cambios de esta build».

## Motor

Unity 6.3 LTS (6000.3.25f1); solo versiones LTS y nada marcado como experimental. Las reglas viven en `unity/Assets/Awakening/Core/` sin dependencias de Unity y se prueban con `dotnet test core-tests/Tests`. Guía de builds, pruebas y capturas en [unity/README.md](unity/README.md); plan y verificación del port en [docs/unity/](docs/unity/).

## Diseño

Respetar los pilares de `docs/gdd/01-overview.md` y `docs/gdd/02_Game_Design/Experiencia_y_loops.md`. Revisar el GDD antes de cambiar reglas y actualizarlo junto con el cambio.
