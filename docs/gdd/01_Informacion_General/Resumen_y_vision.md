# Resumen y visión

**Versión:** 0.2 · **Fecha:** 2026-10-28 · **Estado:** base implementada y objetivos confirmados; detalles de dirección pendientes.

## Concepto en una frase

Dirige un culto mundial de Cthulhu, convierte seguidores en poder ritual y prepara
su despertar antes de que termine la alineación astral o la humanidad descubra la red.

## Identidad actual

- **Título de trabajo:** The Awakening of Cthulhu.
- **Estudio:** Lucid Labworks Studio.
- **Género:** estrategia para un jugador sobre mapa mundial, con simulación en tiempo
  real, pausa y economía de crecimiento.
- **Cámara y escala:** vista global de ocho regiones; gestión abstracta de población,
  sin unidades individuales controlables.
- **Base jugable:** Godot 4.7, reglas `alignment-1`. Existe un prototipo React/TypeScript
  que se conserva como referencia de reglas.
- **Plataformas del proyecto actual:** navegador orientado a itch.io, con exportaciones
  Linux y Windows. Esto no prueba que exista un lanzamiento público.
- **Duración actual:** unos siete minutos de simulación a 1× sin pausas; el tiempo
  total de sesión todavía no se ha medido en esta revisión.

## Propuesta jugable

El jugador decide dónde iniciar, crecer, sacrificarse, permanecer oculto y preparar
sitios rituales. Necesita una red capaz de sostener el despertar, pero cada seguidor
también aumenta el conocimiento global. Los rituales atraen oposición; proteger una
región consume recursos y conexiones que tardan en recuperarse.

Tres rasgos resumen la base: crecimiento que puede volverse peligroso, sacrificios
que financian el objetivo debilitando la economía y sitios que exigen elegir dónde
conservar seguidores. El final exige sostener sitios preparados durante el rito.

**Ejemplo:** sacrificar en una región genera Dread para financiar otro sitio. Si se
sacrifica donde el apoyo ritual era justo, ese sitio se interrumpe. El jugador tiene
que decidir dónde producir, dónde sacrificar y dónde conservar una reserva.

## Objetivos confirmados y diferencia con la base

Dirección confirmó documentar primero lo implementado. El objetivo de duración es
**15–30 minutos por partida**, con una mezcla de **estrategia, narrativa y horror**.
Falta precisar cómo se contabilizan las pausas dentro de ese rango.

La base ya ofrece planificación, tensión y recuperación frente al calendario, y una
narrativa sistémica mediante eventos y crónica. Todavía no se ha validado que consiga
la mezcla buscada ni la duración objetivo. Esta decisión no autoriza por sí sola
alargar el calendario o añadir contenido: primero se documenta el funcionamiento actual.

La identidad del jugador, la voz narrativa y el tono específico del horror siguen
pendientes. No se ha fijado una prioridad relativa entre las tres dimensiones.

Tampoco están definidos aquí el público, el precio, el presupuesto ni la fecha de salida.
Véanse [decisiones pendientes](../00_Indice_y_Control/Decisiones_pendientes.md).

## Comprobación propuesta

Dar esta ficha a una persona nueva y pedirle que explique qué hará, qué intenta
conseguir y por qué crecer puede perjudicarla. Comparar su expectativa con una partida.
Esta revisión no ha realizado esa prueba.

Fuentes: [README](../../../README.md), [Overview](../01-overview.md) y
[Godot](../../../godot/README.md).

[Volver al índice](../README.md)
