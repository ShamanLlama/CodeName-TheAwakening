# Control, origen y adaptación

**Versión:** 0.5 · **Fecha:** 2026-11-23 · **Estado:** documentación de lo implementado en curso.
**Proyecto:** The Awakening of Cthulhu · **Estudio:** Lucid Labworks Studio.
**Responsable de aprobar visión y alcance:** por confirmar con dirección del proyecto.

## Estados y autoridad

- **Implementado:** observado en los archivos del proyecto; no equivale a validado con jugadores.
- **Propuesta:** recomendación de esta revisión; requiere una decisión antes de convertirse en requisito.
- **Por definir:** no hay evidencia suficiente para completar la decisión.
- **Histórico:** describe otra revisión; se conserva con su contexto.

La versión de trabajo es Markdown en `docs/gdd/`. El [índice](../README.md) es la
entrada canónica. La versión 0.3 identifica esta revisión documental; `alignment-3`
identifica el conjunto de reglas de la implementación (juegos 0.4.0–0.5.1).

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
| Principios de UX y accesibilidad | Flujos, jerarquía, barreras y pruebas de la interfaz implementada |
| Stack y arquitectura | Base técnica con enlaces al README de desarrollo |
| Alcance y riesgos | Inventario actual, propuesta de validación y dependencias |
| Público objetivo y modelo de negocio | Hipótesis y preguntas, sin inventar precio, ventas ni campaña |

Se omiten por ahora fichas vacías de niveles lineales, combate espacial, elenco,
cinemáticas, rigging, voces, sprints, presupuesto, publishers y postlanzamiento.
Se añadirán cuando una decisión real las necesite. La estructura se adapta al juego.

## Fuentes consultadas y límites

Base de la revisión 0.2: commit local `b32c57d`, README del proyecto, README de Godot,
capítulos previos del GDD, datos de regiones, dificultades, acciones y balance,
motor/director TypeScript y scripts de interfaz, paleta y audio de Godot.

Base de la revisión 0.3 (2026-11-18): commit `12f8f16` más los cambios locales de 0.5.1;
`Balance.cs`, `GameData.cs`, `Director.cs`, `Threats.cs` y `GameEngine.cs` de
`unity/Assets/Awakening/Core/Rules/`, y la interfaz, el audio y la paleta de
`unity/Assets/Awakening/UI/`. Se ejecutó `measure --seeds=48` (103/144, igual que la
fotografía de `alignment-3`).

La revisión comprueba documentación y código fuente. No incluye una nueva partida,
prueba de usabilidad, ejecución de la batería de tests ni investigación de mercado.
Las afirmaciones de pruebas anteriores se atribuyen a sus registros, no a esta revisión.

Comprobación documental de esta revisión: 108 enlaces locales en 25 documentos
(`docs/GDD.md` y `docs/gdd/`) resuelven a destinos existentes. Se revisaron diferencias
y espacios en blanco. Esto no comprueba el contenido de una build ejecutada.
En la revisión 0.3 (2026-11-18), 133 enlaces locales en los mismos 25 documentos
resuelven a destinos existentes.

## Registro de cambios

| Fecha | Versión | Cambio | Motivo |
| --- | --- | --- | --- |
| 2026-10-28 | 0.1 | Adaptación de las áreas GTC, primer llenado, preguntas y enlaces a reglas existentes | Ampliar el GDD desde reglas implementadas hacia diseño de producto |
| 2026-10-28 | 0.1 | Aclaración del efecto del sacrificio sobre awareness | Reduce su crecimiento futuro; no resta conocimiento ya acumulado |
| 2026-10-28 | 0.2 | D01: documentar primero lo implementado | Prioridad confirmada por dirección antes de abrir cambios de diseño |
| 2026-10-28 | 0.2 | D02: objetivo de 15–30 minutos por partida | Separar intención de duración de los siete minutos actuales de simulación más pausas; medición por precisar |
| 2026-10-28 | 0.2 | D03: mezcla de estrategia, narrativa y horror | Registrar la experiencia buscada sin fijar una prioridad relativa ni comprometer contenido nuevo |
| 2026-10-30 | 0.2 | D09: época 1908–1926 y archivo del culto | Dirección visual y calendario real, sin renombrar mecánicas |
| 2026-11-02 | 0.2 | `alignment-2` y D10 | Mantenimiento de sitios, ocultamiento según awareness y caza de sitios (juego 0.3.0) |
| 2026-11-12 | 0.2 | `alignment-3`, D04, D05, D08 y D11 | Tres respuestas a cada amenaza y awareness como precio (juego 0.4.0); juego gratuito y completo |
| 2026-11-21 | 0.3 | `alignment-4`, decisiones del usuario y D05 | Ocultar con plazo mundial, margen del rito y cerrar la partida (juego 0.6.0); árbol dentro de la partida, eventos que pausan, efecto para Mysticism y definición de «completo» |
| 2026-11-21 | 0.3 | `alignment-5` y D15 | Árbol de talentos en lugar de la doctrina, Bury the record, Restore the site, amenazas acorraladas, Mysticism y dificultades que cambian reglas (juego 0.7.0), tras la primera partida humana de 0.6.0 |
| 2026-11-22 | 0.4 | `alignment-6`, D18–D20 y revisión general | Remates que cambian la partida, rendimiento por talento, rito detenido visible y Normal más exigente (juego 0.8.0), tras la primera partida humana de 0.7.0 |
| 2026-11-23 | 0.5 | `alignment-7`, D21–D24 y revisión general | Madness como recurso (Dread, sitios más rápidos, ocultistas), Dreams of R'lyeh antes del rito, respiro de 5 meses tras obstruir o silenciar, y Easy con sitios de 20 meses y talentos más baratos (juego 0.9.0), tras dos partidas humanas de 0.8.0 rendidas en el mes 200 |
| 2026-11-18 | 0.3 | D12 y revisión contra el juego 0.5.1 | Port a Unity, Steam como salida principal e itch.io para la beta. Se actualizaron motor, rutas de código, interfaz y plataformas; reglas y valores ya coincidían con `Core/Rules` y el bot reproduce las 103/144 victorias registradas |

Al resolver una pregunta, actualizar su ficha y el
[registro de decisiones](Decisiones_pendientes.md), indicando respuesta, fecha,
motivo y documentos afectados. Las nuevas propuestas aún no están aprobadas.

[Volver al índice](../README.md)
