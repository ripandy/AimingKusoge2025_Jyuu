using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain;
using Domain.Chapters.BeeHarvest;
using Soar;
using Soar.Variables;
using UnityEngine;
using UnityEngine.SceneManagement;
using YukiQuest.SOAR;
using Random = System.Random;

namespace YukiQuest.Gameplay
{
    /// <summary>
    /// Builds the bee chapter's stage from a <see cref="LevelData"/>. The level authors a
    /// one-dimensional strip of blocks; everything spatial is decided here — how wide a block is,
    /// where the ground sits, how the parallax layers tile, where the hive goes.
    /// </summary>
    /// <remarks>
    /// Everything generated lives under a single scene-root object so <see cref="Clear"/> is one
    /// Destroy. The stage is deliberately built in world space at the origin rather than under this
    /// component, so a stray transform on the generator object cannot shift the whole level.
    /// </remarks>
    public class StageGenerator : MonoBehaviour, IStagePresenter
    {
        /// <summary>One tiled, parallaxing band of scenery.</summary>
        [Serializable]
        private struct GroundLayer
        {
            public string name;
            public GameObject tilePrefab;

            [Tooltip("0 pins the layer to the world, 1 pins it to the camera.")]
            [Range(0f, 1f)] public float horizontalFactor;

            [Tooltip("Keep this at 0 for the band that reads as the ground surface, or it will " +
                     "drift away from the collider as the camera rises.")]
            [Range(0f, 1f)] public float verticalFactor;

            public float yOffset;
        }

        [Header("References")]
        [SerializeField] private StageBoundsVariable stageBounds;
        [Tooltip("Moved to the right edge on build. The hive art hangs off a branch, so it only " +
                 "reads correctly against the edge of the stage.")]
        [SerializeField] private Transform beeHive;

        [Header("Layout")]
        [Tooltip("World units per level block. Must stay wider than twice the largest scaled flower " +
                 "trigger radius, or neighbouring flowers fight over the bee.")]
        [SerializeField] private float blockWidth = 3f;
        [SerializeField] private float groundY = -5.02f;
        [Tooltip("Playable height above the ground. Exiting the top returns the bee to the hive.")]
        [SerializeField] private float stageHeight = 14f;
        [SerializeField] private float edgePadding = 2f;
        [Tooltip("Gap between the right stage edge and the hive's rightmost drawn pixel — not its " +
                 "root, whose pivot is nowhere near the art.")]
        [SerializeField] private float hiveEdgeInset = 0.5f;

        [Header("Flowers")]
        [SerializeField] private SerializedKeyValuePair<FlowerType, GameObject>[] flowerPrefabs;
        [Tooltip("Lifts flower roots off the ground line; their pivots sit slightly below the soil.")]
        [SerializeField] private float flowerYOffset = 0.5f;

        [Header("Ground")]
        [SerializeField] private GroundLayer[] groundLayers;
        [SerializeField] private string boundsTag = "Bounds";

        [Header("Decoration")]
        [SerializeField] private GameObject[] decorationPrefabs;
        [Tooltip("Decoration items per world unit of stage width.")]
        [SerializeField] private float decorationPerUnit = 0.12f;
        [SerializeField, Range(0f, 1f)] private float decorationParallax = 0.05f;
        [SerializeField] private float decorationYOffset;
        [SerializeField] private Vector2 decorationYJitter = new(-0.3f, 0.4f);
        [Tooltip("Mixed with the level's own numbers, so a level always scatters the same way.")]
        [SerializeField] private int decorationSeed = 1;

        [Header("Decorative bees")]
        [Tooltip("Background swarm. Collides with itself but ignores the player — that separation is " +
                 "the DecorBee row of the Physics2D matrix, not anything in code.")]
        [SerializeField] private GameObject decorBeePrefab;
        [SerializeField] private int decorBeeCount = 8;
        [Tooltip("Height band above the ground the swarm hangs around in.")]
        [SerializeField] private Vector2 decorBeeHeightRange = new(2f, 7f);
        [SerializeField] private string decorBeeLayerName = "DecorBee";
        [Tooltip("How many of the swarm tag along with the player instead of wandering. They are " +
                 "company only — they harvest nothing. Kept low: a follower hangs around the player, " +
                 "so it is near the flower whenever they are harvesting.")]
        [SerializeField] private int followerBeeCount = 2;
        [Tooltip("The same PlayerBeeTransform the Core camera follows. Left empty, every bee wanders.")]
        [SerializeField] private Variable<Transform> playerBeeTransform;
        [Tooltip("Decor bees weigh a fraction of the player, so a shared collision throws them and " +
                 "only nudges the player. Untick if a bump ever drags the bee off a flower it is " +
                 "harvesting — the swarm still bounces off itself either way.")]
        [SerializeField] private bool decorBeesCollideWithPlayer = true;

