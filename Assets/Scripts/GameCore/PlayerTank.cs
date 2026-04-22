using UnityEngine;
using LevelGeneration;
using Unity.VisualScripting;


public class PlayerTank : MonoBehaviour,ITakeDamage
{
    [Header("Player Tank")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private Color tankColor = Color.green;
    [SerializeField] private int maxHealth = 1;


    private GameFramework.GameFramework Framework=> GameFramework.GameFramework.Instance; 
    private RectTransform rectTransform;
    private Rigidbody2D rb2d;
    private BoxCollider2D boxCollider;
    private InputManager inputManager;
    private LevelData levelData;
    private Vector2 currentPosition;
    private Vector2 currentVelocity;

    private GameObject FirePos;
    private Vector2Int gridSize;
    private float tileSize;
    private int playerIndex = 1;
    private bool initialized;
    private int currentHealth;


    private void Awake()
    {
        
    }

    private void Start()
    {
        inputManager = Framework?.GetFeature<InputManager>();
        if (inputManager == null)
        {
            Debug.LogWarning($"PlayerTank{playerIndex}: InputManager not found. Tank input will be disabled.");
        }
    }

    private void Update()
    {
        if (!initialized || inputManager == null) return;
        HandleMovement();
        Attack();
        
    }

    private void FixedUpdate()
    {
        if (!initialized || rb2d == null) return;
        rb2d.velocity = currentVelocity;
        // rb2d.transform.position = currentPosition;
        // 
        
        // if (rectTransform != null)
        // {
        //     rectTransform.anchorMin = new Vector2(0, 0);
        //     rectTransform.anchorMax = new Vector2(0, 0);
        //     rectTransform.pivot = new Vector2(0, 0);
        //     rectTransform.anchoredPosition = currentPosition;
        // }
    }

    public void Initialize(float tileSize, Vector2Int spawnGridPosition, Vector2Int gridSize, Color tankColor, int playerIndex = 1, LevelData levelData = null)
    {

        rectTransform = GetComponent<RectTransform>();
        rb2d = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        FirePos = transform.Find(nameof(FirePos)).gameObject;
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
        this.playerIndex = playerIndex;
        this.levelData = levelData;

        this.currentHealth = maxHealth;
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
            boxCollider.offset = new Vector2(0f, 0f);
        }

        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(0f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
      

        currentPosition = new Vector2((spawnGridPosition.x+0.5f) * this.tileSize, (spawnGridPosition.y+0.5f) * this.tileSize);
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

        initialized = true;
    }

    private void HandleMovement()
    {
        string upKey = playerIndex == 1 ? "MoveUp" : "P2MoveUp";
        string downKey = playerIndex == 1 ? "MoveDown" : "P2MoveDown";
        string leftKey = playerIndex == 1 ? "MoveLeft" : "P2MoveLeft";
        string rightKey = playerIndex == 1 ? "MoveRight" : "P2MoveRight";

        Vector2 inputDirection = inputManager.GetVector2(upKey, downKey, leftKey, rightKey);
        if (inputDirection.sqrMagnitude < 0.001f)
        {
            currentVelocity = Vector2.zero;
            return;
        }
        
        Vector2 movementVelocity = inputDirection.normalized * moveSpeed * tileSize;
        Vector2 predictedPosition = rb2d.position + movementVelocity * Time.deltaTime;


        float targetAngle = Mathf.Atan2(inputDirection.y, inputDirection.x) * Mathf.Rad2Deg-90;
        // Debug.Log(targetAngle);

        if (currentVelocity != Vector2.zero)
        {
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            rectTransform.rotation = targetRotation;  
        }
          

        // int newGridX = Mathf.RoundToInt(predictedPosition.x / tileSize);
        // int newGridY = Mathf.RoundToInt(predictedPosition.y / tileSize);
        

        currentVelocity = movementVelocity;
        // if (CanMoveTo(newGridX, newGridY))
        // {
        //     currentVelocity = movementVelocity;
        // }
        // else
        // {
        //     currentVelocity = Vector2.zero;
        // }
    }


    private float attackCooldown = 1f;
    private float lastAttackTime = 0f;
    private void Attack()
    {
        // 这里可以实现攻击逻辑，例如发射子弹等
        string attackKey = playerIndex == 1 ? "Attack" : "P2Attack";
        lastAttackTime -= Time.deltaTime;
        if (inputManager.GetButton(attackKey) && lastAttackTime <= 0f)
        {
            lastAttackTime = attackCooldown;
            Framework.PublishEvent<GameCoreManager.BulletEvent>(new GameCoreManager.BulletEvent(FirePos));
             // 可以在这里添加攻击逻辑，例如实例化子弹等
        }
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

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            // 可以在这里添加死亡逻辑
            
            Framework.PublishEvent<GameCoreManager.PlayerDieEvent>(new GameCoreManager.PlayerDieEvent(this));
            
        }
    }
}
