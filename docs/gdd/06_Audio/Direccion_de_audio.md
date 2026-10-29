# Dirección de audio

**Versión:** 0.1 · **Fecha:** 2026-10-28 · **Estado:** efectos implementados; dirección sonora final abierta.

## Base actual

Godot sintetiza efectos al iniciar: clic, acción, rechazo, amenaza, presagio, sitio y
final. El sonido usa tonos y envolventes generados por código; no depende de archivos
de audio externos. El menú permite silenciar y ajustar volumen.

La implementación de [Sfx](../../../godot/ui/sfx.gd) define esos sonidos y mantiene
cuatro reproductores. Esto limita cuántos efectos pueden sonar a la vez. No hay
música ni voz en ese catálogo; no se asumen como entregables comprometidos.

## Función propuesta

Confirmar acciones y rechazos, llamar la atención sobre amenazas y marcar hitos
rituales. El audio debe apoyar la lectura del mapa y los plazos. Probar que las
señales críticas sigan disponibles en texto y gráficos con el juego silenciado.

**Jerarquía propuesta para evaluar:** advertencias de pérdida o plazo, hitos de fase,
confirmaciones y sonidos ambientales. La prioridad final depende del tono y de si
se incorpora una capa musical.

## Decisiones y comprobación

Definir identidad sonora, necesidad de música o ambientes, tratamiento de voces y
cantidad de variación. No abrir un pipeline de doblaje o middleware sin esa decisión.

Revisar una secuencia con varias amenazas simultáneas, otra con acciones repetidas y
el inicio/final del rito. Observar si se pierden alertas, si hay fatiga y si el
volumen distrae. Repetir el flujo sin sonido para detectar información dependiente
de una sola modalidad. Esta revisión documental no realizó pruebas auditivas.

[Volver al índice](../README.md)
