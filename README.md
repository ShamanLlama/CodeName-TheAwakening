# The Awakening of Cthulhu

A single-player world-map strategy game by Lucid Labworks Studio. You are the hidden will behind a cult spread across eight world regions. Grow congregations, spend Influence, sacrifice followers for Dread, and prepare three ritual sites before the stars pass out of alignment, or before the world works out what you are.

The game is set in a lamp-lit cult archive from 1908 to 1926: the map is an old sea chart, each region is a case file, and threats arrive as press cuttings. Every graphic and sound is generated in code.

## Status

- **Version 0.7.0**, in development. Rules `alignment-5`. Changes per build are in the [version log](docs/playtests/versiones.md).
- **Engine:** Unity 6.3 LTS (6000.3.25f1) since 0.5.0. Versions 0.2.0–0.4.0 were made in Godot 4.7. They stay in `godot/` and on the `godot` branch until that version is retired.
- **Release:** a free, short, complete game, released mainly on **Steam** as a desktop build (Linux now, Windows later).
- **Beta:** the web build is on itch.io by invitation. Each build gets its own round in GTC Playtest Lab, and data is recorded only with the player's consent. The itch.io page is for testing, not the release.

The design, the decisions taken and the open questions are in the [GDD](docs/gdd/README.md), written in Spanish.

## Playing

1. Choose a difficulty, optionally enter a world seed, and click **Begin**. The first time, the game asks whether you want to join the playtest. Both choices let you play.
2. Click a region on the map, then **Begin Here** to found your first cult.
3. Click any region to open its case file. Actions are grouped under **CULT**, **RITES** and **OMENS**. With a mouse, hover over an action to see its cost, risk and why it is unavailable, then click it. On a touch screen, the first tap explains the action and a button below performs it.
4. Threats appear as cuttings at the top left. Each one has three answers: **OBSTRUCT**, **SCATTER** and **SILENCE**.
5. Click the ledger line at the bottom to open the chronicle.

| Key | Action |
|---|---|
| Space | Pause or resume |
| 1, 2 | Normal or fast speed |
| Esc | Close the case file or open the menu |
| Tab / Shift+Tab | Next or previous region |
| C, T, H | Chronicle, talents, guide |
| R | Show the whole world again |
| N | Dismiss the current note |

Actions take no time, so pausing to plan is expected. The game pauses when the window loses focus and saves after every action and month. **Continue** resumes the run. Interface size (100–200 %) and reduced motion are in the menu. The menu also exports the local save and the playtest record.

Development builds add playtest controls to the menu that advance 1, 12 or 120 months, without granting resources.

## How it plays

- **Followers** are the cult. They recruit on their own, earn Influence, and carry ritual sites.
- **Influence** pays for recruiting, opening cells in neighbouring regions, hiding evidence, answering threats and converting prominent figures.
- **Dread** comes from sacrifice (40 followers for 25 Dread) and omens. It pays for sites, manifestations and silencing.
- **Awareness** is how much the world knows. It grows with the share of humanity that follows you and slows income and recruitment. At 100 the run is lost. Every action that raises it shows the amount in its price. Concealing evidence lowers it by 3, at a price that rises with awareness; that world effect then rests for 6 months in every region (9 on Hard), while local concealment keeps working. **Bury the record** lowers it by 5 for 50 Influence, plus 25 for every earlier burial (15 on Easy, 40 on Hard), and leaves +8 Exposure in that region. Manifestations draw less attention and convert more in mystical regions (Africa, Latin America) than in sceptical ones (Europe, North America).
- **Ritual sites** cost 40 Influence and 60, then 100, then 160 Dread, plus 1 Influence a month. Each needs 24 supported months to prepare. A site is supported while a quarter of its region's population follows you: 75 followers in Oceania, 450 in South Asia.
- **Threats** grow out of exposure, public worship, sites and awareness. You can answer each one in three ways, each paid in a different currency:
  - **Obstruct:** 30 Influence per severity, and no further obstructing in that region for a year.
  - **Scatter:** free, but the cell hides for 8 months without recruiting, earning or advancing its site.
  - **Silence:** 20 Dread per severity, and world awareness rises.
- **Omens and figures:** nightmares and eclipses give Dread or speed up a site. Converted celebrities and politicians help in secret until you reveal them.
- **The talent tree** replaces the doctrine. Three branches (Congregation, Hidden hand, Harbinger) hold four talents each: a root, two middle talents that exclude each other, and a capstone. Seals open with time (5 on Normal, 6 on Easy, 4 on Hard) and each talent costs more Influence than the last (40, 70, 100…), so no run takes the whole tree.
- **Restore the site:** an ignored occult threat takes 6 preparation months from a site (4 on Easy); you can buy back half of them for 30 Influence a month.
- **The end:** after 200 months (September 1924) the alignment opens and the game pauses. With 100 Dread and three prepared, supported sites, begin the final rite and hold the sites for 12 months within the 24-month window. From that point, threats hunt your sites.

A run is about seven and a half minutes at 1×, plus pauses. The target is 15–30 minute sessions. The values are still being tuned with playtests. Exact numbers are in [`Balance.cs`](unity/Assets/Awakening/Core/Rules/Balance.cs) and [`GameData.cs`](unity/Assets/Awakening/Core/Rules/GameData.cs).

## Development

You need:

- the .NET 8 SDK for the rule tests;
- Unity 6000.3.25f1 (6.3 LTS) to build and play from the editor.

Only LTS versions are used, and nothing marked experimental.

```bash
dotnet test core-tests/Tests                                       # rules, saves, playtest capture
dotnet run --project core-tests/Tools -- measure --seeds=48        # bot win rates per strategy
```

The rules in `unity/Assets/Awakening/Core/` do not depend on Unity, so they are tested with plain .NET. Builds, screenshots, the 60-combination layout check and web load measurement are in [unity/README.md](unity/README.md).

Every change to the game goes with its playtest record, in the same change:

- raise `bundleVersion` in `unity/ProjectSettings/ProjectSettings.asset`: minor for rules or balance, patch for UI or text;
- a rule or balance change needs a new ruleset;
- a change to actions, regions, doctrines or events needs a new playtest adapter;
- add an entry to the [version log](docs/playtests/versiones.md).

The details are in [CLAUDE.md](CLAUDE.md). A rule change also updates the GDD.

## Repository

| Path | Contents |
|---|---|
| `unity/Assets/Awakening/Core/` | Rules, saves, playtest capture and the game session, with no Unity dependencies |
| `unity/Assets/Awakening/Platform/` | Browser and desktop saves, playtest transport |
| `unity/Assets/Awakening/UI/` | Screens, case files, dialogs, the chart, palette and sound |
| `core-tests/` | `dotnet` tests and tools (bot measurement, recorded traces) |
| `docs/gdd/` | Game design document |
| `docs/playtests/` | Version log and playtest records |
| `docs/unity/` | Port plan and verification |
| `godot/` | Godot 0.4.0 version, kept as a reference |
| `src/` | Original React/TypeScript prototype, historical only |
| `scripts/` | Map geometry, WebGL template and web load measurement |

## Credits

A game by Lucid Labworks Studio. The map uses [Natural Earth](unity/Assets/Awakening/Licenses/NATURAL_EARTH.md) public-domain outlines. The fonts are IM Fell English, Old Standard TT and Courier Prime, under the SIL Open Font License ([licences](unity/Assets/Awakening/Licenses/)). The GDD adapts the GDD templates of Game Time Continuum, with permission.
