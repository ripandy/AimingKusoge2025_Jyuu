# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity 6000.5.5f1 (URP 2D) game — **YukiQuest** (display title ユキちゃんたちの大冒険), a
kid-friendly nature/insects adventure structured as **chapters of mini-games**. Company
**WanderWonder Games**, bundle `com.ripandy.yukiquest`. Chapter 1 is a bee that gathers nectar from
flowers and returns to the hive; future chapters (e.g. a ダンゴムシ side-scroller) plug in as isolated
per-scene modules. Currently on branch `refactor/maintenance_and_rebranding`.

## Build, Run & Test

There is no CLI build script; work through the Unity Editor (open with Unity 6000.5.5f1).

- **Play/run**: open `Assets/4_Scenes/Core.unity` and enter Play mode. Build scene order is
  `Core` → `Title` → `Gameplay` (see `ProjectSettings/EditorBuildSettings.asset`). `Core` is the
  bootstrap scene.
- **Build**: File → Build Settings (or Build Profiles) in the Editor.
- **Tests**: Unity Test Framework via Window → General → Test Runner. The only test assembly is
  `Soar.Tests` (in the SOAR package, not this project's `Assets`). Run a single test by selecting
  it in the Test Runner tree, or via CLI:
  `Unity -runTests -projectPath . -testFilter <FullyQualifiedTestName> -testPlatform EditMode`.

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
  glue (`YukiQuest.Core`, e.g. `ChapterCatalog`).
- **`Assets/3_Contents`** — art, prefabs, audio, ScriptableObject asset instances.
- **`Assets/4_Scenes`** — `Core`, `Title`, `Gameplay`, `DefaultUIEnvironment`.

### Game loop / state machine
`GameplayStateMachine` (a MonoBehaviour) runs an async loop: each `IGameState.Running(ct)` returns
the **next** `GameStateEnum`; the machine keeps calling states until `GameStateEnum.None`, then
executes `resetAppCommand`. States: `IntroGameState` → `PlayGameState` → `GameOverGameState`.
`PlayGameState` orchestrates gameplay with recursive `UniTaskVoid` loops (bee deployment, harvest,
store-nectar) gated by `UniTaskCompletionSource` and a linked `CancellationTokenSource` used as the
"game over" token.

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
- Project-wide context: `Assets/Resources/ProjectContext.asset`.

### SOAR — ScriptableObject architecture (`com.ripandy.soar`)
The author's own framework (data/events live in ScriptableObjects). Common base types:
`Variable<T>`, `JsonableVariable<T>`, `Command`, and SO-backed lists (`SoarList`). This project
subclasses them, e.g. `GameJsonableVariable : JsonableVariable<Game>, IGamePresenter` — writing
`Value = game` both persists state (JSON) and drives the presenter. `BeeList`/`FlowerList` are
SO lists bound as `IList<Bee>`/`IList<Flower>`. Presenters generally hold a `[SerializeField]` SO
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