        /// <summary>Below this a tile would tile forever; treated as a misconfigured prefab.</summary>
        private const float MinTileWidth = 0.01f;

        /// <summary>Where the player bee, the ground and the flowers all live.</summary>
        private const int DefaultLayer = 0;

        private Transform stageRoot;

        public UniTask<IReadOnlyList<IFlowerPresenter>> BuildAsync(
            LevelData level, CancellationToken cancellationToken = default)
        {
            Clear();

            stageRoot = new GameObject($"Stage ({level.BlockCount} blocks)").transform;
            stageRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            // Left unparented so nothing can offset or scale the level, but explicitly moved into
            // this chapter's scene — a bare `new GameObject` lands in the *active* scene, which is
            // the persistent Core scene, and the stage would then outlive the chapter it belongs to.
            SceneManager.MoveGameObjectToScene(stageRoot.gameObject, gameObject.scene);

            // Bounds first: every width below is derived from them, and BoundaryHandler and
            // CameraFollow both read the published rect.
            var bounds = ComputeBounds(level);
            if (stageBounds != null) stageBounds.Value = bounds;
            else Debug.LogError($"[{GetType().Name}] No StageBoundsVariable assigned; the bee will " +
                                $"be clamped to whatever the asset last held.");

            BuildGround(bounds);
            BuildDecoration(bounds, level);
            BuildDecorBees(bounds, level);
            BuildGroundCollider(bounds);
            PlaceHive(bounds);

            var presenters = BuildFlowers(level);
            return UniTask.FromResult<IReadOnlyList<IFlowerPresenter>>(presenters);
        }

        public void Clear()
        {
            if (stageRoot == null) return;

            if (Application.isPlaying) Destroy(stageRoot.gameObject);
            else DestroyImmediate(stageRoot.gameObject);

            stageRoot = null;
        }

        /// <summary>World x of a block's centre. Block 0 sits at the origin.</summary>
        private float BlockX(int blockIndex) => blockWidth * blockIndex;

        private Rect ComputeBounds(LevelData level)
        {
            var lastBlock = Mathf.Max(0, level.BlockCount - 1);
            var xMin = -blockWidth * 0.5f - edgePadding;
            var xMax = BlockX(lastBlock) + blockWidth * 0.5f + edgePadding;

            return Rect.MinMaxRect(xMin, groundY, xMax, groundY + stageHeight);
        }

        private List<IFlowerPresenter> BuildFlowers(LevelData level)
        {
            var presenters = new List<IFlowerPresenter>();
            var widestTrigger = 0f;

            var container = new GameObject("Flowers").transform;
            container.SetParent(stageRoot, worldPositionStays: false);

            foreach (var (blockIndex, flowerId, block) in level.Flowers)
            {
                var prefab = PrefabFor(block.Type);
                if (prefab == null) continue;

                var position = new Vector3(BlockX(blockIndex), groundY + flowerYOffset, 0f);
                var instance = Instantiate(prefab, position, Quaternion.identity, container);
                instance.name = $"Flower_{flowerId:D2}_{block.Type}";
                instance.transform.localScale = prefab.transform.localScale * block.Scale;

                var presenter = instance.GetComponent<FlowerPresenter>();
                if (presenter == null)
                {
                    // Skipping this one would shorten the list mid-way and slide every later flower
                    // onto the wrong entity — a quietly wrong stage. Fail the whole build instead, so
                    // IntroGameState's guard trips immediately and the error is the one that matters.
                    Debug.LogError($"[{GetType().Name}] {prefab.name} has no FlowerPresenter; " +
                                   $"abandoning the flower build rather than misaligning ids.");
                    presenters.Clear();
                    return presenters;
                }

                presenter.Id = flowerId;
                presenters.Add(presenter);

                widestTrigger = Mathf.Max(widestTrigger, TriggerRadiusOf(instance));
            }

            // Only the level asset knows the scales and only this component knows the block width, so
            // this is the one place both are in hand. Overlapping triggers do not break the dwell
            // logic any more, but they make it impossible for a small child to tell which flower
            // they are aiming at.
            if (widestTrigger * 2f > blockWidth)
            {
                Debug.LogWarning(
                    $"[{GetType().Name}] Widest flower trigger is {widestTrigger:0.##} units but " +
                    $"blockWidth is {blockWidth}. Neighbouring flowers overlap — widen the blocks or " +
                    $"scale the flowers down.");
            }

            return presenters;
        }

