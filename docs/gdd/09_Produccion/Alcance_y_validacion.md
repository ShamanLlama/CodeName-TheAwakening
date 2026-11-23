# Alcance, riesgos y validación

**Versión:** 0.4 · **Fecha:** 2026-11-22 · **Estado:** documentación de lo implementado como primera etapa; producción final por definir.

## Base disponible

Existe un ciclo completo de partida con mapa de ocho regiones, economía, población,
acciones, amenazas, oportunidades, árbol de talentos de tres ramas con remates (desde 0.7.0 y 0.8.0), rituales, condiciones de resultado,
guardado, guía, crónica y reporte de playtest. Desde 0.5.0 la versión jugable es la de
Unity 6.3 LTS; la de Godot (0.4.0) se conserva en la rama `godot` hasta retirarla.
Los registros previos de pruebas se conservan en el repositorio.

La beta se prueba en itch.io por invitación, con rondas de GTC Playtest Lab por build. La
salida principal prevista es Steam; la integración con Steamworks y los requisitos de la
tienda quedan para después de la paridad del port.

No hay compromiso nuevo de contenido por copiar una ficha de la plantilla.
Presupuesto, capacidad del equipo, responsables por área, fecha y definición de
la primera entrega publicable se confirmarán con dirección.

## Secuencia propuesta de trabajo

1. Documentar y contrastar lo implementado antes de proponer cambios. Esta prioridad
   fue confirmada por dirección (D01); se hizo con `alignment-1` en Godot y se mantiene
   al día con cada ruleset.
2. Medir después sesiones reales frente al objetivo confirmado de 15–30 minutos y
   observar la mezcla de estrategia, narrativa y horror. Precisar el cómputo de pausas.
3. Si se decide abrir una etapa de diseño, elegir un problema observable y comparar
   un cambio con la base. Todavía no hay un rediseño aprobado.
4. Actualizar las fichas según las decisiones y evidencias de esa etapa.
5. Definir alcance de presentación y publicación con estimaciones de trabajo.

Es una secuencia de validación, sin fechas ni funcionalidades nuevas aprobadas.
El [registro de implementación](../../implementation-checklist.md) sigue siendo
la referencia histórica del trabajo realizado.

## Riesgos que ya justifican atención

| Riesgo o incertidumbre | Evidencia y límite | Próxima comprobación propuesta |
| --- | --- | --- |
| Oportunidades poco utilizadas | Ignoradas en los playtests de `alignment-1`; usadas 2–5 veces por partida en 0.3.x | Observar descubrimiento y utilidad en la interfaz Unity actual |
| Ramas o dificultades desiguales | Bot 0.9.0: Normal 27/35/21 de 48 por rama; Hard 4/31/12, con late casi igual que en Normal; tres nodos sin política de bot | Rondas con jugadores externos; reparto de ramas, remates y Madness en el Lab (`awakening-6`) |
| Awareness difícil de entender | La regla puede castigar crecimiento que parece beneficioso | Preguntar al jugador por su causa antes de la derrota |
| Final poco legible | En 0.7.0 el jugador no entendió que dispersar un sitio detiene el rito; 0.8.0 lo muestra | Comprobar con `rite_month` si los meses detenidos bajan y si el jugador lo entiende |
| Sitios que no llegan a tiempo | En 0.8.0 el jugador se rindió dos veces en el mes 200 con uno o dos sitios; 0.9.0 suma Madness, respiro y sitios de 20 meses en Easy | Medir con `first_site_ready`, `quickened_sites` y `grace_months` si los sitios llegan antes del mes 188 |
| Ritmo alejado de la intención | Objetivo de 15–30 minutos con pausas incluidas (decisión del 2026-11-21). Partidas humanas: 14 minutos (0.6.0) y 22 minutos (0.7.0) | Seguir midiendo con el reloj del Lab (simulación y planificación) |
| Mezcla de experiencia sin validar | Estrategia, narrativa y horror confirmados como objetivo; peso y efecto actuales sin comprobar | Registrar decisiones estratégicas y sucesos recordados, y preguntar por la sensación de horror |
| Presentación y alcance abiertos | Arte procedural, narrativa y oferta sin decisión final | Resolver D04–D08 antes de producir contenido adicional |

Responsables y fechas de estas comprobaciones: por asignar. Los detalles observados
y las hipótesis originales están en [Open problems](../09-open-problems.md).

## Cómo leer la evidencia anterior

El [informe de septiembre](../../playtests/endgame-balance-2026-09-06.md) mide políticas
automatizadas sobre una base de 600 meses y otros costes. No describe la economía
actual de 200 meses. Las rondas del Lab con Godot (0.2.0–0.4.0) y con Unity (desde 0.5.0)
usan las mismas reglas solo cuando comparten ruleset; el registro de versiones lo indica. El [rationale](../08-rationale.md) documenta cambios posteriores.
Los resultados automatizados permiten comprobar comportamientos y explorar balance;
no reemplazan pruebas humanas de comprensión, ritmo o atractivo.

**Registro propuesto para nuevas sesiones:** revisión del juego, semilla, dificultad,
talentos y remates, región inicial, duración real, pausas, decisiones relevantes, acciones
ignoradas, motivo de derrota/victoria y explicación del jugador. El reporte actual
aporta acciones y estado; el tiempo real debe registrarse aparte.

## Definición de «completo» (decisión del usuario, 2026-11-21)

El juego está completo cuando:

- el árbol de talentos está probado;
- gente externa lo juega;
- el balance se siente parejo;
- hay varios caminos para ganar y varias formas de perder.

El árbol tiene que dar opciones, ninguna partida puede completarlo y cada modo de dificultad tiene que hacer pensar al jugador. Easy no tiene que ganarse a la primera, pero no debe abrumar. Estado al 2026-11-23: árbol con remates y Madness en prueba (0.9.0), un solo jugador interno, sin rondas externas todavía.

## Criterio propuesto para cerrar esta primera revisión

Visión acordada, preguntas de alto impacto respondidas o explícitamente aplazadas,
reglas enlazadas y una lista corta de hipótesis comprobables. Antes de declarar una
versión publicable harán falta criterios específicos de calidad, plataforma y alcance.

[Volver al índice](../README.md)
