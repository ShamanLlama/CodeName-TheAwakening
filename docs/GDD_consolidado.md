# The Awakening of Cthulhu: GDD consolidado

**Estudio:** Shaman Llama (Shaman Llama Games / Shaman Llama SAS, Ecuador)  
**Versión del juego:** `v0.9.1` (`alignment-7`, Unity 6.3 LTS, demo público en `https://shamanllamagames.itch.io/the-awakening-of-cthulhu`)  
**Fecha de compilación:** 2026-11-25

Este archivo reúne en un solo documento de lectura corrida los capítulos vigentes del GDD modular de [docs/gdd/README.md](gdd/README.md). Los valores ejecutables viven en `unity/Assets/Awakening/Core/Rules/Balance.cs` y `GameData.cs`; el historial detallado de iteraciones (`alignment-1` a `alignment-7`) se conserva aparte en [Experiencia y loops](gdd/02_Game_Design/Experiencia_y_loops.md), [07 — Balance](gdd/07-balance.md) y [versiones.md](playtests/versiones.md).

## Índice

- 1. Resumen y visión *(fuente: [Resumen_y_vision.md](gdd/01_Informacion_General/Resumen_y_vision.md))*
- 2. Los tres Old Gods *(fuente: [Old_Gods.md](gdd/02_Game_Design/Old_Gods.md))*
- 3. Reglas generales y flujo de partida *(fuente: [01-overview.md](gdd/01-overview.md))*
- 4. Economía *(fuente: [02-economy.md](gdd/02-economy.md))*
- 5. Población, Mysticism y Madness *(fuente: [03-population.md](gdd/03-population.md))*
- 6. Visibilidad, exposición y awareness *(fuente: [04-visibility.md](gdd/04-visibility.md))*
- 7. Oposición, amenazas y oportunidades *(fuente: [05-opposition.md](gdd/05-opposition.md))*
- 8. Sitios rituales, árbol de talentos y rito final *(fuente: [06-ritual-sites.md](gdd/06-ritual-sites.md))*
- 9. Mapa estratégico y ritmo de partida *(fuente: [Mapa_y_ritmo.md](gdd/03_Mapa_Estrategico/Mapa_y_ritmo.md))*
- 10. Mundo, narrativa y eventos escritos *(fuente: [Mundo_y_narrativa.md](gdd/04_Narrativa/Mundo_y_narrativa.md))*
- 11. Dirección visual *(fuente: [Direccion_visual.md](gdd/05_Arte/Direccion_visual.md))*
- 12. Dirección de audio *(fuente: [Direccion_de_audio.md](gdd/06_Audio/Direccion_de_audio.md))*
- 13. Base técnica y límites *(fuente: [Base_tecnica.md](gdd/08_Programacion_y_Technical_Design/Base_tecnica.md))*
- 14. Alcance, roadmap de 12 meses y validación *(fuente: [Alcance_y_validacion.md](gdd/09_Produccion/Alcance_y_validacion.md))*
- 15. Público, distribución y negocio *(fuente: [Publico_y_distribucion.md](gdd/10_Marketing_y_Negocio/Publico_y_distribucion.md))*
- 16. Problemas abiertos y futuras features *(fuente: [09-open-problems.md](gdd/09-open-problems.md))*
- 17. Glosario *(fuente: [Glosario.md](gdd/00_Indice_y_Control/Glosario.md))*

---

## 1. Resumen y visión

**Versión:** 0.6 · **Fecha:** 2026-11-25 · **Estado:** base implementada en Unity (`0.9.1` abierta como demo público en itch.io) y dos caminos de producción definidos.

### Concepto en una frase

Encarna a un Dios Antiguo (Cthulhu) entre 1908 y 1926: decide qué hacer con tus cultistas,
cuándo manifestarte y a quién controlar para preparar tu despertar antes de que pase la
alineación astral o la humanidad descubra la red.

### Identidad actual

- **Título de trabajo:** The Awakening of Cthulhu.
- **Estudio:** Shaman Llama (Shaman Llama Games / Shaman Llama SAS, Ecuador).
- **Género:** estrategia para un jugador sobre mapa mundial, con simulación en tiempo real,
  pausa activa y gestión de crecimiento y secreto.
- **Cámara y escala:** vista global de ocho regiones y gestión abstracta de población y
  células, sin unidades individuales.
- **Base jugable:** Unity 6.3 LTS desde 0.5.0. La versión vigente (`0.9.1`, publicada en
  itch.io el 2026-11-25) usa las reglas `alignment-7`, con árbol de talentos intra-partida,
  remates diferenciados y `Madness` como recurso regional. Godot (`0.2.0`–`0.4.0`) queda en
  la rama `godot` como referencia, y React/TypeScript (`src/`) es antecedente histórico.
- **Distribución actual:** demo público gratuito en web en
  `https://shamanllamagames.itch.io/the-awakening-of-cthulhu` (abierto desde 2026-11-24)
  para beta testing durante los próximos meses con GTC Playtest Lab (participación opcional).
- **Duración actual y objetivo:** unos 7,5 minutos de simulación a 1× sin pausas; el objetivo
  confirmado es de 15 a 30 minutos por partida contando pausas y planificación (D02). En
  partidas internas duró entre 14 y 22 minutos; falta medir sesiones externas en la beta.

### Propuesta jugable

Eliges dónde iniciar, cómo crecer, dónde sacrificar, cuándo ocultarte, cómo usar la locura
regional, qué ramas del árbol de talentos abrir y dónde preparar tres sitios rituales. Cada
seguidor produce Influence y sostiene rituales, pero también sube el conocimiento global. Las
amenazas te obligan a elegir entre monedas distintas (Influence, meses de inactividad o
Dread + awareness), y el cierre cambia según el remate que elijas.

### Objetivos confirmados y filosofía de diseño

- **Experiencia buscada (D03, D04, D09):** mezcla de estrategia, narrativa y horror en un
  archivo nocturno de culto entre 1908 y 1926, con voz lovecraftiana y sucesos ficticios. Los
  eventos escritos que pausan el juego (D13) quedan como futura feature ya diseñada a alto
  nivel.
- **Filosofía de balance (D25):** el ajuste fino de balance va al final del proyecto. Al ser
  un juego single-player, la prioridad es que se sienta bien, ofrezca opciones reales y haga
  sentir inteligente al jugador cuando resuelve la partida.
- **Presentación (D06):** el arte procedural y los SFX sintetizados actuales son temporales.
  El plan con financiamiento es contratar un diseñador gráfico/ilustrador y un ingeniero de
  audio en Ecuador.

### Dos caminos de producción y entrega (D26)

1. **Camino 1 (base propia, sin grant):** completar y pulir la experiencia de Cthulhu (un solo
   Dios Antiguo) y publicarla en itch.io, posiblemente gratuita.
2. **Camino 2 (con Unity Fellowship de 50.000 USD, ventana de 12 meses de marzo 2027 a marzo
   2028):** contratar 1 diseñador gráfico/ilustrador ($3.000 por 2 meses) y 1 ingeniero de
   audio ($3.000 por 2 meses); sumar a Nyarlathotep y Shub-Niggurath como futuros Old Gods;
   añadir eventos escritos y epílogos; hacer campaña orgánica en redes y asistir a gamescom
   latam (abr. 2027) y gamescom Alemania (ago. 2027); registrar el juego en Ecuador y EE. UU.,
   abrir LLC en EE. UU. y publicar en Steam en el Q1 2028.

El trabajo específico de cierre para Steam (Windows, Steamworks, mando, lector de pantalla e
idiomas) se deja para el tramo final del desarrollo (D07). Véanse
[Decisiones pendientes](gdd/00_Indice_y_Control/Decisiones_pendientes.md) y
[Alcance, roadmap y validación](gdd/09_Produccion/Alcance_y_validacion.md).

### Comprobación en beta abierta