        /// <summary>Largest trigger reach of a placed flower, in world units.</summary>
        private static float TriggerRadiusOf(GameObject instance)
        {
            var widest = 0f;

            foreach (var collider in instance.GetComponentsInChildren<Collider2D>(includeInactive: true))
            {
                if (!collider.isTrigger) continue;

                var extents = collider.bounds.extents;
                widest = Mathf.Max(widest, extents.x);
            }

            return widest;
        }

        /// <summary>
        /// Resolves a block's prefab, falling back to the first configured one when the table has a
        /// hole.
        /// </summary>
        /// <remarks>
        /// The fallback matters more than it looks: the domain indexes the returned presenter list by
        /// flower id, so silently skipping one flower would shift every later flower onto the wrong
        /// entity. Substituting keeps the level playable and the ids aligned while the error names
        /// the misconfiguration.
        /// </remarks>
        private GameObject PrefabFor(FlowerType type)
        {
            GameObject fallback = null;

            if (flowerPrefabs != null)
            {
                foreach (var entry in flowerPrefabs)
                {
                    if (entry.Value == null) continue;
                    if (entry.Key == type) return entry.Value;
                    fallback ??= entry.Value;
                }
            }

            if (fallback != null)
            {
                Debug.LogError($"[{GetType().Name}] No prefab mapped for {type}; substituting " +
                               $"{fallback.name} to keep flower ids aligned.");
                return fallback;
            }

            Debug.LogError($"[{GetType().Name}] No flower prefabs configured at all; the stage will " +
                           $"have nothing to harvest.");
            return null;
        }

        private void BuildGround(Rect bounds)
        {
            if (groundLayers == null) return;

            foreach (var layer in groundLayers)
            {
                if (layer.tilePrefab == null) continue;

                var container = new GameObject(
                    string.IsNullOrEmpty(layer.name) ? layer.tilePrefab.name : layer.name).transform;
                container.SetParent(stageRoot, worldPositionStays: false);
                container.position = new Vector3(bounds.center.x, groundY + layer.yOffset, 0f);
                container.gameObject.AddComponent<ParallaxLayer>()
                    .Configure(layer.horizontalFactor, layer.verticalFactor);

                TileAcross(container, layer.tilePrefab, bounds.width);
            }
        }

        /// <summary>
        /// Fills <paramref name="container"/> with repeats of <paramref name="tilePrefab"/>, centred
        /// on the container.
        /// </summary>
        /// <remarks>
        /// A layer scrolling at factor <c>f</c> drifts <c>(1 - f)</c> times the camera's travel
        /// relative to the camera, so it needs <c>viewportWidth + (1 - f) * travel</c> of coverage —
        /// which is at most the full stage width, hit when <c>f</c> is 0. Centring a full
        /// stage-width band therefore covers every factor, and does so no matter where the camera
        /// happened to be when <see cref="ParallaxLayer"/> sampled its origin. The two spare tiles
        /// absorb the rounding.
        /// </remarks>
        private void TileAcross(Transform container, GameObject tilePrefab, float stageWidth)
        {
            // Measured from a real instance: Renderer.bounds is unreliable on an uninstantiated
            // prefab asset.
            var first = Instantiate(tilePrefab, container);
            first.transform.localPosition = Vector3.zero;
            first.name = $"{tilePrefab.name}_00";

            var tileWidth = MeasureWidth(first);
            if (tileWidth <= MinTileWidth)
            {
                Debug.LogError($"[{GetType().Name}] {tilePrefab.name} has no measurable sprite width; " +
                               $"placing a single tile instead of tiling.");
                return;
            }

            var count = Mathf.CeilToInt(stageWidth / tileWidth) + 2;
            var startX = -(count * tileWidth) * 0.5f + tileWidth * 0.5f;

            first.transform.localPosition = new Vector3(startX, 0f, 0f);

            for (var index = 1; index < count; index++)
            {
                var tile = Instantiate(tilePrefab, container);
                tile.transform.localPosition = new Vector3(startX + index * tileWidth, 0f, 0f);
                tile.name = $"{tilePrefab.name}_{index:D2}";
            }
        }

