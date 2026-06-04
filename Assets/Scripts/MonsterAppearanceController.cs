using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class MonsterAppearanceController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private PlayerPanicController panicController;
    [SerializeField] private bool autoFindReferences = true;

    [Header("Spawn Interval (seconds)")] [SerializeField, Min(0f)]
    private float minSpawnInterval = 8f;

    [SerializeField, Min(0f)] private float maxSpawnInterval = 12f;

    [Header("Distance")] [SerializeField, Min(0f)]
    private float closeDistance = 2f;

    [SerializeField, Min(0f)] private float equalDistanceTolerance = 0.25f;

    [Header("State")] [SerializeField] private bool hideMonstersOnAwake = true;

    [Header("Idle Animations")] [SerializeField]
    private IdleMonsterAnimation[] idleAnimations;

    [Header("One Shot Animations")] [SerializeField]
    private OneShotMonsterAnimation[] oneShotAnimations;

    private Coroutine routine;
    private MonsterAnimation activeAnimation;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();

        if (hideMonstersOnAwake)
        {
            HideAllAnimations();
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
        ScheduleOneShotAnimations(Time.time);
        routine = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (activeAnimation != null)
        {
            activeAnimation.Hide();
            activeAnimation = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));

            ResolveReferences();
            if (TryPickCandidate(out MonsterAnimation animation))
            {
                yield return PlayAnimation(animation);
            }
        }
    }

    private IEnumerator PlayAnimation(MonsterAnimation animation)
    {
        activeAnimation = animation;
        animation.Show();

        if (animation is IdleMonsterAnimation idleAnimation)
        {
            float duration = idleAnimation.RollVisibleDuration();
            float elapsed = 0f;

            while (elapsed < duration && idleAnimation.CanPlay(GetPanic()) && !IsPlayerClose(idleAnimation))
            {
                idleAnimation.TickLoop();
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        else if (animation is OneShotMonsterAnimation oneShotAnimation)
        {
            float duration = oneShotAnimation.GetDuration();
            yield return new WaitForSeconds(duration);
            oneShotAnimation.ScheduleNext(Time.time);
        }

        animation.Hide();
        activeAnimation = null;
    }

    private bool TryPickCandidate(out MonsterAnimation pickedAnimation)
    {
        Candidate bestCandidate = default;
        bool hasCandidate = false;
        int equalCandidateCount = 0;
        float panic = GetPanic();

        if (idleAnimations != null)
        {
            for (int i = 0; i < idleAnimations.Length; i++)
            {
                IdleMonsterAnimation animation = idleAnimations[i];
                if (animation != null && animation.CanPlay(panic))
                {
                    ConsiderCandidate(animation, ref bestCandidate, ref hasCandidate, ref equalCandidateCount);
                }
            }
        }

        if (oneShotAnimations != null)
        {
            for (int i = 0; i < oneShotAnimations.Length; i++)
            {
                OneShotMonsterAnimation animation = oneShotAnimations[i];
                if (animation != null && animation.CanPlay(Time.time))
                {
                    ConsiderCandidate(animation, ref bestCandidate, ref hasCandidate, ref equalCandidateCount);
                }
            }
        }

        pickedAnimation = hasCandidate ? bestCandidate.Animation : null;
        return pickedAnimation != null;
    }

    private void ConsiderCandidate(
        MonsterAnimation animation,
        ref Candidate bestCandidate,
        ref bool hasCandidate,
        ref int equalCandidateCount)
    {
        float distance = GetDistanceToPlayer(animation.DistancePoint);

        if (!hasCandidate || distance > bestCandidate.Distance + equalDistanceTolerance)
        {
            bestCandidate = new Candidate(animation, distance);
            hasCandidate = true;
            equalCandidateCount = 1;
            return;
        }

        if (Mathf.Abs(distance - bestCandidate.Distance) <= equalDistanceTolerance)
        {
            equalCandidateCount++;
            if (UnityEngine.Random.Range(0, equalCandidateCount) == 0)
            {
                bestCandidate = new Candidate(animation, distance);
            }
        }
    }

    private float GetDistanceToPlayer(Transform point)
    {
        if (player == null || point == null)
        {
            return 0f;
        }

        return Vector3.Distance(point.position, player.position);
    }

    private bool IsPlayerClose(MonsterAnimation animation)
    {
        if (player == null || animation.DistancePoint == null)
        {
            return false;
        }

        float distance = animation.GetCloseDistance(closeDistance);
        return distance > 0f && GetDistanceToPlayer(animation.DistancePoint) <= distance;
    }

    private float GetPanic()
    {
        ResolveReferences();
        return panicController != null ? panicController.Panic : 0f;
    }

    private void ResolveReferences()
    {
        if (!autoFindReferences)
        {
            return;
        }

        if (player == null)
        {
            FirstPersonController controller = FindAnyObjectByType<FirstPersonController>();
            if (controller != null)
            {
                player = controller.transform;
            }
        }

        if (panicController == null)
        {
            panicController = FindAnyObjectByType<PlayerPanicController>();
        }
    }

    private void HideAllAnimations()
    {
        if (idleAnimations != null)
        {
            for (int i = 0; i < idleAnimations.Length; i++)
            {
                idleAnimations[i]?.Hide();
            }
        }

        if (oneShotAnimations != null)
        {
            for (int i = 0; i < oneShotAnimations.Length; i++)
            {
                oneShotAnimations[i]?.Hide();
            }
        }
    }

    private void ScheduleOneShotAnimations(float time)
    {
        if (oneShotAnimations == null)
        {
            return;
        }

        for (int i = 0; i < oneShotAnimations.Length; i++)
        {
            oneShotAnimations[i]?.ScheduleNext(time);
        }
    }

    private void OnValidate()
    {
        if (maxSpawnInterval < minSpawnInterval)
        {
            maxSpawnInterval = minSpawnInterval;
        }

        closeDistance = Mathf.Max(0f, closeDistance);
        equalDistanceTolerance = Mathf.Max(0f, equalDistanceTolerance);

        if (idleAnimations != null)
        {
            for (int i = 0; i < idleAnimations.Length; i++)
            {
                idleAnimations[i]?.Validate();
            }
        }

        if (oneShotAnimations != null)
        {
            for (int i = 0; i < oneShotAnimations.Length; i++)
            {
                oneShotAnimations[i]?.Validate();
            }
        }
    }

    private readonly struct Candidate
    {
        public Candidate(MonsterAnimation animation, float distance)
        {
            Animation = animation;
            Distance = distance;
        }

        public MonsterAnimation Animation { get; }
        public float Distance { get; }
    }

    [Serializable]
    private abstract class MonsterAnimation
    {
        [SerializeField] private GameObject monsterObject;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform distancePoint;
        [SerializeField] private string stateName;
        [SerializeField, Min(0.01f)] private float playbackSpeed = 1f;
        [SerializeField, Min(0f)] private float closeDistanceOverride;
        [SerializeField] private bool deactivateWhenHidden = true;

        private float previousAnimatorSpeed = 1f;

        public Transform DistancePoint
        {
            get
            {
                if (distancePoint != null)
                {
                    return distancePoint;
                }

                if (animator != null)
                {
                    return animator.transform;
                }

                return monsterObject != null ? monsterObject.transform : null;
            }
        }

        public virtual bool CanPlay()
        {
            return monsterObject != null || animator != null;
        }

        public virtual void Show()
        {
            GameObject targetObject = GetTargetObject();
            if (targetObject != null)
            {
                targetObject.SetActive(true);
            }

            if (animator == null)
            {
                return;
            }

            previousAnimatorSpeed = animator.speed;
            animator.speed = playbackSpeed;

            if (!string.IsNullOrEmpty(stateName))
            {
                animator.Play(stateName, 0, 0f);
            }

            animator.Update(0f);
        }

        public virtual void Hide()
        {
            if (animator != null)
            {
                animator.speed = previousAnimatorSpeed;
            }

            GameObject targetObject = GetTargetObject();
            if (deactivateWhenHidden && targetObject != null)
            {
                targetObject.SetActive(false);
            }
        }

        public float GetCloseDistance(float fallback)
        {
            return closeDistanceOverride > 0f ? closeDistanceOverride : fallback;
        }

        protected float GetFirstClipDuration(float fallback)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return fallback;
            }

            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            if (clips == null || clips.Length == 0)
            {
                return fallback;
            }

            AnimationClip clip = clips[0];
            if (!string.IsNullOrEmpty(stateName))
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i] != null && clips[i].name == stateName)
                    {
                        clip = clips[i];
                        break;
                    }
                }
            }

            if (clip == null || clip.length <= 0f)
            {
                return fallback;
            }

            return clip.length / Mathf.Max(0.01f, playbackSpeed);
        }

        protected void RestartAnimator()
        {
            if (animator == null || string.IsNullOrEmpty(stateName))
            {
                return;
            }

            animator.Play(stateName, 0, 0f);
            animator.Update(0f);
        }

        protected bool IsAnimatorAtEnd()
        {
            if (animator == null)
            {
                return false;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return !stateInfo.loop && stateInfo.normalizedTime >= 1f;
        }

        private GameObject GetTargetObject()
        {
            if (monsterObject != null)
            {
                return monsterObject;
            }

            return animator != null ? animator.gameObject : null;
        }

        public virtual void Validate()
        {
            playbackSpeed = Mathf.Max(0.01f, playbackSpeed);
            closeDistanceOverride = Mathf.Max(0f, closeDistanceOverride);
        }
    }

    [Serializable]
    private sealed class IdleMonsterAnimation : MonsterAnimation
    {
        [SerializeField, Range(0f, 1f)] private float minPanic = 0f;
        [SerializeField, Range(0f, 1f)] private float maxPanic = 1f;
        [SerializeField, Min(0f)] private float minVisibleTime = 3f;
        [SerializeField, Min(0f)] private float maxVisibleTime = 5f;

        public bool CanPlay(float panic)
        {
            return CanPlay() && panic >= minPanic && panic <= maxPanic;
        }

        public float RollVisibleDuration()
        {
            return UnityEngine.Random.Range(minVisibleTime, maxVisibleTime);
        }

        public void TickLoop()
        {
            if (IsAnimatorAtEnd())
            {
                RestartAnimator();
            }
        }

        public override void Validate()
        {
            base.Validate();

            if (maxPanic < minPanic)
            {
                maxPanic = minPanic;
            }

            if (maxVisibleTime < minVisibleTime)
            {
                maxVisibleTime = minVisibleTime;
            }
        }
    }

    [Serializable]
    private sealed class OneShotMonsterAnimation : MonsterAnimation
    {
        [SerializeField, Min(0f)] private float minDelay = 8f;
        [SerializeField, Min(0f)] private float maxDelay = 20f;
        [SerializeField, Min(0f)] private float durationOverride;
        [SerializeField] private bool requireSolvedLock;
        [SerializeField] private LockPuzzle lockPuzzle;

        [NonSerialized] private float nextTime;

        public bool CanPlay(float time)
        {
            return CanPlay() && time >= nextTime && IsLockConditionMet();
        }

        public void ScheduleNext(float time)
        {
            nextTime = time + UnityEngine.Random.Range(minDelay, maxDelay);
        }

        public float GetDuration()
        {
            return durationOverride > 0f ? durationOverride : GetFirstClipDuration(1f);
        }

        private bool IsLockConditionMet()
        {
            return !requireSolvedLock || (lockPuzzle != null && lockPuzzle.IsSolved);
        }

        public override void Validate()
        {
            base.Validate();

            if (maxDelay < minDelay)
            {
                maxDelay = minDelay;
            }
        }
    }
}