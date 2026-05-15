using UnityEngine;
using UnityEngine.UI;

public class GameItem : MonoBehaviour
{
    [SerializeField] private GameItemType itemType;
    [SerializeField] private Sprite[] itemSprites;

    private RectTransform rectTransform;
    private BoxCollider2D boxCollider;
    private Image image;

    public GameItemType ItemType => itemType;

    public void Initialize(GameItemType type, float tileSize, Vector2 anchoredPosition, Sprite sprite = null)
    {
        itemType = type;
        rectTransform = GetComponent<RectTransform>();
        boxCollider = GetComponent<BoxCollider2D>();
        image = GetComponent<Image>();

        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }

        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(tileSize, tileSize);

        boxCollider.isTrigger = true;
        boxCollider.size = new Vector2(tileSize * 0.75f, tileSize * 0.75f);

        if (image != null)
        {
            image.color = Color.white;
            image.sprite = sprite != null ? sprite : GetItemSprite(type);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerTank playerTank = collision.GetComponent<PlayerTank>();
        if (playerTank == null)
        {
            return;
        }

        GameCoreManager manager = GameFramework.GameFramework.Instance?.GetFeature<GameCoreManager>();
        manager?.ApplyItem(playerTank, itemType);
        manager?.OnItemPicked(this);
    }

    private Sprite GetItemSprite(GameItemType type)
    {
        if (itemSprites == null || itemSprites.Length == 0)
        {
            return null;
        }

        int spriteIndex = Mathf.Clamp((int)type, 0, itemSprites.Length - 1);
        return itemSprites[spriteIndex];
    }
}
