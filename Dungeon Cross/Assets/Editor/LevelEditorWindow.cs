using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LevelEditorWindow : EditorWindow
{
    private const int Columns = 9;
    private const int Rows = 12;
    private const float CellSize = 30f;
    private const string LevelsFolder = "Assets/Resources/Levels";

    private LevelConfig[] levelConfigs = new LevelConfig[0];
    private LevelConfig selectedConfig;
    private SerializedObject serializedConfig;
    private SerializedProperty hazardsProperty;
    private SerializedProperty wallCellsProperty;
    private Vector2 levelListScroll;
    private Vector2 inspectorScroll;

    [MenuItem("DungeonCross/Level Editor")]
    public static void Open()
    {
        LevelEditorWindow window = GetWindow<LevelEditorWindow>("Level Editor");
        window.minSize = new Vector2(1120f, 760f);
        window.RefreshLevels();
    }

    private void OnEnable()
    {
        RefreshLevels();
    }

    private void OnProjectChange()
    {
        RefreshLevels();
        Repaint();
    }

    private void OnGUI()
    {
        try
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawLeftPanel();
                DrawRightPanel();
            }
        }
        catch (System.Exception exception)
        {
            EditorGUILayout.HelpBox("Level Editor error: " + exception.Message, MessageType.Error);
            Debug.LogException(exception);
        }
    }

    private void DrawLeftPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(280f)))
        {
            EditorGUILayout.LabelField("Levels", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(LevelsFolder, EditorStyles.miniLabel);
            EditorGUILayout.Space(6f);

            if (levelConfigs.Length == 0)
            {
                EditorGUILayout.HelpBox("No level assets found. Import built-in levels or create a new one.", MessageType.Info);
            }

            using (EditorGUILayout.ScrollViewScope scrollView = new EditorGUILayout.ScrollViewScope(levelListScroll, GUILayout.ExpandHeight(true)))
            {
                levelListScroll = scrollView.scrollPosition;

                for (int i = 0; i < levelConfigs.Length; i++)
                {
                    LevelConfig config = levelConfigs[i];
                    if (config == null)
                    {
                        continue;
                    }

                    string assetPath = AssetDatabase.GetAssetPath(config);
                    string label = config.name;
                    if (!IsRuntimeLevelPath(assetPath))
                    {
                        label += " (outside runtime folder)";
                    }

                    GUIStyle style = new GUIStyle(EditorStyles.miniButton);
                    if (config == selectedConfig)
                    {
                        style.fontStyle = FontStyle.Bold;
                    }

                    if (GUILayout.Button(label, style, GUILayout.Height(28f)))
                    {
                        SelectConfig(config);
                    }
                }
            }

            if (GUILayout.Button("Import Built-In Levels", GUILayout.Height(32f)))
            {
                ImportBuiltInLevels(false);
            }

            if (GUILayout.Button("Reimport Built-In Levels (Overwrite)", GUILayout.Height(28f)))
            {
                ImportBuiltInLevels(true);
            }

            if (GUILayout.Button("New Level", GUILayout.Height(32f)))
            {
                CreateLevel();
            }

            if (selectedConfig != null)
            {
                if (GUILayout.Button("Ping Asset", GUILayout.Height(28f)))
                {
                    EditorGUIUtility.PingObject(selectedConfig);
                }

                if (GUILayout.Button("Select In Project", GUILayout.Height(28f)))
                {
                    Selection.activeObject = selectedConfig;
                    EditorGUIUtility.PingObject(selectedConfig);
                }
            }
        }
    }

    private void DrawRightPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
        {
            if (selectedConfig == null)
            {
                EditorGUILayout.HelpBox("Select a level, import built-in levels, or create a new asset.", MessageType.Info);
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(selectedConfig);
            if (!IsRuntimeLevelPath(assetPath))
            {
                EditorGUILayout.HelpBox("This LevelConfig is outside Assets/Resources/Levels and will not be loaded by the runtime level pipeline.", MessageType.Warning);
            }

            EnsureSerializedSelection();
            if (serializedConfig == null || hazardsProperty == null || wallCellsProperty == null)
            {
                EditorGUILayout.HelpBox("Selected level could not be loaded.", MessageType.Warning);
                return;
            }

            serializedConfig.Update();

            using (EditorGUILayout.ScrollViewScope scrollView = new EditorGUILayout.ScrollViewScope(inspectorScroll))
            {
                inspectorScroll = scrollView.scrollPosition;

                EditorGUILayout.LabelField(selectedConfig.name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(assetPath, EditorStyles.miniLabel);
                DrawGridPreview();
                EditorGUILayout.Space(12f);
                DrawHazardsInspector();
            }

            if (serializedConfig.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(selectedConfig);
                AssetDatabase.SaveAssets();
            }
        }
    }

    private void DrawGridPreview()
    {
        EditorGUILayout.LabelField("Room Preview", EditorStyles.boldLabel);
        Rect previewRect = GUILayoutUtility.GetRect(Columns * CellSize, Rows * CellSize);
        EditorGUI.DrawRect(previewRect, new Color(0.1f, 0.1f, 0.12f));

        for (int row = Rows - 1; row >= 0; row--)
        {
            for (int column = 0; column < Columns; column++)
            {
                Rect cellRect = new Rect(
                    previewRect.x + (column * CellSize),
                    previewRect.y + ((Rows - 1 - row) * CellSize),
                    CellSize - 1f,
                    CellSize - 1f);

                EditorGUI.DrawRect(cellRect, GetBaseCellColor(row));

                if (column == Columns / 2 && row == 0)
                {
                    GUI.Label(cellRect, "IN", CenterMiniLabel());
                }
                else if (column == Columns / 2 && row == Rows - 1)
                {
                    GUI.Label(cellRect, "OUT", CenterMiniLabel());
                }
            }
        }

        DrawWallCells(previewRect);
        DrawHazardPreview(previewRect);
        HandleWallPaint(previewRect);

        EditorGUILayout.HelpBox("Click cells to toggle interior walls. Entry and exit rows are reserved.", MessageType.None);
        DrawWallWarnings();
    }

    private void DrawHazardPreview(Rect previewRect)
    {
        if (hazardsProperty == null)
        {
            return;
        }

        for (int i = 0; i < hazardsProperty.arraySize; i++)
        {
            SerializedProperty hazard = hazardsProperty.GetArrayElementAtIndex(i);
            int startColumn = hazard.FindPropertyRelative("startColumn").intValue;
            int startRow = hazard.FindPropertyRelative("startRow").intValue;
            float dangerRadius = hazard.FindPropertyRelative("dangerRadius").floatValue;
            bool useOrbitingBlade = hazard.FindPropertyRelative("useOrbitingBlade").boolValue;
            float orbitRadius = hazard.FindPropertyRelative("orbitRadius").floatValue;
            TrapType trapType = (TrapType)hazard.FindPropertyRelative("trapType").enumValueIndex;
            TrapPattern pattern = (TrapPattern)hazard.FindPropertyRelative("pattern").enumValueIndex;

            Rect cellRect = new Rect(
                previewRect.x + (startColumn * CellSize),
                previewRect.y + ((Rows - 1 - startRow) * CellSize),
                CellSize - 1f,
                CellSize - 1f);

            Vector2 center = cellRect.center;
            float radiusPixels = dangerRadius * CellSize;
            EditorGUI.DrawRect(
                new Rect(center.x - radiusPixels, center.y - radiusPixels, radiusPixels * 2f, radiusPixels * 2f),
                new Color(1f, 0.15f, 0.15f, 0.12f));

            if (useOrbitingBlade)
            {
                float orbitPixels = orbitRadius * CellSize;
                EditorGUI.DrawRect(
                    new Rect(center.x - orbitPixels, center.y - orbitPixels, orbitPixels * 2f, orbitPixels * 2f),
                    new Color(1f, 0.85f, 0.2f, 0.08f));
            }

            EditorGUI.DrawRect(cellRect, trapType == TrapType.Arrow ? new Color(1f, 0.55f, 0.2f) : new Color(0.9f, 0.2f, 0.2f));
            GUI.Label(cellRect, GetPatternLabel(pattern), CenterMiniLabel());
        }
    }

    private void DrawWallCells(Rect previewRect)
    {
        if (wallCellsProperty == null)
        {
            return;
        }

        for (int i = 0; i < wallCellsProperty.arraySize; i++)
        {
            SerializedProperty wallCell = wallCellsProperty.GetArrayElementAtIndex(i);
            int column = wallCell.FindPropertyRelative("column").intValue;
            int row = wallCell.FindPropertyRelative("row").intValue;

            Rect cellRect = new Rect(
                previewRect.x + (column * CellSize),
                previewRect.y + ((Rows - 1 - row) * CellSize),
                CellSize - 1f,
                CellSize - 1f);

            EditorGUI.DrawRect(cellRect, new Color(0.26f, 0.26f, 0.3f, 1f));
            EditorGUI.DrawRect(new Rect(cellRect.x + 4f, cellRect.y + 4f, cellRect.width - 8f, cellRect.height - 8f), new Color(0.45f, 0.45f, 0.5f, 0.3f));
            GUI.Label(cellRect, "#", CenterMiniLabel());
        }
    }

    private void HandleWallPaint(Rect previewRect)
    {
        Event currentEvent = Event.current;
        if (currentEvent == null || currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
        {
            return;
        }

        if (!previewRect.Contains(currentEvent.mousePosition))
        {
            return;
        }

        int column = Mathf.FloorToInt((currentEvent.mousePosition.x - previewRect.x) / CellSize);
        int row = Rows - 1 - Mathf.FloorToInt((currentEvent.mousePosition.y - previewRect.y) / CellSize);
        Vector2Int cell = new Vector2Int(column, row);

        if (!IsEditableWallCell(cell))
        {
            currentEvent.Use();
            return;
        }

        ToggleWallCell(cell);
        serializedConfig.ApplyModifiedProperties();
        EditorUtility.SetDirty(selectedConfig);
        currentEvent.Use();
        Repaint();
    }

    private void ToggleWallCell(Vector2Int cell)
    {
        int existingIndex = FindWallCellIndex(cell);
        if (existingIndex >= 0)
        {
            wallCellsProperty.DeleteArrayElementAtIndex(existingIndex);
            return;
        }

        int newIndex = wallCellsProperty.arraySize;
        wallCellsProperty.InsertArrayElementAtIndex(newIndex);
        SerializedProperty wallCell = wallCellsProperty.GetArrayElementAtIndex(newIndex);
        wallCell.FindPropertyRelative("column").intValue = cell.x;
        wallCell.FindPropertyRelative("row").intValue = cell.y;
    }

    private int FindWallCellIndex(Vector2Int cell)
    {
        for (int i = 0; i < wallCellsProperty.arraySize; i++)
        {
            SerializedProperty wallCell = wallCellsProperty.GetArrayElementAtIndex(i);
            if (wallCell.FindPropertyRelative("column").intValue == cell.x &&
                wallCell.FindPropertyRelative("row").intValue == cell.y)
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsEditableWallCell(Vector2Int cell)
    {
        if (cell.x < 0 || cell.x >= Columns || cell.y < 0 || cell.y >= Rows)
        {
            return false;
        }

        if (cell.y == 0 || cell.y == Rows - 1)
        {
            return false;
        }

        Vector2Int entryCell = new Vector2Int(Columns / 2, 0);
        Vector2Int exitCell = new Vector2Int(Columns / 2, Rows - 1);
        return cell != entryCell && cell != exitCell;
    }

    private void DrawWallWarnings()
    {
        List<string> warnings = BuildWallWarnings();
        for (int i = 0; i < warnings.Count; i++)
        {
            EditorGUILayout.HelpBox(warnings[i], MessageType.Warning);
        }
    }

    private List<string> BuildWallWarnings()
    {
        List<string> warnings = new List<string>();
        if (wallCellsProperty == null || hazardsProperty == null)
        {
            return warnings;
        }

        HashSet<Vector2Int> wallCells = new HashSet<Vector2Int>();
        for (int i = 0; i < wallCellsProperty.arraySize; i++)
        {
            SerializedProperty wallCell = wallCellsProperty.GetArrayElementAtIndex(i);
            wallCells.Add(new Vector2Int(
                wallCell.FindPropertyRelative("column").intValue,
                wallCell.FindPropertyRelative("row").intValue));
        }

        for (int i = 0; i < hazardsProperty.arraySize; i++)
        {
            SerializedProperty hazard = hazardsProperty.GetArrayElementAtIndex(i);
            Vector2Int startCell = new Vector2Int(
                hazard.FindPropertyRelative("startColumn").intValue,
                hazard.FindPropertyRelative("startRow").intValue);

            if (wallCells.Contains(startCell))
            {
                warnings.Add($"Hazard {i + 1} starts inside a wall at ({startCell.x}, {startCell.y}).");
            }

            List<Vector2Int> trajectory = BuildHazardPreviewCells(hazard);
            for (int cellIndex = 0; cellIndex < trajectory.Count; cellIndex++)
            {
                if (!wallCells.Contains(trajectory[cellIndex]))
                {
                    continue;
                }

                warnings.Add($"Hazard {i + 1} trajectory intersects a wall at ({trajectory[cellIndex].x}, {trajectory[cellIndex].y}).");
                break;
            }
        }

        return warnings;
    }

    private List<Vector2Int> BuildHazardPreviewCells(SerializedProperty hazard)
    {
        TrapPattern pattern = (TrapPattern)hazard.FindPropertyRelative("pattern").enumValueIndex;
        int startColumn = hazard.FindPropertyRelative("startColumn").intValue;
        int startRow = hazard.FindPropertyRelative("startRow").intValue;
        int direction = hazard.FindPropertyRelative("direction").intValue >= 0 ? 1 : -1;
        List<Vector2Int> result = new List<Vector2Int>();

        switch (pattern)
        {
            case TrapPattern.Vertical:
                {
                    int minRow = Mathf.Max(1, startRow - 2);
                    int maxRow = Mathf.Min(Rows - 2, startRow + 2);
                    for (int row = minRow; row <= maxRow; row++)
                    {
                        result.Add(new Vector2Int(startColumn, row));
                    }
                    break;
                }
            case TrapPattern.Square:
                {
                    int anchorX = Mathf.Clamp(startColumn, 1, Columns - 3);
                    int anchorY = Mathf.Clamp(startRow, 3, Rows - 2);
                    result.Add(new Vector2Int(anchorX, anchorY));
                    result.Add(new Vector2Int(anchorX + 1, anchorY));
                    result.Add(new Vector2Int(anchorX + 2, anchorY));
                    result.Add(new Vector2Int(anchorX + 2, anchorY - 1));
                    result.Add(new Vector2Int(anchorX + 2, anchorY - 2));
                    result.Add(new Vector2Int(anchorX + 1, anchorY - 2));
                    result.Add(new Vector2Int(anchorX, anchorY - 2));
                    result.Add(new Vector2Int(anchorX, anchorY - 1));
                    break;
                }
            default:
                {
                    for (int column = 1; column < Columns - 1; column++)
                    {
                        result.Add(new Vector2Int(column, startRow));
                    }
                    if (direction < 0)
                    {
                        result.Reverse();
                    }
                    break;
                }
        }

        return result;
    }

    private void DrawHazardsInspector()
    {
        EditorGUILayout.LabelField("Hazards", EditorStyles.boldLabel);

        if (hazardsProperty.arraySize == 0)
        {
            EditorGUILayout.HelpBox("This level currently has no hazards. Add one below or import built-in levels.", MessageType.Info);
        }

        for (int i = 0; i < hazardsProperty.arraySize; i++)
        {
            SerializedProperty hazard = hazardsProperty.GetArrayElementAtIndex(i);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"Hazard {i + 1}", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    hazard.FindPropertyRelative("startRow").intValue = EditorGUILayout.IntSlider("Row", hazard.FindPropertyRelative("startRow").intValue, 1, 10);
                    hazard.FindPropertyRelative("startColumn").intValue = EditorGUILayout.IntSlider("Col", hazard.FindPropertyRelative("startColumn").intValue, 0, 8);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    hazard.FindPropertyRelative("trapType").enumValueIndex =
                        EditorGUILayout.Popup("Type", hazard.FindPropertyRelative("trapType").enumValueIndex, new[] { "Boulder", "Arrow" });
                    hazard.FindPropertyRelative("pattern").enumValueIndex =
                        EditorGUILayout.Popup("Pattern", hazard.FindPropertyRelative("pattern").enumValueIndex, new[] { "H", "V", "S" });
                }

                hazard.FindPropertyRelative("moveInterval").floatValue =
                    EditorGUILayout.Slider("Move Interval", hazard.FindPropertyRelative("moveInterval").floatValue, 0.2f, 2f);
                hazard.FindPropertyRelative("dangerRadius").floatValue =
                    EditorGUILayout.Slider("Danger Radius", hazard.FindPropertyRelative("dangerRadius").floatValue, 0.2f, 1.25f);

                int directionIndex = hazard.FindPropertyRelative("direction").intValue >= 0 ? 0 : 1;
                directionIndex = EditorGUILayout.Popup("Direction", directionIndex, new[] { "+1", "-1" });
                hazard.FindPropertyRelative("direction").intValue = directionIndex == 0 ? 1 : -1;

                hazard.FindPropertyRelative("useOrbitingBlade").boolValue =
                    EditorGUILayout.Toggle("Orbiting Blade", hazard.FindPropertyRelative("useOrbitingBlade").boolValue);

                if (hazard.FindPropertyRelative("useOrbitingBlade").boolValue)
                {
                    hazard.FindPropertyRelative("orbitRadius").floatValue =
                        EditorGUILayout.Slider("Orbit Radius", hazard.FindPropertyRelative("orbitRadius").floatValue, 0.25f, 1.5f);
                    hazard.FindPropertyRelative("orbitBladeRadius").floatValue =
                        EditorGUILayout.Slider("Blade Radius", hazard.FindPropertyRelative("orbitBladeRadius").floatValue, 0.1f, 0.75f);
                    hazard.FindPropertyRelative("orbitAngularSpeed").floatValue =
                        EditorGUILayout.Slider("Blade Speed", hazard.FindPropertyRelative("orbitAngularSpeed").floatValue, 30f, 360f);
                }

                if (GUILayout.Button("Remove"))
                {
                    hazardsProperty.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
        }

        if (GUILayout.Button("Add Hazard", GUILayout.Height(28f)))
        {
            AddDefaultHazard();
        }
    }

    private void RefreshLevels()
    {
        EnsureLevelsFolderExists();

        string[] guids = AssetDatabase.FindAssets("t:LevelConfig", new[] { LevelsFolder });
        levelConfigs = new LevelConfig[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            levelConfigs[i] = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
        }

        System.Array.Sort(levelConfigs, CompareLevelConfigs);

        if (selectedConfig == null)
        {
            if (levelConfigs.Length > 0)
            {
                SelectConfig(levelConfigs[0]);
            }
            return;
        }

        string selectedPath = AssetDatabase.GetAssetPath(selectedConfig);
        if (!string.IsNullOrEmpty(selectedPath))
        {
            LevelConfig reloaded = AssetDatabase.LoadAssetAtPath<LevelConfig>(selectedPath);
            if (reloaded != null)
            {
                SelectConfig(reloaded);
                return;
            }
        }

        SelectConfig(levelConfigs.Length > 0 ? levelConfigs[0] : null);
    }

    private void CreateLevel()
    {
        EnsureLevelsFolderExists();
        LevelConfig asset = CreateInstance<LevelConfig>();
        asset.hazards = new List<RoomHazardConfig>();
        asset.wallCells = new List<WallCellData>();

        string assetPath = AssetDatabase.GenerateUniqueAssetPath(LevelsFolder + "/Level_01.asset");
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        RefreshLevels();
        SelectConfig(AssetDatabase.LoadAssetAtPath<LevelConfig>(assetPath));
    }

    private void ImportBuiltInLevels(bool overwriteExisting)
    {
        EnsureLevelsFolderExists();

        for (int levelIndex = 1; levelIndex <= LevelManager.BuiltInLevelCount; levelIndex++)
        {
            string assetPath = $"{LevelsFolder}/Level_{levelIndex:00}.asset";
            LevelConfig existingAsset = AssetDatabase.LoadAssetAtPath<LevelConfig>(assetPath);

            if (existingAsset != null && !overwriteExisting)
            {
                continue;
            }

            LevelConfig target = existingAsset;
            if (target == null)
            {
                target = CreateInstance<LevelConfig>();
                AssetDatabase.CreateAsset(target, assetPath);
            }

            target.hazards = CloneHazards(LevelManager.GetBuiltInLevel(levelIndex));
            target.wallCells = new List<WallCellData>();
            target.traps.Clear();
            EditorUtility.SetDirty(target);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RefreshLevels();

        if (levelConfigs.Length > 0)
        {
            SelectConfig(levelConfigs[0]);
        }
    }

    private static List<RoomHazardConfig> CloneHazards(List<RoomHazardConfig> source)
    {
        List<RoomHazardConfig> result = new List<RoomHazardConfig>();
        if (source == null)
        {
            return result;
        }

        for (int i = 0; i < source.Count; i++)
        {
            RoomHazardConfig hazard = source[i];
            if (hazard == null)
            {
                continue;
            }

            result.Add(new RoomHazardConfig
            {
                trapType = hazard.trapType,
                pattern = hazard.pattern,
                startColumn = hazard.startColumn,
                startRow = hazard.startRow,
                moveInterval = hazard.moveInterval,
                direction = hazard.direction,
                dangerRadius = hazard.dangerRadius,
                useOrbitingBlade = hazard.useOrbitingBlade,
                orbitRadius = hazard.orbitRadius,
                orbitBladeRadius = hazard.orbitBladeRadius,
                orbitAngularSpeed = hazard.orbitAngularSpeed
            });
        }

        return result;
    }

    private void AddDefaultHazard()
    {
        int index = hazardsProperty.arraySize;
        hazardsProperty.InsertArrayElementAtIndex(index);
        SerializedProperty hazard = hazardsProperty.GetArrayElementAtIndex(index);
        hazard.FindPropertyRelative("trapType").enumValueIndex = (int)TrapType.Boulder;
        hazard.FindPropertyRelative("pattern").enumValueIndex = (int)TrapPattern.Horizontal;
        hazard.FindPropertyRelative("startColumn").intValue = 4;
        hazard.FindPropertyRelative("startRow").intValue = 5;
        hazard.FindPropertyRelative("moveInterval").floatValue = 0.8f;
        hazard.FindPropertyRelative("direction").intValue = 1;
        hazard.FindPropertyRelative("dangerRadius").floatValue = 0.45f;
        hazard.FindPropertyRelative("useOrbitingBlade").boolValue = false;
        hazard.FindPropertyRelative("orbitRadius").floatValue = 0.7f;
        hazard.FindPropertyRelative("orbitBladeRadius").floatValue = 0.28f;
        hazard.FindPropertyRelative("orbitAngularSpeed").floatValue = 180f;
    }

    private void EnsureLevelsFolderExists()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        if (!AssetDatabase.IsValidFolder(LevelsFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Levels");
        }
    }

    private void SelectConfig(LevelConfig config)
    {
        selectedConfig = config;
        serializedConfig = selectedConfig != null ? new SerializedObject(selectedConfig) : null;
        hazardsProperty = serializedConfig != null ? serializedConfig.FindProperty("hazards") : null;
        wallCellsProperty = serializedConfig != null ? serializedConfig.FindProperty("wallCells") : null;
    }

    private void EnsureSerializedSelection()
    {
        if (selectedConfig == null)
        {
            serializedConfig = null;
            hazardsProperty = null;
            wallCellsProperty = null;
            return;
        }

        if (serializedConfig == null || serializedConfig.targetObject != selectedConfig)
        {
            serializedConfig = new SerializedObject(selectedConfig);
            hazardsProperty = serializedConfig.FindProperty("hazards");
            wallCellsProperty = serializedConfig.FindProperty("wallCells");
        }
    }

    private static bool IsRuntimeLevelPath(string assetPath)
    {
        return !string.IsNullOrEmpty(assetPath) && assetPath.StartsWith(LevelsFolder + "/");
    }

    private static int CompareLevelConfigs(LevelConfig left, LevelConfig right)
    {
        if (left == right)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        return string.CompareOrdinal(left.name, right.name);
    }

    private static GUIStyle CenterMiniLabel()
    {
        GUIStyle style = new GUIStyle(EditorStyles.whiteMiniLabel);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontStyle = FontStyle.Bold;
        return style;
    }

    private static string GetPatternLabel(TrapPattern pattern)
    {
        switch (pattern)
        {
            case TrapPattern.Vertical:
                return "V";
            case TrapPattern.Square:
                return "S";
            default:
                return "H";
        }
    }

    private static Color GetBaseCellColor(int row)
    {
        if (row == 0)
        {
            return new Color(0.3f, 0.7f, 0.3f);
        }

        if (row == Rows - 1)
        {
            return new Color(0.95f, 0.8f, 0.2f);
        }

        return new Color(0.35f, 0.37f, 0.42f);
    }
}
