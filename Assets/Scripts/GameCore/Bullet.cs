using UnityEngine;
using LevelGeneration;


public class Bullet : MonoBehaviour
{
    float moveSpeed = 500f;
    private Rigidbody2D rgb2D;

    public Vector2 movedir = Vector2.zero;

    private string targetTag = string.Empty;
    public string selfTag = string.Empty;
    public bool canBreakSteel;
    void Awake()
    {
    }

    void Start()
    {
        rgb2D = this.GetComponent<Rigidbody2D>();
        if (this.selfTag == "Player") targetTag="Enemy";
        else if(this.selfTag=="Enemy") targetTag = "Player";
        else Debug.LogError($"{this.name}Has Else Tag!!");
       
    }
    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        // time+=Time.deltaTime;
        

        // if (time > SurviveTime)
        // {
        //     Destroy(this.gameObject);
        // }
    }

    void FixedUpdate()
    {
        rgb2D.velocity = movedir*moveSpeed;
    }


    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(selfTag))
        {
            return;
        }

        RuntimeLevelTile runtimeTile = collision.GetComponent<RuntimeLevelTile>();
        if (runtimeTile != null && runtimeTile.TileType == LevelTileType.Steel)
        {
            if (canBreakSteel)
            {
                Destroy(collision.gameObject);
            }

            Destroy(this.gameObject);
            return;
        }

        if (collision.CompareTag(targetTag))
        {
            collision.gameObject.GetComponent<ITakeDamage>()?.TakeDamage(1);
        }

        if (!collision.CompareTag("Water"))
        {
            Destroy(this.gameObject);
        }
    }


    
}
