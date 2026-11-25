# 03. Población, Mysticism y Madness

**Estado:** `alignment-7` (Unity `0.9.0` / `0.9.1`). Código en `Balance.cs`, `GameData.cs`, `Derived.cs` y `GameEngine.cs`.

## Los cinco grupos poblacionales

Cada región divide su población en cinco cubetas (`Population` en `GameState.cs`):

| Grupo | Significado |
| --- | --- |
| `Unaware` | Personas ajenas a lo sobrenatural. Reclutables. |
| `Deniers` | Personas que rechazan lo sobrenatural. Solo se reclutan tras convertirlas con una manifestación. |
| `Believers` | Personas que aceptan lo imposible pero aún no entran al culto. Se reclutan antes que `Unaware` y suben la presión de amenazas. |
| `Followers` | Tu culto en la región. Genera Influence, sostiene sitios y sufre bajas. |
| `Losses` | Bajas permanentes por sacrificios u oposición. Nunca vuelven. |

## Invariante de conservación

La suma de los cinco grupos equivale siempre a la capacidad fija de la región:

```
Derived.Capacity(r) = Unaware + Deniers + Believers + Followers + Losses = PopulationWeight × 100
```

Los ocho pesos suman 100, así que el mundo tiene 10.000 personas (`GameData.WorldPopulation`).
Como el apoyo requerido por un sitio depende de `Capacity(r)`, el código y las pruebas siempre
mueven gente entre grupos (`Derived.Recruit`, `Derived.LoseFollowers`) y nunca asignan
`Followers` de forma directa.

## Composición inicial y rasgos regionales

Al iniciar la partida (`GameEngine.CreateGameState`), cada región reparte su población entre
`Deniers` (según `Skepticism`) y `Unaware`. Al fundar la primera célula (`PlaceStartingFoothold`)
reclutas `Balance.StartingFollowers` (100) seguidores.

Cada región tiene tres rasgos fijos en `GameData.Regions`:
- `Skepticism`: fija el muro inicial de negadores.
- `InstitutionalResistance`: multiplica la presión de amenazas ([05](05-opposition.md)).
- `Mysticism`: cambia cuánto cuesta y cuánto rinde manifestar lo imposible.

## Reclutamiento

La reserva reclutable es `Unaware + Believers` (`Derived.Recruitable`), consumiendo primero
`Believers`.

El reclutamiento automático mensual (`Derived.RecruitmentPerMonth`) se detiene si la célula está
dispersa o vacía. Cuando está activa parte de `0,6 + Followers × 0,0075` y se multiplica por:

- Culto público: `×2,0` (o `×2,5` con *Open worship*).
- Talento *Swelling flock*: `+35 %`.
- Backlash: `×0,55` tras hacer pública la célula o revelar una figura.
- Madness: bono pequeño `1 + Madness / 500`.
- Dificultad: `×1,2` en Easy, `×1,0` en Normal y `×0,85` en Hard.
- Lastre por awareness (`AwarenessDrag`): aplica a todas las células salvo las públicas cuando
  tienes *Open worship*.

La acción `recruit` suma un bloque inmediato de hasta 35 seguidores (18 Influence, 12 meses de
cooldown regional). Sirve sobre todo para arrancar células o reponer bajas tras un golpe.

## Manifestaciones y Mysticism

`manifest` gasta Dread (20, o 15 con *Red altars*; 18 meses de cooldown) para convertir
`Deniers` y `Unaware` en `Believers` y dejar +20 de Madness regional. Es la única forma de
romper el muro de negadores.

En una región de Mysticism `m` (pivote `Balance.MysticismPivot = 50`):
- Awareness generada (`Derived.ManifestAwareness`): `round(8 × (1 − (m − 50)/100))`, desde 6
  en África (`m=70`) hasta 10 en Europa (`m=30`).
- Conversiones (`ManifestDeniers`, `ManifestUnaware`): `round(65 × (1 + (m − 50)/100))`
  negadores y `round(50 × (1 + (m − 50)/100))` desprevenidos (de 78/60 en África a 52/40 en
  Europa).

El panel regional resume este rasgo en una línea mediante `Derived.MysticismNote`.

## Madness (locura regional)

`Madness` va de 0 a 100 por región. Sube +20 al manifestar y +10 al sacrificar (+20 con
*Dreams of R'lyeh*), y baja 1 por mes (`Balance.MadnessDecay`). Con *Dreams of R'lyeh*, cada
región con 30 o más contagia +2 al mes a cada vecina con célula (+1 neto tras el decaimiento).

Mientras la región tiene célula activa, la Madness produce beneficios y riesgos al mismo tiempo:

| Efecto | Regla vigente (`alignment-7`) | Referencia en código |
| --- | --- | --- |
| Dread pasivo | `+Madness / 20` de Dread por mes (hasta `DreadCap`) | `GameEngine.MadnessDread` |
| Aceleración de sitio | Un sitio financiado, activo y con apoyo gana `+Madness / 100` meses extra de preparación al mes (fracción en `Site.Quickening`) | `GameEngine.MadnessQuickens` / `MadnessHaste` |
| Reclutamiento | Bono pequeño `1 + Madness / 500` | `Derived.RecruitmentPerMonth` |
| Presión de amenazas | Suma `+Madness / 400` a la presión regional | `Director.ThreatPressure` |
| Umbral alto (`≥ 50`) | Una célula oculta sin sitio atrae sociedades ocultistas en lugar de investigadores y la región suma `+0,05` de awareness mundial al mes | `GameEngine.MadnessHigh`, `Director.ScheduleThreat` |

`GameEngine.MadnessText` resume en vivo qué produce la Madness en esa región.

## Pérdida de seguidores y colapso de célula

`Derived.LoseFollowers` traslada seguidores a `Losses`. Si una región cae a 0 seguidores, la
célula colapsa: termina el culto público, se cancela la dispersión y cualquier sitio ritual en
esa región se destruye por completo, incluida su inversión.

[Volver al índice](README.md)
