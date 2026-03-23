using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static bool GameStarted = false;
    public static PlayerController Instance { get; private set; }

    [SerializeField] private Vector2Int startGridPosition = new Vector2Int(4, 0);
    [SerializeField] private float swipeThreshold = 50f;

    public Vector2Int gridPosition => currentGridPosition;
    public Vector2Int CurrentGridPosition => currentGridPosition;

    private Vector2Int currentGridPosition;
    private Vector2 swipeStartPosition;
    private bool swipeInProgress;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (GridManager.Instance == null)
        {
            Debug.LogError("PlayerController requires a GridManager in the scene.");
            enabled = false;
            return;
        }

        currentGridPosition = GridManager.Instance.ClampToBounds(startGridPosition);
        transform.position = GridManager.Instance.GridToWorld(currentGridPosition);
    }

    private void Update()
    {
        ReadKeyboardInput();
        ReadTouchInput();
        ReadMouseInput();
    }

    private void ReadKeyboardInput()
    {
        if (!GameStarted) return;
        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsLevelComplete || GameManager.Instance.IsPaused)) return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Move(Vector2Int.up);
            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            Move(Vector2Int.down);
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Move(Vector2Int.left);
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Move(Vector2Int.right);
        }
    }

    private void ReadTouchInput()
    {
        if (Input.touchCount == 0)
        {
            return;
        }

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            swipeStartPosition = touch.position;
            swipeInProgress = true;
            return;
        }

        if (!swipeInProgress)
        {
            return;
        }

        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            TryMoveFromSwipe(touch.position - swipeStartPosition);
            swipeInProgress = false;
        }
    }

    private void ReadMouseInput()
    {
        if (Input.touchCount > 0)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            swipeStartPosition = Input.mousePosition;
            swipeInProgress = true;
            return;
        }

        if (!swipeInProgress)
        {
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            TryMoveFromSwipe((Vector2)Input.mousePosition - swipeStartPosition);
            swipeInProgress = false;
        }
    }

    private void TryMoveFromSwipe(Vector2 swipeDelta)
    {
        if (!GameStarted) return;
        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsLevelComplete || GameManager.Instance.IsPaused)) return;

        if (swipeDelta.magnitude < swipeThreshold)
        {
            return;
        }

        Vector2Int moveDirection;

        if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
        {
            moveDirection = swipeDelta.x > 0f ? Vector2Int.right : Vector2Int.left;
        }
        else
        {
            moveDirection = swipeDelta.y > 0f ? Vector2Int.up : Vector2Int.down;
        }

        Move(moveDirection);
    }

    private void Move(Vector2Int direction)
    {
        if (!GameStarted) return;
        if (GameManager.Instance != null && (GameManager.Instance.IsLevelComplete || GameManager.Instance.IsPaused)) return;

        Debug.Log($"[MOVE START] Player pos: {currentGridPosition} | HP: {(GameManager.Instance != null ? GameManager.Instance.CurrentHp : -1)}");

        Vector2Int targetGridPosition = currentGridPosition + direction;

        if (!GridManager.Instance.IsWithinBounds(targetGridPosition))
        {
            return;
        }

        currentGridPosition = targetGridPosition;
        transform.position = GridManager.Instance.GridToWorld(currentGridPosition);
        TrapManager.Instance?.CheckPlayerPosition();
        GameManager.Instance?.CheckWin(currentGridPosition);
    }

    public void RespawnToStart()
    {
        if (GridManager.Instance == null)
        {
            return;
        }

        currentGridPosition = GridManager.Instance.ClampToBounds(startGridPosition);
        transform.position = GridManager.Instance.GridToWorld(currentGridPosition);
    }
}
