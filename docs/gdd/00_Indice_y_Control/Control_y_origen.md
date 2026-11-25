# Control, origen y adaptación

**Versión:** 0.6 · **Fecha:** 2026-11-25 · **Estado:** GDD consolidado sobre la implementación vigente (`alignment-7`, juego 0.9.0 / 0.9.1).
**Proyecto:** The Awakening of Cthulhu · **Estudio:** Shaman Llama (Shaman Llama Games / Shaman Llama SAS, Ecuador).
**Responsable de aprobar visión y alcance:** dirección del proyecto.

## Estados y autoridad

- **Implementado:** observado en los archivos del proyecto; no equivale a validado con jugadores externos.
- **Planeado / Futuro:** dirección aprobada para etapas posteriores; todavía no existe en el código.
- **Propuesta:** recomendación de trabajo; requiere confirmación antes de convertirse en requisito.
- **Por definir:** no hay decisión o evidencia suficiente todavía.
- **Histórico:** describe una revisión anterior; se conserva con su contexto.

La versión de trabajo es Markdown en `docs/gdd/`. El [índice](../README.md) es la
entrada canónica. La versión `0.6` identifica esta revisión documental; `alignment-7`
identifica el conjunto de reglas vigente en Unity (juegos `0.9.0` y `0.9.1`).

Para saber qué hace hoy el juego, manda el código en `unity/Assets/Awakening/Core/Rules/`.
Para definir qué debería hacer, registramos una decisión en
[Decisiones pendientes](Decisiones_pendientes.md) y luego verificamos su implementación. Una
diferencia entre ambos se registra como trabajo pendiente; no se oculta reescribiendo el
significado de una regla. Los números en el GDD son fotografías fechadas, nunca un segundo
archivo de configuración.

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
| Pilares, core loops y sistemas | Ficha de experiencia conectada a los capítulos de reglas consolidados (`01`–`07`) |
| Estructura de niveles, ritmo y playtesting | Mapa estratégico, regiones, adyacencias y fases de campaña |
| Pilares narrativos y mundo | Narrativa sistémica vigente + diseño futuro de eventos escritos |
| Dirección visual | Prototipo procedural actual y plan de arte/UI contratados |
| Dirección de audio | Feedback procedural actual y plan de banda sonora/SFX con grant |
| Principios de UX y accesibilidad | Flujos, jerarquía, soporte táctil y pruebas de la interfaz implementada |
| Stack y arquitectura | Base técnica de Unity 6.3 LTS con enlaces al README de desarrollo |
| Alcance y riesgos | Inventario actual, beta abierta y los dos caminos de producción (base vs. Unity Grant) |
| Público objetivo y modelo de negocio | Beta pública en itch.io, hipótesis de audiencia y ruta comercial a Steam con grant |

Se omiten por ahora fichas vacías de niveles lineales, combate espacial, rigging, doblaje,
sprints detallados o postlanzamiento. Se abrirán cuando el avance del proyecto las necesite.

## Fuentes consultadas y límites

- **Revisión 0.2 (2026-10-28 a 2026-11-12):** commit `b32c57d` y prototipo Godot `0.2.0`–`0.4.0`.
- **Revisión 0.3 (2026-11-18):** port a Unity `0.5.0`–`0.5.1` (`alignment-3`).
- **Revisiones 0.4 y 0.5 (2026-11-21 a 2026-11-23):** reglas `alignment-4` a `alignment-7` (juegos `0.6.0`–`0.9.0`).
- **Revisión 0.6 (2026-11-25):** consolidación documental contra `unity/Assets/Awakening/Core/Rules/` (`Balance.cs`, `GameData.cs`, `Derived.cs`, `Director.cs`, `Threats.cs`, `Forecast.cs`, `GameEngine.cs`) con `alignment-7`, cambio visible `South America` (`0.9.1`), apertura pública en itch.io (2026-11-24) y decisiones estratégicas de dirección del 2026-11-25 (D06, D07, D13, D25, D26).

La revisión comprueba documentación y código fuente; no reemplaza las pruebas humanas de la
beta pública ni la verificación en teléfono físico o ejecutable de Windows.

## Registro de cambios

| Fecha | Versión | Cambio | Motivo |
| --- | --- | --- | --- |
| 2026-10-28 | 0.1 | Adaptación de las áreas GTC, primer llenado, preguntas y enlaces a reglas existentes | Ampliar el GDD desde reglas implementadas hacia diseño de producto |
| 2026-10-28 | 0.1 | Aclaración del efecto del sacrificio sobre awareness | Reduce su crecimiento futuro; no resta conocimiento ya acumulado |
| 2026-10-28 | 0.2 | D01–D03: documentar primero lo implementado; 15–30 min; mezcla estrategia/narrativa/horror | Fijar prioridades iniciales con dirección |
| 2026-10-30 | 0.2 | D09: época 1908–1926 y archivo del culto | Dirección visual y calendario real, sin renombrar mecánicas |
| 2026-11-02 | 0.2 | `alignment-2` y D10 | Mantenimiento de sitios, ocultamiento según awareness y caza de sitios (juego 0.3.0) |
| 2026-11-12 | 0.2 | `alignment-3`, D04, D05, D08 y D11 | Tres respuestas a cada amenaza y awareness como precio (juego 0.4.0) |
| 2026-11-18 | 0.3 | D12 y revisión contra el juego 0.5.1 | Port a Unity 6.3 LTS, Steam como salida principal e itch.io para pruebas |
| 2026-11-21 | 0.3 | `alignment-4`, decisiones del usuario y D13–D17 | Ocultar con plazo mundial, margen del rito y cierre anticipado (juego 0.6.0); árbol intra-partida, eventos que pausan, Mysticism y definición de «completo» |
| 2026-11-21 | 0.3 | `alignment-5` y D15 | Árbol de talentos en lugar de doctrina, `bury`, `restore`, amenazas acorraladas, Mysticism y dificultades diferenciadas (juego 0.7.0) |
| 2026-11-22 | 0.4 | `alignment-6`, D18–D20 y revisión general | Remates diferenciados, rendimiento por talento, rito detenido visible y Normal más exigente (juego 0.8.0) |
| 2026-11-23 | 0.5 | `alignment-7`, D21–D24 y revisión general | Madness como recurso, Dreams of R'lyeh antes del rito, respiro de 5 meses tras responder y Easy con sitios de 20 meses (juego 0.9.0) |
| 2026-11-25 | 0.6 | Consolidación de capítulos `01`–`06` a `alignment-7`, corrección de desfases (`0.9.0`/`0.9.1`, demo público), ratificación de D18–D24 como implementadas, cierre de D04 (juegas como el Old God), A–H como futuras features y decisiones D06, D13, D25 y D26 (con costos de equipo en Ecuador) | Dejar una lectura limpia y única de lo implementado frente a las futuras mejoras, fijar identidad del jugador, filosofía de balance single-player y los dos caminos (itch.io base vs. Unity Grant $50k) |

Al resolver una pregunta, actualizar su ficha y el
[registro de decisiones](Decisiones_pendientes.md), indicando respuesta, fecha,
motivo y documentos afectados.

[Volver al índice](../README.md)
