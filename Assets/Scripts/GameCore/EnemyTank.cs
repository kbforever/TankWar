using UnityEngine;
using GameFramework;
using UnityEngine.UI;

public enum GameItemType
{
    FreezeEnemies = 0,
    PlayerShield = 1,
    PlayerLife = 2,
    PlayerPower = 3,
    DestroyAllEnemies = 4,
    BaseInvincible = 5
}

public enum EnemyTankType
{
    Basic,
    Fast,
    Strong,
    Heavy
}

public class EnemyTank : TankEffectHost, ITakeDamage
{
    private const int BonusExtraLife = 1;

    [Header("Enemy Tank")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float changeDirectionTime = 2f;
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private EnemyTankType tankType = EnemyTankType.Basic;
    [SerializeField] private Sprite[] normalStateSprites;
    [SerializeField] private Sprite[] bonusStateSprites;

    public int Id;
    public EnemyTankType TankType => tankType;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool HasBonusItem => hasBonusItem;
    public GameItemType BonusItemType => bonusItemType;

    private RectTransform rectTransform;
    private GameFramework.GameFramework Framework => GameFramework.GameFramework.Instance;
    private Rigidbody2D rb2d;
    private BoxCollider2D boxCollider;
    private Image tankImage;
    private Vector2 currentPosition;
    private Vector2 currentVelocity;
    private float tileSize;
    private Vector2 currentDirection;
    private float directionTimer;
    private bool initialized;
    private int currentHealth;
    private GameObject FirePos;
    private bool hasBonusItem;
    private GameItemType bonusItemType;

    private bool shouldMove;
    private Vector2 boxSize;
    private float attackCooldown = 1f;
    private float lastAttackTime = 0f;

    private void Update()
    {
        if (!initialized || !CanRunTankLogic || Framework.GetFeature<GameCoreManager>()?.AreEnemiesFrozen() == true)
        {
            return;
        }

        directionTimer -= Time.deltaTime;
        if (directionTimer <= 0f)
        {
            ChangeDirection();
        }

        Attack();
    }

    public override bool Equals(object other)
    {
        EnemyTank obj = other as EnemyTank;
        return obj != null && obj.Id == Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    private void FixedUpdate()
    {
        if (Framework.CurrentState != GameState.Playing || !initialized || rb2d == null || !CanRunTankLogic)
        {
            return;
        }

        if (Framework.GetFeature<GameCoreManager>()?.AreEnemiesFrozen() == true)
        {
            rb2d.velocity = Vector2.zero;
            return;
        }

        float targetAngle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg - 90f;
        if (currentDirection != Vector2.zero)
        {
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
            rectTransform.rotation = targetRotation;
        }

        CheckMove();
        if (shouldMove)
        {
            currentVelocity = moveSpeed * tileSize * currentDirection;
            rb2d.velocity = currentVelocity;
        }
        else
        {
            rb2d.velocity = Vector2.zero;
        }
    }

    private void CheckMove()
    {
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            (Vector2)transform.position + currentDirection * boxSize * 0.05f,
            boxSize,
            0f,
            currentDirection,
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

    public void Initialize(float tileSize, Vector2Int spawnGridPosition, Color tankColor, int id, int restoredHealth = -1, bool restoredHasItem = false, GameItemType restoredItemType = GameItemType.FreezeEnemies)
    {
        SetupCoreComponents(tileSize, tankColor, id, restoredHealth, restoredHasItem, restoredItemType);

        currentPosition = new Vector2((spawnGridPosition.x + 0.5f) * this.tileSize, (spawnGridPosition.y + 0.5f) * this.tileSize);
        rectTransform.anchoredPosition = currentPosition;
        rectTransform.sizeDelta = new Vector2(this.tileSize, this.tileSize);
        boxSize = rectTransform.sizeDelta;
        rb2d.position = currentPosition;

        ChangeDirection();
        initialized = true;
        BeginSpawnSequence();
    }

    public void Initialize(float tileSize, Vector2 pos, Color tankColor, int id, int restoredHealth = -1, bool restoredHasItem = false, GameItemType restoredItemType = GameItemType.FreezeEnemies)
    {
        SetupCoreComponents(tileSize, tankColor, id, restoredHealth, restoredHasItem, restoredItemType);

        currentPosition = pos;
        rectTransform.anchoredPosition = currentPosition;
        rectTransform.sizeDelta = new Vector2(this.tileSize, this.tileSize);
        boxSize = new Vector2(this.tileSize, this.tileSize);
        rb2d.position = currentPosition;

        ChangeDirection();
        initialized = true;
        BeginSpawnSequence();
    }

    private void SetupCoreComponents(float tileSize, Color tankColor, int id, int restoredHealth, bool restoredHasItem, GameItemType restoredItemType)
    {
        rectTransform = GetComponent<RectTransform>();
        rb2d = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        tankImage = GetComponent<UnityEngine.UI.Image>();
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
        Id = id;
        currentVelocity = Vector2.zero;
        lastAttackTime = 0f;
        hasBonusItem = restoredHasItem;
        bonusItemType = restoredItemType;
        currentHealth = ResolveInitialHealth(restoredHealth, restoredHasItem);

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

        InitializeEffectHost(rectTransform, rb2d, boxCollider, this.tileSize);
        RefreshVisualState();
    }

    private void Attack()
    {
        lastAttackTime -= Time.deltaTime;
        if (lastAttackTime <= 0f)
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

    private void ChangeDirection()
    {
        int dir = Random.Range(0, 4);
        switch (dir)
        {
            case 0:
                currentDirection = Vector2.up;
                break;
            case 1:
                currentDirection = Vector2.down;
                break;
            case 2:
                currentDirection = Vector2.left;
                break;
            default:
                currentDirection = Vector2.right;
                break;
        }

        changeDirectionTime = Random.Range(1f, 3f);
        directionTimer = changeDirectionTime;
    }

    public void TakeDamage(int damage)
    {
        if (IsDeathSequenceTriggered)
        {
            return;
        }

        currentHealth -= damage;

        if (hasBonusItem && currentHealth > 0 && currentHealth <= maxHealth)
        {
            hasBonusItem = false;
            currentHealth = Mathf.Clamp(currentHealth, 1, maxHealth);
            GameCoreManager manager = Framework.GetFeature<GameCoreManager>();
            if (manager != null && rectTransform != null)
            {
                manager.SpawnDroppedItem(rectTransform.anchoredPosition, bonusItemType);
            }
        }

        RefreshVisualState();
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            TriggerDieEffect();
            Framework.PublishEvent(new GameCoreManager.EnemyDieEvent(this));
        }
    }

    public void ConfigureBonusItem(bool value, GameItemType itemType)
    {
        hasBonusItem = value;
        bonusItemType = itemType;
        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        if (tankImage == null)
        {
            return;
        }

        tankImage.color = Color.white;
        Sprite sprite = GetCurrentSprite();
        if (sprite != null)
        {
            tankImage.sprite = sprite;
        }

        UpdateHeavyScale();
    }

    private Sprite GetCurrentSprite()
    {
        if (hasBonusItem && currentHealth > maxHealth && bonusStateSprites != null && bonusStateSprites.Length > 0)
        {
            return bonusStateSprites[0];
        }

        if (normalStateSprites == null || normalStateSprites.Length == 0)
        {
            return null;
        }

        int spriteIndex = tankType == EnemyTankType.Heavy
            ? Mathf.Clamp(maxHealth - currentHealth, 0, normalStateSprites.Length - 1)
            : Mathf.Clamp(maxHealth - currentHealth, 0, normalStateSprites.Length - 1);

        return normalStateSprites[spriteIndex];
    }

    private void UpdateHeavyScale()
    {
        if (rectTransform == null)
        {
            return;
        }

        float scale = 1f;
        if (tankType == EnemyTankType.Heavy && maxHealth > 0)
        {
            if (hasBonusItem && currentHealth > maxHealth)
            {
                scale = 1.14f;
            }
            else
            {
                float ratio = Mathf.Clamp01((float)currentHealth / maxHealth);
                scale = ratio switch
                {
                    > 0.66f => 1.1f,
                    > 0.33f => 1.02f,
                    _ => 0.92f
                };
            }
        }

        rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    private int ResolveInitialHealth(int restoredHealth, bool restoredHasItem)
    {
        int totalHealth = maxHealth + (restoredHasItem ? BonusExtraLife : 0);
        if (restoredHealth > 0)
        {
            return Mathf.Clamp(restoredHealth, 1, totalHealth);
        }

        return totalHealth;
    }
}
