using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LevelEditorWindow : EditorWindow
{
    private enum EditorLayer
    {
        Walls,
        Hazards
    }


    private enum LinearHandleType
    {
        None,
        Min,
        Max
    }
    private const int Columns = 9;
    private const int Rows = 12;
    private const float CellSize = 30f;
    private const string LevelsFolder = "Assets/Resources/Levels";
    private const float ValidationPanelHeight = 132f;

    private LevelConfig[] levelConfigs = new LevelConfig[0];
    private LevelConfig selectedConfig;
    private SerializedObject serializedConfig;
    private SerializedProperty hazardsProperty;
    private SerializedProperty wallCellsProperty;
    private Vector2 levelListScroll;
    private Vector2 inspectorScroll;
    private Vector2 validationScroll;

    private EditorLayer activeLayer = EditorLayer.Walls;
    private int selectedHazardIndex = -1;
    private int paintedWallValue = -1;
    private LinearHandleType activeLinearHandle = LinearHandleType.None;
    private bool showHazardBrush;

    private TrapType brushTrapType = TrapType.Boulder;
    private TrapPattern brushPattern = TrapPattern.Horizontal;
    private int brushDirection = 1;
    private int brushMinColumn = 1;
    private int brushMaxColumn = 7;
    private int brushMinRow = 3;
    private int brushMaxRow = 7;
    private float brushMoveInterval = 0.8f;
    private float brushDangerRadius = 0.45f;
    private bool brushUseOrbitingBlade;
    private float brushOrbitRadius = 0.7f;
    private float brushOrbitBladeRadius = 0.28f;
    private float brushOrbitAngularSpeed = 180f;

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

    private void OnDisable()
    {
        paintedWallValue = -1;
        activeLinearHandle = LinearHandleType.None;
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
                DrawLayerToolbar();
                DrawHazardBrush();
                DrawGridPreview();
                EditorGUILayout.Space(8f);
                DrawValidationPanel();
                EditorGUILayout.Space(12f);
                DrawSelectionInspector();
            }

            if (serializedConfig.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(selectedConfig);
                AssetDatabase.SaveAssets();
            }
        }
    }

    private void DrawLayerToolbar()
    {
        activeLayer = (EditorLayer)GUILayout.Toolbar((int)activeLayer, new[] { "Wall Layer", "Hazard Layer" });
        EditorGUILayout.Space(6f);
    }

    private void DrawHazardBrush()
    {
        if (activeLayer != EditorLayer.Hazards)
        {
            return;
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            showHazardBrush = EditorGUILayout.Foldout(showHazardBrush, "Active Hazard Brush", true);
            if (!showHazardBrush)
            {
                EditorGUILayout.LabelField($"Brush: {brushTrapType} / {brushPattern}", EditorStyles.miniLabel);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                brushTrapType = (TrapType)EditorGUILayout.Popup("Type", (int)brushTrapType, new[] { "Boulder", "Arrow" });
                brushPattern = (TrapPattern)EditorGUILayout.Popup("Pattern", (int)brushPattern, new[] { "H", "V", "S" });
            }

            DrawLinearBoundsFieldsForBrush();

            brushMoveInterval = EditorGUILayout.Slider("Move Interval", brushMoveInterval, 0.2f, 2f);
            brushDangerRadius = EditorGUILayout.Slider("Danger Radius", brushDangerRadius, 0.2f, 1.25f);

            int directionIndex = brushDirection >= 0 ? 0 : 1;
            directionIndex = EditorGUILayout.Popup("Direction", directionIndex, new[] { "+1", "-1" });
            brushDirection = directionIndex == 0 ? 1 : -1;

            brushUseOrbitingBlade = EditorGUILayout.Toggle("Orbiting Blade", brushUseOrbitingBlade);
            if (brushUseOrbitingBlade)
            {
                brushOrbitRadius = EditorGUILayout.Slider("Orbit Radius", brushOrbitRadius, 0.25f, 1.5f);
                brushOrbitBladeRadius = EditorGUILayout.Slider("Blade Radius", brushOrbitBladeRadius, 0.1f, 0.75f);
                brushOrbitAngularSpeed = EditorGUILayout.Slider("Blade Speed", brushOrbitAngularSpeed, 30f, 360f);
            }
        }
    }
    private void DrawLinearBoundsFieldsForBrush()
    {
        if (brushPattern == TrapPattern.Horizontal)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                brushMinColumn = EditorGUILayout.IntSlider("Min Col", brushMinColumn, 1, Columns - 2);
                brushMaxColumn = EditorGUILayout.IntSlider("Max Col", brushMaxColumn, 1, Columns - 2);
            }

            NormalizeBrushBounds();
        }
        else if (brushPattern == TrapPattern.Vertical)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                brushMinRow = EditorGUILayout.IntSlider("Min Row", brushMinRow, 1, Rows - 2);
                brushMaxRow = EditorGUILayout.IntSlider("Max Row", brushMaxRow, 1, Rows - 2);
            }

            NormalizeBrushBounds();
        }
        else
        {
            EditorGUILayout.HelpBox("Square path stays fixed at 3x3 in this version.", MessageType.None);
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
                Rect cellRect = GetCellRect(previewRect, column, row);
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
        DrawConflictOverlay(previewRect);
        DrawHazardPreview(previewRect);
        HandlePreviewInput(previewRect);

        EditorGUILayout.HelpBox(
            activeLayer == EditorLayer.Walls
                ? "Wall Layer: click or drag to paint walls. Click an existing wall to erase. Ctrl+click also erases."
                : "Hazard Layer: click an empty cell to place the active hazard. Click an existing hazard to select it. Drag the endpoint handles of a selected horizontal or vertical hazard to resize its path.",
            MessageType.None);
    }

    private void DrawWallCells(Rect previewRect)
    {
        Color fillColor = activeLayer == EditorLayer.Walls
            ? new Color(0.24f, 0.24f, 0.3f, 0.96f)
            : new Color(0.24f, 0.24f, 0.3f, 0.55f);
        Color innerColor = activeLayer == EditorLayer.Walls
            ? new Color(0.46f, 0.46f, 0.52f, 0.35f)
            : new Color(0.46f, 0.46f, 0.52f, 0.2f);

        for (int i = 0; i < wallCellsProperty.arraySize; i++)
        {
            SerializedProperty wallCell = wallCellsProperty.GetArrayElementAtIndex(i);
            Rect cellRect = GetCellRect(
                previewRect,
                wallCell.FindPropertyRelative("column").intValue,
                wallCell.FindPropertyRelative("row").intValue);

            EditorGUI.DrawRect(cellRect, fillColor);
            EditorGUI.DrawRect(new Rect(cellRect.x + 4f, cellRect.y + 4f, cellRect.width - 8f, cellRect.height - 8f), innerColor);
            GUI.Label(cellRect, "#", CenterMiniLabel());
        }
    }

    private void DrawHazardPreview(Rect previewRect)
    {
        HashSet<Vector2Int> wallCells = BuildWallCellSet();

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
            bool isSelected = i == selectedHazardIndex;
            bool hasValidationIssues = HasHazardValidationIssues(i, hazard, wallCells);

            List<Vector2Int> pathCells = BuildHazardPreviewCells(hazard);
            Color pathColor = isSelected
                ? new Color(0.85f, 0.95f, 1f, 0.24f)
                : (activeLayer == EditorLayer.Hazards ? new Color(1f, 1f, 1f, 0.12f) : new Color(1f, 1f, 1f, 0.06f));

            for (int pathIndex = 0; pathIndex < pathCells.Count; pathIndex++)
            {
                Rect pathRect = GetCellRect(previewRect, pathCells[pathIndex].x, pathCells[pathIndex].y);
                EditorGUI.DrawRect(new Rect(pathRect.x + 8f, pathRect.y + 8f, pathRect.width - 16f, pathRect.height - 16f), pathColor);
            }

            if ((pattern == TrapPattern.Horizontal || pattern == TrapPattern.Vertical) && pathCells.Count > 0)
            {
                Rect minRect = GetCellRect(previewRect, pathCells[0].x, pathCells[0].y);
                Rect maxRect = GetCellRect(previewRect, pathCells[pathCells.Count - 1].x, pathCells[pathCells.Count - 1].y);
                DrawOutline(minRect, new Color(0.3f, 0.95f, 1f, 0.95f), 2f);
                DrawOutline(maxRect, new Color(1f, 0.75f, 0.3f, 0.95f), 2f);

                if (isSelected)
                {
                    DrawLinearHandle(previewRect, hazard, true);
                    DrawLinearHandle(previewRect, hazard, false);
                }
            }

            Rect cellRect = GetCellRect(previewRect, startColumn, startRow);
            Vector2 center = cellRect.center;
            float radiusPixels = dangerRadius * CellSize;
            Color dangerColor = isSelected
                ? new Color(1f, 0.2f, 0.2f, 0.2f)
                : new Color(1f, 0.15f, 0.15f, activeLayer == EditorLayer.Hazards ? 0.12f : 0.08f);
            EditorGUI.DrawRect(new Rect(center.x - radiusPixels, center.y - radiusPixels, radiusPixels * 2f, radiusPixels * 2f), dangerColor);

            if (useOrbitingBlade)
            {
                float orbitPixels = orbitRadius * CellSize;
                Color orbitColor = isSelected
                    ? new Color(1f, 0.9f, 0.3f, 0.16f)
                    : new Color(1f, 0.85f, 0.2f, activeLayer == EditorLayer.Hazards ? 0.08f : 0.05f);
                EditorGUI.DrawRect(new Rect(center.x - orbitPixels, center.y - orbitPixels, orbitPixels * 2f, orbitPixels * 2f), orbitColor);
            }

            Color bodyColor = trapType == TrapType.Arrow ? new Color(1f, 0.55f, 0.2f) : new Color(0.9f, 0.2f, 0.2f);
            if (activeLayer != EditorLayer.Hazards && !isSelected)
            {
                bodyColor.a = 0.75f;
            }

            EditorGUI.DrawRect(cellRect, bodyColor);
            GUI.Label(cellRect, GetPatternLabel(pattern), CenterMiniLabel());
            DrawOutline(cellRect, new Color(0.25f, 1f, 0.8f, isSelected ? 1f : 0.55f), isSelected ? 3f : 2f);
            DrawCornerMarker(cellRect, new Color(0.25f, 1f, 0.8f, 0.95f));

            if (isSelected)
            {
                DrawOutline(cellRect, hasValidationIssues ? new Color(1f, 0.82f, 0.2f, 1f) : new Color(1f, 1f, 1f, 0.95f), 2f);
            }
        }
    }

    private void HandlePreviewInput(Rect previewRect)
    {
        Event currentEvent = Event.current;
        if (currentEvent == null)
        {
            return;
        }

        if (HandleLinearPathHandleInput(currentEvent, previewRect))
        {
            return;
        }

        if (currentEvent.type == EventType.MouseUp || currentEvent.type == EventType.Ignore)
        {
            paintedWallValue = -1;
            activeLinearHandle = LinearHandleType.None;
        }

        if (!previewRect.Contains(currentEvent.mousePosition))
        {
            return;
        }

        Vector2Int cell = GetCellFromMouse(previewRect, currentEvent.mousePosition);
        if (activeLayer == EditorLayer.Walls)
        {
            HandleWallPaint(currentEvent, cell);
        }
        else
        {
            HandleHazardPaint(currentEvent, cell);
        }
    }
    private bool HandleLinearPathHandleInput(Event currentEvent, Rect previewRect)
    {
        if (activeLayer != EditorLayer.Hazards)
        {
            activeLinearHandle = currentEvent.type == EventType.MouseUp ? LinearHandleType.None : activeLinearHandle;
            return false;
        }

        if (selectedHazardIndex < 0 || selectedHazardIndex >= hazardsProperty.arraySize)
        {
            activeLinearHandle = currentEvent.type == EventType.MouseUp ? LinearHandleType.None : activeLinearHandle;
            return false;
        }

        SerializedProperty hazard = hazardsProperty.GetArrayElementAtIndex(selectedHazardIndex);
        TrapPattern pattern = (TrapPattern)hazard.FindPropertyRelative("pattern").enumValueIndex;
        if (pattern != TrapPattern.Horizontal && pattern != TrapPattern.Vertical)
        {
            activeLinearHandle = currentEvent.type == EventType.MouseUp ? LinearHandleType.None : activeLinearHandle;
            return false;
        }

        Rect minHandleRect;
        Rect maxHandleRect;
        if (!TryGetLinearHandleRects(previewRect, hazard, out minHandleRect, out maxHandleRect))
        {
            return false;
        }

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            if (minHandleRect.Contains(currentEvent.mousePosition))
            {
                activeLinearHandle = LinearHandleType.Min;
                currentEvent.Use();
                return true;
            }

            if (maxHandleRect.Contains(currentEvent.mousePosition))
            {
                activeLinearHandle = LinearHandleType.Max;
                currentEvent.Use();
                return true;
            }
        }

        if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0 && activeLinearHandle != LinearHandleType.None)
        {
            UpdateSelectedHazardHandle(previewRect, hazard, currentEvent.mousePosition);
            currentEvent.Use();
            Repaint();
            return true;
        }

        if ((currentEvent.type == EventType.MouseUp || currentEvent.type == EventType.Ignore) && activeLinearHandle != LinearHandleType.None)
        {
            activeLinearHandle = LinearHandleType.None;
            currentEvent.Use();
            return true;
        }

        return false;
    }

    private bool TryGetLinearHandleRects(Rect previewRect, SerializedProperty hazard, out Rect minHandleRect, out Rect maxHandleRect)
    {
        minHandleRect = Rect.zero;
        maxHandleRect = Rect.zero;

        List<Vector2Int> pathCells = BuildHazardPreviewCells(hazard);
        if (pathCells.Count == 0)
        {
            return false;
        }

        minHandleRect = GetHandleRect(GetCellRect(previewRect, pathCells[0].x, pathCells[0].y));
        maxHandleRect = GetHandleRect(GetCellRect(previewRect, pathCells[pathCells.Count - 1].x, pathCells[pathCells.Count - 1].y));
        return true;
    }

    private void UpdateSelectedHazardHandle(Rect previewRect, SerializedProperty hazard, Vector2 mousePosition)
    {
        Vector2Int cell = GetClampedCellFromMouse(previewRect, mousePosition);
        TrapPattern pattern = (TrapPattern)hazard.FindPropertyRelative("pattern").enumValueIndex;
        bool changed = false;

        if (pattern == TrapPattern.Horizontal)
        {
            SerializedProperty minColumnProperty = hazard.FindPropertyRelative("minColumn");
            SerializedProperty maxColumnProperty = hazard.FindPropertyRelative("maxColumn");
            int maxColumn = Mathf.Clamp(maxColumnProperty.intValue, 1, Columns - 2);
            int minColumn = Mathf.Clamp(minColumnProperty.intValue, 1, Columns - 2);
            int column = Mathf.Clamp(cell.x, 1, Columns - 2);

            if (activeLinearHandle == LinearHandleType.Min)
            {
                int nextValue = Mathf.Min(column, maxColumn);
                if (minColumnProperty.intValue != nextValue)
                {
                    RecordLevelUndo("Resize Hazard Path");
                    minColumnProperty.intValue = nextValue;
                    changed = true;
                }
            }
            else if (activeLinearHandle == LinearHandleType.Max)
            {
                int nextValue = Mathf.Max(column, minColumn);
                if (maxColumnProperty.intValue != nextValue)
                {
                    RecordLevelUndo("Resize Hazard Path");
                    maxColumnProperty.intValue = nextValue;
                    changed = true;
                }
            }
        }
        else if (pattern == TrapPattern.Vertical)
        {
            SerializedProperty minRowProperty = hazard.FindPropertyRelative("minRow");
            SerializedProperty maxRowProperty = hazard.FindPropertyRelative("maxRow");
            int minRow = Mathf.Clamp(minRowProperty.intValue, 1, Rows - 2);
            int maxRow = Mathf.Clamp(maxRowProperty.intValue, 1, Rows - 2);
            int row = Mathf.Clamp(cell.y, 1, Rows - 2);

            if (activeLinearHandle == LinearHandleType.Min)
            {
                int nextValue = Mathf.Min(row, maxRow);
                if (minRowProperty.intValue != nextValue)
                {
                    RecordLevelUndo("Resize Hazard Path");
                    minRowProperty.intValue = nextValue;
                    changed = true;
                }
            }
            else if (activeLinearHandle == LinearHandleType.Max)
            {
                int nextValue = Mathf.Max(row, minRow);
                if (maxRowProperty.intValue != nextValue)
                {
                    RecordLevelUndo("Resize Hazard Path");
                    maxRowProperty.intValue = nextValue;
                    changed = true;
                }
            }
        }

        if (!changed)
        {
            return;
        }

        serializedConfig.ApplyModifiedProperties();
        EditorUtility.SetDirty(selectedConfig);
    }
    private Rect GetHandleRect(Rect cellRect)
    {
        float size = 12f;
        return new Rect(cellRect.center.x - size * 0.5f, cellRect.center.y - size * 0.5f, size, size);
    }

    private void DrawLinearHandle(Rect previewRect, SerializedProperty hazard, bool isMinHandle)
    {
        Rect minHandleRect;
        Rect maxHandleRect;
        if (!TryGetLinearHandleRects(previewRect, hazard, out minHandleRect, out maxHandleRect))
        {
            return;
        }

        Rect handleRect = isMinHandle ? minHandleRect : maxHandleRect;
        Color handleColor = isMinHandle ? new Color(0.3f, 0.95f, 1f, 0.95f) : new Color(1f, 0.75f, 0.3f, 0.95f);
        if ((isMinHandle && activeLinearHandle == LinearHandleType.Min) || (!isMinHandle && activeLinearHandle == LinearHandleType.Max))
        {
            handleColor = Color.white;
        }

        EditorGUI.DrawRect(handleRect, handleColor);
        DrawOutline(handleRect, new Color(0.08f, 0.08f, 0.08f, 0.95f), 1f);
    }

    private void DrawCornerMarker(Rect cellRect, Color color)
    {
        EditorGUI.DrawRect(new Rect(cellRect.x + 3f, cellRect.y + 3f, 7f, 7f), color);
    }

    private Vector2Int GetClampedCellFromMouse(Rect previewRect, Vector2 mousePosition)
    {
        float normalizedX = Mathf.Clamp(mousePosition.x - previewRect.x, 0f, previewRect.width - 0.001f);
        float normalizedY = Mathf.Clamp(mousePosition.y - previewRect.y, 0f, previewRect.height - 0.001f);
        int column = Mathf.Clamp(Mathf.FloorToInt(normalizedX / CellSize), 0, Columns - 1);
        int rowFromTop = Mathf.Clamp(Mathf.FloorToInt(normalizedY / CellSize), 0, Rows - 1);
        int row = Rows - 1 - rowFromTop;
        return new Vector2Int(column, row);
    }
    private void HandleWallPaint(Event currentEvent, Vector2Int cell)
    {
        if (currentEvent.button != 0)
        {
            return;
        }

        if (currentEvent.type == EventType.MouseDown)
        {
            if (!IsEditableWallCell(cell))
            {
                currentEvent.Use();
                return;
            }

            bool erase = currentEvent.control || FindWallCellIndex(cell) >= 0;
            paintedWallValue = erase ? 0 : 1;
            ApplyWallPaint(cell, paintedWallValue == 1);
            currentEvent.Use();
            Repaint();
        }
        else if (currentEvent.type == EventType.MouseDrag)
        {
            if (paintedWallValue < 0 || !IsEditableWallCell(cell))
            {
                currentEvent.Use();
                return;
            }

            ApplyWallPaint(cell, paintedWallValue == 1);
            currentEvent.Use();
            Repaint();
        }
    }

    private void HandleHazardPaint(Event currentEvent, Vector2Int cell)
    {
        if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
        {
            return;
        }

        int existingIndex = FindHazardIndexAtCell(cell);
        if (existingIndex >= 0)
        {
            selectedHazardIndex = existingIndex;
            currentEvent.Use();
            Repaint();
            return;
        }

        AddHazardAtCell(cell);
        selectedHazardIndex = hazardsProperty.arraySize - 1;
        serializedConfig.ApplyModifiedProperties();
        EditorUtility.SetDirty(selectedConfig);
        currentEvent.Use();
        Repaint();
    }

    private void ApplyWallPaint(Vector2Int cell, bool shouldExist)
    {
        int existingIndex = FindWallCellIndex(cell);
        if (shouldExist)
        {
            if (existingIndex >= 0)
            {
                return;
            }

            RecordLevelUndo("Paint Walls");
            int newIndex = wallCellsProperty.arraySize;
            wallCellsProperty.InsertArrayElementAtIndex(newIndex);
            SerializedProperty wallCell = wallCellsProperty.GetArrayElementAtIndex(newIndex);
            wallCell.FindPropertyRelative("column").intValue = cell.x;
            wallCell.FindPropertyRelative("row").intValue = cell.y;
        }
        else if (existingIndex >= 0)
        {
            RecordLevelUndo("Erase Walls");
            wallCellsProperty.DeleteArrayElementAtIndex(existingIndex);
        }
        else
        {
            return;
        }

        serializedConfig.ApplyModifiedProperties();
        EditorUtility.SetDirty(selectedConfig);
    }
    private void AddHazardAtCell(Vector2Int cell)
    {
        RecordLevelUndo("Add Hazard");
        int index = hazardsProperty.arraySize;
        hazardsProperty.InsertArrayElementAtIndex(index);
        SerializedProperty hazard = hazardsProperty.GetArrayElementAtIndex(index);
        ApplyBrushToHazard(hazard, cell);
    }
    private void ApplyBrushToHazard(SerializedProperty hazard, Vector2Int cell)
    {
        hazard.FindPropertyRelative("trapType").enumValueIndex = (int)brushTrapType;
        hazard.FindPropertyRelative("pattern").enumValueIndex = (int)brushPattern;
        hazard.FindPropertyRelative("startColumn").intValue = Mathf.Clamp(cell.x, 0, Columns - 1);
        hazard.FindPropertyRelative("startRow").intValue = Mathf.Clamp(cell.y, 0, Rows - 1);
        hazard.FindPropertyRelative("moveInterval").floatValue = brushMoveInterval;
        hazard.FindPropertyRelative("direction").intValue = brushDirection;
        hazard.FindPropertyRelative("dangerRadius").floatValue = brushDangerRadius;
        hazard.FindPropertyRelative("useOrbitingBlade").boolValue = brushUseOrbitingBlade;
        hazard.FindPropertyRelative("orbitRadius").floatValue = brushOrbitRadius;
        hazard.FindPropertyRelative("orbitBladeRadius").floatValue = brushOrbitBladeRadius;
        hazard.FindPropertyRelative("orbitAngularSpeed").floatValue = brushOrbitAngularSpeed;

        if (brushPattern == TrapPattern.Horizontal)
        {
            hazard.FindPropertyRelative("minColumn").intValue = brushMinColumn;
            hazard.FindPropertyRelative("maxColumn").intValue = brushMaxColumn;
            hazard.FindPropertyRelative("minRow").intValue = Mathf.Max(1, cell.y - 2);
            hazard.FindPropertyRelative("maxRow").intValue = Mathf.Min(Rows - 2, cell.y + 2);
        }
        else if (brushPattern == TrapPattern.Vertical)
        {
            hazard.FindPropertyRelative("minColumn").intValue = 1;
            hazard.FindPropertyRelative("maxColumn").intValue = Columns - 2;
            hazard.FindPropertyRelative("minRow").intValue = brushMinRow;
            hazard.FindPropertyRelative("maxRow").intValue = brushMaxRow;
        }
        else
        {
            ApplyDefaultSquareBounds(hazard);
        }

        EnsureHazardBounds(hazard, false);
    }

    private void DrawSelectionInspector()
    {
        if (activeLayer == EditorLayer.Hazards)
        {
            DrawSelectedHazardInspector();
        }
        else
        {
            EditorGUILayout.LabelField("Interior Walls", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"Wall cells: {wallCellsProperty.arraySize}. Paint directly on the room preview.", MessageType.Info);
        }
    }

    private void DrawSelectedHazardInspector()
    {
        EditorGUILayout.LabelField("Hazard Inspector", EditorStyles.boldLabel);

        if (selectedHazardIndex < 0 || selectedHazardIndex >= hazardsProperty.arraySize)
        {
            EditorGUILayout.HelpBox("No hazard selected. Click a hazard on the grid or place a new one with the active brush.", MessageType.Info);
            return;
        }

        SerializedProperty hazard = hazardsProperty.GetArrayElementAtIndex(selectedHazardIndex);
        SerializedProperty startRowProperty = hazard.FindPropertyRelative("startRow");
        SerializedProperty startColumnProperty = hazard.FindPropertyRelative("startColumn");
        SerializedProperty trapTypeProperty = hazard.FindPropertyRelative("trapType");
        SerializedProperty patternProperty = hazard.FindPropertyRelative("pattern");
        SerializedProperty moveIntervalProperty = hazard.FindPropertyRelative("moveInterval");
        SerializedProperty dangerRadiusProperty = hazard.FindPropertyRelative("dangerRadius");
        SerializedProperty directionProperty = hazard.FindPropertyRelative("direction");
        SerializedProperty useOrbitingBladeProperty = hazard.FindPropertyRelative("useOrbitingBlade");
        SerializedProperty orbitRadiusProperty = hazard.FindPropertyRelative("orbitRadius");
        SerializedProperty orbitBladeRadiusProperty = hazard.FindPropertyRelative("orbitBladeRadius");
        SerializedProperty orbitAngularSpeedProperty = hazard.FindPropertyRelative("orbitAngularSpeed");
        TrapPattern previousPattern = (TrapPattern)patternProperty.enumValueIndex;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField($"Selected Hazard {selectedHazardIndex + 1}", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                SetIntPropertyWithUndo(startRowProperty, EditorGUILayout.IntSlider("Row", startRowProperty.intValue, 0, Rows - 1), "Edit Hazard");
                SetIntPropertyWithUndo(startColumnProperty, EditorGUILayout.IntSlider("Col", startColumnProperty.intValue, 0, Columns - 1), "Edit Hazard");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                SetEnumPropertyWithUndo(trapTypeProperty, EditorGUILayout.Popup("Type", trapTypeProperty.enumValueIndex, new[] { "Boulder", "Arrow" }), "Edit Hazard");
                SetEnumPropertyWithUndo(patternProperty, EditorGUILayout.Popup("Pattern", patternProperty.enumValueIndex, new[] { "H", "V", "S" }), "Edit Hazard");
            }

            TrapPattern currentPattern = (TrapPattern)patternProperty.enumValueIndex;
            EnsureHazardBounds(hazard, currentPattern != previousPattern, "Edit Hazard");

            DrawLinearBoundsFieldsForSelectedHazard(hazard);

            SetFloatPropertyWithUndo(moveIntervalProperty, EditorGUILayout.Slider("Move Interval", moveIntervalProperty.floatValue, 0.2f, 2f), "Edit Hazard");
            SetFloatPropertyWithUndo(dangerRadiusProperty, EditorGUILayout.Slider("Danger Radius", dangerRadiusProperty.floatValue, 0.2f, 1.25f), "Edit Hazard");

            int directionIndex = directionProperty.intValue >= 0 ? 0 : 1;
            directionIndex = EditorGUILayout.Popup("Direction", directionIndex, new[] { "+1", "-1" });
            SetIntPropertyWithUndo(directionProperty, directionIndex == 0 ? 1 : -1, "Edit Hazard");

            bool useOrbitingBlade = EditorGUILayout.Toggle("Orbiting Blade", useOrbitingBladeProperty.boolValue);
            SetBoolPropertyWithUndo(useOrbitingBladeProperty, useOrbitingBlade, "Edit Hazard");
            if (useOrbitingBladeProperty.boolValue)
            {
                SetFloatPropertyWithUndo(orbitRadiusProperty, EditorGUILayout.Slider("Orbit Radius", orbitRadiusProperty.floatValue, 0.25f, 1.5f), "Edit Hazard");
                SetFloatPropertyWithUndo(orbitBladeRadiusProperty, EditorGUILayout.Slider("Blade Radius", orbitBladeRadiusProperty.floatValue, 0.1f, 0.75f), "Edit Hazard");
                SetFloatPropertyWithUndo(orbitAngularSpeedProperty, EditorGUILayout.Slider("Blade Speed", orbitAngularSpeedProperty.floatValue, 30f, 360f), "Edit Hazard");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Duplicate Selected"))
                {
                    DuplicateSelectedHazard();
                }

                if (GUILayout.Button("Delete Selected"))
                {
                    DeleteSelectedHazard();
                    return;
                }

                if (GUILayout.Button("Clear Selection"))
                {
                    selectedHazardIndex = -1;
                }
            }
        }

        DrawSelectedHazardWarnings(hazard);
    }
    private void DrawLinearBoundsFieldsForSelectedHazard(SerializedProperty hazard)
    {
        TrapPattern pattern = (TrapPattern)hazard.FindPropertyRelative("pattern").enumValueIndex;
        if (pattern == TrapPattern.Horizontal)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                SetIntPropertyWithUndo(hazard.FindPropertyRelative("minColumn"), EditorGUILayout.IntSlider("Min Col", hazard.FindPropertyRelative("minColumn").intValue, 1, Columns - 2), "Edit Hazard");
                SetIntPropertyWithUndo(hazard.FindPropertyRelative("maxColumn"), EditorGUILayout.IntSlider("Max Col", hazard.FindPropertyRelative("maxColumn").intValue, 1, Columns - 2), "Edit Hazard");
            }
        }
        else if (pattern == TrapPattern.Vertical)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                SetIntPropertyWithUndo(hazard.FindPropertyRelative("minRow"), EditorGUILayout.IntSlider("Min Row", hazard.FindPropertyRelative("minRow").intValue, 1, Rows - 2), "Edit Hazard");
                SetIntPropertyWithUndo(hazard.FindPropertyRelative("maxRow"), EditorGUILayout.IntSlider("Max Row", hazard.FindPropertyRelative("maxRow").intValue, 1, Rows - 2), "Edit Hazard");
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Square path stays fixed at 3x3 in this version.", MessageType.None);
        }

        EnsureHazardBounds(hazard, false, "Edit Hazard");
    }
    private void DuplicateSelectedHazard()
    {
        if (selectedHazardIndex < 0 || selectedHazardIndex >= hazardsProperty.arraySize)
        {
            return;
        }

        RecordLevelUndo("Duplicate Hazard");
        SerializedProperty source = hazardsProperty.GetArrayElementAtIndex(selectedHazardIndex);
        int newIndex = hazardsProperty.arraySize;
        hazardsProperty.InsertArrayElementAtIndex(newIndex);
        SerializedProperty duplicate = hazardsProperty.GetArrayElementAtIndex(newIndex);

        duplicate.FindPropertyRelative("trapType").enumValueIndex = source.FindPropertyRelative("trapType").enumValueIndex;
        duplicate.FindPropertyRelative("pattern").enumValueIndex = source.FindPropertyRelative("pattern").enumValueIndex;
        duplicate.FindPropertyRelative("startColumn").intValue = source.FindPropertyRelative("startColumn").intValue;
        duplicate.FindPropertyRelative("startRow").intValue = Mathf.Clamp(source.FindPropertyRelative("startRow").intValue + 1, 0, Rows - 1);
        duplicate.FindPropertyRelative("minColumn").intValue = source.FindPropertyRelative("minColumn").intValue;
        duplicate.FindPropertyRelative("maxColumn").intValue = source.FindPropertyRelative("maxColumn").intValue;
        duplicate.FindPropertyRelative("minRow").intValue = source.FindPropertyRelative("minRow").intValue;
        duplicate.FindPropertyRelative("maxRow").intValue = source.FindPropertyRelative("maxRow").intValue;
        duplicate.FindPropertyRelative("moveInterval").floatValue = source.FindPropertyRelative("moveInterval").floatValue;
        duplicate.FindPropertyRelative("direction").intValue = source.FindPropertyRelative("direction").intValue;
        duplicate.FindPropertyRelative("dangerRadius").floatValue = source.FindPropertyRelative("dangerRadius").floatValue;
        duplicate.FindPropertyRelative("useOrbitingBlade").boolValue = source.FindPropertyRelative("useOrbitingBlade").boolValue;
        duplicate.FindPropertyRelative("orbitRadius").floatValue = source.FindPropertyRelative("orbitRadius").floatValue;
        duplicate.FindPropertyRelative("orbitBladeRadius").floatValue = source.FindPropertyRelative("orbitBladeRadius").floatValue;
        duplicate.FindPropertyRelative("orbitAngularSpeed").floatValue = source.FindPropertyRelative("orbitAngularSpeed").floatValue;

        selectedHazardIndex = newIndex;
        serializedConfig.ApplyModifiedProperties();
        EditorUtility.SetDirty(selectedConfig);
    }
    private void DeleteSelectedHazard()
    {
        if (selectedHazardIndex < 0 || selectedHazardIndex >= hazardsProperty.arraySize)
        {
            return;
        }

        RecordLevelUndo("Delete Hazard");
        hazardsProperty.DeleteArrayElementAtIndex(selectedHazardIndex);
        selectedHazardIndex = Mathf.Clamp(selectedHazardIndex, 0, hazardsProperty.arraySize - 1);
        if (hazardsProperty.arraySize == 0)
        {
            selectedHazardIndex = -1;
        }

        serializedConfig.ApplyModifiedProperties();
        EditorUtility.SetDirty(selectedConfig);
    }

    private void RecordLevelUndo(string action)
    {
        if (selectedConfig == null)
        {
            return;
        }

        Undo.RecordObject(selectedConfig, action);
    }

    private void SetIntPropertyWithUndo(SerializedProperty property, int value, string action)
    {
        if (property.intValue == value)
        {
            return;
        }

        RecordLevelUndo(action);
        property.intValue = value;
    }

    private void SetFloatPropertyWithUndo(SerializedProperty property, float value, string action)
    {
        if (Mathf.Approximately(property.floatValue, value))
        {
            return;
        }

        RecordLevelUndo(action);
        property.floatValue = value;
    }

    private void SetBoolPropertyWithUndo(SerializedProperty property, bool value, string action)
    {
        if (property.boolValue == value)
        {
            return;
        }

        RecordLevelUndo(action);
        property.boolValue = value;
    }

    private void SetEnumPropertyWithUndo(SerializedProperty property, int value, string action)
    {
        if (property.enumValueIndex == value)
        {
            return;
        }

        RecordLevelUndo(action);
        property.enumValueIndex = value;
    }
    private void DrawValidationPanel()
    {
        List<string> warnings = BuildWarnings();

        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope("box", GUILayout.Height(ValidationPanelHeight)))
        {
            using (EditorGUILayout.ScrollViewScope scrollView = new EditorGUILayout.ScrollViewScope(validationScroll, GUILayout.Height(ValidationPanelHeight - 10f)))
            {
                validationScroll = scrollView.scrollPosition;

                if (warnings.Count == 0)
                {
                    EditorGUILayout.HelpBox("No current validation warnings.", MessageType.Info);
                    return;
                }

                for (int i = 0; i < warnings.Count; i++)
                {
                    EditorGUILayout.HelpBox(warnings[i], MessageType.Warning);
                }
            }
        }
    }

    private List<string> BuildWarnings()
    {
        List<string> warnings = new List<string>();
        HashSet<Vector2Int> wallCells = BuildWallCellSet();

        for (int i = 0; i < hazardsProperty.arraySize; i++)
        {
            SerializedProperty hazard = hazardsProperty.GetArrayElementAtIndex(i);
            warnings.AddRange(BuildHazardWarnings(i, hazard, wallCells, false));
        }

        return warnings;
    }

    private void DrawSelectedHazardWarnings(SerializedProperty hazard)
    {
        List<string> warnings = BuildHazardWarnings(selectedHazardIndex, hazard, BuildWallCellSet(), true);
        if (warnings.Count == 0)
        {
            return;
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox($"Selected hazard has {warnings.Count} warning(s). See the Validation panel for details.", MessageType.Warning);
    }

    private List<string> BuildHazardWarnings(int hazardIndex, SerializedProperty hazard, HashSet<Vector2Int> wallCells, bool includeDuplicateDetails)
    {
        List<string> warnings = new List<string>();
        Vector2Int startCell = GetHazardStartCell(hazard);
        TrapPattern pattern = (TrapPattern)hazard.FindPropertyRelative("pattern").enumValueIndex;

        if (!IsInsideRoomBounds(startCell))
        {
            warnings.Add($"Hazard {hazardIndex + 1} start cell ({startCell.x}, {startCell.y}) is out of room bounds.");
        }

        if (startCell.y == 0 || startCell.y == Rows - 1)
        {
            warnings.Add($"Hazard {hazardIndex + 1} is placed on reserved row {startCell.y}. Entry/exit rows are usually kept clear.");
        }

        if (wallCells.Contains(startCell))
        {
            warnings.Add($"Hazard {hazardIndex + 1} starts inside a wall at ({startCell.x}, {startCell.y}).");
        }

        int duplicateIndex = FindDuplicateHazardIndex(hazardIndex, startCell);
        if (duplicateIndex >= 0 && (includeDuplicateDetails || duplicateIndex > hazardIndex))
        {
            warnings.Add($"Hazard {hazardIndex + 1} shares start cell ({startCell.x}, {startCell.y}) with hazard {duplicateIndex + 1}.");
        }

        if (pattern == TrapPattern.Horizontal)
        {
            AppendLinearRangeWarnings(
                warnings,
                hazardIndex,
                "column",
                hazard.FindPropertyRelative("minColumn").intValue,
                hazard.FindPropertyRelative("maxColumn").intValue,
                1,
                Columns - 2);
        }
        else if (pattern == TrapPattern.Vertical)
        {
            AppendLinearRangeWarnings(
                warnings,
                hazardIndex,
                "row",
                hazard.FindPropertyRelative("minRow").intValue,
                hazard.FindPropertyRelative("maxRow").intValue,
                1,
                Rows - 2);
        }

        List<Vector2Int> trajectory = BuildHazardPreviewCells(hazard);
        if ((pattern == TrapPattern.Horizontal || pattern == TrapPattern.Vertical) && trajectory.Count <= 1)
        {
            warnings.Add($"Hazard {hazardIndex + 1} path collapses to a single cell and will not meaningfully move.");
        }

        for (int cellIndex = 0; cellIndex < trajectory.Count; cellIndex++)
        {
            if (!wallCells.Contains(trajectory[cellIndex]))
            {
                continue;
            }

            warnings.Add($"Hazard {hazardIndex + 1} trajectory intersects a wall at ({trajectory[cellIndex].x}, {trajectory[cellIndex].y}).");
            break;
        }

        return warnings;
    }

    private void AppendLinearRangeWarnings(List<string> warnings, int hazardIndex, string axisLabel, int minValue, int maxValue, int validMin, int validMax)
    {
        if (minValue < validMin || minValue > validMax || maxValue < validMin || maxValue > validMax)
        {
            warnings.Add($"Hazard {hazardIndex + 1} {axisLabel} path range ({minValue}..{maxValue}) extends outside valid room bounds ({validMin}..{validMax}).");
        }

        if (minValue > maxValue)
        {
            warnings.Add($"Hazard {hazardIndex + 1} has inverted {axisLabel} path range ({minValue} > {maxValue}).");
        }

        int clampedMin = Mathf.Clamp(minValue, validMin, validMax);
        int clampedMax = Mathf.Clamp(maxValue, validMin, validMax);
        if (clampedMin > clampedMax)
        {
            int swap = clampedMin;
            clampedMin = clampedMax;
            clampedMax = swap;
        }

        if (clampedMin == clampedMax)
        {
            warnings.Add($"Hazard {hazardIndex + 1} {axisLabel} path range collapses to {clampedMin}, so the linear pattern has no travel distance.");
        }
    }

    private HashSet<Vector2Int> BuildWallCellSet()
    {
        HashSet<Vector2Int> wallCells = new HashSet<Vector2Int>();

        for (int i = 0; i < wallCellsProperty.arraySize; i++)
        {
            SerializedProperty wallCell = wallCellsProperty.GetArrayElementAtIndex(i);
            wallCells.Add(new Vector2Int(
                wallCell.FindPropertyRelative("column").intValue,
                wallCell.FindPropertyRelative("row").intValue));
        }

        return wallCells;
    }

    private void DrawConflictOverlay(Rect previewRect)
    {
        HashSet<Vector2Int> wallCells = BuildWallCellSet();
        if (wallCells.Count == 0 || hazardsProperty == null || hazardsProperty.arraySize == 0)
        {
            return;
        }

        HashSet<Vector2Int> pathConflictCells = new HashSet<Vector2Int>();
        HashSet<Vector2Int> startConflictCells = new HashSet<Vector2Int>();
        CollectHazardConflictCells(wallCells, pathConflictCells, startConflictCells);

        Color pathFillColor = new Color(1f, 0.18f, 0.18f, 0.2f);
        Color pathOutlineColor = new Color(1f, 0.28f, 0.28f, 0.92f);
        foreach (Vector2Int cell in pathConflictCells)
        {
            Rect cellRect = GetCellRect(previewRect, cell.x, cell.y);
            EditorGUI.DrawRect(new Rect(cellRect.x + 4f, cellRect.y + 4f, cellRect.width - 8f, cellRect.height - 8f), pathFillColor);
            DrawOutline(new Rect(cellRect.x + 3f, cellRect.y + 3f, cellRect.width - 6f, cellRect.height - 6f), pathOutlineColor, 2f);
        }

        Color startFillColor = new Color(1f, 0.05f, 0.05f, 0.26f);
        Color startOutlineColor = new Color(1f, 0.92f, 0.35f, 0.98f);
        foreach (Vector2Int cell in startConflictCells)
        {
            Rect cellRect = GetCellRect(previewRect, cell.x, cell.y);
            EditorGUI.DrawRect(cellRect, startFillColor);
            DrawOutline(cellRect, startOutlineColor, 2f);
            GUI.Label(cellRect, "!", CenterMiniLabel());
        }
    }

    private void CollectHazardConflictCells(HashSet<Vector2Int> wallCells, HashSet<Vector2Int> pathConflictCells, HashSet<Vector2Int> startConflictCells)
    {
        for (int i = 0; i < hazardsProperty.arraySize; i++)
        {
            SerializedProperty hazard = hazardsProperty.GetArrayElementAtIndex(i);
            Vector2Int startCell = GetHazardStartCell(hazard);
            if (IsInsideRoomBounds(startCell) && wallCells.Contains(startCell))
            {
                startConflictCells.Add(startCell);
            }

            List<Vector2Int> pathCells = BuildHazardPreviewCells(hazard);
            for (int pathIndex = 0; pathIndex < pathCells.Count; pathIndex++)
            {
                Vector2Int pathCell = pathCells[pathIndex];
                if (IsInsideRoomBounds(pathCell) && wallCells.Contains(pathCell))
                {
                    pathConflictCells.Add(pathCell);
                }
            }
        }
    }

    private bool HasHazardValidationIssues(int hazardIndex, SerializedProperty hazard, HashSet<Vector2Int> wallCells)
    {
        return BuildHazardWarnings(hazardIndex, hazard, wallCells, true).Count > 0;
    }

    private Vector2Int GetHazardStartCell(SerializedProperty hazard)
    {
        return new Vector2Int(
            hazard.FindPropertyRelative("startColumn").intValue,
            hazard.FindPropertyRelative("startRow").intValue);
    }

    private bool IsInsideRoomBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < Columns && cell.y >= 0 && cell.y < Rows;
    }

    private int FindDuplicateHazardIndex(int hazardIndex, Vector2Int startCell)
    {
        for (int i = 0; i < hazardsProperty.arraySize; i++)
        {
            if (i == hazardIndex)
            {
                continue;
            }

            SerializedProperty otherHazard = hazardsProperty.GetArrayElementAtIndex(i);
            if (GetHazardStartCell(otherHazard) == startCell)
            {
                return i;
            }
        }

        return -1;
    }

    private List<Vector2Int> BuildHazardPreviewCells(SerializedProperty hazard)
    {
        TrapPattern pattern = (TrapPattern)hazard.FindPropertyRelative("pattern").enumValueIndex;
        int startColumn = hazard.FindPropertyRelative("startColumn").intValue;
        int startRow = hazard.FindPropertyRelative("startRow").intValue;
        List<Vector2Int> result = new List<Vector2Int>();

        switch (pattern)
        {
            case TrapPattern.Vertical:
                int minRow = Mathf.Clamp(hazard.FindPropertyRelative("minRow").intValue, 1, Rows - 2);
                int maxRow = Mathf.Clamp(hazard.FindPropertyRelative("maxRow").intValue, 1, Rows - 2);
                if (minRow > maxRow)
                {
                    int swap = minRow;
                    minRow = maxRow;
                    maxRow = swap;
                }
                for (int row = minRow; row <= maxRow; row++)
                {
                    result.Add(new Vector2Int(Mathf.Clamp(startColumn, 1, Columns - 2), row));
                }
                break;
            case TrapPattern.Square:
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
            default:
                int minColumn = Mathf.Clamp(hazard.FindPropertyRelative("minColumn").intValue, 1, Columns - 2);
                int maxColumn = Mathf.Clamp(hazard.FindPropertyRelative("maxColumn").intValue, 1, Columns - 2);
                if (minColumn > maxColumn)
                {
                    int swap = minColumn;
                    minColumn = maxColumn;
                    maxColumn = swap;
                }
                for (int column = minColumn; column <= maxColumn; column++)
                {
                    result.Add(new Vector2Int(column, Mathf.Clamp(startRow, 1, Rows - 2)));
                }
                break;
        }

        return result;
    }
    private void EnsureHazardBounds(SerializedProperty hazard, bool resetForPattern, string undoAction = null)
    {
        SerializedProperty startColumnProperty = hazard.FindPropertyRelative("startColumn");
        SerializedProperty startRowProperty = hazard.FindPropertyRelative("startRow");
        SerializedProperty minColumnProperty = hazard.FindPropertyRelative("minColumn");
        SerializedProperty maxColumnProperty = hazard.FindPropertyRelative("maxColumn");
        SerializedProperty minRowProperty = hazard.FindPropertyRelative("minRow");
        SerializedProperty maxRowProperty = hazard.FindPropertyRelative("maxRow");

        TrapPattern pattern = (TrapPattern)hazard.FindPropertyRelative("pattern").enumValueIndex;
        int startColumn = Mathf.Clamp(startColumnProperty.intValue, 0, Columns - 1);
        int startRow = Mathf.Clamp(startRowProperty.intValue, 0, Rows - 1);
        int targetMinColumn;
        int targetMaxColumn;
        int targetMinRow;
        int targetMaxRow;

        if (pattern == TrapPattern.Horizontal)
        {
            int minColumn = resetForPattern || AreHorizontalBoundsUnset(hazard)
                ? 1
                : Mathf.Clamp(minColumnProperty.intValue, 1, Columns - 2);
            int maxColumn = resetForPattern || AreHorizontalBoundsUnset(hazard)
                ? Columns - 2
                : Mathf.Clamp(maxColumnProperty.intValue, 1, Columns - 2);
            if (minColumn > maxColumn)
            {
                int swap = minColumn;
                minColumn = maxColumn;
                maxColumn = swap;
            }

            targetMinColumn = minColumn;
            targetMaxColumn = maxColumn;
            targetMinRow = Mathf.Max(1, startRow - 2);
            targetMaxRow = Mathf.Min(Rows - 2, startRow + 2);
        }
        else if (pattern == TrapPattern.Vertical)
        {
            int minRow = resetForPattern || AreVerticalBoundsUnset(hazard)
                ? Mathf.Max(1, startRow - 2)
                : Mathf.Clamp(minRowProperty.intValue, 1, Rows - 2);
            int maxRow = resetForPattern || AreVerticalBoundsUnset(hazard)
                ? Mathf.Min(Rows - 2, startRow + 2)
                : Mathf.Clamp(maxRowProperty.intValue, 1, Rows - 2);
            if (minRow > maxRow)
            {
                int swap = minRow;
                minRow = maxRow;
                maxRow = swap;
            }

            targetMinColumn = 1;
            targetMaxColumn = Columns - 2;
            targetMinRow = minRow;
            targetMaxRow = maxRow;
        }
        else
        {
            targetMinColumn = 1;
            targetMaxColumn = Columns - 2;
            targetMinRow = Mathf.Max(1, startRow - 2);
            targetMaxRow = Mathf.Min(Rows - 2, startRow + 2);
        }

        bool changed = startColumnProperty.intValue != startColumn
            || startRowProperty.intValue != startRow
            || minColumnProperty.intValue != targetMinColumn
            || maxColumnProperty.intValue != targetMaxColumn
            || minRowProperty.intValue != targetMinRow
            || maxRowProperty.intValue != targetMaxRow;

        if (changed && !string.IsNullOrEmpty(undoAction))
        {
            RecordLevelUndo(undoAction);
        }

        minColumnProperty.intValue = targetMinColumn;
        maxColumnProperty.intValue = targetMaxColumn;
        minRowProperty.intValue = targetMinRow;
        maxRowProperty.intValue = targetMaxRow;
        startColumnProperty.intValue = startColumn;
        startRowProperty.intValue = startRow;
    }
    private void ApplyDefaultSquareBounds(SerializedProperty hazard)
    {
        int startRow = Mathf.Clamp(hazard.FindPropertyRelative("startRow").intValue, 0, Rows - 1);
        hazard.FindPropertyRelative("minColumn").intValue = 1;
        hazard.FindPropertyRelative("maxColumn").intValue = Columns - 2;
        hazard.FindPropertyRelative("minRow").intValue = Mathf.Max(1, startRow - 2);
        hazard.FindPropertyRelative("maxRow").intValue = Mathf.Min(Rows - 2, startRow + 2);
    }

    private bool AreHorizontalBoundsUnset(SerializedProperty hazard)
    {
        return hazard.FindPropertyRelative("minColumn").intValue <= 0 && hazard.FindPropertyRelative("maxColumn").intValue <= 0;
    }

    private bool AreVerticalBoundsUnset(SerializedProperty hazard)
    {
        return hazard.FindPropertyRelative("minRow").intValue <= 0 && hazard.FindPropertyRelative("maxRow").intValue <= 0;
    }

    private void NormalizeBrushBounds()
    {
        brushMinColumn = Mathf.Clamp(brushMinColumn, 1, Columns - 2);
        brushMaxColumn = Mathf.Clamp(brushMaxColumn, 1, Columns - 2);
        if (brushMinColumn > brushMaxColumn)
        {
            int swap = brushMinColumn;
            brushMinColumn = brushMaxColumn;
            brushMaxColumn = swap;
        }

        brushMinRow = Mathf.Clamp(brushMinRow, 1, Rows - 2);
        brushMaxRow = Mathf.Clamp(brushMaxRow, 1, Rows - 2);
        if (brushMinRow > brushMaxRow)
        {
            int swap = brushMinRow;
            brushMinRow = brushMaxRow;
            brushMaxRow = swap;
        }
    }

    private int FindWallCellIndex(Vector2Int cell)
    {
        for (int i = 0; i < wallCellsProperty.arraySize; i++)
        {
            SerializedProperty wallCell = wallCellsProperty.GetArrayElementAtIndex(i);
            if (wallCell.FindPropertyRelative("column").intValue == cell.x && wallCell.FindPropertyRelative("row").intValue == cell.y)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindHazardIndexAtCell(Vector2Int cell)
    {
        for (int i = 0; i < hazardsProperty.arraySize; i++)
        {
            SerializedProperty hazard = hazardsProperty.GetArrayElementAtIndex(i);
            if (hazard.FindPropertyRelative("startColumn").intValue == cell.x && hazard.FindPropertyRelative("startRow").intValue == cell.y)
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

        string assetPath = GetNextLevelAssetPath();
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        RefreshLevels();
        SelectConfig(AssetDatabase.LoadAssetAtPath<LevelConfig>(assetPath));
    }

    private string GetNextLevelAssetPath()
    {
        string[] guids = AssetDatabase.FindAssets("t:LevelConfig", new[] { LevelsFolder });
        int maxLevelNumber = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (!fileName.StartsWith("Level_", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string suffix = fileName.Substring("Level_".Length);
            if (int.TryParse(suffix, out int levelNumber))
            {
                maxLevelNumber = Mathf.Max(maxLevelNumber, levelNumber);
            }
        }

        return $"{LevelsFolder}/Level_{maxLevelNumber + 1:00}.asset";
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
                minColumn = hazard.minColumn,
                maxColumn = hazard.maxColumn,
                minRow = hazard.minRow,
                maxRow = hazard.maxRow,
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
        selectedHazardIndex = -1;
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

        if (selectedHazardIndex >= hazardsProperty.arraySize)
        {
            selectedHazardIndex = hazardsProperty.arraySize - 1;
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

    private static Rect GetCellRect(Rect previewRect, int column, int row)
    {
        return new Rect(previewRect.x + (column * CellSize), previewRect.y + ((Rows - 1 - row) * CellSize), CellSize - 1f, CellSize - 1f);
    }

    private static Vector2Int GetCellFromMouse(Rect previewRect, Vector2 mousePosition)
    {
        int column = Mathf.FloorToInt((mousePosition.x - previewRect.x) / CellSize);
        int row = Rows - 1 - Mathf.FloorToInt((mousePosition.y - previewRect.y) / CellSize);
        return new Vector2Int(column, row);
    }

    private static void DrawOutline(Rect rect, Color color, float thickness)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }
}









