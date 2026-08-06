# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity 6000.5.6f1 (URP 2D) game — **YukiQuest** (display title ユキちゃんたちの大冒険), a
kid-friendly nature/insects adventure structured as **chapters of mini-games**. Company
**WanderWonder Games**, bundle `com.ripandy.yukiquest`. Chapter 1 is a bee that gathers nectar from
flowers and returns to the hive; future chapters (e.g. a ダンゴムシ side-scroller) plug in as isolated
per-scene modules.

Audience is roughly kindergarten to 3rd grade, and the art and gameplay concepts come from the
author's daughter. Bias towards realising her ideas simply and legibly; there is no fail state
anywhere in Chapter 1, by design.

## Build, Run & Test

There is no CLI build script; work through the Unity Editor (open with Unity 6000.5.6f1).

- **Play/run**: open `Assets/4_Scenes/Core.unity` and enter Play mode. Build scene order is
  `Core` → `Title` → `Gameplay` (see `ProjectSettings/EditorBuildSettings.asset`). `Core` is the
  bootstrap scene.
- **Build**: File → Build Settings (or Build Profiles) in the Editor.
- **Tests**: Unity Test Framework via Window → General → Test Runner. The only test assembly is
  `Soar.Tests` (in the SOAR package, not this project's `Assets`). Run a single test by selecting
  it in the Test Runner tree, or via CLI:
  `Unity -runTests -projectPath . -testFilter <FullyQualifiedTestName> -testPlatform EditMode`.
- **Compile-checking without the Editor**: the Editor holds the project lock, so batchmode is
  usually unavailable. `csc` can build the assemblies directly against
  `<UnityInstall>/Contents/Resources/Scripting/UnityReferenceAssemblies/unity-4.8-api` (plus its
  `Facades`), `.../Scripting/Managed/UnityEngine/UnityEngine*.dll`, this project's
  `Library/ScriptAssemblies/*.dll` and `Assets/Packages/**/*.dll`. Pass `-nostdlib+`. This catches
  API and reference errors, not behaviour.
  Note the domain assembly is `autoReferenced: false`, so `Assets/Editor/` carries its own
  `YukiQuest.EditorTools.asmdef` to see `Domain` types.
- **One-shot setup** (`Assets/Editor/`, all idempotent and deletable once run):
  - `YukiQuest → Setup → Wire Normal Gameplay` — creates and wires the SOAR assets the gameplay
    components need.
  - `YukiQuest → Setup → Prepare Procedural Stage` — migrates the hand-authored stage to a generated
    one: extracts the scene's ground art into tile prefabs, seeds `LevelCollection.asset`, strips the
    now-generated scene objects and rewires the installer. It never overwrites an existing level
    collection.
- **`YukiQuest → Validate Levels`** — checks every authored level for the soft-lock (quota above
  available nectar) and for scales that make flower triggers overlap their neighbours.

## Architecture

Clean Architecture, with dependencies pointing inward. Folders are numbered by layer:

- **`Assets/1_Domain`** (`Domain` asmdef, `Domain.*` namespaces) — pure game logic, no Unity
  engine references beyond `[Serializable]`/`[SerializeField]`. Contains entities (`Game`, `Bee`,
  `Flower` — note `Game`/`Bee`/`Flower` are **structs**, so they are copied by value and must be
  written back into their lists after mutation), game states (`IGameState` implementations), and
  presenter **interfaces** (`IGamePresenter`, `IBeePresenter`, `IIntroPresenter`, …). The domain
  depends only on abstractions; it never references `UnityEngine` components.
- **`Assets/2_InterfaceAdaptors`** (`InterfaceAdaptors` asmdef, `YukiQuest.*` namespaces) —
  MonoBehaviour **presenters** and controllers that implement the domain interfaces, DI installers
  (`YukiQuest.Installer`), SOAR ScriptableObject wrappers (`YukiQuest.SOAR`), and chapter-system
  glue (`YukiQuest.Core`, e.g. `ChapterCatalog`). Also holds view-only helpers that implement no
  domain interface at all and are wired purely through the Inspector — `CameraFollow`,
  `ParallaxLayer`, `StageBoundsPublisher`.
- **`Assets/3_Contents`** — art, prefabs, audio, ScriptableObject asset instances. Per-chapter
  content lives under `Chapters/<Name>/` (levels, generated-stage prefabs).
- **`Assets/4_Scenes`** — `Core`, `Title`, `Gameplay`, `DefaultUIEnvironment`.

### Game loop / state machine
`GameplayStateMachine` (a MonoBehaviour) runs an async loop: each `IGameState.Running(ct)` returns
the **next** `GameStateEnum`; the machine keeps calling states until `GameStateEnum.None`, then
executes `resetAppCommand`. States: `IntroGameState` → `PlayGameState` → `GameOverGameState`.
`PlayGameState` deploys the player bee, then runs recursive `UniTaskVoid` loops (harvest,
store-nectar) gated by a `UniTaskCompletionSource` and a linked `CancellationTokenSource` used as
the "game over" token. It finishes when `Game.IsLevelCleared` — `CollectedNectar` reaches the quota
authored on the level (`LevelData.RequiredNectar`). A level with **no** quota counts as never cleared
rather than instantly cleared.

`Game` is a **struct bound by value at install time**, so each state gets its own copy and mutations
never flow back. `IntroGameState` and `PlayGameState` therefore both re-read the level and call
`game.Initialize(requiredNectar)` for themselves. It also means `GameOverGameState` still reports the
install-time `CollectedNectar` (zero) — a known wart, not a new one.

### Chapter 1 — bee collect & deliver
One player-controlled bee (entry 0 of `BeeList`); the remaining entries are tuning data for AI
helper bees that do not exist yet. Fly to a flower, hover until the dwell ring fills to collect
pollen, carry it home, hover at the hive to deliver the **whole** load, repeat until the quota
clears the stage.

- **No gravity.** `BeeMoveController` sets `gravityScale = 0` and freezes rotation in code so the
  prefab cannot fight it, then drives `linearVelocity` directly through `SmoothDamp`. Transient
  kicks (flap, ground bounce) live in a *separate* `impulseVelocity` that decays on its own —
  folding them into the smoothed movement erases them within a frame or two. A bounce **replaces**
  the current kick rather than adding to it; accumulating them let a player who holds into the
  ground launch off the top of the stage. The body is also forced to
  `RigidbodySleepMode2D.NeverSleep`: without gravity the bee stops dead when the player lets go,
  and Unity does not send `OnTriggerStay2D` to a sleeping body — so it would freeze the very dwell
  timer it was hovering to fill. Finally `RigidbodyInterpolation2D.Interpolate`, because movement runs
  on FixedUpdate while the camera samples the transform in LateUpdate; without it the camera chases a
  50 Hz position at display rate and the bee shimmers against the scrolling stage.
- **Anything that repositions the bee must run on FixedUpdate and only write when the value actually
  changes** (see `BoundaryHandler`). Between fixed steps an interpolated body's transform is a visual
  guess; clamping *that* and assigning it back feeds the guess into the simulation. An unconditional
  per-frame write also discards the interpolation state, which looks like jitter even far from any
  edge. Prefer `Rigidbody2D.position` over `transform.position`.
- **`CameraFollow` and `ParallaxLayer` both run in LateUpdate**, so they carry explicit
  `[DefaultExecutionOrder]` values (100 / 200). At equal ordering Unity may move the layers first,
  leaving them a frame behind the camera — the background visibly wobbles against the ground.
- **The stage is generated, not authored.** See "Procedural stage" below. `Gameplay.unity` holds no
  flowers, no ground and no stage-bounds object — only the DI context, the UI and the hive.
- **Boundaries are data, not colliders.** `StageGenerator` writes the computed extent into a
  `StageBoundsVariable`; `BoundaryHandler` clamps the sides and returns the bee to the hive when it
  exits the top, keeping its pollen. No wrapping, no ceiling collider. (`StageBoundsPublisher` is
  the hand-authored equivalent, kept for chapters that don't generate their stage.)
- **The camera lives in `Core.unity`**, which outlives every chapter and so cannot reference chapter
  scene objects. `CameraFollow` reads a `Variable<Transform>` that the bee assigns to itself, plus
  the shared `StageBoundsVariable` for edge clamping. Use this SOAR-variable handoff for anything
  else that must cross the Core/chapter boundary — a runtime-instantiated prefab cannot hold a
  scene reference either, which is why the hive spawn point is published the same way.
- **Dwell actions** are `BeeTriggerAction` subclasses (`BeeHarvestPresenter`,
  `BeeStoreNectarPresenter`): trigger-stay timers that fill a radial `Image`, then resolve a
  `UniTask` the domain is already awaiting. Flower triggers can overlap, so the base class commits
  to exactly one target and ignores the rest. **Commitment is taken in `OnTriggerStay2D`, never in
  `OnTriggerEnter2D`** — drifting off one flower while already inside its neighbour fires no Enter
  for that neighbour, so an Enter-based version could never re-commit and the ring stuck at full
  forever. For the same reason `ExecuteAction` must not decline: the domain is awaiting it, and a
  consumed request that resolves into nothing hangs that loop for the rest of the run.
- **Flower identity is `FlowerPresenter.Id`**, stamped by the generator. It used to be the scene
  sibling index, which forced the flower asset, the installer array and the scene hierarchy to agree
  on an order and broke silently when any of them changed.

### Procedural stage (Chapter 1)
A level is one `LevelData` on `LevelCollection.asset` (`Domain.Chapters.BeeHarvest`): a nectar quota
plus a **one-dimensional array of `FlowerBlock`** — `{FlowerType, nectar, scale}`, where
`FlowerType.None` is an empty block used purely for spacing. **Level N is index N−1.** `FlowerType`
values are persisted in the asset, so the enum is append-only.

`StageGenerator` (bound as the domain's `IStagePresenter`) turns that into the world during
`IntroGameState`: it publishes the stage bounds, tiles the parallax ground, scatters seeded
decoration, lays one `EdgeCollider2D` tagged `Bounds`, moves the hive to the right edge, and
instantiates the flowers. Everything it makes lives under one scene-root object, so `Clear()` is one
Destroy.

- **Blocks carry no position.** Block *i* sits at `blockWidth * i`; `blockWidth` is a generator
  constant, because spacing is a presentation concern. Vertical extent is a constant too — the bee's
  ceiling is feel, not level design.
- **`LevelData.Flowers` is the single source of truth for block index → flower id.** The domain walks
  it to build `Flower` entities and the generator walks it to instantiate prefabs; anything that
  makes the two walks disagree (skipping a flower, a prefab without a `FlowerPresenter`) puts every
  later flower on the wrong entity, so both failure paths abort loudly instead of continuing.
- **Parallax width.** A layer scrolling at factor `f` drifts `(1−f)×` the camera's travel relative to
  the camera, so it needs at most a full **stage width** of tiles — the worst case being `f = 0`.
  Tiling that much, centred on the stage, covers every factor regardless of where the camera happened
  to be when `ParallaxLayer` sampled its origin. Tile pitch is measured off a real instance, because
  `Renderer.bounds` is unreliable on an uninstantiated prefab asset.
- **No pooling.** The stage is finite and its width is known before the first tile is placed; a long
  level is a few dozen batched `SpriteRenderer`s that Unity frustum-culls anyway. Pooling buys
  nothing here and costs seam bugs and per-position variation. If it ever matters, the escape hatch
  is `SpriteRenderer.drawMode = Tiled`, not a recycler.
- **The hive is moved, never rebuilt** — the installer references its `BeePresenterFactory` by scene
  id. Its `SpawnPoint` is a child, so the bee spawns at the right edge and flies left. **Anchor it by
  its rendered right edge, never by its root**: the branch art's pivot sits ~9.5 units left of the
  sprite and the delivery trigger a further ~2.5 left of that, so placing the root at the stage edge
  throws the trigger clean outside the bounds — where `BoundaryHandler`'s x-clamp means the bee can
  never touch it. The generator asserts the trigger landed inside the stage.
- **Levels can soft-lock.** `PlayGameState` only harvests while some flower still holds nectar, so a
  level whose flowers hold less than its quota simply stops with no state to end it. `YukiQuest →
  Validate Levels` treats that as an error; author a surplus.

### Bee voice audio
Clips are named `<Line>_<Member>_<take>.mp3` for five family members (Apap, Ibun, Ranca, Raina,
Aya). A bee picks one `BeeVoice` on `Awake` and keeps it for life, so it does not switch speakers
between lines. `BeeAudioPresenter` therefore selects **by clip name, not by index** — the
`SoarList`s are not ordered consistently by member (`BeeAudio_Mitsuda` has `Ranca_4` sitting among
the Ibun takes), so equal indices mean different speakers across lines. Not every line was recorded
by every member (`Pyon` is Raina only; `Watashimo` has no Aya); those combinations fall back to any
available take rather than going silent.

### Chapter system (mini-games)
The game is a set of chapters, each a self-contained mini-game. **`AppStateManagement` is the chapter
loader**: `Core.unity` is a persistent bootstrap running `AppStateMachine`; flow is driven by a SOAR
`GameEvent<string> setNextStateEvent` — raising it with a scene name (registered in an
`AppStateCollection` + Build Settings) fades out (ScreenTransition), additively unloads the current
scene, and additively loads the target. Each chapter scene owns its own Doinject `SceneContext` +
installer, so chapters are DI-isolated. **A chapter ≈ one scene + one installer + one inner state
machine, discovered by name — the core never changes to add one.**
- Domain metadata: `Domain.Chapters` — `ChapterId` (stable enum; append-only), `IChapter`,
  `ChapterInfo` (inspector-authored `{id, displayName, sceneName}`).
- `YukiQuest.Core.ChapterCatalog : SoarList<ChapterInfo>` — the data-driven list the chapter-select
  UI reads. Adding a chapter is a catalog edit + scene registration, not core code.
- Adding a chapter later: drop a `Chapters/<Name>` folder (its own `Domain.Chapters.<Name>` states,
  installer, scene + SceneContext), add one `ChapterInfo` row, and register the scene in Build
  Settings + `AppStateCollection`. (In progress: generalizing the inner loop to
  `IGameState<TState>` / `ChapterStateMachine<TState>` so each chapter owns its own state enum.)

### Dependency Injection — Doinject (`st.mewli.di`)
Not Zenject/VContainer. Key patterns:
- **Installers** implement `IBindingInstaller.Install(DIContainer, IContextArg)` and bind with
  `container.BindSingleton<T>()` / `container.BindFromInstance<TInterface>(instance)`. See
  `Assets/2_InterfaceAdaptors/DOInject/GameplayInstaller.cs`.
- **Injection targets** implement `IInjectableComponent` and receive dependencies via an
  `[Inject] public void Construct(...)` method (see `GameplayStateMachine`).
- Domain services (game states) are bound by concrete type; presenters are bound by their domain
  interface. Per-bee presenters are held in `IDictionary<int, I...Presenter>` bindings keyed by
  bee id.
- Collections that only exist once the stage or the bees are built are **bound empty and filled at
  runtime** — `IList<Flower>` and `IList<IFlowerPresenter>` by `IntroGameState`, the per-bee
  dictionaries by `BeePresenterFactory`. Bind the instance, not the contents.
- Project-wide context: `Assets/Resources/ProjectContext.asset`.

### SOAR — ScriptableObject architecture (`com.ripandy.soar`)
The author's own framework (data/events live in ScriptableObjects). Common base types:
`Variable<T>`, `JsonableVariable<T>`, `Command`, and SO-backed lists (`SoarList`). This project
subclasses them, e.g. `GameJsonableVariable : JsonableVariable<Game>, IGamePresenter` — writing
`Value = game` both persists state (JSON) and drives the presenter. `BeeList`/`LevelCollection` are
SO lists bound as `IList<Bee>`/`IList<LevelData>`. Presenters generally hold a `[SerializeField]` SO
list and read entities by id.

### Async / reactive stack
- **UniTask** (`Cysharp.Threading.Tasks`) for all async — prefer `UniTask`/`UniTaskVoid` over
  coroutines; use `.Forget()` for fire-and-forget and `.SuppressCancellationThrow()` on
  cancellable delays.
- **R3** (`R3`, Cysharp) for reactive streams (`Observable.Interval(...).SubscribeAwait(...)`);
  dispose subscriptions in `OnDestroy`.
- **LitMotion** for tweening.

## Local package dependencies (important)
Several dependencies are **local file references** resolved to siblings of the project directory,
outside this repo (see `Packages/manifest.json`):
`/Users/ripandy/Development/Unity/SOAR` and `/Users/ripandy/Development/Unity/ModuleCollections/*`
(AppStateManagement, AudioManagement, DebugTools, InputSystemHandler, ModularScreens,
ScreenTransition, SoarExtensions). Editing those `.cs` files changes shared packages used by other
projects — treat them as external unless the task is specifically about them. That sibling
`ModuleCollections` directory has its own `CLAUDE.md`.

Other notable packages: Unity Input System (`InputSystem_Actions.inputactions`), URP 17.5 (2D),
NuGetForUnity (`Assets/packages.config`), MewCore.

## Conventions
- Namespaces: domain code under `Domain` / `Domain.*` (chapters under `Domain.Chapters.*`); app code
  under `YukiQuest.*` (`YukiQuest.Gameplay`, `YukiQuest.SOAR`, `YukiQuest.Installer`,
  `YukiQuest.Core`). Folder layer numbers encode dependency direction — never reference a
  higher-numbered layer from a lower-numbered one.
- Entities are value-type structs; after mutating a copy, assign it back into its SO list
  (`beeList[bee.Id] = bee;`).
- Presenters are MonoBehaviours implementing a `Domain.Interfaces` interface and wired via Doinject.