Observar en las partidas públicas si una persona nueva entiende qué intenta lograr, por qué
crecer puede perjudicarla y si siente que sus elecciones de región, talentos y respuestas
cambian el curso de la partida.

Fuentes: [README](../README.md), [01. Overview](gdd/01-overview.md),
[README de Unity](../unity/README.md) y [registro de versiones](playtests/versiones.md).

---

## 2. Los tres Old Gods

**Versión:** 0.2 · **Fecha:** 2026-11-25 · **Estado:** Cthulhu está implementado (`0.9.1`, `alignment-7`); Nyarlathotep y Shub-Niggurath están definidos a alto nivel como futuras features para el desarrollo financiado (Camino 2 / Unity Fellowship).

### Premisa compartida (D04)

Juegas como el Old God: decides qué hacer con tus cultistas, cuándo manifestarte ante los
mortales y a quién controlar entre 1908 y 1926. Los tres dioses comparten el mismo motor
determinista en Unity (`Core/Rules`), el mapa mundial de 8 regiones, el marco de archivo
nocturno y la duración objetivo de 15 a 30 minutos, pero cambian cómo expandes la red, qué
tensiones manejas y cómo alcanzas el despertar.

### Comparativa de alto nivel

| # | Old God | Estado | Fantasía y estilo de juego (alto nivel) | Qué aporta a la rejugabilidad |
| --- | --- | --- | --- | --- |
| 1 | **Cthulhu** (*The Dreamer Beneath R'lyeh*) | Implementado (`0.9.1`) | Culto mundial clandestino o público que gestiona `Influence`, `Dread`, `Madness` regional y 3 sitios rituales hasta la alineación de 1924–1926. | Experiencia completa con 3 ramas de talentos (*Congregation*, *Hidden hand*, *Harbinger*) y 3 remates distintos (*The Great Procession*, *The world forgets*, *Dreams of R'lyeh*). |
| 2 | **Nyarlathotep** (*The Crawling Chaos*) | Futura feature (alto nivel) | Infiltración, engaño y control institucional: corrompe figuras públicas, prensa y gobiernos, y aprovecha la resistencia institucional a su favor para convertir la atención en caos controlado. | Cambia la lectura del mapa: las regiones escépticas e institucionales (como Europa o Norteamérica), hoy hostiles para Cthulhu, pasan a ser su terreno fuerte. |
| 3 | **Shub-Niggurath** (*The Black Goat of the Woods*) | Futura feature (alto nivel) | Crecimiento orgánico y fertilidad oscura: expansión acelerada hacia regiones vecinas, oleadas de seguidores y sacrificios masivos frente a una contención física constante. | Invierte la tensión del juego: prioriza volumen territorial y empuje orgánico sobre el sigilo tradicional. |

### Alcance y principio de producción

- **Hoy (`0.9.1`, beta pública):** Cthulhu es el juego completo que valida el loop, el árbol
  de talentos, la economía y el ritmo con jugadores reales.
- **Con financiamiento (Unity Fellowship, 12 meses desde marzo 2027):** Nyarlathotep y
  Shub-Niggurath se construyen sobre la misma arquitectura `Core/Rules` (cada uno con su
  propia identidad, árbol de talentos, giro mecánico y eventos/epílogos), manteniendo acotado
  el costo técnico y artístico.

---

## 3. Reglas generales y flujo de partida

**Estado:** `alignment-7` (Unity `0.9.0` / `0.9.1`). Código en `unity/Assets/Awakening/Core/Rules/`.

### Premisa mecánica

Juegas como un Old God (Dios Antiguo). Hoy encarnas a Cthulhu entre enero de 1908 y septiembre
de 1926, y decides qué hacer con tus cultistas, cuándo manifestarte y a quién controlar. La
humanidad es tu recurso, el secreto es tu límite y las estrellas solo se alinean una vez.
Construyes células en ocho regiones, eliges talentos dentro de la partida, conviertes
seguidores y locura en Dread, preparas tres sitios rituales y sostienes el rito final dentro
de la ventana de alineación antes de que el mundo te descubra o venza el plazo.

### Pilares de diseño

1. **Crecer tiene consecuencias.** Un culto grande genera Influence y sostiene sitios, pero
   cada seguidor sube el conocimiento global (`Awareness`).
2. **Defender exige elegir.** Toda amenaza tiene tres respuestas garantizadas en monedas
   distintas: Influence y 12 meses de espera (`counter`), meses con la célula oculta
   (`scatter`), o Dread y awareness (`silence`).
3. **La ubicación cambia el plan.** El apoyo de un sitio depende de la población total de su
   región. Además, el escepticismo, la resistencia institucional y el `Mysticism` cambian lo
   que cuesta reclutar, manifestar y defender.
4. **Todos los plazos están a la vista.** La alineación, la awareness, el margen del rito, la
   preparación de cada sitio y las amenazas se muestran en pantalla. Perder se siente como el
   resultado de tus decisiones previas, no de una tirada oculta.

### Fases de una partida

| Fase | `GameState.Status` | Qué ocurre |
| --- | --- | --- |
| Colocación | `placing` | Eliges región inicial (`GameEngine.PlaceStartingFoothold`). El tiempo está detenido. |
| Preparación | `playing` | Meses 0 a 199 (ene. 1908 a ago. 1924): expandes la red, tomas talentos al abrirse sellos, gestionas visibilidad y `Madness`, y preparas 3 sitios. |
| Alineación | `alignment` | Mes 200 (sept. 1924): el reloj pausa y se abre la ventana de 24 meses (`Balance.AlignmentMonths`) para iniciar el rito. |
| Rito final | `awakening` | Sostienes 3 sitios listos durante 12 meses acumulados (u 8 con *The Great Procession*) mientras el mundo caza tus sitios. |
| Resuelta | `won` / `lost` | La partida termina con su código en `GameState.ResultCode`. |

El calendario dura `Balance.PreparationMonths` (200) + `Balance.AlignmentMonths` (24) = 224
meses (`Balance.Deadline`). A velocidad 1× cada mes dura `Balance.SecondsPerMonth` (2 segundos),
unos 7,5 minutos sin pausas. El rito solo puede empezar durante la alineación
(`GameEngine.AwakeningReason`): no puedes ganar antes, pero sí quedarte sin margen.

### Ciclo mensual

Cada mes simulado corre en `GameEngine.AdvanceSimulation` en este orden:

1. Avanza el calendario (`ElapsedMonths`), regresan las células dispersas cumplidas y se
   procesa el decaimiento o contagio de `Madness`.
2. Se resuelve el mundo (`Director.ResolveWorld`): vencen las oportunidades caducadas y golpean
   las amenazas cuyo plazo llegó a cero.
3. Entra el ingreso neto de Influence (`Derived.InfluencePerMonth` menos el mantenimiento de
   sitios; los sitios sin pago pasan a `Dormant`). Se suma el Dread pasivo por `Madness`
   (`GameEngine.MadnessDreadTotal`).
4. Por región: reclutamiento automático, deriva de exposición, avance de preparación de sitios
   (más aceleración por `Madness`) y awareness mensual por sitios, células públicas o `Madness`
   alta.
5. Sube la `Awareness` global según la fracción de la humanidad que sigue al culto
   (`Derived.CultShare`, sin contar células públicas si tienes *Open worship*).
6. Si el rito está activo y hay 3 sitios listos y apoyados, avanza 1 mes (`FinalProgress`).
7. Se comprueban en orden: victoria, derrota por awareness (100) y derrota por calendario
   (mes 224).
8. Corre el director (`Director.Tick`): tiradas de amenazas cada 4 meses y oportunidades cada
   12 meses.

Las acciones del jugador no consumen meses; las limitan la Influence, el Dread, los seguidores,
los sellos de talentos y los cooldowns. Pausar para pensar y actuar es parte normal del juego.

### Victoria y derrotas

- **Victoria (`awakened`):** inicias el rito dentro de la ventana (con 3 sitios listos y
  `Balance.FinalDread` = 100 Dread) y acumulas los meses requeridos antes del mes 224. El
  progreso es acumulativo: perder apoyo pausa el conteo sin reiniciarlo.
