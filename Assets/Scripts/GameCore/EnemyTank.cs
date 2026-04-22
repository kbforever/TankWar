using UnityEngine;
using LevelGeneration;
using Unity.VisualScripting;


public enum EnemyTankType
{
    Basic,
    Fast,
    Strong
}


public class EnemyTank : MonoBehaviour,ITakeDamage
{
    [Header("Enemy Tank")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private Color tankColor = Color.red;
    [SerializeField] private float changeDirectionTime = 2f;
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private EnemyTankType tankType = EnemyTankType.Basic;



    public EnemyTankType TankType => tankType;
    private RectTransform rectTransform;

    private GameFramework.GameFramework Framework=> GameFramework.GameFramework.Instance;
    private Rigidbody2D rb2d;
    private BoxCollider2D boxCollider;
    private Vector2 currentPosition;
    private Vector2 currentVelocity;
  
    private float tileSize;
    private Vector2 currentDirection;
    private float directionTimer;
    private bool initialized;
    private int currentHealth;

    private GameObject FirePos;

    private void Awake()
    {
        
    }

    private void Update()
    {
        if (!initialized) return;
        HandleMovement();
        Attack();
    }

    private void FixedUpdate()
    {
        if (!initialized || rb2d == null) return;
        rb2d.velocity = currentVelocity;
        currentPosition = rb2d.position;
    
    }

    public void Initialize(float tileSize, Vector2Int spawnGridPosition, Color tankColor)
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

        this.currentHealth = this.maxHealth;

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
            boxCollider.offset = new Vector2(0, 0);
        }

        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
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

        // 初始随机方向
        ChangeDirection();

        initialized = true;
    }


     public void Initialize(float tileSize, Vector2 pos,Color tankColor)
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

        this.currentHealth = this.maxHealth;

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
            boxCollider.offset = new Vector2(0, 0);
        }

        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        currentPosition = pos;
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

        


        float targetAngle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg-90;
        if (currentVelocity != Vector2.zero)
        {
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            rectTransform.rotation = targetRotation;  
        }

        currentVelocity = desiredVelocity;
      
    }

    private float attackCooldown = 1f;
    private float lastAttackTime = 0f;
    private void Attack()
    {
        // 这里可以实现攻击逻辑，例如发射子弹等
        
        lastAttackTime -= Time.deltaTime;
        if (lastAttackTime <= 0f)
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
        changeDirectionTime = Random.Range(1f, 3f);
        directionTimer = changeDirectionTime;
    }



    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            // 可以在这里添加死亡逻辑
            
            Framework.PublishEvent<GameCoreManager.EnemyDieEvent>(new GameCoreManager.EnemyDieEvent(this));
            
        }
    }


}