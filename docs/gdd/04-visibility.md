# 04. Visibilidad, exposición y awareness

**Estado:** consolidado sobre `alignment-7` (Unity `0.9.0` / `0.9.1`). Fuente de verdad en código: `Balance.cs`, `Derived.cs`, `GameEngine.cs`.

El juego separa dos medidores distintos. Confundirlos es el error de lectura más frecuente:

| | **Exposure (Exposición)** | **Awareness (Conocimiento global)** |
| --- | --- | --- |
| Alcance | Por región | Global (todo el mundo) |
| Qué representa | Evidencia local | Cuánto sabe la humanidad sobre el culto |
| Rango | 0–100 | 0–100 |
| Efecto | Frecuencia y severidad de amenazas locales | Frena ingresos y reclutamiento (`AwarenessDrag`), eleva severidad y **causa derrota** |
| Cómo baja | `conceal`, `scatter` (−10) y deriva natural de células ocultas hasta su piso | `conceal` (con descanso mundial), `bury`, *Scapegoats* y *The world forgets*. Sacrificar solo reduce su crecimiento futuro |
| ¿Es fatal? | No | **Sí, derrota inmediata al llegar a 100** |

## Exposición regional (`Exposure`)

Sube con tus acciones en la región (reclutar poco, enterrar +8, silenciar +6, restaurar +10,
sacrificar +12, manifestar/hacerse público/sitios y revelar figuras mucho) y deriva cada mes:
las células públicas acumulan exposición; las ocultas la pierden lentamente hasta su **piso**.

**Pisos de evidencia:** una célula clandestina sin sitio puede bajar hasta 0; el culto público
impone un piso intermedio y un **sitio ritual financiado impone un piso alto permanente** (un
sitio nunca puede ocultarse por completo). La exposición alimenta tanto la probabilidad de
amenaza como su **severidad (1–3)**.

## Awareness mundial (condición de derrota)

Es la condición de derrota contra el crecimiento codicioso. Sube por tres vías:

1. **Tamaño del culto (`Derived.CultShare`):** cada mes suma `cultShare × 1,5`
   (`Balance.AwarenessPerCapita`). Con el talento **Open worship**, los seguidores de las
   células públicas dejan de contarse en este término (`Derived.OpenWorshipShare`).
2. **Actos llamativos (mostrados como precio rojo en la UI vía `GameEngine.AwarenessCost`):**
   - `reveal` (hacerse público): **+5 solo la primera vez** en cada región (0 con *Open
     worship*).
   - `manifest`: **6 a 10** según el `Mysticism` de la región.
   - `prepare` (financiar sitio) y `reveal_figure`: **+8**.
   - `silence` (silenciar amenaza): **+3 por severidad** (**+2** con *Mute witnesses*).
   - `sacrifice` durante el rito con *Dreams of R'lyeh* (cuando adelanta el rito): **+2**.
3. **Visibilidad sostenida mensual:** sitios financiados, células públicas (0 con *Open
   worship*), regiones con célula y `Madness ≥ 50` (**+0,05/mes** cada una), y regiones
   públicas durante *The Great Procession* (**+0,3/mes** cada una).

### Consecuencias

- **Lastre (`Derived.AwarenessDrag`):** `max(0, 1 − awareness / 150)`. Multiplica a la vez los
  ingresos de Influence y el reclutamiento (salvo células públicas con *Open worship*). Crea
  una espiral palpable mucho antes de llegar a 100.
- **Derrota (`world_awareness`):** al alcanzar `100`, la partida termina inmediatamente ese mes.

## Herramientas para gestionar y bajar la awareness

A diferencia de las primeras versiones, hoy existen varias palancas con costes distintos:

1. **Ocultar evidencia (`conceal`):** limpia hasta 25 de exposición local (12 meses de cooldown
   regional). Además quita **−3 de awareness mundial**, pero ese efecto mundial tiene un
   **descanso global de 6 meses** (`9` en Hard, `GameEngine.ConcealWorldCooldown`). Cuando el
   efecto mundial está listo y hay awareness, cuesta `20 + awareness/2` Influence; mientras
   descansa, cuesta solo `20` y limpia únicamente exposición local.
2. **Enterrar el registro (`bury`):** quita **−5 de awareness mundial** al instante desde
   cualquier célula, sin compartir el descanso de `conceal`. Cuesta `50 + BuryStep × usos`
   (`+15` Easy, `+35` Normal, `+40` Hard), suma +8 exposición y tiene 6 meses de cooldown
   regional.
3. **Talentos:**
   - **Open worship** (*Congregation*): evita el goteo y el conteo por tamaño de las células
     públicas y elimina el +5 al revelarse.
   - **Scapegoats** (*Harbinger*): cada amenaza que golpea baja la awareness en `1 × severidad`.
   - **The world forgets** (*Hidden hand*, remate invocable una vez): quita **−25 de awareness**
     y disuelve todas las amenazas activas, dispersando toda la red durante 6 meses.
4. **Reducir el culto (`sacrifice`):** no resta awareness acumulada, pero reduce el goteo futuro.

## Culto público (`reveal` / `hide`)

Hacerse público duplica el reclutamiento automático (`×2,5` con *Open worship*), pero suma
exposición, impone un piso de evidencia, aplica `backlash` temporal y convierte futuras
amenazas en redadas (`raid`, con más bajas). `hide` termina el culto público pero deja la
exposición ya generada para que decaiga sola. Durante *The Great Procession*, ninguna célula
pública puede volver a la clandestinidad.

[Volver al índice](README.md)
