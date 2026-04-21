using UnityEngine;
using LevelGeneration;
using Unity.VisualScripting;

public class EnemyTank : MonoBehaviour
{
    [Header("Enemy Tank")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private Color tankColor = Color.red;
    [SerializeField] private float changeDirectionTime = 2f;
    [SerializeField] private int maxHealth = 1;

    private RectTransform rectTransform;
    private Rigidbody2D rb2d;
    private BoxCollider2D boxCollider;
    private LevelData levelData;
    private Vector2 currentPosition;
    private Vector2 currentVelocity;
    private Vector2Int gridSize;
    private float tileSize;
    private Vector2 currentDirection;
    private float directionTimer;
    private bool initialized;
    private int currentHealth;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rb2d = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        if (!initialized) return;
        HandleMovement();
    }

    private void FixedUpdate()
    {
        if (!initialized || rb2d == null) return;
        rb2d.velocity = currentVelocity;
        currentPosition = rb2d.position;
    
    }

    public void Initialize(float tileSize, Vector2Int spawnGridPosition, Vector2Int gridSize, Color tankColor, LevelData levelData)
    {
        if (rectTransform == null)
        {
            rectTransform = this.AddComponent<RectTransform>();
        }

        if (rb2d == null)
        {
            
            rb2d = this.AddComponent<Rigidbody2D>();
        }

        if (boxCollider == null)
        {
            boxCollider = this.AddComponent<BoxCollider2D>();
        }

        this.tileSize = Mathf.Max(1f, tileSize);
        this.gridSize = gridSize;
        this.levelData = levelData;

        if (rb2d != null)
        {
            rb2d.gravityScale = 0f;
            rb2d.freezeRotation = true;
            rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb2d.velocity = Vector2.zero;
        }

        if (boxCollider != null)
        {
            boxCollider.size = new Vector2(this.tileSize, this.tileSize);
            boxCollider.offset = new Vector2(this.tileSize / 2f, this.tileSize / 2f);
        }

        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.pivot = new Vector2(0, 0);

        currentPosition = new Vector2(spawnGridPosition.x * this.tileSize, spawnGridPosition.y * this.tileSize);
        rectTransform.anchoredPosition = currentPosition;
        rectTransform.sizeDelta = new Vector2(this.tileSize, this.tileSize);

        if (rb2d != null)
        {
            rb2d.position = currentPosition;
        }

        var image = GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            image.color = tankColor;
        }

        // 初始随机方向
        ChangeDirection();

        initialized = true;
    }

    private void HandleMovement()
    {
        directionTimer -= Time.deltaTime;
        if (directionTimer <= 0)
        {
            ChangeDirection();
        }

        Vector2 desiredVelocity = currentDirection * moveSpeed * tileSize;
        Vector2 predictedPosition = currentPosition + desiredVelocity * Time.deltaTime;

        int newGridX = Mathf.RoundToInt(predictedPosition.x / tileSize);
        int newGridY = Mathf.RoundToInt(predictedPosition.y / tileSize);

        currentVelocity = desiredVelocity;
      
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider != null )
        {
            currentVelocity = Vector2.zero;
            if (rb2d != null)
            {
                rb2d.velocity = Vector2.zero;
            }
        }
    }

    private void ChangeDirection()
    {
        // 随机选择方向：上、下、左、右
        int dir = Random.Range(0, 4);
        switch (dir)
        {
            case 0: currentDirection = Vector2.up; break;
            case 1: currentDirection = Vector2.down; break;
            case 2: currentDirection = Vector2.left; break;
            case 3: currentDirection = Vector2.right; break;
        }
        directionTimer = changeDirectionTime;
    }




}