- **Derrota por conocimiento global (`world_awareness`):** `Awareness` llega a 100 y la partida
  termina al instante ese mes.
- **Derrota por calendario (`deadline`):** llega el mes 224 sin completar el rito.
- **Cierre voluntario (`conceded`):** si el rito aún no empezó y `Forecast.Margin` confirma que
  ya no cabe antes del mes 224, `GameEngine.CanConcede` permite cerrar la partida como derrota
  (`GameEngine.Concede`) sin esperar meses vacíos.

### Determinismo

Cada partida queda determinada por su semilla (`Seed`), la dificultad y la secuencia de
acciones. El RNG (`Rng.Next`) vive en `GameState.RngState` y solo avanza en el director.
Previsualizar una acción (`GameEngine.PreviewAction`) nunca consume azar ni cambia el estado,
lo que permite correr las trazas grabadas y el bot de medición en `core-tests/`.

---

## 4. Economía

**Estado:** consolidado sobre `alignment-7` (Unity `0.9.0` / `0.9.1`). Fuente de verdad en código: `Balance.cs`, `GameData.cs`, `Derived.cs` y `GameEngine.cs`.

Tres magnitudes sostienen la economía: **Influence**, **Dread** y **Followers**. Solo las dos
primeras se gastan como moneda; los seguidores son a la vez motor económico, apoyo ritual y
riesgo de visibilidad ([04 — Visibilidad y awareness](gdd/04-visibility.md)).

### Influence

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

### Dread

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

#### El sacrificio como intercambio central

Sacrificar hace cuatro cosas a la vez:

1. Entrega Dread inmediato y **Madness** regional (que seguirá rindiendo Dread y acelerando
   sitios).
2. Mueve seguidores de forma permanente a `Losses` ([03 — Población](gdd/03-population.md)).
3. Reduce tus ingresos y frena el **crecimiento futuro** de `Awareness` (no borra la awareness
   ya acumulada).
4. Con *Dreams of R'lyeh* durante el rito final, adelanta el rito 1 mes (o 2 meses si la región
   tiene 30+ Madness) siempre que queden tres sitios con apoyo, a cambio de +2 awareness.

### Followers (Seguidores)

Viven por región, nunca en una reserva global. 500 seguidores repartidos en cinco regiones no
equivalen a 500 en una sola, porque el apoyo ritual, las amenazas y los sacrificios son
regionales.

### Costes variables principales

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

### Previsualización y validación

`GameEngine.PreviewAction` es la autoridad única sobre si una acción es legal, cuánto cuesta
(incluyendo la `Awareness` que suma), qué produce y qué riesgos conlleva (por ejemplo, si
detendría el rito activo). La interfaz, las pruebas y el bot pasan por ella; `PerformAction`
revalida la vista previa antes de cobrar nada.

---

## 5. Población, Mysticism y Madness

**Estado:** `alignment-7` (Unity `0.9.0` / `0.9.1`). Código en `Balance.cs`, `GameData.cs`, `Derived.cs` y `GameEngine.cs`.

### Los cinco grupos poblacionales

Cada región divide su población en cinco cubetas (`Population` en `GameState.cs`):

| Grupo | Significado |
| --- | --- |
| `Unaware` | Personas ajenas a lo sobrenatural. Reclutables. |
| `Deniers` | Personas que rechazan lo sobrenatural. Solo se reclutan tras convertirlas con una manifestación. |
| `Believers` | Personas que aceptan lo imposible pero aún no entran al culto. Se reclutan antes que `Unaware` y suben la presión de amenazas. |
| `Followers` | Tu culto en la región. Genera Influence, sostiene sitios y sufre bajas. |
| `Losses` | Bajas permanentes por sacrificios u oposición. Nunca vuelven. |

### Invariante de conservación

La suma de los cinco grupos equivale siempre a la capacidad fija de la región:

```
Derived.Capacity(r) = Unaware + Deniers + Believers + Followers + Losses = PopulationWeight × 100
```

Los ocho pesos suman 100, así que el mundo tiene 10.000 personas (`GameData.WorldPopulation`).
Como el apoyo requerido por un sitio depende de `Capacity(r)`, el código y las pruebas siempre
mueven gente entre grupos (`Derived.Recruit`, `Derived.LoseFollowers`) y nunca asignan
`Followers` de forma directa.

### Composición inicial y rasgos regionales

Al iniciar la partida (`GameEngine.CreateGameState`), cada región reparte su población entre
`Deniers` (según `Skepticism`) y `Unaware`. Al fundar la primera célula (`PlaceStartingFoothold`)
reclutas `Balance.StartingFollowers` (100) seguidores.

Cada región tiene tres rasgos fijos en `GameData.Regions`:
- `Skepticism`: fija el muro inicial de negadores.
- `InstitutionalResistance`: multiplica la presión de amenazas ([05](gdd/05-opposition.md)).
- `Mysticism`: cambia cuánto cuesta y cuánto rinde manifestar lo imposible.

### Reclutamiento

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

### Manifestaciones y Mysticism

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

### Madness (locura regional)

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

### Pérdida de seguidores y colapso de célula

`Derived.LoseFollowers` traslada seguidores a `Losses`. Si una región cae a 0 seguidores, la
célula colapsa: termina el culto público, se cancela la dispersión y cualquier sitio ritual en
esa región se destruye por completo, incluida su inversión.

---

## 6. Visibilidad, exposición y awareness

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

### Exposición regional (`Exposure`)

Sube con tus acciones en la región (reclutar poco, enterrar +8, silenciar +6, restaurar +10,
sacrificar +12, manifestar/hacerse público/sitios y revelar figuras mucho) y deriva cada mes:
las células públicas acumulan exposición; las ocultas la pierden lentamente hasta su **piso**.

**Pisos de evidencia:** una célula clandestina sin sitio puede bajar hasta 0; el culto público
impone un piso intermedio y un **sitio ritual financiado impone un piso alto permanente** (un
sitio nunca puede ocultarse por completo). La exposición alimenta tanto la probabilidad de
amenaza como su **severidad (1–3)**.

### Awareness mundial (condición de derrota)

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

#### Consecuencias

- **Lastre (`Derived.AwarenessDrag`):** `max(0, 1 − awareness / 150)`. Multiplica a la vez los
  ingresos de Influence y el reclutamiento (salvo células públicas con *Open worship*). Crea
  una espiral palpable mucho antes de llegar a 100.
- **Derrota (`world_awareness`):** al alcanzar `100`, la partida termina inmediatamente ese mes.

### Herramientas para gestionar y bajar la awareness

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

### Culto público (`reveal` / `hide`)

Hacerse público duplica el reclutamiento automático (`×2,5` con *Open worship*), pero suma
exposición, impone un piso de evidencia, aplica `backlash` temporal y convierte futuras
amenazas en redadas (`raid`, con más bajas). `hide` termina el culto público pero deja la
exposición ya generada para que decaiga sola. Durante *The Great Procession*, ninguna célula
pública puede volver a la clandestinidad.

---

## 7. Oposición, amenazas y oportunidades

**Estado:** consolidado sobre `alignment-7` (Unity `0.9.0` / `0.9.1`). Fuente de verdad en código: `Director.cs`, `Threats.cs`, `GameEngine.cs` y `Balance.cs`.

El mundo no es una IA rival independiente, sino un sistema de presión determinista
(`Director.Tick`) que reacciona a lo visible y arriesgado que se vuelve tu culto.

### Cadencias del Director

Corre al cierre de cada mes con dos ritmos independientes:

- **Tiradas de amenaza:** cada **4 meses** (`Balance.ThreatRollMonths`), cada región con
  seguidores, sin amenaza pendiente, sin estar dispersa y fuera de respiro tira contra su
  presión regional (`Director.ThreatPressure`).