        private static float MeasureWidth(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

            var measured = false;
            var bounds = new Bounds();

            foreach (var spriteRenderer in renderers)
            {
                if (spriteRenderer.sprite == null) continue;

                if (!measured)
                {
                    bounds = spriteRenderer.bounds;
                    measured = true;
                }
                else
                {
                    bounds.Encapsulate(spriteRenderer.bounds);
                }
            }

            return measured ? bounds.size.x : 0f;
        }

        private void BuildDecoration(Rect bounds, LevelData level)
        {
            if (decorationPrefabs == null || decorationPrefabs.Length == 0) return;

            var container = new GameObject("Decoration").transform;
            container.SetParent(stageRoot, worldPositionStays: false);
            container.position = new Vector3(bounds.center.x, groundY + decorationYOffset, 0f);
            container.gameObject.AddComponent<ParallaxLayer>().Configure(decorationParallax, 0f);

            // Derived from the level's own numbers rather than its index, so each level scatters
            // differently but always identically to itself.
            var random = new Random(decorationSeed ^ (level.BlockCount * 397) ^ level.RequiredNectar);
            var count = Mathf.Max(0, Mathf.RoundToInt(bounds.width * decorationPerUnit));

            for (var index = 0; index < count; index++)
            {
                var prefab = decorationPrefabs[random.Next(decorationPrefabs.Length)];
                if (prefab == null) continue;

                var x = (float)(random.NextDouble() - 0.5) * bounds.width;
                var y = Mathf.Lerp(decorationYJitter.x, decorationYJitter.y, (float)random.NextDouble());

                var item = Instantiate(prefab, container);
                item.transform.localPosition = new Vector3(x, y, 0f);
                item.name = $"{prefab.name}_{index:D2}";
            }
        }

        /// <summary>
        /// Scatters the background swarm across the stage.
        /// </summary>
        /// <remarks>
        /// Goes straight under <see cref="stageRoot"/> in world space, never under a
        /// <see cref="ParallaxLayer"/> container: parallax writes its transform every LateUpdate, and
        /// a Rigidbody2D parented to that would have its physics transform fought every frame. The
        /// swarm reads as background through sorting order on the prefab instead — and since it moves
        /// under its own power, the missing parallax does not show.
        /// </remarks>
        private void BuildDecorBees(Rect bounds, LevelData level)
        {
            if (decorBeePrefab == null || decorBeeCount <= 0) return;

            SeparateDecorBeePhysics();

            var container = new GameObject("DecorBees").transform;
            container.SetParent(stageRoot, worldPositionStays: false);

            var random = new Random(decorationSeed ^ (level.BlockCount * 7919) ^ level.RequiredNectar);

            for (var index = 0; index < decorBeeCount; index++)
            {
                var anchor = new Vector2(
                    Mathf.Lerp(bounds.xMin, bounds.xMax, (float)random.NextDouble()),
                    groundY + Mathf.Lerp(decorBeeHeightRange.x, decorBeeHeightRange.y,
                        (float)random.NextDouble()));

                var bee = Instantiate(decorBeePrefab, container);
                bee.name = $"DecorBee_{index:D2}";

                var controller = bee.GetComponent<DecorBeeController>();
                if (controller == null)
                {
                    Debug.LogError($"[{GetType().Name}] {decorBeePrefab.name} has no " +
                                   $"DecorBeeController; the swarm will not move.");
                    return;
                }

                // The first few tag along with the player; the rest stay where they were scattered.
                // Followers still get an anchor, so they have somewhere sensible to drift around
                // before the player bee is deployed and after it is destroyed.
                var follow = index < followerBeeCount ? playerBeeTransform : null;

                // Each bee gets its own seed off the shared stream, so adding one bee does not
                // reshuffle the flight paths of all the others.
                controller.Initialize(anchor, random.Next(), follow);
            }
        }

        /// <summary>
        /// Keeps the swarm to itself. Decor bees collide with each other — that is the whole point —
        /// but pass through everything on the Default layer: the player, the ground, the flower
        /// triggers.
        /// </summary>
        /// <remarks>
        /// Set here rather than in the Physics2D matrix asset so the decision reads as one checkbox
        /// on this component instead of a hidden cell in Project Settings. It is global state, so it
        /// is written on every build rather than assumed.
        /// </remarks>
        private void SeparateDecorBeePhysics()
        {
            var decorLayer = LayerMask.NameToLayer(decorBeeLayerName);
            if (decorLayer < 0)
            {
                Debug.LogError($"[{GetType().Name}] No '{decorBeeLayerName}' layer — run " +
                               $"YukiQuest → Setup → Add Decorative Bees. The swarm will collide " +
                               $"with the player until you do.");
                return;
            }

            Physics2D.IgnoreLayerCollision(decorLayer, DefaultLayer, !decorBeesCollideWithPlayer);
        }

