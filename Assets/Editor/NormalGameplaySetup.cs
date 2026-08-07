using System.IO;
using System.Linq;
using Soar.Variables;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YukiQuest.Gameplay;
using YukiQuest.SOAR;

namespace YukiQuest.EditorTools
{
    /// <summary>
    /// One-shot wiring for the Chapter 1 "normal gameplay" refactor: creates the SOAR assets the
    /// new components need and hooks them up across the Bee prefab, the Gameplay scene and the
    /// Core scene. Safe to run more than once — every step checks before it creates.
    ///
    /// This is setup, not gameplay. Once the project is wired it can be deleted.
    /// </summary>
    public static class NormalGameplaySetup
    {
        private const string SoDirectory = "Assets/3_Contents/Characters/ScriptableObjects";
        private const string BeePrefabPath = "Assets/3_Contents/Characters/Bee/Bee.prefab";
        private const string GameplayScenePath = "Assets/4_Scenes/Gameplay.unity";
        private const string CoreScenePath = "Assets/4_Scenes/Core.unity";

        // Starting extent. Widen it on the StageBoundsPublisher once the stage art is laid out.
        private static readonly Rect DefaultStageBounds = Rect.MinMaxRect(-10f, -6f, 10f, 8f);

        [MenuItem("YukiQuest/Setup/Wire Normal Gameplay")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[NormalGameplaySetup] Cancelled — nothing was changed.");
                return;
            }

            var playerBeeTransform = CreateOrLoad<TransformVariable>("PlayerBeeTransform");
            var hivePoint = CreateOrLoad<TransformVariable>("HivePoint");
            var stageBounds = CreateOrLoad<StageBoundsVariable>("StageBounds");
            AssetDatabase.SaveAssets();

            WireBeePrefab(playerBeeTransform, hivePoint, stageBounds);
            WireGameplayScene(hivePoint, stageBounds);
            WireCoreScene(playerBeeTransform, stageBounds);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[NormalGameplaySetup] Done. Press Play from Core.unity.");
        }

        private static T CreateOrLoad<T>(string assetName) where T : ScriptableObject
        {
            var path = $"{SoDirectory}/{assetName}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                Debug.Log($"[NormalGameplaySetup] Reusing existing {path}");
                return existing;
            }

            Directory.CreateDirectory(SoDirectory);
            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            Debug.Log($"[NormalGameplaySetup] Created {path}");
            return created;
        }

        private static void WireBeePrefab(
            Object playerBeeTransform, Object hivePoint, Object stageBounds)
        {
            var root = PrefabUtility.LoadPrefabContents(BeePrefabPath);
            if (root == null)
            {
                Debug.LogError($"[NormalGameplaySetup] Bee prefab not found at {BeePrefabPath}");
                return;
            }

            try
            {
                var body = root.GetComponent<Rigidbody2D>();
                if (body != null)
                {
                    body.gravityScale = 0f;
                    body.constraints = RigidbodyConstraints2D.FreezeRotation;
                }

                var move = root.GetComponent<BeeMoveController>();
                if (move != null)
                {
                    // The only Animator on the bee drives the wings; prefer the one named for them
                    // in case more are added later.
                    var animators = root.GetComponentsInChildren<Animator>(includeInactive: true);
                    var wing = animators.FirstOrDefault(a => a.name.Contains("Wing"))
                               ?? animators.FirstOrDefault();

                    Assign(move, ("playerBeeTransform", playerBeeTransform), ("wingAnimator", wing));
                }

                var boundary = root.GetComponent<BoundaryHandler>();
                if (boundary != null)
                    Assign(boundary, ("stageBounds", stageBounds), ("hivePoint", hivePoint));

                PrefabUtility.SaveAsPrefabAsset(root, BeePrefabPath);
                Debug.Log("[NormalGameplaySetup] Bee prefab wired (gravity 0, rotation frozen).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void WireGameplayScene(Object hivePoint, Object stageBounds)
        {
            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);

            RetireCeilingCollider(scene);

            var factory = Object.FindAnyObjectByType<BeePresenterFactory>(FindObjectsInactive.Include);
            if (factory == null)
                Debug.LogError("[NormalGameplaySetup] No BeePresenterFactory in Gameplay.");
            else
            {
                Assign(factory, ("hivePoint", hivePoint));
                PrefabUtility.RecordPrefabInstancePropertyModifications(factory);
            }

            var publisher = Object.FindAnyObjectByType<StageBoundsPublisher>(FindObjectsInactive.Include);
            if (publisher == null)
            {
                publisher = new GameObject("StageBounds").AddComponent<StageBoundsPublisher>();
                Debug.Log("[NormalGameplaySetup] Added a StageBounds object to Gameplay.");
            }

            Assign(publisher,
                ("stageBounds", stageBounds),
                ("left", DefaultStageBounds.xMin),
                ("right", DefaultStageBounds.xMax),
                ("bottom", DefaultStageBounds.yMin),
                ("top", DefaultStageBounds.yMax));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[NormalGameplaySetup] Gameplay scene wired.");
        }

        private static void RetireCeilingCollider(UnityEngine.SceneManagement.Scene scene)
        {
            var ceiling = scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Transform>(includeInactive: true))
                .FirstOrDefault(t => t.name == "CeilingCollider");

            if (ceiling == null) return;

            // Destroying a child of a prefab instance is not allowed, so fall back to disabling it —
            // functionally the same here, since a disabled collider stops the bee bouncing.
            if (PrefabUtility.IsPartOfPrefabInstance(ceiling.gameObject))
            {
                ceiling.gameObject.SetActive(false);
                Debug.Log("[NormalGameplaySetup] CeilingCollider is inside a prefab — disabled instead of deleted.");
                return;
            }

            Object.DestroyImmediate(ceiling.gameObject);
            Debug.Log("[NormalGameplaySetup] Deleted CeilingCollider.");
        }

        private static void WireCoreScene(Object playerBeeTransform, Object stageBounds)
        {
            var scene = EditorSceneManager.OpenScene(CoreScenePath, OpenSceneMode.Single);

            var mainCamera = scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Camera>(includeInactive: true))
                .FirstOrDefault(c => c.CompareTag("MainCamera"));

            if (mainCamera == null)
            {
                Debug.LogError("[NormalGameplaySetup] No MainCamera-tagged camera in Core.");
                return;
            }

            var follow = mainCamera.GetComponent<CameraFollow>();
            if (follow == null) follow = mainCamera.gameObject.AddComponent<CameraFollow>();

            Assign(follow, ("target", playerBeeTransform), ("stageBounds", stageBounds));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[NormalGameplaySetup] Core camera wired. Core.unity is left open.");
        }

        /// <summary>
        /// Writes private [SerializeField] values, which are unreachable from outside the class.
        /// </summary>
        private static void Assign(Object component, params (string field, object value)[] entries)
        {
            var serialized = new SerializedObject(component);

            foreach (var (field, value) in entries)
            {
                var property = serialized.FindProperty(field);
                if (property == null)
                {
                    Debug.LogError($"[NormalGameplaySetup] {component.GetType().Name} has no field '{field}'.");
                    continue;
                }

                switch (value)
                {
                    case float f: property.floatValue = f; break;
                    case Object o: property.objectReferenceValue = o; break;
                    case null: property.objectReferenceValue = null; break;
                    default:
                        Debug.LogError($"[NormalGameplaySetup] Unsupported value type for '{field}'.");
                        break;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
        }
    }
}
