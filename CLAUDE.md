# The Awakening of Cthulhu — instrucciones del proyecto

## Cambios y playtest

Todo cambio al juego se refleja en el sistema de playtest en el mismo cambio, según [docs/playtests/versiones.md](docs/playtests/versiones.md):

- Subir `config/version` en `godot/project.godot` (menor para reglas/balance, parche para UI/texto).
- Cambio de reglas o balance → nuevo ruleset; cambio de acciones, regiones, doctrinas o eventos → nuevo adaptador y contrato GTC (`../../GTC-page/gtc-web/contracts/playtests/`).
- Agregar la entrada con fecha, versión, commit, ruleset, adaptador, cambios, motivo y medición.

## Diseño

Respetar los pilares de `docs/gdd/01-overview.md` y `docs/gdd/02_Game_Design/Experiencia_y_loops.md`. Revisar el GDD antes de cambiar reglas y actualizarlo junto con el cambio.
