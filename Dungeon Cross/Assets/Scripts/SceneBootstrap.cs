using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class SceneBootstrap : MonoBehaviour
{
    private static readonly FieldInfo GridCenterField = typeof(GridManager).GetField("gridCenter", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo PlayerStartGridPositionField = typeof(PlayerController).GetField("startGridPosition", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo RecalculateOriginMethod = typeof(GridManager).GetMethod("RecalculateOrigin", BindingFlags.Instance | BindingFlags.NonPublic);

    private void Awake()
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            gameManager = new GameObject("GameManager").AddComponent<GameManager>();
        }

        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null)
        {
            gridManager = new GameObject("GridManager").AddComponent<GridManager>();
        }

        ConfigureGrid(gridManager);

        TrapManager trapManager = FindObjectOfType<TrapManager>();
        if (trapManager == null)
        {
            trapManager = new GameObject("TrapManager").AddComponent<TrapManager>();
        }

        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager == null)
        {
            levelManager = new GameObject("LevelManager").AddComponent<LevelManager>();
        }

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null)
        {
            GameObject playerObject = new GameObject("Player");
            player = playerObject.AddComponent<PlayerController>();
            playerObject.AddComponent<PlayerVisual>();
        }
        else if (player.GetComponent<PlayerVisual>() == null)
        {
            player.gameObject.AddComponent<PlayerVisual>();
        }

        ConfigurePlayer(player);
        ConfigureCamera();
        ConfigureUI();
        levelManager.SpawnLevel(1);
    }

    private void ConfigureGrid(GridManager gridManager)
    {
        GridCenterField?.SetValue(gridManager, new Vector2(3f, 4f));
        RecalculateOriginMethod?.Invoke(gridManager, null);
    }

    private void ConfigurePlayer(PlayerController player)
    {
        Vector2Int startGridPosition = new Vector2Int(3, 0);
        PlayerStartGridPositionField?.SetValue(player, startGridPosition);

        if (GridManager.Instance != null)
        {
            player.transform.position = GridManager.Instance.GridToWorld(startGridPosition);
        }
    }

    private void ConfigureCamera()
    {
        Camera targetCamera = Camera.main;

        if (targetCamera == null)
        {
            targetCamera = FindObjectOfType<Camera>();
        }

        if (targetCamera == null)
        {
            targetCamera = new GameObject("Main Camera").AddComponent<Camera>();
            targetCamera.tag = "MainCamera";
        }

        targetCamera.orthographic = true;
        targetCamera.orthographicSize = 5f;
        targetCamera.transform.position = new Vector3(3f, 4f, -10f);
    }

    private void ConfigureUI()
    {
        GameUI gameUi = FindObjectOfType<GameUI>();
        if (gameUi != null)
        {
            return;
        }

        Canvas existingCanvas = FindObjectOfType<Canvas>();
        GameObject canvasObject;

        if (existingCanvas == null)
        {
            canvasObject = new GameObject("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasObject = existingCanvas.gameObject;

            if (existingCanvas.GetComponent<CanvasScaler>() == null)
            {
                canvasObject.AddComponent<CanvasScaler>();
            }

            if (existingCanvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvasObject.AddComponent<GraphicRaycaster>();
            }
        }

        if (canvasObject.GetComponent<GameUI>() == null)
        {
            canvasObject.AddComponent<GameUI>();
        }
    }
}
