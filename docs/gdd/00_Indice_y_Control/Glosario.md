# Glosario de trabajo

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
| `alignment-7` | Identificador del conjunto de reglas vigente (juegos 0.9.0 y 0.9.1). El historial completo vive en [versiones.md](../../playtests/versiones.md) |

[Volver al índice](../README.md)
