// 11/16/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;

using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;


public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    private Animator animatorComponent;
    private Rigidbody2D rigidBody2D;
    private PlayerInput playerInput;

    [Header("Input Actions")]
    private InputAction moveAction;
    private InputAction attackAction;

    [Header("Movement")]
    public float moveSpeed = 5f;
    private Vector2 movement;
    private Vector2 lastMovementDirection = Vector2.down; // Default facing down
    private bool isAttacking = false;

    [Header("Water Tilemap")]
    public Tilemap waterTilemap; // Assign your water Tilemap in Inspector
    [Header("Water Tile")]
    public TileBase waterTile; // Assign your water tile asset in Inspector


    void Awake()
    {
        // Get component references
        animatorComponent = GetComponent<Animator>();
        rigidBody2D = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();

        // Validate PlayerInput component
        if (playerInput == null)
        {
            Debug.LogError($"PlayerInput component is missing on {gameObject.name}. Please add a PlayerInput component.", this);
            enabled = false;
            return;
        }

        if (playerInput.actions == null)
        {
            Debug.LogError($"Input Actions asset is not assigned to PlayerInput on {gameObject.name}. Please assign PlayerInputActions in the Inspector.", this);
            enabled = false;
            return;
        }

        // Get input actions
        moveAction = playerInput.actions["Move"];
        attackAction = playerInput.actions["Attack"];

        // Enable actions
        if (moveAction != null) moveAction.Enable();
        if (attackAction != null) attackAction.Enable();

        // Disable gravity for top-down movement
        if (rigidBody2D != null)
        {
            rigidBody2D.gravityScale = 0f;
        }
    }

    void OnDestroy()
    {
        // Disable actions when destroyed
        if (moveAction != null) moveAction.Disable();
        if (attackAction != null) attackAction.Disable();
    }

    void Update()
    {
        // Handle attack input
        if (attackAction != null && attackAction.triggered && !isAttacking)
        {
            Attack();
        }

        // Check if attack animation is still playing
        if (isAttacking && animatorComponent != null)
        {
            AnimatorStateInfo stateInfo = animatorComponent.GetCurrentAnimatorStateInfo(0);

            // Only reset isAttacking if the animation has actually finished
            // Check normalizedTime >= 1f means the animation completed
            // Also check if we're still in an attack-related state
            bool animationFinished = stateInfo.normalizedTime >= 1f;
            bool isInAttackState = stateInfo.IsName("Attack");

            // Only set to false if animation is finished AND we're not looping
            if (animationFinished && !stateInfo.loop)
            {
                // Double-check: wait a frame to ensure we're really done
                if (stateInfo.normalizedTime >= 1f)
                {
                    isAttacking = false;
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (isAttacking)
        {
            // Stop movement during attack
            StopMovement(false);
            return;
        }



        if (moveAction != null && rigidBody2D != null && !isAttacking)
        {
            // Read movement input FIRST
            movement = moveAction.ReadValue<Vector2>();

            // Check if trying to move into water BEFORE applying movement
            if (movement.magnitude > 0.1f)
            {
                lastMovementDirection = movement.normalized;

                // Check if the NEXT position (where we're trying to go) is impassable
                Vector3 nextPosition = transform.position + (Vector3)(movement.normalized * moveSpeed * Time.fixedDeltaTime);
                if (IsImpassable(nextPosition))
                {
                    Debug.Log("Trying to move into water, stopping movement");
                    StopMovement(false);
                    return; // Exit early - don't apply movement
                }

                // Update last movement direction (only when actually moving)
                if (movement.magnitude > 0.1f)
                {
                    lastMovementDirection = movement.normalized;
                }

                // Apply movement to Rigidbody2D (only if not blocked)
                rigidBody2D.linearVelocity = movement.normalized * moveSpeed;

                // Update animator if available
                if (animatorComponent != null)
                {
                    bool isMoving = movement.magnitude > 0.1f;
                    animatorComponent.SetBool("isMoving", isMoving);

                    // Use integer direction instead
                    int direction = GetDirectionIndex(lastMovementDirection);
                    // animatorComponent.SetInteger("Direction", direction);
                }

                // Optional: Flip sprite based on horizontal direction
                UpdateSpriteDirection();
            }
            else
            {
                StopMovement(false);
            }
        }

    }


    void UpdateSpriteDirection()
    {
        // Flip sprite horizontally when moving left/right
        if (Mathf.Abs(lastMovementDirection.x) > 0.1f)
        {
            // Flip sprite based on direction
            // Assuming sprite renderer is on this GameObject or a child
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = lastMovementDirection.x < 0;
            }
        }
    }

    int GetDirectionIndex(Vector2 direction)
    {
        // Returns: 0=Down, 1=Left, 2=Right, 3=Up
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        if (angle >= -45f && angle < 45f) return 2;   // Right
        if (angle >= 45f && angle < 135f) return 3;    // Up
        if (angle >= 135f || angle < -135f) return 1;  // Left
        return 0; // Down
    }

    void StopMovement(bool isMoving = false)
    {
        rigidBody2D.linearVelocity = Vector2.zero;
        animatorComponent.SetBool("isMoving", isMoving);
    }

    void Attack()
    {
        if (isAttacking) return;

        Debug.Log("Attack!");
        isAttacking = true;

        // Stop movement IMMEDIATELY when attack starts
        if (rigidBody2D != null)
        {
            rigidBody2D.linearVelocity = Vector2.zero;
        }

        // Stop movement animation immediately
        if (animatorComponent != null)
        {
            animatorComponent.SetBool("isMoving", false);
            Debug.Log("Triggering attack animation");
            animatorComponent.SetTrigger("isAttacking");
        }

        // Debug: Check sprite renderer
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Debug.Log($"Sprite Renderer enabled: {spriteRenderer.enabled}, Visible: {spriteRenderer.isVisible}");
        }

        // Add your attack logic here (damage, hit detection, etc.)
        // You can use lastMovementDirection to determine attack direction
    }

    bool IsImpassable(Vector3 worldPosition)
    {
        if (waterTilemap == null)
        {
            Debug.LogError("Water tilemap is not assigned");
            return false;
        }

        // Get character's BoxCollider2D bounds
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        Debug.Log($"Collider: {collider?.bounds}");
        if (collider != null)
        {
            // Calculate what the bounds would be at the next position
            // Get the offset from current position to next position
            Vector3 offset = worldPosition - transform.position;

            // Get current bounds and offset them to the next position
            Bounds currentBounds = collider.bounds;
            Bounds nextBounds = new Bounds(
                currentBounds.center + offset,
                currentBounds.size
            );

            // Check if any part of the bounds overlaps with water
            // Check center and four corners of the collider
            Vector3[] checkPoints = new Vector3[]
            {
            nextBounds.center,                                    // Center
            new Vector3(nextBounds.min.x, nextBounds.min.y, nextBounds.center.z),  // Bottom-left
            new Vector3(nextBounds.max.x, nextBounds.min.y, nextBounds.center.z),  // Bottom-right
            new Vector3(nextBounds.min.x, nextBounds.max.y, nextBounds.center.z),  // Top-left
            new Vector3(nextBounds.max.x, nextBounds.max.y, nextBounds.center.z)   // Top-right
            };

            foreach (Vector3 point in checkPoints)
            {
                Vector3Int cellPosition = waterTilemap.WorldToCell(point);
                TileBase tile = waterTilemap.GetTile(cellPosition);
                if (tile != null && tile.name == waterTile?.name)
                {
                    return true; // Any corner is on water
                }
            }
        }

        return false;
    }
}