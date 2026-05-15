using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public abstract class TankEffectHost : MonoBehaviour
{
    private const string SpawnEffectPath = "Assets/Prefabs/Effect/SpawnEffect.prefab";
    private const string DieEffectPath = "Assets/Prefabs/Effect/DieEffect.prefab";
    private const string ShieldEffectPath = "Assets/Prefabs/Effect/ShieldEffect.prefab";

    private static Task<GameObject> sharedSpawnEffectTask;
    private static Task<GameObject> sharedDieEffectTask;
    private static Task<GameObject> sharedShieldEffectTask;
    private static TankEffectRoutineRunner routineRunner;

    private Coroutine activeShieldCoroutine;
    // private Image tankImage;

    protected RectTransform TankRectTransform { get; private set; }
    protected Rigidbody2D TankRigidbody { get; private set; }
    protected BoxCollider2D TankCollider { get; private set; }
    protected float TankTileSize { get; private set; }
    protected bool IsSpawnSequenceActive { get; private set; }
    protected bool IsDeathSequenceTriggered { get; private set; }
    protected bool CanRunTankLogic => !IsSpawnSequenceActive && !IsDeathSequenceTriggered;

    protected void InitializeEffectHost(RectTransform tankRectTransform, Rigidbody2D tankRigidbody, BoxCollider2D tankCollider, float tileSize)
    {
        TankRectTransform = tankRectTransform;
        TankRigidbody = tankRigidbody;
        TankCollider = tankCollider;
        TankTileSize = tileSize;
        // tankImage = GetComponent<Image>();

        _ = GetSharedSpawnEffectTask();
        _ = GetSharedDieEffectTask();
        _ = GetSharedShieldEffectTask();
    }

    protected void BeginSpawnSequence(bool withShield = false, float shieldDuration = 0f, System.Action<bool> onShieldStateChanged = null)
    {
        StartCoroutine(SpawnSequenceRoutine(withShield, shieldDuration, onShieldStateChanged));
    }

    protected void BeginShieldEffect(float shieldDuration, System.Action<bool> onShieldStateChanged = null)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (activeShieldCoroutine != null)
        {
            StopCoroutine(activeShieldCoroutine);
            activeShieldCoroutine = null;
        }

        activeShieldCoroutine = StartCoroutine(TrackedShieldRoutine(shieldDuration, onShieldStateChanged));
    }

    protected void TriggerDieEffect()
    {
        if (IsDeathSequenceTriggered)
        {
            return;
        }

        IsDeathSequenceTriggered = true;

        if (TankRectTransform == null || transform.parent == null)
        {
            return;
        }

        RunDetachedRoutine(PlayDetachedDieEffectRoutine(transform.parent, TankRectTransform.anchoredPosition, TankTileSize));
    }

    private IEnumerator SpawnSequenceRoutine(bool withShield, float shieldDuration, System.Action<bool> onShieldStateChanged)
    {
        IsSpawnSequenceActive = true;
        SetTankPresentationActive(false);

        GameObject spawnEffectPrefab = null;
        yield return WaitForEffectAsset(GetSharedSpawnEffectTask(), prefab => spawnEffectPrefab = prefab);

        if (spawnEffectPrefab != null && TankRectTransform != null && transform.parent != null)
        {
            GameObject effectObject = CreateWorldEffectInstance(
                spawnEffectPrefab,
                transform.parent,
                TankRectTransform.anchoredPosition,
                TankTileSize);

            yield return WaitForEffectToFinish(effectObject);
        }

        SetTankPresentationActive(true);
        IsSpawnSequenceActive = false;

        if (withShield)
        {
            yield return ShieldRoutine(shieldDuration, onShieldStateChanged);
        }
    }

    private IEnumerator ShieldRoutine(float shieldDuration, System.Action<bool> onShieldStateChanged)
    {
        onShieldStateChanged?.Invoke(true);

        GameObject shieldEffectPrefab = null;
        yield return WaitForEffectAsset(GetSharedShieldEffectTask(), prefab => shieldEffectPrefab = prefab);

        GameObject shieldEffectObject = null;
        if (shieldEffectPrefab != null)
        {
            shieldEffectObject = CreateAttachedEffectInstance(
                shieldEffectPrefab,
                transform,
                Vector2.zero,
                TankTileSize * 1.2f);
        }

        float remaining = shieldDuration;
        while (remaining > 0f && this != null)
        {
            remaining -= Time.deltaTime;
            yield return null;
        }

        if (shieldEffectObject != null)
        {
            Destroy(shieldEffectObject);
        }

        onShieldStateChanged?.Invoke(false);
    }

    private IEnumerator TrackedShieldRoutine(float shieldDuration, System.Action<bool> onShieldStateChanged)
    {
        yield return ShieldRoutine(shieldDuration, onShieldStateChanged);
        activeShieldCoroutine = null;
    }

    private IEnumerator PlayDetachedDieEffectRoutine(Transform effectParent, Vector2 effectPosition, float effectSize)
    {
        GameObject dieEffectPrefab = null;
        yield return WaitForEffectAsset(GetSharedDieEffectTask(), prefab => dieEffectPrefab = prefab);

        if (dieEffectPrefab == null || effectParent == null)
        {
            yield break;
        }

        GameObject effectObject = CreateWorldEffectInstance(dieEffectPrefab, effectParent, effectPosition, effectSize);
        yield return WaitForEffectToFinish(effectObject);
    }

    private void SetTankPresentationActive(bool isActive)
    {
        // if (tankImage != null)
        // {
        //     tankImage.enabled = isActive;
        // }

        if (TankCollider != null)
        {
            TankCollider.enabled = isActive;
        }

        if (TankRigidbody != null)
        {
            TankRigidbody.velocity = Vector2.zero;
            TankRigidbody.simulated = isActive;
        }
    }

    private static Task<GameObject> GetSharedSpawnEffectTask()
    {
        sharedSpawnEffectTask ??= ResourceManager.AsycnLoadAddressable<GameObject>(SpawnEffectPath);
        return sharedSpawnEffectTask;
    }

    private static Task<GameObject> GetSharedDieEffectTask()
    {
        sharedDieEffectTask ??= ResourceManager.AsycnLoadAddressable<GameObject>(DieEffectPath);
        return sharedDieEffectTask;
    }

    private static Task<GameObject> GetSharedShieldEffectTask()
    {
        sharedShieldEffectTask ??= ResourceManager.AsycnLoadAddressable<GameObject>(ShieldEffectPath);
        return sharedShieldEffectTask;
    }

    private static IEnumerator WaitForEffectAsset(Task<GameObject> effectTask, System.Action<GameObject> onLoaded)
    {
        if (effectTask == null)
        {
            yield break;
        }

        while (!effectTask.IsCompleted)
        {
            yield return null;
        }

        if (effectTask.IsFaulted)
        {
            Debug.LogException(effectTask.Exception);
            yield break;
        }

        if (effectTask.IsCanceled)
        {
            yield break;
        }

        onLoaded?.Invoke(effectTask.Result);
    }

    private static GameObject CreateWorldEffectInstance(GameObject effectPrefab, Transform parent, Vector2 anchoredPosition, float effectSize)
    {
        GameObject effectObject = Instantiate(effectPrefab);
        effectObject.transform.SetParent(parent, false);
        effectObject.transform.SetAsLastSibling();

        RectTransform effectRect = effectObject.GetComponent<RectTransform>();
        if (effectRect != null)
        {
            effectRect.anchorMin = Vector2.zero;
            effectRect.anchorMax = Vector2.zero;
            effectRect.pivot = new Vector2(0.5f, 0.5f);
            effectRect.anchoredPosition = anchoredPosition;
            effectRect.sizeDelta = new Vector2(effectSize, effectSize);
        }
        else
        {
            effectObject.transform.localPosition = anchoredPosition;
        }

        return effectObject;
    }

    private static GameObject CreateAttachedEffectInstance(GameObject effectPrefab, Transform parent, Vector2 anchoredPosition, float effectSize)
    {
        GameObject effectObject = Instantiate(effectPrefab);
        effectObject.transform.SetParent(parent, false);
        effectObject.transform.SetAsLastSibling();

        RectTransform effectRect = effectObject.GetComponent<RectTransform>();
        if (effectRect != null)
        {
            effectRect.anchorMin = new Vector2(0.5f, 0.5f);
            effectRect.anchorMax = new Vector2(0.5f, 0.5f);
            effectRect.pivot = new Vector2(0.5f, 0.5f);
            effectRect.anchoredPosition = anchoredPosition;
            effectRect.sizeDelta = new Vector2(effectSize, effectSize);
        }
        else
        {
            effectObject.transform.localPosition = anchoredPosition;
        }

        return effectObject;
    }

    private static IEnumerator WaitForEffectToFinish(GameObject effectObject)
    {
        if (effectObject == null)
        {
            yield break;
        }

        Animator animator = effectObject.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            yield return null;

            while (effectObject != null && animator != null)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (!animator.IsInTransition(0) && stateInfo.normalizedTime >= 1f)
                {
                    break;
                }

                yield return null;
            }
        }
        else
        {
            ParticleSystem[] particleSystems = effectObject.GetComponentsInChildren<ParticleSystem>();
            if (particleSystems.Length > 0)
            {
                while (effectObject != null)
                {
                    bool isAlive = false;
                    foreach (ParticleSystem particleSystem in particleSystems)
                    {
                        if (particleSystem != null && particleSystem.IsAlive(true))
                        {
                            isAlive = true;
                            break;
                        }
                    }

                    if (!isAlive)
                    {
                        break;
                    }

                    yield return null;
                }
            }
            else
            {
                yield return null;
            }
        }

        if (effectObject != null)
        {
            Destroy(effectObject);
        }
    }

    private static void RunDetachedRoutine(IEnumerator routine)
    {
        if (routine == null)
        {
            return;
        }

        if (routineRunner == null)
        {
            GameObject runnerObject = new GameObject(nameof(TankEffectRoutineRunner));
            DontDestroyOnLoad(runnerObject);
            routineRunner = runnerObject.AddComponent<TankEffectRoutineRunner>();
        }

        routineRunner.StartCoroutine(routine);
    }

    private sealed class TankEffectRoutineRunner : MonoBehaviour
    {
    }
}
