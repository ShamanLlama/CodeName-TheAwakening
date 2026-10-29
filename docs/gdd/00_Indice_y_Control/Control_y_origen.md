# Control, origen y adaptación

**Versión:** 0.2 · **Fecha:** 2026-10-28 · **Estado:** documentación de lo implementado en curso.
**Proyecto:** The Awakening of Cthulhu · **Estudio:** Lucid Labworks Studio.
**Responsable de aprobar visión y alcance:** por confirmar con dirección del proyecto.

## Estados y autoridad

- **Implementado:** observado en los archivos del proyecto; no equivale a validado con jugadores.
- **Propuesta:** recomendación de esta revisión; requiere una decisión antes de convertirse en requisito.
- **Por definir:** no hay evidencia suficiente para completar la decisión.
- **Histórico:** describe otra revisión; se conserva con su contexto.

La versión de trabajo es Markdown en `docs/gdd/`. El [índice](../README.md) es la
entrada canónica. La versión 0.2 identifica esta revisión documental; `alignment-1`
identifica el conjunto de reglas de la implementación.

Para saber qué hace hoy el juego, manda el código. Para definir qué debería hacer,
registramos una decisión de diseño y luego verificamos su implementación. Una diferencia
entre ambos se registra como trabajo pendiente; no se resuelve cambiando silenciosamente
el significado de una regla. Los números de ejemplo son fotografías, nunca otro archivo
de configuración.

## Plantilla utilizada

Se tomó la edición 1.0 de **Plantillas de GDD de Game Time Continuum**, encontrada en:

`/mnt/1tb-p/Proyects/GTC-page/Contenido gratis web/GDD_GTC/`

Se eligió la entrega `Markdown/`. Se copiaron y adaptaron las fichas pertinentes,
consolidando sus preguntas y comprobaciones en los documentos del índice. La fuente
original permanece intacta. El [permiso de uso copiado](PERMISO_DE_USO_GTC.md) permite
copiar, modificar y combinar el material.

| Base de la plantilla GTC | Adaptación para este juego |
| --- | --- |
| Índice, guía, control y registro de cambios | Índice único, este control y decisiones pendientes |
| Resumen del juego, High Concept, objetivos | Una ficha de resumen y visión |
| Pilares, core loops y sistemas | Ficha de experiencia conectada a las referencias de reglas existentes |
| Estructura de niveles, ritmo y playtesting | Mapa estratégico, regiones, adyacencias y fases de campaña |
| Pilares narrativos y mundo | Narrativa sistémica; personajes y capítulos se abrirán si el diseño los necesita |
| Dirección visual | Mapa, iconografía, interfaz y efectos; sin fichas de modelado o animación 3D |
| Dirección de audio | Feedback procedural actual; música y voz como decisiones abiertas |
| Principios de UX y accesibilidad | Flujos, jerarquía, barreras y pruebas específicas de Godot |
| Stack y arquitectura | Base técnica con enlaces al README de desarrollo |
| Alcance y riesgos | Inventario actual, propuesta de validación y dependencias |
| Público objetivo y modelo de negocio | Hipótesis y preguntas, sin inventar precio, ventas ni campaña |

Se omiten por ahora fichas vacías de niveles lineales, combate espacial, elenco,
cinemáticas, rigging, voces, sprints, presupuesto, publishers y postlanzamiento.
Se añadirán cuando una decisión real las necesite. La estructura se adapta al juego.

## Fuentes consultadas y límites

Base de esta revisión: commit local `b32c57d`, README del proyecto, README de Godot,
capítulos previos del GDD, datos de regiones, dificultades, acciones y balance,
motor/director TypeScript y scripts de interfaz, paleta y audio de Godot.

La revisión comprueba documentación y código fuente. No incluye una nueva partida,
prueba de usabilidad, ejecución de la batería de tests ni investigación de mercado.
Las afirmaciones de pruebas anteriores se atribuyen a sus registros, no a esta revisión.

Comprobación documental de esta revisión: 108 enlaces locales en 25 documentos
(`docs/GDD.md` y `docs/gdd/`) resuelven a destinos existentes. Se revisaron diferencias
y espacios en blanco. Esto no comprueba el contenido de una build ejecutada.

## Registro de cambios

| Fecha | Versión | Cambio | Motivo |
| --- | --- | --- | --- |
| 2026-10-28 | 0.1 | Adaptación de las áreas GTC, primer llenado, preguntas y enlaces a reglas existentes | Ampliar el GDD desde reglas implementadas hacia diseño de producto |
| 2026-10-28 | 0.1 | Aclaración del efecto del sacrificio sobre awareness | Reduce su crecimiento futuro; no resta conocimiento ya acumulado |
| 2026-10-28 | 0.2 | D01: documentar primero lo implementado | Prioridad confirmada por dirección antes de abrir cambios de diseño |
| 2026-10-28 | 0.2 | D02: objetivo de 15–30 minutos por partida | Separar intención de duración de los siete minutos actuales de simulación más pausas; medición por precisar |
| 2026-10-28 | 0.2 | D03: mezcla de estrategia, narrativa y horror | Registrar la experiencia buscada sin fijar una prioridad relativa ni comprometer contenido nuevo |

Al resolver una pregunta, actualizar su ficha y el
[registro de decisiones](Decisiones_pendientes.md), indicando respuesta, fecha,
motivo y documentos afectados. Las nuevas propuestas aún no están aprobadas.

[Volver al índice](../README.md)