- **Oportunidades:** cada **12 meses**, una región con célula y sin oportunidad activa recibe
  un presagio o figura con vigencia de 8 meses.

En el mes 176 (`PreparationMonths − 24`), el director emite un aviso anunciando que al abrirse
la alineación (mes 200) se intensificará la caza contra los sitios rituales.

### Presión regional de amenazas (`Director.ThreatPressure`)

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

### Tipos de amenaza y severidad

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

### Las tres respuestas y el respiro

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

### Oportunidades

Cada región puede tener una oportunidad activa durante 8 meses:

| Oportunidad | Acción | Efecto |
| --- | --- | --- |
| `nightmare` (Pesadilla compartida) | `exploit` (10 Inf) | Entrega un bloque inmediato de Dread |
| `eclipse` (Eclipse sin luz) | `exploit` (10 Inf) | Adelanta varios meses la preparación de un sitio incompleto |
| `celebrity` (Artista célebre) | `convert` (45 Inf) → `reveal_figure` | Secreta: `+0,6` Influence/mes. Al revelarse: hasta 60 reclutas y `+8` meses de sitio (+8 awareness, exposición y backlash) |
| `politician` (Ministro estudioso) | `convert` (45 Inf) → `reveal_figure` | Secreto: `+2` meses de aviso ante amenazas. Al revelarse: mismo efecto que artista y termina el bono secreto |

Su nivel real de uso frente a las amenazas queda como observación activa para la beta pública
([09 — Open problems](gdd/09-open-problems.md)).

---

## 8. Sitios rituales, árbol de talentos y rito final

**Estado:** consolidado sobre `alignment-7` (Unity `0.9.0` / `0.9.1`). Fuente de verdad en código: `Balance.cs`, `GameData.cs`, `GameEngine.cs` y `Forecast.cs`.

Tres sitios rituales preparados y sostenidos son la base para ganar. Todo el resto de la
economía y la defensa existe para financiar esos sitios, mantenerlos vivos y completar el rito
antes del cierre de la alineación.

### Apoyo regional (`Derived.SiteRequirement` / `Derived.Supported`)

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

### Financiación y mantenimiento (`prepare`)

Financiar un sitio cuesta **40 Influence** más un coste creciente en Dread
(`Balance.SiteDreadCosts`: **60, 100 y 160** para el 1.º, 2.º y 3.º). Además:

- Impone un piso alto permanente de exposición local y suma **+8 de awareness**.
- Programa inmediatamente una amenaza ocultista (`occult`) en esa región.
- Exige **1 Influence/mes** de mantenimiento (`Balance.SiteUpkeep`). Si no alcanza la Influence
  para cubrirlo, el sitio queda `Dormant` ese mes (no avanza ni sostiene el rito).

`GameEngine.PreviewAction` bloquea financiar un sitio si matemáticamente ya no queda tiempo
para prepararlo y completar el rito antes del mes 224 (descontando eclipses, figuras secretas
presentes y aceleración disponible).

### Preparación, aceleración y restauración (`restore`)

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

### Árbol de talentos y remates (`alignment-5` / `alignment-6` / `alignment-7`)

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

### El rito final y su previsión (`Forecast`)

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

---

## 9. Mapa estratégico y ritmo de partida

**Versión:** 0.4 · **Fecha:** 2026-11-25 · **Estado:** mapa y calendario implementados (`alignment-7`); duración objetivo de 15–30 min con pausas en validación durante la beta abierta.

### Función del mapa

La partida se desarrolla en un mapa mundial de ocho regiones conectadas. Las conexiones
limitan la expansión mientras exista alguna célula activa. Si toda la red cae, `expand`
permite reconstruir en cualquier región elegible siempre que haya recursos y población
reclutable. No hay secuencia de niveles lineales.

Las capacidades son abstracciones de juego (fotografía de
[GameData.cs](../unity/Assets/Awakening/Core/Rules/GameData.cs) al 2026-11-25; no
describen demografía ni instituciones reales):

| Región visible | ID | Peso poblacional | Mysticism | Resistencia institucional | Seguidores para apoyar un sitio |
| --- | --- | ---: | ---: | ---: | ---: |
| North America | `north_america` | 14 | 35 | 75 | 350 |
| South America *(antes Latin America en 0.9.0)* | `latin_america` | 12 | 65 | 45 | 300 |
| Europe | `europe` | 13 | 30 | 80 | 325 |
| Africa | `africa` | 15 | 70 | 35 | 375 |
| West Asia | `middle_east` | 10 | 60 | 70 | 250 |
| South Asia | `south_asia` | 18 | 65 | 50 | 450 |
| East Asia | `east_asia` | 15 | 35 | 85 | 375 |
| Oceania | `oceania` | 3 | 55 | 45 | 75 |

La capacidad regional es `PopulationWeight × 100` (10.000 personas en todo el mundo) y el
requisito de apoyo de un sitio es `max(60, round(capacidad × 0,25))`. Las adyacencias y
etiquetas viven en [GameData.cs](../unity/Assets/Awakening/Core/Rules/GameData.cs).
La geometría usa [Natural Earth](../unity/Assets/Awakening/Licenses/NATURAL_EARTH.md).

Las regiones pobladas sirven mejor para reclutar, generar Influence y sacrificar; las
regiones pequeñas exigen menos seguidores para sostener un sitio ritual. Escepticismo,
resistencia institucional y `Mysticism` cambian el muro inicial de negadores, la presión de
amenazas y el coste/efecto de manifestar (`alignment-5`).

### Fases y cadencia actuales

1. **Colocación (`placing`):** elegir región inicial; el tiempo no avanza.
2. **Preparación (`playing`):** meses 0–199 (enero 1908 – agosto 1924). Expandir la red,
   tomar talentos al abrirse sellos, acumular Dread, gestionar `Madness` y preparar 3 sitios.
3. **Alineación (`alignment`):** mes 200 (septiembre 1924). Pausa automática para revisar si
   el rito puede empezar. La ventana dura 24 meses (hasta mes 224, septiembre 1926).
