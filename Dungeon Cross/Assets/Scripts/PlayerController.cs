using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Vector2Int startGridPosition = new Vector2Int(3, 0);
    [SerializeField] private float swipeThreshold = 50f;

    private Vector2Int currentGridPosition;
    private Vector2 swipeStartPosition;
    private bool swipeInProgress;

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
        ReadTouchInput();
        ReadMouseInput();
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
        Vector2Int targetGridPosition = currentGridPosition + direction;

        if (!GridManager.Instance.IsWithinBounds(targetGridPosition))
        {
            return;
        }

        currentGridPosition = targetGridPosition;
        transform.position = GridManager.Instance.GridToWorld(currentGridPosition);
    }
}
