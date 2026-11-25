# 01. Resumen de reglas y flujo de partida

**Estado:** `alignment-7` (Unity `0.9.0` / `0.9.1`). Código en `unity/Assets/Awakening/Core/Rules/`.

## Premisa mecánica

Juegas como un Old God (Dios Antiguo). Hoy encarnas a Cthulhu entre enero de 1908 y septiembre
de 1926, y decides qué hacer con tus cultistas, cuándo manifestarte y a quién controlar. La
humanidad es tu recurso, el secreto es tu límite y las estrellas solo se alinean una vez.
Construyes células en ocho regiones, eliges talentos dentro de la partida, conviertes
seguidores y locura en Dread, preparas tres sitios rituales y sostienes el rito final dentro
de la ventana de alineación antes de que el mundo te descubra o venza el plazo.

## Pilares de diseño

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

## Fases de una partida

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

## Ciclo mensual

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

## Victoria y derrotas

- **Victoria (`awakened`):** inicias el rito dentro de la ventana (con 3 sitios listos y
  `Balance.FinalDread` = 100 Dread) y acumulas los meses requeridos antes del mes 224. El
  progreso es acumulativo: perder apoyo pausa el conteo sin reiniciarlo.
- **Derrota por conocimiento global (`world_awareness`):** `Awareness` llega a 100 y la partida
  termina al instante ese mes.
- **Derrota por calendario (`deadline`):** llega el mes 224 sin completar el rito.
- **Cierre voluntario (`conceded`):** si el rito aún no empezó y `Forecast.Margin` confirma que
  ya no cabe antes del mes 224, `GameEngine.CanConcede` permite cerrar la partida como derrota
  (`GameEngine.Concede`) sin esperar meses vacíos.

## Determinismo

Cada partida queda determinada por su semilla (`Seed`), la dificultad y la secuencia de
acciones. El RNG (`Rng.Next`) vive en `GameState.RngState` y solo avanza en el director.
Previsualizar una acción (`GameEngine.PreviewAction`) nunca consume azar ni cambia el estado,
lo que permite correr las trazas grabadas y el bot de medición en `core-tests/`.

[Volver al índice](README.md)
