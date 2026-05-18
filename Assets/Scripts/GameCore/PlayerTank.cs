using UnityEngine;
using UnityEngine.UI;
using LevelGeneration;

public enum PlayerTankType
{
    Standard = 0,
    Rapid = 1,
    Assault = 2
}

public class PlayerTank : TankEffectHost, ITakeDamage
{
    [Header("Player Tank")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private Color tankColor = Color.green;
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private Sprite[] powerLevelSprites;
    [SerializeField] private PlayerTankType playerTankType = PlayerTankType.Standard;

    private const float ShieldDuration = 1.5f;
    private const float BonusShieldDuration = 5f;
    private const int MaxPowerLevel = 2;

    private GameFramework.GameFramework Framework => GameFramework.GameFramework.Instance;
    private RectTransform rectTransform;
    private Rigidbody2D rb2d;
    private BoxCollider2D boxCollider;
    private Image tankImage;
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
    private bool isShielded;
    private bool IsAttacking;
    private bool shouldMove;
    private Vector2 boxSize;
    private float attackCooldown = 1f;
    private float lastAttackTime;
    private int powerLevel;

    private void Update()
    {
        if (!initialized || inputManager == null || !CanRunTankLogic)
        {
            return;
        }

        HandleMovement();
        Attack();
    }

    private void FixedUpdate()
    {
        if (!initialized || rb2d == null || !CanRunTankLogic)
        {
            return;
        }

        float targetAngle = Mathf.Atan2(currentVelocity.y, currentVelocity.x) * Mathf.Rad2Deg - 90f;
        if (currentVelocity != Vector2.zero)
        {
            rectTransform.rotation = Quaternion.Euler(0f, 0f, targetAngle);
        }

        CheckMove();

        if (shouldMove)
        {
            rb2d.velocity = currentVelocity * moveSpeed * tileSize;
        }
        else
        {
            rb2d.velocity = Vector2.zero;
        }
    }

    private void CheckMove()
    {
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            (Vector2)transform.position + currentVelocity * boxSize * 0.05f,
            boxSize,
            0f,
            currentVelocity,
            moveSpeed * tileSize * Time.fixedDeltaTime);

        shouldMove = true;
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.collider.CompareTag("Bullet") || hit.collider.gameObject == gameObject)
            {
                continue;
            }

