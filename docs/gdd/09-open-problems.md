# 09 — Problemas abiertos e ítems diferidos

**Versión:** 0.5 · **Fecha:** 2026-11-25 · **Estado:** reorganizado sobre `alignment-7` (juego 0.9.0 beta pública / 0.9.1) y filosofía de balance D25.

Este documento separa tres cosas: (1) qué observar ahora en la beta abierta, (2) qué ideas
diferidas siguen sobre la mesa para decidir si entran al Camino 1, al Camino 2 o se descartan,
y (3) qué problemas históricos ya quedaron cerrados.

---

## 1. Abiertos actuales para observar en beta abierta (`alignment-7`)

Según D25, el ajuste fino de balance se deja para una fase tardía: hoy la prioridad es observar
con personas reales si las opciones se entienden, se sienten divertidas y hacen sentir
inteligente al jugador.

1. **Sin datos de jugadores externos todavía.** Todas las partidas humanas de `0.6.0`–`0.8.0`
   son de un solo jugador interno. La beta pública abierta en itch.io (`0.9.0`, `awakening-6`)
   es la primera oportunidad de validar comprensión, duración real (objetivo 15–30 min con
   pausas) y atractivo fuera del equipo.
2. **Adopción y lectura de `Madness`.** Recién pasó de decorativa a recurso en `0.9.0`. Falta
   ver si los jugadores la buscan a propósito para acelerar sitios o generar Dread, y si las
   amenazas ocultistas desde 50 se perciben justas.
3. **Sensibilidad del respiro y diferencias entre dificultades (bot).**
   - En Normal el bot gana 83/144 (`early` 27, `late` 35, `terror` 21 de 48): *Hidden hand*
     sigue siendo la rama más cómoda.
   - El respiro de 5 meses salta por escalones respecto a la cadencia de 4 meses (4 meses →
     50/144; 6 meses → 93/144); Normal subió presión a ×1,45 y Hard quedó en ×1,15.
   - En Hard, *late* gana casi lo mismo que en Normal (31 vs. 35/48).
   - En Easy, *Harbinger* con `Dreams of R'lyeh` dispara la Madness en cadena (Dread al tope,
     awareness mediana 93).
   - Tres nodos del árbol (`Martyrs`, `Mute witnesses`, `Contingency`) no tienen política de
     bot que los recorra (solo pruebas unitarias).
4. **Oportunidades infrautilizadas.** `exploit`, `convert` y `reveal_figure` pasaron de 0 usos
   en `alignment-1` a 2–5 usos por partida en `0.3.x`, pero siguen compitiendo con poca
   visibilidad frente a las amenazas (solo aparece una cada 12 meses y la cadena de figuras
   requiere dos pasos). Falta ver en la beta pública si los jugadores las aprovechan o las
   ignoran.
5. **Accesibilidad del port a Unity.** Falta restaurar equivalencia de lector de pantalla
   (`accessibility_name` de Godot), validar teclado completo/mando y probar en teléfono físico
   y ejecutable de Windows — todo aplazado al tramo final (D07).

---

## 2. Futuras features y mejoras (backlog de diseño)

Decisión de dirección (2026-11-25): todas las ideas siguientes permanecen registradas como
**futuras features** (sin descartes prematuros) para analizarse y priorizarse conforme avancen
el diseño, los datos de la beta abierta y el camino de producción activo:

| # | Futura feature / mejora | Qué aportaría | Nota de diseño |
| --- | --- | --- | --- |
| A | **Eventos escritos que pausan (D13)** | Capa principal de narrativa y horror lovecraftiano (1908–1926); quiebra la monotonía del medio juego | Confirmada como futura feature; ~5–8 por partida con consecuencias mecánicas visibles |
| B | **Eventos de umbral de awareness (p. ej. 25 / 50 / 75)** | Hace sentir que el mundo escala su reacción a medida que descubre al culto | Encaja naturalmente dentro del sistema de eventos escritos (A) |
| C | **Mejorar visibilidad y peso de oportunidades (`exploit` / `convert`)** | Aprovecha 3 de los 16 verbos ya programados que hoy pasan desapercibidos | Bajo coste en reglas y UI; evaluar tras datos de la beta pública |
| D | **Investigador persistente que persigue tu región más fuerte** | Antagonista reconocible para la narrativa y tensión táctica continua | Cuidar que no rompa la claridad del sistema actual de amenazas |
| E | **Cultos rivales** | Otro actor en el mapa compitiendo por regiones o seguidores | Alto impacto en reglas, UI y balance; evaluar más adelante |
| F | **Eventos exclusivos por región** | Refuerza identidad geográfica (p. ej. R'lyeh en Oceanía, expediciones en África/Europa) | Se acopla bien de forma acotada dentro de (A) |
| G | **Escenarios o variantes entre partidas** | Rejugabilidad adicional más allá de semilla, dificultad y talentos | En Camino 2 se combina o potencia con los **1–2 nuevos Old Gods** |
| H | **Epílogos diferenciados y representación visual del despertar** | Cierre memorable acorde al remate usado (Procesión, Olvido, Sueños) o a la derrota | En texto primero; con ilustración, música y feedback audiovisual al contratar equipo |

---

## 3. Problemas históricos ya resueltos o superados (referencia)

- **Influence sin destino en el medio/final juego (`alignment-1`):** resuelto con mantenimiento
  de sitios (`alignment-2`), árbol de talentos, `bury` y `restore` (`alignment-5`) y precios
  ajustados (`alignment-6`/`7`).
- **Textos de reglas desfasados con los números del código:** mitigado derivando cifras y
  previsiones desde `Balance.cs` / `GameData.cs` / `PreviewAction` en Unity.
- **Dificultades sin medir ni diferenciadas:** desde `alignment-5`–`7` cambian reglas reales
  (sellos, pérdida ocultista, paso de `bury`, descanso mundial de `conceal`, meses de sitio en
  Easy y precio de talentos) y las tres se miden con `core-tests/Tools`.
- **Rito detenido sin aviso claro y partidas ya perdidas que no se podían cerrar:** resuelto en
  `alignment-4` (`Forecast.Margin`, `CanConcede`) y `alignment-6` (`Forecast.Rite` y avisos
  en rojo al dispersar/sacrificar durante el rito).

[Volver al índice](README.md)