        private void BuildGroundCollider(Rect bounds)
        {
            var colliderObject = new GameObject("GroundCollider");
            colliderObject.transform.SetParent(stageRoot, worldPositionStays: false);

            // The tag is what BeeMoveController watches for to fire the bounce impulse and the
            // "ouch" face; an untagged floor is silently unbouncy.
            if (!string.IsNullOrEmpty(boundsTag)) colliderObject.tag = boundsTag;
            else Debug.LogError($"[{GetType().Name}] No bounds tag set; the ground will not bounce.");

            // Not parented under a parallax layer: this is the surface the bee actually stands on,
            // and it must not drift away from where the player sees the ground.
            var edge = colliderObject.AddComponent<EdgeCollider2D>();
            edge.points = new[]
            {
                new Vector2(bounds.xMin, groundY),
                new Vector2(bounds.xMax, groundY),
            };
        }

        private void PlaceHive(Rect bounds)
        {
            if (beeHive == null)
            {
                Debug.LogWarning($"[{GetType().Name}] No hive assigned; it will stay wherever the " +
                                 $"scene left it.");
                return;
            }

            // Anchor on what is actually drawn, never on the root. The hive art hangs off a branch
            // whose pivot sits ~9.5 units to the left of the sprite, and the delivery trigger is
            // another ~2.5 units left of the sprite again. Placing the root at the edge therefore put
            // the trigger outside the stage entirely — and since BoundaryHandler clamps the bee to
            // the stage bounds, the hive became impossible to reach.
            var visualRight = VisualRightEdge(beeHive);
            if (float.IsNegativeInfinity(visualRight))
            {
                Debug.LogWarning($"[{GetType().Name}] Hive has no sprite to measure; falling back to " +
                                 $"its root, which may not line up with the art.");
                visualRight = beeHive.position.x;
            }

            // Only x moves. Height and depth stay as authored in the scene, and the bee's spawn point
            // is a child of the hive so it follows for free.
            beeHive.position += new Vector3(bounds.xMax - hiveEdgeInset - visualRight, 0f, 0f);

            // Collider bounds are read off the physics transform, which does not follow a moved
            // Transform until the next simulation step.
            Physics2D.SyncTransforms();
            WarnIfHiveUnreachable(bounds);
        }

        /// <summary>Rightmost drawn edge of <paramref name="root"/>, or -infinity if it draws nothing.</summary>
        private static float VisualRightEdge(Transform root)
        {
            var rightEdge = float.NegativeInfinity;

            foreach (var spriteRenderer in root.GetComponentsInChildren<SpriteRenderer>(includeInactive: true))
            {
                if (spriteRenderer.sprite == null) continue;
                rightEdge = Mathf.Max(rightEdge, spriteRenderer.bounds.max.x);
            }

            return rightEdge;
        }

        /// <summary>
        /// The hive is only useful if the bee can physically overlap its trigger, and the bee cannot
        /// leave the stage bounds. Art with an odd pivot breaks that silently, so say so loudly.
        /// </summary>
        private void WarnIfHiveUnreachable(Rect bounds)
        {
            Collider2D trigger = null;
            foreach (var collider in beeHive.GetComponentsInChildren<Collider2D>(includeInactive: true))
            {
                if (!collider.isTrigger) continue;
                trigger = collider;
                break;
            }

            if (trigger == null)
            {
                Debug.LogError($"[{GetType().Name}] The hive has no trigger collider; nectar can never " +
                               $"be delivered.");
                return;
            }

            var center = trigger.bounds.center.x;
            if (center >= bounds.xMin && center <= bounds.xMax) return;

            Debug.LogError(
                $"[{GetType().Name}] The hive's delivery trigger sits at x={center:0.##}, outside the " +
                $"stage ({bounds.xMin:0.##}..{bounds.xMax:0.##}). The bee is clamped to the stage and " +
                $"cannot deliver — raise hiveEdgeInset or fix the hive art's pivot.");
        }
    }
}
