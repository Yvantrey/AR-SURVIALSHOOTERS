using UnityEngine;

public class MeleeEnemy : EnemyBase
{
    [Header("Melee")]
    public float attackRange = 1.1f;
    public int attackDamage = 20;
    public float attackCooldown = 1.2f;

    [Header("Animation")]
    public Animator animator;

    static readonly int AttackTrigger = Animator.StringToHash("Attack");
    static readonly int IsWalkingBool = Animator.StringToHash("IsWalking");

    float _attackTimer;

    protected override void Awake()
    {
        maxHealth = 2;
        base.Awake();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    protected override void HandleBehavior()
    {
        float dist = FlatDistanceToPlayer();
        float range = attackRange * WorldScale;

        if (dist > range)
        {
            MoveToward(_player.position);
            SetWalking(true);
        }
        else
        {
            FaceDirection(_player.position - transform.position);
            SetWalking(false);
        }

        _attackTimer -= Time.deltaTime;
        if (dist <= range && _attackTimer <= 0f)
        {
            _attackTimer = attackCooldown;
            if (HasParameter(AttackTrigger)) animator.SetTrigger(AttackTrigger);
            PlayerController.Instance?.TakeDamage(attackDamage);
            AudioManager.Instance?.PlayMeleeDamage(transform.position);
        }
    }

    void SetWalking(bool walking)
    {
        if (HasParameter(IsWalkingBool)) animator.SetBool(IsWalkingBool, walking);
    }

    bool HasParameter(int hash)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (var p in animator.parameters) if (p.nameHash == hash) return true;
        return false;
    }
}
