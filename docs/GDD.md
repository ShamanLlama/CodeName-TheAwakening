# Game Design Document

- **[GDD consolidado (un solo archivo para leer de corrido)](GDD_consolidado.md):** reúne todo
  el diseño vigente (`v0.9.1`, `alignment-7`), los 3 Old Gods, los eventos escritos futuros y
  el roadmap de 12 meses en un solo documento.
- **[GDD modular (índice por capítulos)](gdd/README.md):** versión de trabajo separada por
  áreas (adaptación de las plantillas de Game Time Continuum) usada para mantener cada sistema
  al día junto con el código.

Empieza por el [resumen y visión](gdd/01_Informacion_General/Resumen_y_vision.md),
[experiencia y loops](gdd/02_Game_Design/Experiencia_y_loops.md) y
[decisiones pendientes](gdd/00_Indice_y_Control/Decisiones_pendientes.md).
Los capítulos de reglas ([01](gdd/01-overview.md)–[06](gdd/06-ritual-sites.md)) describen
directamente la implementación vigente (`alignment-7`, Unity `0.9.0`–`0.9.1`). Las funciones
futuras y los dos caminos de producción se distinguen expresamente de lo ya implementado.

Los valores ejecutables viven en el código (`unity/Assets/Awakening/Core/Rules/`); el GDD
explica sus funciones y mantiene una [fotografía fechada de balance](gdd/07-balance.md).
Véanse [control y origen](gdd/00_Indice_y_Control/Control_y_origen.md) y
[base técnica](gdd/08_Programacion_y_Technical_Design/Base_tecnica.md).

Los diseños [MVP](archive/GDD_mvp_proposal.md) y
[legacy](archive/GDD_legacy_prototype.md) son antecedentes archivados.
El [plan](implementation-plan.md), el [registro de implementación](implementation-checklist.md)
y los [playtests](playtests/) conservan evidencia con el contexto de sus versiones.
