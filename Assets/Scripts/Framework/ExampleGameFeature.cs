using System;
using UnityEngine;
using GameFramework;

[DisallowMultipleComponent]
public class ExampleGameFeature : MonoBehaviour, IGameFeature
{
    [Header("2D 示例：玩家移动")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Rigidbody2D playerRigidbody;
    [SerializeField] private Transform playerTransform;

    private Vector2 moveInput;

    public bool IsActive { get; private set; }

    protected GameFramework.GameFramework Framework => GameFramework.GameFramework.Instance;
    protected EventBus Events => Framework?.Events;

    public void Initialize()
    {
        IsActive = true;
        if (playerTransform == null)
        {
            playerTransform = transform;
        }

        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponent<Rigidbody2D>();
        }

        Subscribe<GameStateChangedEvent>(OnStateChanged);
    }

    public void FeatureUpdate()
    {
        if (!IsActive) return;
        if (Framework == null || Framework.CurrentState != GameState.Playing) return;

        ReadInput();
        if (playerRigidbody == null)
        {
            playerTransform.Translate(moveInput * (moveSpeed * Time.deltaTime));
            if (moveInput != Vector2.zero)
            {
                Publish(new PlayerMovedEvent(playerTransform.position, moveInput));
            }
        }
    }

    public void FeatureFixedUpdate()
    {
        if (!IsActive) return;
        if (Framework == null || Framework.CurrentState != GameState.Playing) return;

        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = moveInput * moveSpeed;
        }
    }

    public void FeatureLateUpdate() { }

    public void OnGameStateChanged(GameState previousState, GameState nextState)
    {
        enabled = nextState == GameState.Playing;
    }

    public void Shutdown()
    {
        IsActive = false;
        Unsubscribe<GameStateChangedEvent>(OnStateChanged);
    }

    private void ReadInput()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }
    }

    private void OnStateChanged(GameStateChangedEvent stateEvent)
    {
        enabled = stateEvent.NextState == GameState.Playing;
        if (stateEvent.NextState == GameState.Playing)
        {
            Debug.Log("ExampleGameFeature: 进入 Playing 状态，开始接收输入。");
        }
    }

    protected void Publish<T>(T gameEvent) where T : GameEvent
    {
        Framework?.PublishEvent(gameEvent);
    }

    protected void Subscribe<T>(Action<T> callback) where T : GameEvent
    {
        Framework?.SubscribeEvent(callback);
    }

    protected void Unsubscribe<T>(Action<T> callback) where T : GameEvent
    {
        Framework?.UnsubscribeEvent(callback);
    }

    public sealed class PlayerMovedEvent : GameEvent
    {
        public Vector2 Position { get; }
        public Vector2 Direction { get; }

        public PlayerMovedEvent(Vector2 position, Vector2 direction)
        {
            Position = position;
            Direction = direction;
        }
    }
}