            if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
            {
                shouldMove = false;
            }
        }
    }

    public void Initialize(float tileSize, Vector2Int spawnGridPosition, Vector2Int gridSize, Color tankColor, int playerIndex = 1, LevelData levelData = null)
    {
        inputManager = Framework?.GetFeature<InputManager>();
        if (inputManager == null)
        {
            Debug.LogWarning($"PlayerTank{playerIndex}: InputManager not found. Tank input will be disabled.");
        }

        rectTransform = GetComponent<RectTransform>();
        rb2d = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        tankImage = GetComponent<Image>();
        FirePos = transform.Find(nameof(FirePos)).gameObject;

        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }

        if (rb2d == null)
        {
            rb2d = gameObject.AddComponent<Rigidbody2D>();
        }

        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        this.tileSize = Mathf.Max(1f, tileSize);
        this.gridSize = gridSize;
        this.playerIndex = playerIndex;
        this.levelData = levelData;
        currentHealth = maxHealth;
        isShielded = false;
        IsAttacking = false;
        lastAttackTime = 0f;
        currentVelocity = Vector2.zero;
        powerLevel = 0;

        rb2d.gravityScale = 0f;
        rb2d.freezeRotation = true;
        rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb2d.velocity = Vector2.zero;

        boxCollider.size = new Vector2(this.tileSize, this.tileSize);
        boxCollider.offset = Vector2.zero;

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        currentPosition = new Vector2((spawnGridPosition.x + 0.5f) * this.tileSize, (spawnGridPosition.y + 0.5f) * this.tileSize);
        rectTransform.anchoredPosition = currentPosition;
        rectTransform.sizeDelta = new Vector2(this.tileSize, this.tileSize);
        boxSize = new Vector2(this.tileSize, this.tileSize);
        rb2d.position = currentPosition;

        inputManager.AcitonByName["Player" + playerIndex].FindAction("Attack").started += ctx => IsAttacking = true;
        inputManager.AcitonByName["Player" + playerIndex].FindAction("Attack").canceled += ctx => IsAttacking = false;

        InitializeEffectHost(rectTransform, rb2d, boxCollider, this.tileSize);
        RefreshVisualState();

        initialized = true;
        BeginSpawnSequence(true, ShieldDuration, SetShielded);
    }

    private void HandleMovement()
    {
        Vector2 inputDirection = inputManager.AcitonByName["Player" + playerIndex].FindAction("Move").ReadValue<Vector2>();
        if (inputDirection.sqrMagnitude < 0.001f)
        {
            currentVelocity = Vector2.zero;
            return;
        }

        currentVelocity = inputDirection;
    }

    private void Attack()
    {
        lastAttackTime -= Time.deltaTime;
        if (IsAttacking && lastAttackTime <= 0f)
        {
            lastAttackTime = attackCooldown;
            Framework.PublishEvent(new GameCoreManager.BulletEvent(FirePos));
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider == null)
        {
            return;
        }

        currentVelocity = Vector2.zero;
        if (rb2d != null)
        {
            rb2d.velocity = Vector2.zero;
        }
    }

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsShielded => isShielded;
    public int PowerLevel => powerLevel;
    public bool CanBreakSteel => powerLevel >= 2;
    public PlayerTankType TankType => playerTankType;

    public static string GetDisplayName(PlayerTankType type)
    {
        return type switch
        {
            PlayerTankType.Rapid => "\u901f\u5c04\u578b",
            PlayerTankType.Assault => "\u7a81\u51fb\u578b",
            _ => "\u6807\u51c6\u578b"
        };
    }

    public static string GetShortName(PlayerTankType type)
    {
        return type switch
        {
            PlayerTankType.Rapid => "\u901f\u5c04",
            PlayerTankType.Assault => "\u7a81\u51fb",
            _ => "\u6807\u51c6"
        };
    }

    public static int GetEnergyCost(PlayerTankType type)
    {
        return type switch
        {
            PlayerTankType.Rapid => 4,
            PlayerTankType.Assault => 5,
            _ => 3
        };
    }

    public static Color GetTypeColor(PlayerTankType type)
    {
        return type switch
        {
            PlayerTankType.Rapid => new Color(0.8f, 0.95f, 1f, 1f),
            PlayerTankType.Assault => new Color(1f, 0.88f, 0.7f, 1f),
            _ => Color.white
        };
    }

    public static string GetTypeDescription(PlayerTankType type)
    {
        return type switch
        {
            PlayerTankType.Rapid => "\u5c04\u901f\u5feb\uff0c\u673a\u52a8\u66f4\u5f3a",
            PlayerTankType.Assault => "\u8d39\u7528\u66f4\u9ad8\uff0c\u706b\u529b\u66f4\u7a33",
            _ => "\u8d39\u7528\u8f83\u4f4e\uff0c\u5c5e\u6027\u5747\u8861"
        };
    }

    public void ConfigureType(PlayerTankType type)
    {
        playerTankType = type;
        ApplyPowerLevel(powerLevel);
    }

    public void SetShielded(bool shielded)
    {
        isShielded = shielded;
    }

    public void RestoreState(int restoredHealth, int restoredPowerLevel)
    {
        currentHealth = Mathf.Clamp(restoredHealth, 1, maxHealth);
        ApplyPowerLevel(restoredPowerLevel);
    }

    public void GrantLife()
    {
        currentHealth = Mathf.Min(currentHealth + 1, maxHealth);
    }

    public void GrantShield(float duration = BonusShieldDuration)
    {
        BeginShieldEffect(duration, SetShielded);
    }

    public void UpgradePower()
    {
        ApplyPowerLevel(powerLevel + 1);
    }

    public void TakeDamage(int damage)
    {
        if (isShielded || IsDeathSequenceTriggered)
        {
            return;
        }

        if (powerLevel > 0)
        {
            ApplyPowerLevel(powerLevel - 1);
            currentHealth = maxHealth;
            return;
        }

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            TriggerDieEffect();
            Framework.PublishEvent(new GameCoreManager.PlayerDieEvent(this));
        }
    }

    private void ApplyPowerLevel(int value)
    {
        powerLevel = Mathf.Clamp(value, 0, MaxPowerLevel);

        switch (playerTankType)
        {
            case PlayerTankType.Rapid:
                ApplyRapidStats();
                break;
            case PlayerTankType.Assault:
                ApplyAssaultStats();
                break;
            default:
                ApplyStandardStats();
                break;
        }

        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        if (tankImage == null)
        {
            return;
        }

        Sprite sprite = GetCurrentPowerLevelSprite();
        if (sprite != null)
        {
            tankImage.sprite = sprite;
            tankImage.color = GetTypeColor(playerTankType);
            return;
        }

        tankImage.color = tankColor;
    }

    private Sprite GetCurrentPowerLevelSprite()
    {
        if (powerLevelSprites == null || powerLevelSprites.Length == 0)
        {
            return null;
        }

        int spriteIndex = Mathf.Clamp(powerLevel, 0, powerLevelSprites.Length - 1);
        return powerLevelSprites[spriteIndex];
    }

    private void ApplyStandardStats()
    {
        switch (powerLevel)
        {
            case 0:
                attackCooldown = 1f;
                moveSpeed = 3.5f;
                break;
            case 1:
                attackCooldown = 0.65f;
                moveSpeed = 3.75f;
                break;
            default:
                attackCooldown = 0.4f;
                moveSpeed = 4f;
                break;
        }
    }

    private void ApplyRapidStats()
    {
        switch (powerLevel)
        {
            case 0:
                attackCooldown = 0.85f;
                moveSpeed = 3.9f;
                break;
            case 1:
                attackCooldown = 0.55f;
                moveSpeed = 4.15f;
                break;
            default:
                attackCooldown = 0.32f;
                moveSpeed = 4.35f;
                break;
        }
    }

    private void ApplyAssaultStats()
    {
        switch (powerLevel)
        {
            case 0:
                attackCooldown = 1.1f;
                moveSpeed = 3.2f;
                break;
            case 1:
                attackCooldown = 0.75f;
                moveSpeed = 3.45f;
                break;
            default:
                attackCooldown = 0.45f;
                moveSpeed = 3.75f;
                break;
        }
    }
}
