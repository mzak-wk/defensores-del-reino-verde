using System;
using System.Collections;
using UnityEngine;

public class FinalBossAIController : BossAIController
{
    [Header("Fase 2")]
    [SerializeField] private BossStatsOverride phase2Stats;
    [SerializeField] private float phaseTransitionHPThreshold = 0.5f;
    [SerializeField] private GameObject phase2Prefab;

    public event Action OnPhaseTransition;
    public event Action OnFullyDefeated;

    private bool hasTransitioned;
    private float transitionHP;

    public void Init(BossStatsOverride initialStats, BossStatsOverride p2Stats, float hpThreshold)
    {
        SetStatsOverride(initialStats);
        phase2Stats = p2Stats;
        phaseTransitionHPThreshold = hpThreshold;
        hasTransitioned = false;
    }

    void OnEnable()
    {
        hasTransitioned = false;
    }

    public new void Activate()
    {
        hasTransitioned = false;
        transitionHP = StatsOverride != null ? StatsOverride.maxHP * phaseTransitionHPThreshold : 0f;

        var bh = GetComponent<BossHealth>();
        if (bh != null)
            bh.OnDamaged += CheckPhaseTransition;

        base.Activate();
    }

    public new void Deactivate()
    {
        var bh = GetComponent<BossHealth>();
        if (bh != null)
            bh.OnDamaged -= CheckPhaseTransition;

        base.Deactivate();
    }

    private void CheckPhaseTransition(float damage)
    {
        if (hasTransitioned) return;

        var bh = GetComponent<BossHealth>();
        if (bh == null) return;

        if (bh.CurrentHP <= transitionHP)
        {
            hasTransitioned = true;
            OnPhaseTransition?.Invoke();
        }
    }

    public void TransitionToPhase2()
    {
        if (phase2Stats != null)
        {
            SetStatsOverride(phase2Stats);
            ApplyStatsOverride();
        }

        Deactivate();
        StartCoroutine(RestartAfterDelay(1f));
    }

    private IEnumerator RestartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Activate();
    }

    public void NotifyFullyDefeated()
    {
        OnFullyDefeated?.Invoke();
    }
}
