using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class SceneBootstrap : MonoBehaviour
{
    private void Awake()
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            gameManager = new GameObject("GameManager").AddComponent<GameManager>();
        }

        gameManager.ResetRunState();

        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null)
        {
            gridManager = new GameObject("GridManager").AddComponent<GridManager>();
        }

        ConfigureGrid(gridManager);
        ConfigureDungeonVisual();
        ConfigureMusic();

        if (FindObjectOfType<TrapManager>() == null)
        {
            new GameObject("TrapManager").AddComponent<TrapManager>();
        }

        if (FindObjectOfType<LevelManager>() == null)
        {
            new GameObject("LevelManager").AddComponent<LevelManager>();
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

        player.RespawnToStart();
        ConfigureCamera();
        ConfigureUI();
        ConfigurePauseMenu();
        ConfigureStartScreen();
    }

    private void ConfigureGrid(GridManager gridManager)
    {
        gridManager.ConfigureGridCenter(new Vector2(4f, 5.5f));
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

        if (GridManager.Instance != null)
        {
            GridManager.Instance.FitCameraToGrid();
        }
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


    private void ConfigureMusic()
    {
        MusicManager musicManager = FindObjectOfType<MusicManager>();
        if (musicManager == null)
        {
            musicManager = new GameObject("MusicManager").AddComponent<MusicManager>();
        }

        musicManager.InitializeIfNeeded();
    }
    private void ConfigurePauseMenu()
    {
        if (FindObjectOfType<PauseMenu>() == null)
        {
            new GameObject("PauseMenu").AddComponent<PauseMenu>();
        }
    }

    private void ConfigureStartScreen()
    {
        if (FindObjectOfType<StartScreen>() == null)
        {
            new GameObject("StartScreen").AddComponent<StartScreen>();
        }
    }

    private void ConfigureDungeonVisual()
    {
        DungeonVisual dungeonVisual = FindObjectOfType<DungeonVisual>();
        if (dungeonVisual == null)
        {
            dungeonVisual = new GameObject("DungeonVisual").AddComponent<DungeonVisual>();
        }

        dungeonVisual.Build();
    }
}