4. **Rito final (`awakening`):** sostener 3 sitios listos durante **12 meses acumulados**
   (u **8 meses** con *The Great Procession*, y adelantable con *Dreams of R'lyeh*), mientras
   sube la caza de sitios (`+0,6` de presión).
5. **Resultado (`won` / `lost`):** victoria (`awakened`) o derrota (`world_awareness`,
   `deadline`, o cierre voluntario `conceded` cuando el rito ya no cabe).

Fotografía vigente en [Balance.cs](../unity/Assets/Awakening/Core/Rules/Balance.cs) y
[GameData.cs](../unity/Assets/Awakening/Core/Rules/GameData.cs):

- **Preparación de cada sitio:** **24 meses** en Normal y Hard; **20 meses** en Easy
  (`alignment-7`). Se acelera con `Madness`, eclipses o figuras reveladas.
- **Velocidad:** a 1× cada mes dura 2 segundos reales (~7,5 minutos de simulación completa
  sin pausas); la velocidad 2× acorta los tramos de espera y las pausas permiten planificar
  sin presión de reflejos.
- **Cualquier cambio de calendario** debe mover junto con él las cadencias del director,
  los avisos, las derivas mensuales y los meses de apertura de sellos de talentos.

### Objetivo de duración y medición en beta abierta

**Confirmado por dirección (D02):** partidas de **15–30 minutos incluyendo la pausa y la
planificación**. En las pruebas internas de `0.6.0` y `0.7.0` las partidas duraron 14 y 22
minutos. Con la beta pública abierta en itch.io toca medir con jugadores externos: tiempo
real de sesión, tiempo en pausa, momentos sin decisiones claras y si el ritmo de las cuatro
fases se siente bien antes de cualquier ajuste fino tardío (D25).

La [referencia de rituales](gdd/06-ritual-sites.md) detalla el margen, la aceleración y las
interrupciones; [08 — Design rationale](gdd/08-rationale.md) conserva el historial de la
compresión del calendario.

---

## 10. Mundo, narrativa y eventos escritos

**Versión:** 0.7 · **Fecha:** 2026-11-25 · **Estado:** narrativa sistémica implementada; identidad del jugador (Old God) y estructura de eventos escritos (D13) cerradas a nivel de diseño; implementación de eventos escritos, epílogos y feedback audiovisual planeada como futura feature.

### Premisa e identidad del jugador (D04)

Juegas como un Old God (Dios Antiguo). Hoy encarnas a Cthulhu (`0.9.1`) y, con el
financiamiento del grant, podrás elegir también a Nyarlathotep o Shub-Niggurath
([Old_Gods.md](gdd/02_Game_Design/Old_Gods.md)). Entre enero de 1908 y septiembre de 1926
decides qué hacer con tus cultistas, cuándo manifestarte ante los mortales y a quién controlar
para despertar durante la alineación astral (alrededor del 23 de marzo de 1925, fecha del
surgimiento de R'lyeh en *The Call of Cthulhu*).

La humanidad responde a las señales, al tamaño del culto, a la locura regional y a los
rituales; si te descubre por completo, la red cae. Las figuras convertibles son arquetipos
genéricos (*celebrated artist* y *minister*).

### Qué está implementado hoy (`0.9.1`)

- **Crónica sistémica:** la historia de cada partida sale de tus acciones, manifestaciones,
  amenazas, respuestas, oportunidades, sitios interrumpidos, talentos y desenlace.
- **Marco de época (D09):** la interfaz presenta el archivo secreto de 1908–1926 (expedientes,
  recortes de prensa, telegramas, libro mayor y fechas reales), sin renombrar mecánicas para
  que todo siga siendo legible.
- **Hecho fechado al cierre:** la pantalla final resume la causa de victoria o derrota con un
  dato fechado sacado de la propia partida (`Notes.DatedFact`).

### Diseño confirmado de eventos escritos y epílogos (D13, futura feature)

Definido por dirección el 2026-11-25 para implementarse en el ciclo financiado:

- **Frecuencia:** 5 a 8 eventos por partida (uno cada 25 a 35 meses) para sumar narrativa y
  horror sin estirar la partida fuera de los 15–30 minutos.
- **Interactividad y claridad mecánica:** cada evento pausa el reloj y muestra una hoja o
  telegrama en voz lovecraftiana sobre un suceso ficticio de 1908–1926 (sin hechos históricos
  reales), con entre 3 y 5 opciones. Cada opción muestra su coste, efecto y riesgo exactos
  antes de hacer clic.
- **Dos pools de contenido:**
  1. **Pool compartido de época (1908–1926):** sucesos disparados por hitos globales o
     regionales válidos para cualquier dios (umbrales de `Awareness` 25/50/75, primera región
     con `Madness ≥ 50`, financiación del primer sitio, apertura de la alineación).
  2. **Eventos propios de cada Old God:** sucesos exclusivos que refuerzan la identidad de
     cada dios (Cthulhu primero; luego Nyarlathotep y Shub-Niggurath).
- **Epílogos diferenciados:** una hoja final escrita según el Old God, el remate con el que
  ganaste (*The Great Procession*, *The world forgets*, *Dreams of R'lyeh* o rito estándar) o
  la causa de derrota (`world_awareness`, `deadline`, `conceded`).
- **Feedback visual y auditivo progresivo (D06):** al incorporar al diseñador gráfico/ilustrador
  y al ingeniero de audio, el aspecto del escritorio, las viñetas de eventos, la música
  adaptativa y el ambiente sonoro reaccionarán al avance de `Awareness`, `Madness` y la
  cercanía de 1925.

#### Ejemplo de referencia (no implementado aún)

> **TELEGRAMA · MARZO 1914: «Un vapor ballenero regresa sin tripulación al Pacífico Sur»**
> *(Disparador: primera región con `Madness ≥ 50`, pool de Cthulhu)*
> Un casco a la deriva aparece frente a las costas de Oceanía con diarios llenos de geometría
> imposible. Los periódicos locales ya preguntan qué ocurrió cerca de `47°9′S`.
> - **1. Reivindicar el presagio:** +15 `Madness` en Oceanía, +40 `Believers`, +6 `Awareness`.
> - **2. Hundir el expediente en la capitanía:** −35 `Influence`, −10 `Exposure`, 0 `Awareness`.
> - **3. Alimentar los sueños de los estibadores:** −20 `Followers` locales, +20 `Dread`.
> - **4. Dejar que la historia se enfríe:** sin coste; +3 `Awareness` y aparece una oportunidad de `nightmare` en la región.

### Comprobación en beta abierta

Durante la beta pública (dic. 2026 – feb. 2027) revisaremos partidas reales y comentarios para
ver en qué tramos se aplana el ritmo y ubicar ahí los disparadores de eventos escritos.

Fuentes: [01. Overview](gdd/01-overview.md), [Old_Gods.md](gdd/02_Game_Design/Old_Gods.md),
[GameData.cs](../unity/Assets/Awakening/Core/Rules/GameData.cs),
[Director.cs](../unity/Assets/Awakening/Core/Rules/Director.cs) y
[Dialogs.cs](../unity/Assets/Awakening/UI/Screens/Dialogs.cs).

---

## 11. Dirección visual

**Versión:** 0.4 · **Fecha:** 2026-11-25 · **Estado:** dirección de época confirmada (D09); arte procedural actual marcado explícitamente como **temporal** (D06).

### Dirección estética confirmada (D09)

El archivo secreto de un culto sobre un escritorio iluminado por una lámpara entre **1908 y
1926**. El mapa es una carta de navegación antigua (mar oscuro, doble filete, pergamino,
rosa de los vientos y marca de R'lyeh en el Pacífico Sur) y cada panel funciona como un
documento físico: expediente (región), recortes de prensa (amenazas), telegramas (avisos),
libro mayor (barra inferior) y hojas impresas (diálogos y árbol de talentos).

El tono de época vive en el marco; los nombres de las mecánicas no cambian. La paleta asigna
un acento semántico por función (verdín para el culto, sangre de buey para peligro, violeta
para presagios/Dread/Madness, ocre para advertencias) en
[Palette.cs](../unity/Assets/Awakening/UI/Kit/Palette.cs). Tipografía actual:
IM Fell English, Old Standard TT y Courier Prime.

### Base temporal implementada (0.5.0–0.9.0)

Todo el prototipo actual se dibuja mediante código en Unity UI Toolkit (`Painter2D` para el
mapa, grano de papel y luz de lámpara generados en memoria al iniciar), sin ilustraciones ni
assets artísticos dedicados. Incluye indicadores legibles en mapa para seguidores, alertas,
regiones con `Madness` (`MAD N%`, velado violeta) y sitios acelerados (anillo y `>>`).

### Plan de arte final (D06 y D26 — futuro)

**Decisión confirmada el 2026-11-25:** el arte procedural actual es **temporal**. El objetivo
es contratar artistas profesionales para llevar la identidad visual a calidad de lanzamiento:

- **En Camino 2 (Unity Fellowship de 50.000 USD):** contratar **1 diseñador gráfico /
  ilustrador** (equipo en Ecuador: **$3.000 USD · 2 meses**) para cubrir con una sola línea
  visual coherente tanto la UI como las ilustraciones del juego, con alcance cerrado para no
  salirse del presupuesto:
  1. **UI del archivo 1908–1926:** piezas reutilizables 9-slice para expedientes, libro mayor,
     recortes de amenazas, marco del árbol de talentos, 16 iconos de acciones, 12+ iconos y
     sellos de talentos, estampas de amenazas/presagios y medidores, conservando el layout ya
     validado (60/60 combinaciones escritorio/táctil).
  2. **Arte e ilustraciones del juego:** carta cartográfica ilustrada, emblemas/presencias de
     los **3 Old Gods** (Cthulhu, Nyarlathotep y Shub-Niggurath), **feedback visual progresivo
     de horror** (capas/viñetas al subir `Awareness` y `Madness`), 4–6 ilustraciones de
     finales/despertar y viñetas para eventos escritos futuros (D13).
- **En Camino 1 (sin grant):** mantener la base visual actual pulida (o con mejoras puntuales
  de bajo coste) para cerrar una experiencia completa de Cthulhu en itch.io.

### Principios que el arte final debe preservar

- Reconocer de un vistazo la región seleccionada, sus amenazas, su `Madness` y los sitios
  detenidos o acelerados.
- No depender únicamente del color para estados críticos (etiquetas, símbolos y jerarquía).
- Mantener legibles todos los plazos a 1280×720, 1600×900 y pantallas compactas.

Fuentes: [WorldMap.cs](../unity/Assets/Awakening/UI/Map/WorldMap.cs),
[Palette.cs](../unity/Assets/Awakening/UI/Kit/Palette.cs),
[PaperGrain.cs](../unity/Assets/Awakening/UI/Kit/PaperGrain.cs) y licencias en
[Licenses](../unity/Assets/Awakening/Licenses).

---

## 12. Dirección de audio

**Versión:** 0.3 · **Fecha:** 2026-11-25 · **Estado:** SFX procedurales temporales implementados; banda sonora y diseño sonoro contratados en Camino 2 (D06, D26).

### Base temporal actual

El juego sintetiza efectos de sonido al iniciar en
[Sfx.cs](../unity/Assets/Awakening/UI/Audio/Sfx.cs): clic, acción, rechazo, amenaza,
presagio, sitio y final, sobre cuatro reproductores simultáneos. El menú permite silenciar y
ajustar volumen. No hay música, ambientes ni voces en el prototipo actual.

### Plan de audio final (D06 y D26 — futuro)

- **En Camino 2 (Unity Fellowship de 50.000 USD):** contratar **1 ingeniero de audio**
  (equipo en Ecuador: **$3.000 USD · 2 meses**) para banda sonora y efectos de sonido con
  paquete acotado de entregas:
  - **Banda sonora original (3–4 pistas adaptativas en bucle):** menú/inicio, preparación,
    tensión alta (`Awareness`/`Madness`) y alineación/rito final, acorde al tono lovecraftiano
    de 1908–1926 y pensada para no fatigar en sesiones de 15–30 minutos.
  - **Banco de SFX y capas ambientales (25–35 efectos con feedback progresivo de horror):**
    archivo/papel, sellos del árbol de talentos, amenazas, sacrificios, susurros/ambiente de
    `Madness` y `Awareness` crecientes, eventos escritos y desenlaces de los 3 Old Gods.
- **En Camino 1 (sin grant):** conservar y afinar el sistema ligero actual para no bloquear
  el cierre de la versión base de Cthulhu.

### Función sonora y comprobación

1. **Jerarquía de lectura:** advertencias de plazo o amenaza > hitos de fase (alineación,
   rito detenido/completado, sello de talento) > confirmaciones de acción > ambiente/música.
2. **Sin dependencia exclusiva del audio:** toda señal crítica debe seguir siendo legible en
   texto y gráficos con el juego silenciado.
3. **Pruebas futuras:** verificar varias amenazas simultáneas, repetición de acciones y
   transición al rito para evitar fatiga auditiva o pérdida de alertas.

---

## 13. Base técnica y límites

**Versión:** 0.4 · **Fecha:** 2026-11-25 · **Estado:** arquitectura vigente en Unity 6.3 LTS (`alignment-7`, juegos 0.9.0 / 0.9.1).

### Motor y módulos

Desde `0.5.0` el juego vive en `unity/`, con **Unity 6.3 LTS (`6000.3.25f1`)**, C# y UI
Toolkit (solo versiones LTS, nada experimental — D12). Las reglas vigentes son `alignment-7`
(`0.9.0` publicada como demo público WebGL en itch.io; `0.9.1` en progreso). La versión
Godot 4.7 (`0.2.0`–`0.4.0`) permanece en `godot/` y en la rama `godot` como referencia
histórica. El prototipo React/TypeScript de `src/` dejó de ser referencia en `alignment-2`.

| Módulo | Responsabilidad |
| --- | --- |
| `unity/Assets/Awakening/Core/Rules/` | Estado (`GameState`), constantes (`Balance`), catálogos (`GameData`), simulación y acciones (`GameEngine`), cálculos derivados (`Derived`), director (`Director`), amenazas (`Threats`), previsiones (`Forecast`) y RNG determinista (`Rng`). **Sin dependencias de Unity.** |
| `unity/Assets/Awakening/Core/` | Sesión de juego (`GameSession`), guardado (`SaveData`) y captura/telemetría de playtest (`PlaytestCapture`, `PlaytestOutbox`, `PlaytestStore`) |
| `unity/Assets/Awakening/Platform/` | Persistencia en IndexedDB (WebGL) o archivo con candado (escritorio) y envío de lotes a GTC Playtest Lab |
| `unity/Assets/Awakening/UI/` | Pantallas (`StartScreen`, `GameScreen`, `RegionPanel`, `Dialogs`), mapa procedural (`WorldMap`), textos/notas (`Notes`), estilos (`Palette`, `PaperGrain`) y audio (`Sfx`) |
| `core-tests/` | Pruebas unitarias y trazas grabadas (`dotnet test core-tests/Tests`: 310 pruebas en 0.9.1) y herramienta de medición/bot (`core-tests/Tools`: `measure`, `trace`, `regenerate-goldens`) |

Toda acción se previsualiza y valida mediante `GameEngine.PreviewAction` antes de ejecutarse
en `GameEngine.PerformAction`. Las reglas son deterministas y puras respecto al estado y la
semilla, independientes de cualquier panel de UI.

### Datos, guardado y telemetría

- **Conservación de población:** los cinco grupos regionales (`Unaware`, `Deniers`,
  `Believers`, `Followers`, `Losses`) suman siempre la capacidad fija de la región
  (`PopulationWeight × 100`). Modificar seguidores sin transferir desde otro grupo rompe
  el cálculo de apoyo de sitios.
- **Fuente única de parámetros:** [Balance.cs](../unity/Assets/Awakening/Core/Rules/Balance.cs)
  y [GameData.cs](../unity/Assets/Awakening/Core/Rules/GameData.cs). Con `alignment-7`,
  `dotnet run --project core-tests/Tools -- measure --seeds=48` reproduce 83/144 victorias
  del bot en Normal (142/144 en Easy, 47/144 en Hard).
- **Guardado (`save-v2`):** en WebGL usa IndexedDB (heredando ajustes, tutorial y
  consentimiento desde versiones previas); en escritorio escribe `save-v2.json` bajo
  `Application.persistentDataPath`. Al cambiar de ruleset se retiran las partidas en curso
  anteriores con aviso, conservando ajustes. Sin backend de cuentas ni nube por ahora.
- **Playtest Lab:** adaptador vigente `awakening-6`, con consentimiento explícito del jugador
  y contrato verificado en `GTC-page/gtc-web/contracts/playtests/awakening-6/`.

### Validación y pendientes técnicos aplazados

El [README de Unity](../unity/README.md) documenta pruebas (`dotnet test`), capturas
automatizadas (`unity/shots.sh --check-ui 1`), chequeo de layout (`unity/layout-check.sh`,
60/60 combinaciones) y exportación WebGL/Linux.

**Aplazado hasta acercarse al cierre (D07 / D26):**
- Exportación y pruebas en **Windows** para Steam.
- Integración de **Steamworks** (si entra en Camino 2).
- Soporte completo de **mando/teclado** y equivalencia de **lector de pantalla** (regresión
  conocida del port desde Godot).
- Arquitectura de localización/idiomas y soporte de datos para múltiples Dioses Antiguos
  (Camino 2) y eventos escritos con pausa (D13).

---

## 14. Alcance, roadmap de 12 meses y validación

**Versión:** 0.7 · **Fecha:** 2026-11-25 · **Estado:** beta pública abierta en itch.io (`0.9.1`, `alignment-7`); roadmap de 12 meses (marzo 2027 a marzo 2028) alineado al Unity Fellowship.

### Dónde estamos hoy (completado en `0.9.1`)

Existe un ciclo completo y jugable de Cthulhu en Unity 6.3 LTS (`alignment-7`):
- Mapa mundial de ocho regiones, economía, conservación de población, 16 acciones, amenazas
  con tres respuestas y respiro de 5 meses, y oportunidades.
- Árbol de talentos intra-partida (3 ramas × 4 nodos = 12 nodos, 3 remates diferenciados y
  rendimiento visible por talento), `Mysticism` y `Madness` como recurso regional activo.
- Previsión y margen del rito, aviso de rito detenido, cierre voluntario (`conceded`), 4
  finales, guardado `save-v2`, modo táctil/compacto, 310 pruebas automatizadas de reglas y
  60/60 comprobaciones de layout.
- Desde el 2026-11-24 la build web está abierta al público en itch.io (`v0.9.1` desde el
  2026-11-25) como demo gratuito con telemetría opcional en GTC Playtest Lab (`awakening-6`).

### Dos caminos de producción (D26)

| Dimensión | Camino 1 (base propia, sin grant) | Camino 2 (Unity Fellowship, $50.000 USD en 12 meses) |
| --- | --- | --- |
| Contenido jugable | Completar y pulir Cthulhu (1 Old God) con mejoras de la beta abierta | Todo lo del Camino 1 + Nyarlathotep y Shub-Niggurath ([Old_Gods.md](gdd/02_Game_Design/Old_Gods.md)) + eventos escritos (D13) y epílogos |
| Arte y UI | Base procedural actual pulida | Contratar 1 diseñador gráfico/ilustrador ($3.000 por 2 meses en Ecuador) para UI, mapa, ilustraciones y feedback visual de horror |
| Audio | SFX procedurales actuales | Contratar 1 ingeniero de audio ($3.000 por 2 meses en Ecuador) para OST adaptativa, SFX y ambiente de horror |
| Plataforma | itch.io (web/descargable), posiblemente gratis | Steam (escritorio Windows/Linux, Q1 2028) + itch.io como demo |
| Legal y tienda | Sin estructura societaria adicional | Recibido en Shaman Llama SAS (Régimen General, Ecuador); registro IP en Ecuador y EE. UU., LLC en EE. UU. y Steam Direct |
| Difusión | Orgánica en comunidad y beta abierta | Campaña orgánica en redes, gamescom latam 2027 (abr. 2027), gamescom Alemania 2027 (ago. 2027) y Steam Next Fest |

### Roadmap y cronograma (Pre-Grant + 12 meses de Fellowship)

| Etapa | Ventana | Estado | Qué ocurre y qué se entrega |
| --- | --- | --- | --- |
| Base entregada | Sep – nov 2026 | Completado | Migración a Unity 6.3 LTS, vertical slice completo de Cthulhu (`0.5.0` a `0.9.1`), demo público en itch.io, telemetría `awakening-6`, 310 tests y 60/60 layout checks. |
| Pre-Grant (beta abierta) | Dic 2026 – feb 2027 | En curso | Seguir iterando el juego con feedback real de la beta pública (decisiones, dificultades elegidas, ramas de talentos y victorias/derrotas) y dejar cerrados los briefs de contratación. |
| Meses 1–3 (producción creativa) | Mar – may 2027 | Con grant | Arranque del Fellowship (GDC mar. 2027). Contratos de 2 meses: diseñador gráfico/ilustrador ($3k) e ingeniero de audio ($3k). Presencia en gamescom latam 2027 (22–25 abr. 2027). Arquitectura multi-dios y motor de eventos escritos (D13). |
| Meses 4–6 (integración y Dios #2) | Jun – ago 2027 | Con grant | Integración de UI final, mapa ilustrado, OST y SFX con feedback progresivo de horror. Implementación de Nyarlathotep (2.º Old God) y primera tanda de eventos escritos. Presencia en gamescom Alemania 2027 (23–29 ago. 2027). |
| Meses 7–9 (Dios #3, narrativa y tienda) | Sep – nov 2027 | Con grant | Implementación de Shub-Niggurath (3.er Old God), pool completo de eventos y epílogos diferenciados. Registro legal EC/US, LLC en EE. UU., página *Coming Soon* en Steam y campaña orgánica. |
| Meses 10–12 (polish, Steam y 1.0) | Dic 2027 – mar 2028 | Con grant | Pase tardío de sensación y balance (D25), build de Windows, Steamworks, accesibilidad (mando, teclado y lector de pantalla), localización, Steam Next Fest y lanzamiento 1.0 en Q1 2028. |

Hoja editable con fórmulas: [The_Awakening_Scope_Budget_Timeline.ods](../Grant-Unity/The_Awakening_Scope_Budget_Timeline.ods).

### Filosofía de balance y criterios de la beta abierta (D17, D25)

- **Definición de «completo» (D17):** árbol probado, gente externa jugando, balance que se
  sienta satisfactorio, varios caminos para ganar y varias formas de perder.
- **Filosofía de balance (D25):** el ajuste fino va al final. Como juego single-player, la
  meta es que se sienta bien, dé opciones reales y haga sentir inteligente al jugador cuando
  resuelve la partida.
- **Qué medimos en la beta abierta (`awakening-6`):** decisiones de los jugadores,
  modalidades/dificultades elegidas, nodos del árbol de talentos y tasa de victorias/derrotas
  (junto con duración real frente al objetivo de 15–30 minutos con pausas).

### Riesgos principales y mitigación

| Riesgo | Contexto | Mitigación |
| --- | --- | --- |
| Pocos datos externos todavía | Partidas humanas previas son internas; el demo público abrió el 2026-11-24 | Tres meses de beta abierta (dic. 2026 – feb. 2027) antes de arrancar el Fellowship |
| Retención fiscal US sobre el grant | Ecuador no tiene tratado con EE. UU. y el FAQ de Unity advierte posible *withholding tax* | Consulta enviada a `fellowship@unity3d.com` y reserva bancaria/IVA/ISD (~$1.500) en Shaman Llama SAS |
| Crecimiento de alcance con 3 Old Gods | Nyarlathotep y Shub-Niggurath deben dar variedad sin duplicar el motor | Ambos corren sobre el mismo núcleo `Core/Rules` y mapa de 8 regiones, y entran solo con financiamiento |
| Sensibilidad del respiro y `Madness` nueva | Medido solo con bot en `0.9.0` | Observar en beta abierta y ajustar en el pase tardío de balance (meses 10–12) |

Los ítems diferidos y el backlog de futuras features están en [09. Open problems](gdd/09-open-problems.md).

---

## 15. Público, distribución y negocio

**Versión:** 0.4 · **Fecha:** 2026-11-25 · **Estado:** beta pública abierta en itch.io (`v0.9.1`); dos caminos de publicación y negocio definidos (D26).

### Estado actual de distribución

Desde el 2026-11-24 la página en itch.io (`https://shamanllamagames.itch.io/the-awakening-of-cthulhu`)
está abierta como demo público gratuito en inglés (`v0.9.1` desde el 2026-11-25), con aviso de
participación opcional en GTC Playtest Lab ([itch-page.md](playtests/itch-page.md)).
Sirve como canal de beta testing abierto durante los próximos meses.

### Dos caminos de publicación y negocio (D26)

1. **Camino 1 (publicación base, sin grant):**
   - **Canal:** itch.io.
   - **Alcance:** experiencia completa y probada con Cthulhu (un solo Dios Antiguo).
   - **Precio:** posiblemente gratuito (en línea con D08 original).

2. **Camino 2 (publicación comercial en Steam con Unity Fellowship de $50.000 USD en 12 meses):**
   - **Canal principal:** Steam (escritorio Windows y Linux, Q1 2028), manteniendo itch.io como
     demo/beta.
   - **Propuesta ampliada:** UI e ilustraciones por 1 diseñador gráfico/ilustrador ($3.000 por
     2 meses), OST y SFX por 1 ingeniero de audio ($3.000 por 2 meses), Nyarlathotep y
     Shub-Niggurath como futuros Old Gods, y eventos escritos con epílogos.
   - **Estructura legal y costos base (Shaman Llama SAS, Régimen General, Ecuador):**
     - el grant entra a la cuenta de Shaman Llama SAS y se declara contra los gastos deducibles
       facturados del proyecto (usando NIC 20 si cruza de año fiscal), con ~$1.500 reservados
       para SWIFT ($25–$60), IVA local no recuperable y margen de ISD;
     - registro del juego e IP en Ecuador (≤$1.000 USD);
     - registro en EE. UU. y apertura de LLC en EE. UU. (~$2.500 USD) para cobrar en Steam, más
       Steam Direct ($100).
   - **Promoción y eventos:** campaña orgánica en redes, Steam Next Fest, gamescom latam 2027
     (22–25 abr. 2027, ~$2.150 USD) y gamescom Alemania 2027 (23–29 ago. 2027, ~$3.950 USD como
     asistente Business; +$3.670 opcional si Indie Arena Booth).

### Hipótesis de audiencia

Jugadores de estrategia single-player que disfrutan gestionar recursos bajo presión de reloj,
probar distintas ramas en partidas de 15 a 30 minutos y la temática lovecraftiana. La beta
pública servirá para contrastar esta hipótesis con jugadores reales.

### Trabajo de tienda aplazado (D07)

Queda para el tramo final del desarrollo:
- Idiomas de publicación (hoy solo inglés).
- Build de Windows, compatibilidad de mando, lector de pantalla y logros/cloud de Steamworks.
- Precio final en Steam (en Camino 2), fecha exacta y activos finales de tienda (tráiler,
  cápsulas con el arte definitivo, capturas y clasificación).

Véanse [Decisiones pendientes](gdd/00_Indice_y_Control/Decisiones_pendientes.md) y
[Alcance, roadmap y validación](gdd/09_Produccion/Alcance_y_validacion.md).

---

## 16. Problemas abiertos y futuras features

**Versión:** 0.5 · **Fecha:** 2026-11-25 · **Estado:** reorganizado sobre `alignment-7` (juego 0.9.0 beta pública / 0.9.1) y filosofía de balance D25.

Este documento separa tres cosas: (1) qué observar ahora en la beta abierta, (2) qué ideas
diferidas siguen sobre la mesa para decidir si entran al Camino 1, al Camino 2 o se descartan,
y (3) qué problemas históricos ya quedaron cerrados.

---

### 1. Abiertos actuales para observar en beta abierta (`alignment-7`)

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

### 2. Futuras features y mejoras (backlog de diseño)

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

### 3. Problemas históricos ya resueltos o superados (referencia)

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

---

## 17. Glosario

**Versión:** 0.4 · **Fecha:** 2026-11-25 · **Estado:** referencia del juego vigente (`alignment-7`, 0.9.0 / 0.9.1).

Los términos en español ayudan a conversar sobre el diseño; los términos en inglés reflejan
la interfaz y el código actuales sin comprometer todavía una localización.

| Término del juego | Uso en el GDD |
| --- | --- |
| Influence | Influencia: moneda operativa global para reclutar, expandirse, contener amenazas, comprar talentos y sostener sitios |
| Dread | Pavor/Horror ritual: moneda para manifestaciones, sitios, silenciar amenazas y el rito final. Procede de sacrificios, pesadillas compartidas (`nightmare`), `Martyrs` y, desde `alignment-7`, ingreso pasivo por `Madness` regional (`Madness/20` al mes) |
| Followers | Seguidores locales: producen Influence, sostienen sitios, alimentan sacrificios y elevan la awareness global |
| Believers | Creyentes que aceptan lo imposible pero aún no pertenecen al culto; se reclutan antes que `Unaware` y elevan la presión regional |
| Deniers | Negadores: rechazan lo sobrenatural; solo las manifestaciones los convierten en creyentes |
| Unaware | Población ajena a lo sobrenatural y disponible para reclutamiento |
| Losses | Población perdida permanentemente por sacrificios u oposición; nunca regresa |
| Cell | Célula regional del culto, activa mientras mantenga al menos un seguidor |
| Exposure | Exposición: evidencia local (0–100) que eleva frecuencia y severidad de amenazas; no causa derrota por sí sola |
| Awareness | Conocimiento global del mundo sobre el culto (0–100); frena ingresos/reclutamiento y causa derrota inmediata al llegar a 100 |
| Mysticism | Rasgo regional fijo que mide la apertura a lo imposible; reduce la awareness y aumenta las conversiones al manifestar (`alignment-5`) |
| Madness | Locura regional (0–100) generada por `manifest` (+20) y `sacrifice` (+10, ×2 con *Dreams of R'lyeh*); baja 1/mes. Rinde Dread pasivo, acelera sitios y, desde 50, atrae ocultistas y +0,05 awareness/mes (`alignment-7`) |
| Site | Sitio ritual financiado; cuesta 1 Influence/mes de mantenimiento y exige apoyo continuo de seguidores locales |
| Supported | Con suficientes seguidores locales (y sin estar dispersa) para sostener el sitio |
| Prepared | Con la preparación completa (24 meses en Normal/Hard, 20 en Easy); para contar en el rito requiere además apoyo activo y mantenimiento al día |
| Alignment | Ventana de alineación astral (meses 200–224, sept. 1924 – sept. 1926) en la que puede iniciarse y completarse el rito |
| Talent tree | Árbol de talentos dentro de cada partida (`alignment-5`): 3 ramas (*Congregation*, *Hidden hand*, *Harbinger*) de 4 nodos — raíz, dos medios excluyentes y remate. No se completa en una partida |
| Seal | Sello: permiso temporal para comprar un talento (6 en Easy, 5 en Normal, 4 en Hard) con coste creciente en Influence |
| Capstone | Remate: último nodo de una rama; abre una forma distinta de resolver el cierre (*The Great Procession*, *The world forgets*, *Dreams of R'lyeh*) |
| Bury | Enterrar el registro (`bury`): baja 5 de awareness mundial con Influence creciente por uso y +8 de exposición local |
| Restore | Restaurar el sitio (`restore`): recupera meses de preparación perdidos ante una amenaza ocultista a cambio de Influence y exposición |
| Obstruct | Obstruir (`counter`): anular una amenaza con Influence; 12 meses de cooldown regional y 5 meses de respiro |
| Scatter | Dispersar (`scatter`): ocultar la célula 8 meses (5 con *Sleeper cells*); evita la amenaza gratis, pero frena ingresos, reclutamiento y sitio |
| Silence | Silenciar (`silence`): anular una amenaza con Dread a cambio de awareness mundial y exposición; 12 meses de cooldown y 5 meses de respiro |
| Cornered | Amenaza acorralada: amenaza que no puede obstruirse ni silenciarse en ese momento; solo queda dispersar o recibir el golpe |
| Grace | Respiro: 5 meses sin nuevas tiradas de amenaza en una región tras obstruir o silenciar (`alignment-7`) |
| Doctrine | Especialización de una partida hasta `alignment-4`; reemplazada por las raíces del árbol de talentos |
| Director | Sistema determinista con semilla (`Director.cs`) que programa amenazas y oportunidades |
| Seed | Semilla aleatoria que hace reproducible una partida junto con la secuencia de acciones |
| Old God | Dios Antiguo jugable. Hoy solo existe Cthulhu; en el Camino 2 (Unity Grant) se planea sumar 1 o 2 adicionales para variar reglas y rejugabilidad (futuro) |
| `alignment-7` | Identificador del conjunto de reglas vigente (juegos 0.9.0 y 0.9.1). El historial completo vive en [versiones.md](playtests/versiones.md) |
