using System.IO;
using UnityEditor;
using UnityEngine;
using YukiQuest.Gameplay;

namespace YukiQuest.EditorTools
{
    /// <summary>
    /// Creates the background swarm's physics layer and builds <c>DecorBee.prefab</c> by stripping a
    /// copy of the player bee down to something that only flies and bumps.
    ///
    /// Idempotent: an existing layer or prefab is reused. Delete the prefab to have it rebuilt.
    /// </summary>
    public static class DecorBeeSetup
    {
        public const string DecorBeeLayer = "DecorBee";

        private const string BeePrefabPath = "Assets/3_Contents/Characters/Bee/Bee.prefab";
        private const string DecorBeeDirectory = "Assets/3_Contents/Chapters/BeeHarvest";
        private const string DecorBeePath = DecorBeeDirectory + "/DecorBee.prefab";
        private const string PlayerBeeTransformPath =
            "Assets/3_Contents/Characters/ScriptableObjects/PlayerBeeTransform.asset";

        /// <summary>Behind the player bee (0..3) but still ahead of the back grass band (-10).</summary>
        private const int SortingOffset = -5;

        /// <summary>Reads as "further away" without needing a separate set of art.</summary>
        private const float DecorBeeScale = 0.75f;

        /// <summary>Unity owns layers 0-7; user layers start at 8.</summary>
        private const int FirstUserLayer = 8;

        // Bee_Eye_Closed, the AudioSource and BeeAudioPresenter all stay: the swarm winces and
        // occasionally yelps when it bumps. Bee_Eye_Blink belonged to BeePresenter and
        // Bee_Mouth_Closed to BeeHarvestPresenter, both of which are gone.
        private static readonly string[] StripChildren =
        {
            "Canvas", "Bee_Eye_Blink", "Bee_Mouth_Closed",
        };

        [MenuItem("YukiQuest/Setup/Add Decorative Bees")]
        public static void Run()
        {
            var layer = EnsureLayer(DecorBeeLayer);
            if (layer < 0) return;

            var prefab = CreateOrLoadDecorBee(layer);
            if (prefab == null) return;

            WireGenerator(prefab);

            AssetDatabase.SaveAssets();
            Debug.Log($"[DecorBeeSetup] Done. The swarm ignores the Default layer at runtime — toggle " +
                      $"'Decor Bees Collide With Player' on the StageGenerator to change that.");
        }

        // ---------------------------------------------------------------- layer

        private static int EnsureLayer(string layerName)
        {
            var existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0) return existing;

            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("[DecorBeeSetup] Could not open ProjectSettings/TagManager.asset.");
                return -1;
            }

            var serialized = new SerializedObject(assets[0]);
            var layers = serialized.FindProperty("layers");

            for (var index = FirstUserLayer; index < layers.arraySize; index++)
            {
                var slot = layers.GetArrayElementAtIndex(index);
                if (!string.IsNullOrEmpty(slot.stringValue)) continue;

                slot.stringValue = layerName;
                serialized.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();

                Debug.Log($"[DecorBeeSetup] Added layer '{layerName}' in slot {index}.");
                return index;
            }

