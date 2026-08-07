using System.IO;
using System.Linq;
using Domain.Chapters.BeeHarvest;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using YukiQuest.Gameplay;
using YukiQuest.Installer;
using YukiQuest.SOAR;

namespace YukiQuest.EditorTools
{
    /// <summary>
    /// Migrates Chapter 1 from a hand-authored stage to a generated one: lifts the scene's ground art
    /// into tile prefabs, seeds a level collection, strips the objects the generator now produces and
    /// rewires the installer.
    ///
    /// Idempotent — every step checks before it creates, and an existing level collection is never
    /// overwritten. Deletable once the migration has run.
    /// </summary>
    public static class ProceduralStageSetup
    {
        private const string GameplayScenePath = "Assets/4_Scenes/Gameplay.unity";
        private const string ChapterDirectory = "Assets/3_Contents/Chapters/BeeHarvest";
        private const string GroundDirectory = ChapterDirectory + "/Ground";
        private const string LevelAssetPath = ChapterDirectory + "/LevelCollection.asset";
        private const string StageBoundsPath =
            "Assets/3_Contents/Characters/ScriptableObjects/StageBounds.asset";
        private const string FlowerDirectory = "Assets/3_Contents/Characters/Flowers";
        private const string DecorationPrefabPath = FlowerDirectory + "/Grass.prefab";

        /// <summary>World y of the surface, taken from the EdgeCollider2D the scene used to carry.</summary>
        private const float GroundY = -5.02f;

        /// <summary>Ordered back-to-front. Factors carried over from the earlier parallax tuning.</summary>
        private static readonly (string Source, float Horizontal, float Vertical)[] Layers =
        {
            ("BackDirt", 0.7f, 0.1f),
            ("BackGrass", 0.5f, 0.1f),
            // The front bands read as the ground the bee lands on, so they must not drift vertically
            // away from the collider as the camera rises.
            ("FrontDirt", 0.1f, 0f),
            ("FrontGrass", 0.2f, 0f),
        };

        [MenuItem("YukiQuest/Setup/Prepare Procedural Stage")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[ProceduralStageSetup] Cancelled — nothing was changed.");
                return;
            }

            Directory.CreateDirectory(GroundDirectory);
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);

            // Extraction must happen before the strip: the ground art exists only as scene objects.
            var tiles = ExtractGroundTiles(scene);
            var levels = CreateOrLoadLevelCollection();
            var generator = SetUpGenerator(scene, tiles);

