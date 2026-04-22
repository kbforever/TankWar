using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework
{
    public enum GameState
    {
        None,
        Boot,
        Loading,
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    public abstract class GameEvent { }

    public sealed class GameStateChangedEvent : GameEvent
    {
        public GameState PreviousState { get; }
        public GameState NextState { get; }

        public GameStateChangedEvent(GameState previousState, GameState nextState)
        {
            PreviousState = previousState;
            NextState = nextState;
        }
    }

    public sealed class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> listeners = new Dictionary<Type, List<Delegate>>();

        public void Subscribe<T>(Action<T> callback) where T : GameEvent
        {
            var type = typeof(T);
            if (!listeners.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                listeners[type] = list;
            }

            if (!list.Contains(callback))
            {
                list.Add(callback);
            }
        }

        public void Unsubscribe<T>(Action<T> callback) where T : GameEvent
        {
            var type = typeof(T);
            if (listeners.TryGetValue(type, out var list))
            {
                list.Remove(callback);
                if (list.Count == 0)
                {
                    listeners.Remove(type);
                }
            }
        }

        public void Publish<T>(T gameEvent) where T : GameEvent
        {
            var type = typeof(T);
            if (!listeners.TryGetValue(type, out var list)) return;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is Action<T> callback)
                {
                    callback(gameEvent);
                }
            }
        }

        public void Clear()
        {
            listeners.Clear();
        }
    }

    [DisallowMultipleComponent]
    public class GameFramework : MonoBehaviour
    {
        public static GameFramework Instance { get; private set; }

        [Header("框架配置")]
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private bool autoRegisterFeatures = true;
        [SerializeField] private List<IGameFeature> initialFeatures = new List<IGameFeature>();


        private readonly List<IGameFeature> features = new List<IGameFeature>();
        private readonly EventBus eventBus = new EventBus();
        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        public IReadOnlyList<IGameFeature> Features => features;
        public EventBus Events => eventBus;
        public GameState CurrentState { get; private set; } = GameState.None;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (autoRegisterFeatures)
            {
                RegisterSceneFeatures();
            }

            foreach (var feature in features)
            {
                if (feature == null) continue;
                feature.Initialize();
            }

            ChangeState(GameState.Boot);
        }

        private void Start()
        {
            if (CurrentState == GameState.Boot)
            {
                ChangeState(GameState.MainMenu);
            }
        }

        private void Update()
        {
            foreach (var feature in features)
            {
                if (feature == null || !feature.IsActive) continue;
                feature.FeatureUpdate();
            }
        }

        private void FixedUpdate()
        {
            foreach (var feature in features)
            {
                if (feature == null || !feature.IsActive) continue;
                feature.FeatureFixedUpdate();
            }
        }

        private void LateUpdate()
        {
            foreach (var feature in features)
            {
                if (feature == null || !feature.IsActive) continue;
                feature.FeatureLateUpdate();
            }
        }

        private void OnDestroy()
        {
            foreach (var feature in features)
            {
                if (feature == null) continue;
                feature.Shutdown();
            }

            eventBus.Clear();
            services.Clear();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RegisterFeature(IGameFeature feature)
        {
            if (feature == null || features.Contains(feature)) return;
            features.Add(feature);
            feature.Initialize();
        }
        
        public T GetFeature<T>() where T : class, IGameFeature
        {
            foreach (var feature in features)
            {
                if (feature is T typedFeature)
                {
                    return typedFeature;
                }
            }
            return null;
        }
        public void UnregisterFeature(IGameFeature feature)
        {
            if (feature == null) return;
            features.Remove(feature);
            feature.Shutdown();
        }

        public void ChangeState(GameState nextState)
        {
            if (CurrentState == nextState) return;
            var previousState = CurrentState;
            CurrentState = nextState;
            eventBus.Publish(new GameStateChangedEvent(previousState, nextState));
            // foreach (var feature in features)
            // {
            //     if (feature == null) continue;
            //     feature.OnGameStateChanged(previousState, nextState);
            // }
        }

        public void PublishEvent<T>(T gameEvent) where T : GameEvent
        {
            eventBus.Publish(gameEvent);
        }

        public void SubscribeEvent<T>(Action<T> callback) where T : GameEvent
        {
            eventBus.Subscribe(callback);
        }

        public void UnsubscribeEvent<T>(Action<T> callback) where T : GameEvent
        {
            eventBus.Unsubscribe(callback);
        }

        public void RegisterService<T>(T service) where T : class
        {
            if (service == null) return;
            services[typeof(T)] = service;
        }

        public T GetService<T>() where T : class
        {
            if (services.TryGetValue(typeof(T), out var service))
            {
                return service as T;
            }
            return null;
        }

        private void RegisterSceneFeatures()
        {
            features.Clear();
            foreach (var feature in initialFeatures)
            {
                if (feature != null && !features.Contains(feature))
                {
                    features.Add(feature);
                }
            }

            // Note: Since GameFeature no longer inherits from MonoBehaviour,
            // automatic scene feature registration is removed.
            // Features must be manually added to initialFeatures or registered via RegisterFeature.
        }
    }
}