            Debug.LogError($"[DecorBeeSetup] No free user layer slot for '{layerName}'.");
            return -1;
        }

        // ---------------------------------------------------------------- prefab

        private static GameObject CreateOrLoadDecorBee(int layer)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(DecorBeePath);
            if (existing != null)
            {
                // An earlier version of this script stripped the audio and the wince face. Rebuild
                // rather than make the migration a "delete this file and run it again" step.
                if (existing.GetComponent<BeeAudioPresenter>() != null)
                {
                    Debug.Log($"[DecorBeeSetup] Reusing existing {DecorBeePath} — delete it to rebuild.");
                    return existing;
                }

                Debug.Log($"[DecorBeeSetup] {DecorBeePath} predates bump reactions; rebuilding it.");
                AssetDatabase.DeleteAsset(DecorBeePath);
            }

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(BeePrefabPath);
            if (source == null)
            {
                Debug.LogError($"[DecorBeeSetup] No bee prefab at {BeePrefabPath}.");
                return null;
            }

            Directory.CreateDirectory(DecorBeeDirectory);

            var copy = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(
                copy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            copy.name = "DecorBee";

            StripGameplay(copy);
            var controller = copy.AddComponent<DecorBeeController>();
            WireController(copy, controller);
            ApplyLook(copy, layer);

            var prefab = PrefabUtility.SaveAsPrefabAsset(copy, DecorBeePath);
            Object.DestroyImmediate(copy);

            Debug.Log($"[DecorBeeSetup] Built {DecorBeePath}.");
            return prefab;
        }

        /// <summary>
        /// Removes everything that ties the bee to the game loop. What is left is a body, a wing and a
        /// collider — nothing that can harvest, deliver, speak or be followed by the camera.
        /// </summary>
        private static void StripGameplay(GameObject copy)
        {
            // BeeAudioPresenter is deliberately kept — it carries the wince and the family voices,
            // and its serialized references to the clip lists, the eyes and the AudioSource all
            // survive the copy from Bee.prefab, so there is nothing to re-wire.
            foreach (var component in new Component[]
                     {
                         copy.GetComponent<BeeHarvestPresenter>(),
                         copy.GetComponent<BeeStoreNectarPresenter>(),
                         copy.GetComponent<BeeMoveController>(),
                         copy.GetComponent<BoundaryHandler>(),
                         copy.GetComponent<BeePresenter>(),
                     })
            {
                if (component != null) Object.DestroyImmediate(component, allowDestroyingAssets: true);
            }

            foreach (var childName in StripChildren)
            {
                var child = FindChild(copy.transform, childName);
                if (child != null) Object.DestroyImmediate(child.gameObject, allowDestroyingAssets: true);
            }
        }

        private static void WireController(GameObject copy, DecorBeeController controller)
        {
            var serialized = new SerializedObject(controller);

            var baseTransform = FindChild(copy.transform, "Base");
            var wing = FindChild(copy.transform, "Bee_Wing");

            serialized.FindProperty("baseTransform").objectReferenceValue = baseTransform;
            serialized.FindProperty("beeBody").objectReferenceValue = copy.GetComponent<Rigidbody2D>();
            serialized.FindProperty("wingAnimator").objectReferenceValue =
                wing == null ? null : wing.GetComponent<Animator>();

            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (baseTransform == null) Debug.LogWarning("[DecorBeeSetup] No 'Base' child to flip.");
            if (wing == null) Debug.LogWarning("[DecorBeeSetup] No 'Bee_Wing' child to animate.");

            ShareTheVoiceCooldown(copy);
        }

        /// <summary>
        /// Puts the swarm's voices behind one shared timer. Every bee still winces on its own bump —
        /// that half is not rate-limited — but only one of them speaks per cooldown, which is the
        /// difference between charm and a wall of noise when a dozen bees are ricocheting.
        /// </summary>
        private static void ShareTheVoiceCooldown(GameObject copy)
        {
            var audio = copy.GetComponent<BeeAudioPresenter>();
            if (audio == null)
            {
                Debug.LogWarning("[DecorBeeSetup] No BeeAudioPresenter — the swarm will be silent.");
                return;
            }

            var serialized = new SerializedObject(audio);
            serialized.FindProperty("useSharedVoiceCooldown").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyLook(GameObject copy, int layer)
        {
            copy.transform.localScale *= DecorBeeScale;

            foreach (var child in copy.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                child.gameObject.layer = layer;
            }

            // Shift rather than assign, so the wing stays behind the body the way the art intends.
            foreach (var renderer in copy.GetComponentsInChildren<SpriteRenderer>(includeInactive: true))
            {
                renderer.sortingOrder += SortingOffset;
            }
        }

        // ---------------------------------------------------------------- wiring

        private static void WireGenerator(GameObject prefab)
        {
            var generator = Object.FindAnyObjectByType<StageGenerator>(FindObjectsInactive.Include);
            if (generator == null)
            {
                Debug.LogWarning("[DecorBeeSetup] No StageGenerator in the open scene — assign " +
                                 "DecorBee.prefab to its 'Decor Bee Prefab' field by hand, or open " +
                                 "Gameplay.unity and run this again.");
                return;
            }

            var serialized = new SerializedObject(generator);
            serialized.FindProperty("decorBeePrefab").objectReferenceValue = prefab;

            // The same variable the Core camera follows — it is how anything outside the chapter
            // scene finds the player bee, and the followers need exactly that handoff.
            var playerBee = AssetDatabase.LoadAssetAtPath<Object>(PlayerBeeTransformPath);
            if (playerBee != null)
                serialized.FindProperty("playerBeeTransform").objectReferenceValue = playerBee;
            else
                Debug.LogWarning($"[DecorBeeSetup] No variable at {PlayerBeeTransformPath}; the " +
                                 $"follower bees will wander instead of tagging along.");

            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(generator);
            Debug.Log("[DecorBeeSetup] Wired the prefab into StageGenerator.");
        }

        private static Transform FindChild(Transform node, string name)
        {
            if (node.name == name) return node;

            foreach (Transform child in node)
            {
                var match = FindChild(child, name);
                if (match != null) return match;
            }

            return null;
        }
    }
}
