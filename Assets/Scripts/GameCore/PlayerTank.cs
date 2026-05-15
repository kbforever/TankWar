using UnityEngine;
using LevelGeneration;
using UnityEngine.UI;

public class PlayerTank : TankEffectHost, ITakeDamage
{
    [Header("Player Tank")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private Color tankColor = Color.green;
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private Sprite[] powerLevelSprites;

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
    private float lastAttackTime = 0f;
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
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
            rectTransform.rotation = targetRotation;
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
        foreach (var hit in hits)
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
            tankImage.color = Color.white;
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
}
