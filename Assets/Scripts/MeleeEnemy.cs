using UnityEngine;

public class MeleeEnemy : EnemyBase
{
    [Header("Melee")]
    public float attackRange = 1.2f;
    public int attackDamage = 10;
    public float attackCooldown = 1.5f;

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
        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist > attackRange)
        {
            MoveToward(_player.position);
            animator?.SetBool(IsWalkingBool, true);
        }
        else
        {
            animator?.SetBool(IsWalkingBool, false);
        }

        _attackTimer -= Time.deltaTime;
        if (dist <= attackRange && _attackTimer <= 0f)
        {
            _attackTimer = attackCooldown;
            animator?.SetTrigger(AttackTrigger);
            PlayerController.Instance?.TakeDamage(attackDamage);
            AudioManager.Instance?.PlayMeleeDamage(transform.position);
        }
    }
}