            StripAuthoredStage(scene);
            WireInstaller(scene, levels, generator);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            LevelCollectionValidator.Validate(levels);
            Debug.Log("[ProceduralStageSetup] Done. Enter Play mode from Core.unity to see the stage.");
        }

        // ---------------------------------------------------------------- ground tiles

        private static GameObject[] ExtractGroundTiles(Scene scene)
        {
            var tiles = new GameObject[Layers.Length];

            for (var index = 0; index < Layers.Length; index++)
            {
                var (source, _, _) = Layers[index];
                var path = $"{GroundDirectory}/{source}_Tile.prefab";

                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null)
                {
                    Debug.Log($"[ProceduralStageSetup] Reusing existing {path}");
                    tiles[index] = existing;
                    continue;
                }

                var group = Find(scene, source);
                if (group == null)
                {
                    Debug.LogWarning($"[ProceduralStageSetup] No '{source}' object in the scene; " +
                                     $"that layer will have to be assigned by hand.");
                    continue;
                }

                tiles[index] = ExtractTile(group, path);
            }

            return tiles;
        }

        /// <summary>
        /// Saves one representative sprite of a layer as a standalone tile prefab, flattened to the
        /// origin so the generator can repeat it on a predictable pitch.
        /// </summary>
        private static GameObject ExtractTile(Transform group, string path)
        {
            var source = group.GetComponentInChildren<SpriteRenderer>(includeInactive: true);
            if (source == null)
            {
                Debug.LogWarning($"[ProceduralStageSetup] '{group.name}' has no SpriteRenderer.");
                return null;
            }

            var copy = Object.Instantiate(source.gameObject);
            copy.name = Path.GetFileNameWithoutExtension(path);
            copy.transform.SetParent(null);
            copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            // Bake the accumulated parent scale in: the tile is about to lose the group transforms it
            // used to hang under, and its world size has to survive that.
            copy.transform.localScale = source.transform.lossyScale;

            var prefab = PrefabUtility.SaveAsPrefabAsset(copy, path);
            Object.DestroyImmediate(copy);

            Debug.Log($"[ProceduralStageSetup] Extracted {path}");
            return prefab;
        }

        // ---------------------------------------------------------------- level collection

        private static LevelCollection CreateOrLoadLevelCollection()
        {
            var existing = AssetDatabase.LoadAssetAtPath<LevelCollection>(LevelAssetPath);
            if (existing != null)
            {
                Debug.Log($"[ProceduralStageSetup] Reusing existing {LevelAssetPath} — authored levels " +
                          $"are never overwritten.");
                return existing;
            }

            var collection = ScriptableObject.CreateInstance<LevelCollection>();
            AssetDatabase.CreateAsset(collection, LevelAssetPath);
            SeedFirstLevel(collection);

            Debug.Log($"[ProceduralStageSetup] Created {LevelAssetPath} with a starter level.");
            return collection;
        }

        /// <summary>
        /// Writes a starter level carrying Chapter 1's existing tuning — the same five flowers, the
        /// same nectar values and the same quota of 12 — but spread across a stage several screens
        /// wide, which is the whole point of the change.
        /// </summary>
        private static void SeedFirstLevel(LevelCollection collection)
        {
            const int blockCount = 18;
            var flowers = new (int Block, FlowerType Type, int Nectar)[]
            {
                (2, FlowerType.Flower3, 100),
                (6, FlowerType.Flower2, 60),
                (9, FlowerType.Flower1, 30),
                (13, FlowerType.Flower4, 40),
                (16, FlowerType.Flower5, 20),
            };

            var serialized = new SerializedObject(collection);
            var list = serialized.FindProperty("list");
            list.arraySize = 1;

            var level = list.GetArrayElementAtIndex(0);
            level.FindPropertyRelative("requiredNectar").intValue = 12;

            var blocks = level.FindPropertyRelative("blocks");
            blocks.arraySize = blockCount;

            for (var index = 0; index < blockCount; index++)
            {
                var block = blocks.GetArrayElementAtIndex(index);
                var planted = flowers.Any(flower => flower.Block == index);
                var match = planted ? flowers.First(flower => flower.Block == index) : default;

                block.FindPropertyRelative("type").intValue =
                    (int)(planted ? match.Type : FlowerType.None);
                block.FindPropertyRelative("nectar").intValue = planted ? match.Nectar : 0;
                block.FindPropertyRelative("scale").floatValue = 1f;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(collection);
        }

        // ---------------------------------------------------------------- generator

        private static StageGenerator SetUpGenerator(Scene scene, GameObject[] tiles)
        {
            var generator = Object.FindAnyObjectByType<StageGenerator>(FindObjectsInactive.Include);
            if (generator == null)
            {
                var host = new GameObject("StageGenerator");
                var context = Find(scene, "GameplayContext");
                if (context != null) host.transform.SetParent(context, worldPositionStays: false);

                generator = host.AddComponent<StageGenerator>();
                Debug.Log("[ProceduralStageSetup] Added a StageGenerator to the scene.");
            }

            var serialized = new SerializedObject(generator);

            serialized.FindProperty("stageBounds").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<StageBoundsVariable>(StageBoundsPath);

            var hive = Find(scene, "BeeHive");
            serialized.FindProperty("beeHive").objectReferenceValue = hive;
            if (hive == null) Debug.LogWarning("[ProceduralStageSetup] No BeeHive found to place.");

            serialized.FindProperty("groundY").floatValue = GroundY;

            WriteFlowerPrefabTable(serialized);
            WriteGroundLayers(scene, serialized, tiles);
            WriteDecoration(serialized);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(generator);
            return generator;
        }

        private static void WriteFlowerPrefabTable(SerializedObject serialized)
        {
            var types = new[]
            {
                FlowerType.Flower1, FlowerType.Flower2, FlowerType.Flower3,
                FlowerType.Flower4, FlowerType.Flower5,
            };

            var table = serialized.FindProperty("flowerPrefabs");
            table.arraySize = types.Length;

            for (var index = 0; index < types.Length; index++)
            {
                var type = types[index];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{FlowerDirectory}/Flower_{(int)type}.prefab");

                if (prefab == null)
                    Debug.LogWarning($"[ProceduralStageSetup] No prefab found for {type}.");

                var entry = table.GetArrayElementAtIndex(index);

                // SerializedKeyValuePair exposes [field:SerializeField] auto-properties, so the
                // serialized names are the compiler's backing fields rather than "Key"/"Value".
                entry.FindPropertyRelative("<Key>k__BackingField").intValue = (int)type;
                entry.FindPropertyRelative("<Value>k__BackingField").objectReferenceValue = prefab;
            }
        }

        private static void WriteGroundLayers(Scene scene, SerializedObject serialized, GameObject[] tiles)
        {
            var layers = serialized.FindProperty("groundLayers");
            layers.arraySize = Layers.Length;

            for (var index = 0; index < Layers.Length; index++)
            {
                var (source, horizontal, vertical) = Layers[index];
                var layer = layers.GetArrayElementAtIndex(index);

                layer.FindPropertyRelative("name").stringValue = source;
                layer.FindPropertyRelative("tilePrefab").objectReferenceValue = tiles[index];
                layer.FindPropertyRelative("horizontalFactor").floatValue = horizontal;
                layer.FindPropertyRelative("verticalFactor").floatValue = vertical;
                layer.FindPropertyRelative("yOffset").floatValue = LayerYOffset(scene, source);
            }
        }

        /// <summary>
        /// Height of a layer above the ground line, measured off the scene art rather than guessed —
        /// the tile prefab was flattened to its own origin, so the height it used to sit at has to be
        /// carried over as the layer's offset.
        /// </summary>
        private static float LayerYOffset(Scene scene, string source)
        {
            var group = Find(scene, source);
            if (group == null) return 0f;

            var sprite = group.GetComponentInChildren<SpriteRenderer>(includeInactive: true);
            return sprite == null ? 0f : sprite.transform.position.y - GroundY;
        }

        private static void WriteDecoration(SerializedObject serialized)
        {
            var decoration = AssetDatabase.LoadAssetAtPath<GameObject>(DecorationPrefabPath);
            var prefabs = serialized.FindProperty("decorationPrefabs");

            prefabs.arraySize = decoration == null ? 0 : 1;
            if (decoration != null) prefabs.GetArrayElementAtIndex(0).objectReferenceValue = decoration;
            else Debug.LogWarning($"[ProceduralStageSetup] No decoration prefab at {DecorationPrefabPath}.");
        }

        // ---------------------------------------------------------------- strip & rewire

        private static void StripAuthoredStage(Scene scene)
        {
            // Everything here is now produced by the generator. BeeHive is deliberately absent from
            // this list: it is moved, not rebuilt, because the installer references its
            // BeePresenterFactory by scene id.
            foreach (var name in new[] { "FlowerBeds", "Ground", "StageBounds" })
            {
                var target = Find(scene, name);
                if (target == null) continue;

                Object.DestroyImmediate(target.gameObject);
                Debug.Log($"[ProceduralStageSetup] Removed '{name}' — the generator builds it now.");
            }
        }

        private static void WireInstaller(Scene scene, LevelCollection levels, StageGenerator generator)
        {
            var installer = Object.FindAnyObjectByType<GameplayInstaller>(FindObjectsInactive.Include);
            if (installer == null)
            {
                Debug.LogError("[ProceduralStageSetup] No GameplayInstaller in the scene.");
                return;
            }

            var serialized = new SerializedObject(installer);
            serialized.FindProperty("levelCollection").objectReferenceValue = levels;
            serialized.FindProperty("stageGenerator").objectReferenceValue = generator;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(installer);
            Debug.Log("[ProceduralStageSetup] Installer rewired.");
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>Depth-first search by name across the scene, including inactive objects.</summary>
        private static Transform Find(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var match = FindIn(root.transform, name);
                if (match != null) return match;
            }

            return null;
        }

        private static Transform FindIn(Transform node, string name)
        {
            if (node.name == name) return node;

            foreach (Transform child in node)
            {
                var match = FindIn(child, name);
                if (match != null) return match;
            }

            return null;
        }
    }
}
