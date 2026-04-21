using System;
using UnityEngine;

namespace GameFramework
{
    public interface IGameFeature
    {
        bool IsActive { get; }

        void Initialize();
        void FeatureUpdate();
        void FeatureFixedUpdate();
        void FeatureLateUpdate();
        void OnGameStateChanged(GameState previousState, GameState nextState);
        void Shutdown();
    }
}
