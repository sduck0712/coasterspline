using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

namespace GravitySceneView
{
	public class GravitySceneViewWindow : EditorWindow
	{
		private const string PrefTab = "GravitySceneView_selectedTab";
		private const string PrefFixedDt = "GravitySceneView_fixedDt";
		private const string PrefDropKey = "GravitySceneView_dropKey";
		private const string PrefDropMod = "GravitySceneView_dropMod";
		private const string PrefSpawnKey = "GravitySceneView_spawnKey";
		private const string PrefSpawnMod = "GravitySceneView_spawnMod";
		private const string PrefSpawnerEnabled = "GravitySceneView_spawnerEnabled";
		private const string PrefKeepPhysics = "GravitySceneView_keepPhysics";
		private const string PrefGravityX = "GravitySceneView_gravityX";
		private const string PrefGravityY = "GravitySceneView_gravityY";
		private const string PrefGravityZ = "GravitySceneView_gravityZ";
		private const string PrefLayerMask = "GravitySceneView_layerMask";
		private const string PrefPrefabGuid = "GravitySceneView_prefabGuid";
		private const string PrefSpawnPosition = "GravitySceneView_spawnPos";

		private int selectedTab;
		private float fixedDt;
		private KeyCode dropKey;
		private EventModifiers dropMod;
		private KeyCode spawnKey;
		private EventModifiers spawnMod;
		private bool spawnerEnabled;
		private bool keepPhysicsAlive;
		private bool prevKeepPhysicsAlive;
		private Vector3 gravity;
		private LayerMask layerMask;

		private GameObject prefab;
		private Vector3 spawnPosition;
		private Quaternion spawnRotation = Quaternion.identity;

		private SimulationMode prevSimMode = SimulationMode.FixedUpdate;
		private bool isSimulating;
		private readonly List<Rigidbody> dropRBs = new List<Rigidbody>();
		private readonly Dictionary<Rigidbody, RBState> rbPreExisting = new Dictionary<Rigidbody, RBState>();
		private readonly Dictionary<Collider, bool> colPreExisting = new Dictionary<Collider, bool>();

		[MenuItem("Window/Energise Tools/Gravity Scene View")]
		public static void ShowWindow() => GetWindow<GravitySceneViewWindow>("Gravity Scene View");

		private void OnEnable()
		{
			selectedTab = EditorPrefs.GetInt(PrefTab, 0);
			fixedDt = EditorPrefs.GetFloat(PrefFixedDt, 0.02f);
			dropKey = (KeyCode) EditorPrefs.GetInt(PrefDropKey, (int) KeyCode.D);
			dropMod = (EventModifiers) EditorPrefs.GetInt(PrefDropMod, (int) EventModifiers.None);
			spawnKey = (KeyCode) EditorPrefs.GetInt(PrefSpawnKey, (int) KeyCode.F);
			spawnMod = (EventModifiers) EditorPrefs.GetInt(PrefSpawnMod, (int) EventModifiers.None);
			spawnerEnabled = EditorPrefs.GetBool(PrefSpawnerEnabled, true);
			keepPhysicsAlive = EditorPrefs.GetBool(PrefKeepPhysics, false);
			prevKeepPhysicsAlive = keepPhysicsAlive;
			gravity = new Vector3(
				EditorPrefs.GetFloat(PrefGravityX, Physics.gravity.x),
				EditorPrefs.GetFloat(PrefGravityY, Physics.gravity.y),
				EditorPrefs.GetFloat(PrefGravityZ, Physics.gravity.z)
			);
			layerMask = EditorPrefs.GetInt(PrefLayerMask, ~0);

			var guid = EditorPrefs.GetString(PrefPrefabGuid, string.Empty);
			if (!string.IsNullOrEmpty(guid))
				prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));

