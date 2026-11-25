# 06 — Sitios rituales, árbol de talentos y rito final

**Estado:** consolidado sobre `alignment-7` (Unity `0.9.0` / `0.9.1`). Fuente de verdad en código: `Balance.cs`, `GameData.cs`, `GameEngine.cs` y `Forecast.cs`.

Tres sitios rituales preparados y sostenidos son la base para ganar. Todo el resto de la
economía y la defensa existe para financiar esos sitios, mantenerlos vivos y completar el rito
antes del cierre de la alineación.

## Apoyo regional (`Derived.SiteRequirement` / `Derived.Supported`)

Un sitio cuenta como apoyado cuando la célula **no está dispersa** y mantiene suficientes
seguidores locales respecto a la población total de la región:

```
requisito = max(60, round(Capacity(r) × 0,25))
```

Va de 75 seguidores en Oceanía a 450 en Asia del Sur. Esa es la principal asimetría del
mapa: las regiones pobladas rinden mejor para reclutar y sacrificar, mientras las regiones
pequeñas son mucho más baratas de sostener como sedes de sitios rituales. El apoyo se verifica
de forma continua: si cae por debajo del requisito, el sitio deja de progresar y deja de
contar para el rito hasta recuperar seguidores.

## Financiación y mantenimiento (`prepare`)

Financiar un sitio cuesta **40 Influence** más un coste creciente en Dread
(`Balance.SiteDreadCosts`: **60, 100 y 160** para el 1.º, 2.º y 3.º). Además:

- Impone un piso alto permanente de exposición local y suma **+8 de awareness**.
- Programa inmediatamente una amenaza ocultista (`occult`) en esa región.
- Exige **1 Influence/mes** de mantenimiento (`Balance.SiteUpkeep`). Si no alcanza la Influence
  para cubrirlo, el sitio queda `Dormant` ese mes (no avanza ni sostiene el rito).

`GameEngine.PreviewAction` bloquea financiar un sitio si matemáticamente ya no queda tiempo
para prepararlo y completar el rito antes del mes 224 (descontando eclipses, figuras secretas
presentes y aceleración disponible).

## Preparación, aceleración y restauración (`restore`)

- **Duración:** **24 meses** en Normal y Hard; **20 meses** en Easy (`GameEngine.SiteMonths`).
  Un sitio gana 1 mes de preparación cada mes que esté activo, financiado y apoyado. Si pierde
  apoyo o se dispersa, se pausa sin perder lo ya avanzado.
- **Aceleración:**
  - **Madness regional:** suma `+Madness / 100` meses extra de preparación al mes mientras el
    sitio avanza (`Site.Quickening`; con 50 de Madness gana 1 mes extra cada 2 meses).
  - **Oportunidades:** explotar un eclipse (`eclipse`) o revelar una figura convertida
    (`reveal_figure`, +8 meses).
- **Daño y restauración (`restore`):** una amenaza ocultista ignorada quita preparación (`5`
  meses en Easy, `6` en Normal/Hard) y guarda esa última pérdida en `Site.Recoverable`.
  `restore` recupera la mitad redondeando hacia arriba (**todos** con *Contingency*) a **30
  Influence por mes** y +10 exposición (0 exposición con *Contingency*), una vez por pérdida.
- **Destrucción:** si una región cae a 0 seguidores, la célula y su sitio se destruyen por
  completo.

## Árbol de talentos y remates (`alignment-5` / `alignment-6` / `alignment-7`)

El árbol vive dentro de cada partida sin metaprogresión (D05). Tiene **3 ramas × 4 nodos** (12
en total): una raíz (tier 1), dos nodos medios mutuamente excluyentes (tier 2) y un remate
(tier 3) que requiere la raíz y cualquiera de los dos medios. Los sellos se abren por
calendario (**6 en Easy, 5 en Normal, 4 en Hard**), con coste creciente en Influence, de modo
que ninguna partida completa el árbol. Cada talento tomado registra y muestra lo que rindió
(`GameEngine.TalentYieldText`).

| Rama | Raíz (tier 1) | Medios excluyentes (tier 2) | Remate (tier 3) — cambia cómo cierras |
| --- | --- | --- | --- |
| **Congregation** | *Swelling flock* (+35 % reclutamiento automático) | *Open worship* (público ×2,5, inmune al lastre y sin awareness por tamaño/goteo/revelar) vs. *Martyrs* (1 Dread por cada 3 bajas de amenaza) | **The Great Procession:** con 5+ regiones públicas al iniciar, el rito pide **8 meses** (no 12); durante el rito cada región pública suma +0,3 awareness/mes y no puede ocultarse |
| **Hidden hand** | *Quiet friends* (+2 meses aviso, obstruir ⅔) | *Sleeper cells* (dispersar dura 5 meses, no 8) vs. *Contingency* (`restore` devuelve todos los meses sin exposición) | **The world forgets** (invocable 1 vez): **−25 awareness** y disuelve todas las amenazas, dispersando toda la red 6 meses (detiene el rito si corre) |
| **Harbinger** | *Red altars* (sacrificio 35 Dread, manifestar 15, silenciar 13/sev.) | *Mute witnesses* (silenciar suma +2 awareness/sev., no +3) vs. *Scapegoats* (amenaza que golpea quita 1 awareness/sev.) | **Dreams of R'lyeh:** sacrificios dejan doble Madness; regiones con 30+ contagian +2/mes a vecinas con célula; durante el rito cada sacrificio que deja 3 sitios apoyados adelanta el rito **1 mes** (**2** con 30+ Madness) a +2 awareness |

## El rito final y su previsión (`Forecast`)

1. Al llegar el mes 200 (`alignment`), el reloj pausa. Para iniciar (`GameEngine.StartAwakening`)
   se requieren **3 sitios listos** (`Derived.ReadySites`), **100 Dread** y estar dentro de la
   ventana. La duración requerida (`RiteMonthsRequired`: 12 u 8) se fija al comenzar.
2. El progreso es **acumulativo**: cada mes con 3 sitios listos avanza 1 mes. Desde el mes 200
   opera la caza de sitios (`+0,6` presión).
3. **Previsión y margen (`Forecast.Margin`):** calcula el inicio más temprano, el último inicio
   posible (`Deadline − RiteMonthsFor`) y el margen en meses. Si `!Fits` y el rito no comenzó,
   habilita el cierre voluntario (`GameEngine.CanConcede`).
4. **Rito detenido (`Forecast.Rite`):** mientras el rito corre, indica si avanza este mes o qué
   sitios lo bloquean (`destroyed`, `dormant`, `scattered`, `unsupported`, `unprepared`). La
   vista previa de `scatter` y `sacrifice` avisa explícitamente si ejecutarlos detendrá el rito.

[Volver al índice](README.md)
