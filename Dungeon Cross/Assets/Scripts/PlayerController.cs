using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static bool GameStarted = false;
    public static PlayerController Instance { get; private set; }

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float touchDeadZone = 20f;
    [SerializeField] private float playerRadius = 0.25f;

    public Vector2 CurrentWorldPosition => (Vector2)transform.position;
    public Vector2 MoveInput { get; private set; }
    public float PlayerRadius => playerRadius;
    public Vector2Int gridPosition => GridManager.Instance != null ? GridManager.Instance.WorldToGrid(transform.position) : Vector2Int.zero;
    public Vector2Int CurrentGridPosition => gridPosition;

    private Vector2 touchStartPosition;
    private bool touchHeld;
    private float damageInvulnerabilityTimer;

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
        RespawnToStart();
    }

    private void Update()
    {
        if (damageInvulnerabilityTimer > 0f)
        {
            damageInvulnerabilityTimer -= Time.unscaledDeltaTime;
        }

        if (!GameStarted)
        {
            MoveInput = Vector2.zero;
            return;
        }

        ReadMovementInput();

        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsLevelComplete || GameManager.Instance.IsPaused))
        {
            MoveInput = Vector2.zero;
            return;
        }

        MovePlayer();
        TrapManager.Instance?.CheckPlayerPosition();
        GameManager.Instance?.CheckWin(transform.position);
    }

    public bool CanTakeDamage()
    {
        return damageInvulnerabilityTimer <= 0f;
    }

    public void NotifyDamaged(float invulnerabilityDuration = 0.45f)
    {
        damageInvulnerabilityTimer = Mathf.Max(damageInvulnerabilityTimer, invulnerabilityDuration);
    }

    public void RespawnToStart()
    {
        if (GridManager.Instance == null)
        {
            return;
        }

        transform.position = GridManager.Instance.GetEntryWorldPosition();
        MoveInput = Vector2.zero;
        touchHeld = false;
    }

    private void ReadMovementInput()
    {
        Vector2 keyboardInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));

        Vector2 pointerInput = ReadPointerInput();
        Vector2 desiredInput = pointerInput.sqrMagnitude > 0.001f ? pointerInput : keyboardInput;
        MoveInput = desiredInput.sqrMagnitude > 1f ? desiredInput.normalized : desiredInput;
    }

    private Vector2 ReadPointerInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                touchStartPosition = touch.position;
                touchHeld = true;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                touchHeld = false;
                return Vector2.zero;
            }

            if (touchHeld)
            {
                Vector2 delta = touch.position - touchStartPosition;
                if (delta.magnitude < touchDeadZone)
                {
                    return Vector2.zero;
                }

                return delta.normalized;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                touchStartPosition = Input.mousePosition;
                touchHeld = true;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                touchHeld = false;
                return Vector2.zero;
            }

            if (touchHeld && Input.GetMouseButton(0))
            {
                Vector2 delta = (Vector2)Input.mousePosition - touchStartPosition;
                if (delta.magnitude < touchDeadZone)
                {
                    return Vector2.zero;
                }

                return delta.normalized;
            }
        }

        return Vector2.zero;
    }

    private void MovePlayer()
    {
        if (MoveInput.sqrMagnitude <= 0.0001f || GridManager.Instance == null)
        {
            return;
        }

        Vector2 currentPosition = transform.position;
        Vector2 frameDelta = MoveInput * moveSpeed * Time.deltaTime;
        Vector2 targetPosition = currentPosition;

        Vector2 horizontalTarget = GridManager.Instance.ClampWorldPosition(
            new Vector2(currentPosition.x + frameDelta.x, currentPosition.y),
            playerRadius);
        if (!IsBlocked(horizontalTarget))
        {
            targetPosition.x = horizontalTarget.x;
        }

        Vector2 verticalTarget = GridManager.Instance.ClampWorldPosition(
            new Vector2(targetPosition.x, currentPosition.y + frameDelta.y),
            playerRadius);
        if (!IsBlocked(verticalTarget))
        {
            targetPosition.y = verticalTarget.y;
        }

        transform.position = targetPosition;
    }

    private bool IsBlocked(Vector2 targetPosition)
    {
        return LevelManager.Instance != null && LevelManager.Instance.IsPositionBlocked(targetPosition, playerRadius);
    }
}

