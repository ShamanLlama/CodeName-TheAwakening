# 02 — Economía

**Estado:** consolidado sobre `alignment-7` (Unity `0.9.0` / `0.9.1`). Fuente de verdad en código: `Balance.cs`, `GameData.cs`, `Derived.cs` y `GameEngine.cs`.

Tres magnitudes sostienen la economía: **Influence**, **Dread** y **Followers**. Solo las dos
primeras se gastan como moneda; los seguidores son a la vez motor económico, apoyo ritual y
riesgo de visibilidad ([04 — Visibilidad y awareness](04-visibility.md)).

## Influence

Es la moneda operativa global (sobornos, redes, reclutamiento, talentos y defensa).

**Ingreso mensual (`Derived.InfluencePerMonth` y `Derived.NetInfluencePerMonth`):**

```
ingresoBruto = Σ ingresoRegional(r) × AwarenessDrag(state)
ingresoRegional(r) = 0 si la célula está dispersa o sin seguidores;
                     si no, Followers × InfluencePerFollower + 0,6 (si hay celebridad secreta)
ingresoNeto = ingresoBruto − (sitiosFinanciados × SiteUpkeep)
```

- **Lastre por awareness (`Derived.AwarenessDrag`):** cuanto más sabe el mundo del culto, menos
  rinde cada seguidor. Un culto grande y expuesto gana menos por cabeza que uno discreto.
- **Mantenimiento de sitios (`Balance.SiteUpkeep` = 1/mes por sitio):** se descuenta cada mes.
  Si no alcanza la Influence para pagarlo, el sitio queda **inactivo (`Dormant`)** ese mes: no
  avanza su preparación ni cuenta para el rito final.
- **Tope (`Balance.InfluenceCap` = 4000):** límite técnico de validación, no objetivo de juego.

**En qué se gasta Influence:** reclutamiento inmediato (`recruit`), expansión (`expand`),
ocultar evidencia (`conceal`), enterrar el registro (`bury`), obstruir amenazas (`counter`),
restaurar sitios dañados (`restore`), financiar sitios (`prepare`), aprovechar presagios o
convertir figuras (`exploit`, `convert`) y comprar nodos del **árbol de talentos**
(`GameEngine.TalentCost`: `40 + 25n` en Normal/Hard; `30 + 20n` en Easy).

## Dread

Es la moneda ritual y de terror. Tope real: `Balance.DreadCap` (400), lo bastante bajo como
para impedir acumularlo sin límite antes de los sitios caros.

**Fuentes de Dread:**

1. **Sacrificio (`sacrifice`):** consume 40 seguidores locales a cambio de **25 Dread** (35 con
   *Red altars*) y deja **+10 de Madness** local (+20 con *Dreams of R'lyeh*). Cooldown regional
   de 6 meses y suma exposición.
2. **Madness regional (`GameEngine.MadnessDreadTotal`):** cada región con célula rinde
   **`Madness / 20` de Dread por mes**, hasta `DreadCap`. Mantener alta la Madness implica
   seguir manifestando o sacrificando (baja 1/mes) y pagar su riesgo en amenazas y awareness.
3. **Pesadilla compartida (`nightmare` + `exploit`):** oportunidad puntual que entrega un
   bloque de Dread.
4. **Talento *Martyrs*:** cada amenaza que golpea otorga 1 Dread por cada 3 seguidores perdidos.

**En qué se gasta Dread:** manifestaciones (`manifest`: 20, o 15 con *Red altars*), silenciar
amenazas (`silence`: 20 por severidad, o 13 con *Red altars*), financiar sitios con precio
creciente (`Balance.SiteDreadCosts`: **60, 100, 160**) e iniciar el rito final
(`Balance.FinalDread`: **100**).

### El sacrificio como intercambio central

Sacrificar hace cuatro cosas a la vez:

1. Entrega Dread inmediato y **Madness** regional (que seguirá rindiendo Dread y acelerando
   sitios).
2. Mueve seguidores de forma permanente a `Losses` ([03 — Población](03-population.md)).
3. Reduce tus ingresos y frena el **crecimiento futuro** de `Awareness` (no borra la awareness
   ya acumulada).
4. Con *Dreams of R'lyeh* durante el rito final, adelanta el rito 1 mes (o 2 meses si la región
   tiene 30+ Madness) siempre que queden tres sitios con apoyo, a cambio de +2 awareness.

## Followers (Seguidores)

Viven por región, nunca en una reserva global. 500 seguidores repartidos en cinco regiones no
equivalen a 500 en una sola, porque el apoyo ritual, las amenazas y los sacrificios son
regionales.

## Costes variables principales

Los precios base están en `GameData.Actions`. Los costes calculados dinámicamente son:

| Acción / sistema | Fórmula en código | Notas |
| --- | --- | --- |
| `counter` (Obstruir) | `30 × severidad` (×⅔ con *Quiet friends*) | `GameEngine.CounterCost`; 12 meses de cooldown regional |
| `silence` (Silenciar) | `20 × severidad` Dread (`13` con *Red altars*) + `3 × severidad` awareness (`2` con *Mute witnesses*) | `GameEngine.SilenceCost`; 12 meses de cooldown regional |
| `conceal` (Ocultar) | `20 + awareness/2` si el efecto mundial está listo; `20` si solo limpia exposición local | `GameEngine.ConcealCost`; efecto mundial descansa 6 meses (9 en Hard) |
| `bury` (Enterrar) | `50 + BuryStep × usosPrevios` (`+15` Easy, `+35` Normal, `+40` Hard) | `GameEngine.BuryCost`; quita 5 de awareness y suma +8 exposición |
| `restore` (Restaurar) | `30 × mesesRecuperados` | `GameEngine.RestoreCost`; recupera mitad (o todos con *Contingency*) |
| `prepare` (Sitio) | `40` Influence + `60 / 100 / 160` Dread según sitios financiados | `GameEngine.SiteDreadCost` + `1` Influence/mes de mantenimiento |
| Talentos | `40 + 25 × tomados` (`30 + 20 × tomados` en Easy) | `GameEngine.TalentCost`; limitado además por sellos abiertos |

*The world forgets* (remate de *Hidden hand*) no cuesta Influence ni Dread al invocarse una vez
por partida: su precio son 6 meses con todas las células dispersas (sin ingreso ni avance).

## Previsualización y validación

`GameEngine.PreviewAction` es la autoridad única sobre si una acción es legal, cuánto cuesta
(incluyendo la `Awareness` que suma), qué produce y qué riesgos conlleva (por ejemplo, si
detendría el rito activo). La interfaz, las pruebas y el bot pasan por ella; `PerformAction`
revalida la vista previa antes de cobrar nada.

[Volver al índice](README.md)
