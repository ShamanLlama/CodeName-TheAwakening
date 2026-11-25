# 05 — Oposición, amenazas y oportunidades

**Estado:** consolidado sobre `alignment-7` (Unity `0.9.0` / `0.9.1`). Fuente de verdad en código: `Director.cs`, `Threats.cs`, `GameEngine.cs` y `Balance.cs`.

El mundo no es una IA rival independiente, sino un sistema de presión determinista
(`Director.Tick`) que reacciona a lo visible y arriesgado que se vuelve tu culto.

## Cadencias del Director

Corre al cierre de cada mes con dos ritmos independientes:

- **Tiradas de amenaza:** cada **4 meses** (`Balance.ThreatRollMonths`), cada región con
  seguidores, sin amenaza pendiente, sin estar dispersa y fuera de respiro tira contra su
  presión regional (`Director.ThreatPressure`).
- **Oportunidades:** cada **12 meses**, una región con célula y sin oportunidad activa recibe
  un presagio o figura con vigencia de 8 meses.

En el mes 176 (`PreparationMonths − 24`), el director emite un aviso anunciando que al abrirse
la alineación (mes 200) se intensificará la caza contra los sitios rituales.

## Presión regional de amenazas (`Director.ThreatPressure`)

```
presión = ( 0,02
          + Exposure / 500
          + 0,10 si es pública
          + 0,12 si tiene sitio financiado
          + 0,60 extra en sitio financiado desde el mes 200 (caza de sitios)
          + Awareness / 1500
          + (Believers / poblaciónViva) × 0,15
          + Madness / 400
          + 0,05 desde el mes 100 )
          × dificultad.Pressure
          × (0,6 + InstitutionalResistance / 150)
```

Multiplicadores de `dificultad.Pressure`: **Easy ×1,0**, **Normal ×1,45**, **Hard ×1,15**
(Hard compensa su multiplicador más bajo con avisos más cortos, menos reclutamiento, menos
sellos y descansos más largos).

## Tipos de amenaza y severidad

Determinados al programarse (`Director.ScheduleThreat`):

| Tipo (`Kind`) | Cuándo aparece | Consecuencia adicional si golpea |
| --- | --- | --- |
| `investigation` | Célula clandestina sin sitio y `Madness < 50` | Bajas estándar (`8 %` × severidad, mín. `6 × sev.`) |
| `raid` | Célula con culto público (sin sitio) | Bajas mayores (`12 %` × severidad, mín. `8 × sev.`) |
| `occult` | Región con **sitio financiado**, o célula clandestina sin sitio con **`Madness ≥ 50`** | Si hay sitio, **quita meses de preparación** (`5` en Easy, `6` en Normal/Hard) |

Financiar un sitio dispara inmediatamente una amenaza `occult` en esa región.

- **Ventana de aviso:** base **5 meses** (`+2` Easy, `−1` Hard, `+2` con *Quiet friends*, `+2`
  con político secreto).
- **Severidad (`1–3`):** `clamp(1 + floor((Exposure + Awareness) / 60), 1, 3)`. Escala tanto el
  daño del golpe como el coste de obstruir o silenciar.

## Las tres respuestas y el respiro

Cada amenaza admite tres respuestas garantizadas (`Threats.Responses`), pagadas en monedas
distintas:

| Respuesta | Precio y coste secundario | Cooldown / respiro |
| --- | --- | --- |
| **Obstruir (`counter`)** | `30 × severidad` Influence (`×⅔` con *Quiet friends*) | 12 meses regional · deja **5 meses de respiro** |
| **Dispersar (`scatter`)** | Gratis en monedas; la célula queda oculta **8 meses** (**5** con *Sleeper cells*): no recluta, no ingresa, deja de ser pública, **detiene su sitio y el rito si lo sostiene**, baja −10 exposición y no atrae amenazas mientras dura | 24 meses regional · sin respiro extra al volver |
| **Silenciar (`silence`)** | `20 × severidad` Dread (`13` con *Red altars*), `+3 × severidad` awareness (`+2` con *Mute witnesses*) y `+6` exposición | 12 meses regional · deja **5 meses de respiro** |

- **Respiro (`Balance.ThreatGraceMonths` = 5):** tras obstruir o silenciar, la región queda
  exenta de nuevas tiradas de amenaza durante 5 meses (`GameEngine.ThreatGraceLeft`) para
  evitar cadenas de golpes imposibles sobre un mismo sitio.
- **Amenaza acorralada (`Threats.Cornered`):** al aparecer una amenaza, `Threats.Options`
  evalúa las tres respuestas; si no puedes obstruir ni silenciar en ese momento, la UI marca la
  amenaza como acorralada (solo queda dispersar o soportar el golpe).
- **Soportar el golpe (`Director.ResolveWorld`):** aplica `Threats.Consequences` (la misma
  función que muestra el pronóstico), suma +5 exposición, registra en `Site.Recoverable` los
  meses perdidos por una amenaza ocultista (para `restore`), y activa *Martyrs* (+Dread por
  bajas) o *Scapegoats* (−awareness por severidad) si están tomados.
- **Disolución global:** invocar *The world forgets* cancela todas las amenazas activas del mapa
  a cambio de dispersar toda la red 6 meses.

## Oportunidades

Cada región puede tener una oportunidad activa durante 8 meses:

| Oportunidad | Acción | Efecto |
| --- | --- | --- |
| `nightmare` (Pesadilla compartida) | `exploit` (10 Inf) | Entrega un bloque inmediato de Dread |
| `eclipse` (Eclipse sin luz) | `exploit` (10 Inf) | Adelanta varios meses la preparación de un sitio incompleto |
| `celebrity` (Artista célebre) | `convert` (45 Inf) → `reveal_figure` | Secreta: `+0,6` Influence/mes. Al revelarse: hasta 60 reclutas y `+8` meses de sitio (+8 awareness, exposición y backlash) |
| `politician` (Ministro estudioso) | `convert` (45 Inf) → `reveal_figure` | Secreto: `+2` meses de aviso ante amenazas. Al revelarse: mismo efecto que artista y termina el bono secreto |

Su nivel real de uso frente a las amenazas queda como observación activa para la beta pública
([09 — Open problems](09-open-problems.md)).

[Volver al índice](README.md)