			spawnPosition = StringToVector3(EditorPrefs.GetString(PrefSpawnPosition, Vector3.zero.ToString()));
			if (!EditorApplication.isPlayingOrWillChangePlaymode)
			{
				var prevSimMode = Physics.simulationMode;
				Physics.simulationMode = SimulationMode.Script;

				System.Action<PlayModeStateChange> handler = null;
				handler = state =>
				{
					if (state == PlayModeStateChange.ExitingEditMode)
					{
						Physics.autoSimulation = true;
						Physics.simulationMode = prevSimMode;
						EditorApplication.playModeStateChanged -= handler;
					}
				};

				EditorApplication.playModeStateChanged += handler;
			}

			EditorApplication.update += EditorUpdate;
			SceneView.duringSceneGui += OnSceneGUI;
		}

		private void OnDisable()
		{
			Physics.simulationMode = prevSimMode;
			EditorApplication.update -= EditorUpdate;
			SceneView.duringSceneGui -= OnSceneGUI;
		}

		private void OnGUI()
		{
			EditorGUI.BeginChangeCheck();
			selectedTab = GUILayout.Toolbar(selectedTab, new[] {"Scene Gravity", "Spawner"});
			if (EditorGUI.EndChangeCheck())
				EditorPrefs.SetInt(PrefTab, selectedTab);

			GUILayout.Space(10);
			if (selectedTab == 0) DrawGravityTab();
			else DrawSpawnerTab();
			RenderInfo();
		}

		private static void RenderInfo()
		{
			var infoStyle = new GUIStyle(EditorStyles.label)
			{
				alignment = TextAnchor.MiddleCenter,
				fontStyle = FontStyle.Bold,
				wordWrap = true
			};
			EditorGUILayout.Separator();
			EditorGUILayout.Separator();

			EditorGUILayout.LabelField("Gravity Scene View", infoStyle);
			infoStyle.fontStyle = FontStyle.Normal;
			EditorGUILayout.LabelField(
				"Thanks for using our Gravity Scene View.",
				infoStyle);
			EditorGUILayout.Separator();

			EditorGUILayout.LabelField(
				"There are options to change between dropping the selected object or spawning objects from the desired location",
				infoStyle);

			EditorGUILayout.Separator();

			EditorGUILayout.LabelField(
				"Any comments, requests, feedback, please email: EnergiseTools@gmail.com\n\n Thanks, Stuart Heath (Energise Software)",
				infoStyle);
		}

		private void DrawGravityTab()
		{
			EditorGUI.BeginChangeCheck();
			GUILayout.Label("Simulation Settings", EditorStyles.boldLabel);
			fixedDt = EditorGUILayout.FloatField(new GUIContent("Fixed Timestep", "Time step for physics simulation"),
				fixedDt);
			dropKey = (KeyCode) EditorGUILayout.EnumPopup(new GUIContent("Drop Key", "Key to drop selected objects"),
				dropKey);
			dropMod = (EventModifiers) EditorGUILayout.EnumPopup(
				new GUIContent("Drop Modifier", "Modifier key for drop action"), dropMod);
			gravity = EditorGUILayout.Vector3Field(new GUIContent("Gravity", "Gravity vector for drop simulation"),
				gravity);
			layerMask = EditorGUILayout.MaskField(new GUIContent("Layer Mask", "Layers to include in simulation"),
				layerMask, InternalEditorUtility.layers);
			keepPhysicsAlive =
				EditorGUILayout.Toggle(
					new GUIContent("Keep Physics Alive", "Keep simulated objects until manually cleared"),
					keepPhysicsAlive);
			if (EditorGUI.EndChangeCheck())
			{
				EditorPrefs.SetFloat(PrefFixedDt, fixedDt);
				EditorPrefs.SetInt(PrefDropKey, (int) dropKey);
				EditorPrefs.SetInt(PrefDropMod, (int) dropMod);
				EditorPrefs.SetFloat(PrefGravityX, gravity.x);
				EditorPrefs.SetFloat(PrefGravityY, gravity.y);
				EditorPrefs.SetFloat(PrefGravityZ, gravity.z);
				EditorPrefs.SetInt(PrefLayerMask, layerMask);
				EditorPrefs.SetBool(PrefKeepPhysics, keepPhysicsAlive);

				if (prevKeepPhysicsAlive && !keepPhysicsAlive)
					StopAllSimulations();
				prevKeepPhysicsAlive = keepPhysicsAlive;
			}

			GUILayout.Space(10);
			var dropLabel = dropMod == EventModifiers.None ? $"{dropKey}" : $"{dropMod}+{dropKey}";
			if (GUILayout.Button(new GUIContent($"Drop Selected ({dropLabel})",
				    "Drop selected GameObjects under gravity")))
			{
				BeginDropSelection();
			}

			if (GUILayout.Button(new GUIContent("Stop Simulations", "Remove all simulated physics components")))
			{
				StopAllSimulations();
			}
		}

		private void DrawSpawnerTab()
		{
			EditorGUI.BeginChangeCheck();
			GUILayout.Label("Spawner Settings", EditorStyles.boldLabel);
			spawnerEnabled =
				EditorGUILayout.Toggle(new GUIContent("Enable Spawner", "Toggle prefab spawning functionality"),
					spawnerEnabled);
			prefab = (GameObject) EditorGUILayout.ObjectField(new GUIContent("Prefab", "Prefab to spawn and drop"),
				prefab, typeof(GameObject), false);
			spawnKey = (KeyCode) EditorGUILayout.EnumPopup(new GUIContent("Spawn Key", "Key to spawn selected prefab"),
				spawnKey);
			spawnMod = (EventModifiers) EditorGUILayout.EnumPopup(
				new GUIContent("Spawn Modifier", "Modifier key for spawn action"), spawnMod);
			spawnPosition =
				EditorGUILayout.Vector3Field(new GUIContent("Spawn Position", "Initial spawn position in Scene View"),
					spawnPosition);
			spawnRotation =
				Quaternion.Euler(EditorGUILayout.Vector3Field(
					new GUIContent("Spawn Rotation", "Initial spawn rotation"), spawnRotation.eulerAngles));
			if (EditorGUI.EndChangeCheck())
			{
				EditorPrefs.SetBool(PrefSpawnerEnabled, spawnerEnabled);
				if (prefab != null)
				{
					EditorPrefs.SetString(PrefPrefabGuid,
						AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab)));
				}

				EditorPrefs.SetInt(PrefSpawnKey, (int) spawnKey);
				EditorPrefs.SetInt(PrefSpawnMod, (int) spawnMod);
				EditorPrefs.SetString(PrefSpawnPosition, spawnPosition.ToString());
			}

			GUILayout.Space(10);
			EditorGUI.BeginDisabledGroup(!spawnerEnabled);
			var spawnLabel = spawnMod == EventModifiers.None ? $"{spawnKey}" : $"{spawnMod}+{spawnKey}";
			if (GUILayout.Button(new GUIContent($"Spawn & Drop ({spawnLabel})",
				    "Instantiate and drop the prefab under gravity")))
			{
				SpawnAndDropPrefab();
			}

			if (GUILayout.Button(new GUIContent("Stop Simulations", "Remove all simulated physics components")))
			{
				StopAllSimulations();
			}

			EditorGUI.EndDisabledGroup();
		}

		private void OnSceneGUI(SceneView sceneView)
		{
			var e = Event.current;
			if (selectedTab == 0 && e.type == EventType.KeyDown && e.keyCode == dropKey &&
			    (dropMod == EventModifiers.None || (e.modifiers & dropMod) != 0))
			{
				BeginDropSelection();
				e.Use();
			}

			if (selectedTab == 1 && spawnerEnabled && prefab != null && e.type == EventType.KeyDown &&
			    e.keyCode == spawnKey &&
			    (spawnMod == EventModifiers.None || (e.modifiers & spawnMod) != 0))
			{
				SpawnAndDropPrefab();
				e.Use();
			}

			if (selectedTab == 1 && spawnerEnabled && prefab != null)
			{
				EditorGUI.BeginChangeCheck();
				var newPos = Handles.PositionHandle(spawnPosition, spawnRotation);
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(this, "Move Spawn Position");
					spawnPosition = newPos;
					EditorPrefs.SetString(PrefSpawnPosition, spawnPosition.ToString());
				}

				Handles.Label(spawnPosition + Vector3.up * 0.5f, "Spawn Point");
			}
		}

		private void BeginDropSelection()
		{
			isSimulating = true;
			var prevGr = Physics.gravity;
			Physics.gravity = gravity;
			if (Physics.simulationMode != SimulationMode.Script)
			{
				prevSimMode = Physics.simulationMode;
				Physics.simulationMode = SimulationMode.Script;
			}

			foreach (var go in Selection.gameObjects)
			{
				if (((1 << go.layer) & layerMask) == 0) continue;

				if (!go.TryGetComponent<Collider>(out var col))
				{
					col = go.AddComponent<BoxCollider>();
					colPreExisting[col] = false;
				}

				Undo.RecordObject(go.transform, "Gravity Drop");
				var rb = go.GetComponent<Rigidbody>();
				var existed = rb != null;
				if (!existed) rb = Undo.AddComponent<Rigidbody>(go);

				rbPreExisting[rb] = new RBState {Exists = existed, Kinematic = rb.isKinematic, Gravity = rb.useGravity};

				rb.isKinematic = false;
				rb.useGravity = true;
				dropRBs.Add(rb);
			}

			Physics.gravity = prevGr;
		}

		private void SpawnAndDropPrefab()
		{
			if (prefab == null) return;
			var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
			if (go == null)
			{
				Debug.LogError("Failed to instantiate prefab.");
				return;
			}

			Undo.RegisterCreatedObjectUndo(go, "Spawn Prefab");
			go.transform.position = spawnPosition;
			go.transform.rotation = spawnRotation;

			if (!go.TryGetComponent<Collider>(out var col))
			{
				col = go.AddComponent<BoxCollider>();
				colPreExisting[col] = false;
			}

			Undo.RecordObject(go.transform, "Gravity Drop");
			var rb = go.GetComponent<Rigidbody>();
			var existed = rb != null;
			if (!existed) rb = Undo.AddComponent<Rigidbody>(go);

			rbPreExisting[rb] = new RBState {Exists = existed, Kinematic = rb.isKinematic, Gravity = rb.useGravity};

			rb.isKinematic = false;
			rb.useGravity = true;
			dropRBs.Add(rb);
			isSimulating = true;
		}

		private void StopAllSimulations()
		{
			foreach (var kv in rbPreExisting)
			{
				var rb = kv.Key;
				var st = kv.Value;
				if (rb == null) continue;

				if (!st.Exists) Undo.DestroyObjectImmediate(rb);
				else
				{
					rb.isKinematic = st.Kinematic;
					rb.useGravity = st.Gravity;
				}
			}

			foreach (var kv in colPreExisting)
			{
				if (kv.Key != null && !kv.Value) Undo.DestroyObjectImmediate(kv.Key);
			}

			dropRBs.Clear();
			rbPreExisting.Clear();
			colPreExisting.Clear();
			isSimulating = false;
			SceneView.RepaintAll();
		}

		private void EditorUpdate()
		{
			if (!isSimulating) return;
			Physics.Simulate(fixedDt);
			if (!keepPhysicsAlive)
			{
				var allSleeping = true;
				foreach (var rb in dropRBs)
				{
					if (rb != null && !rb.IsSleeping())
					{
						allSleeping = false;
						break;
					}
				}

				if (allSleeping) StopAllSimulations();
			}
		}

		private static Vector3 StringToVector3(string s)
		{
			s = s.Trim('(', ')');
			var parts = s.Split(',');
			return new Vector3(
				float.Parse(parts[0]),
				float.Parse(parts[1]),
				float.Parse(parts[2])
			);
		}
	}

	internal class RBState
	{
		public bool Exists { get; set; }
		public bool Kinematic { get; set; }
		public bool Gravity { get; set; }
	}
}