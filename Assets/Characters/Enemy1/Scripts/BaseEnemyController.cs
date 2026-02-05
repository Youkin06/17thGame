using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public abstract class BaseEnemyController : MonoBehaviour
{
    public EnemyData enemyData;
    [SerializeField] protected float serchRadius = 6.0f;
    [SerializeField] protected float turnSpeed = 180f;
    [SerializeField] protected float angleOffset = 270f;
    protected float moveSpeed;
    protected bool isHijacked = false;
    protected bool isWandering = false;
    protected bool isAttacking = false;
    protected Rigidbody2D enemyRb;
    protected NavMeshAgent agent;

    [Header("徘徊関係")]
    [SerializeField] protected float wanderRadius = 4f;
    [SerializeField] protected float wandervelocity = 0.3f;
    [SerializeField] protected float wanderMinWait = 0.5f;
    [SerializeField] protected float wanderMaxWait = 1.5f;
    [SerializeField] protected float wanderPointTimeout = 4f;
    [SerializeField] protected float arriveThreshold = 0.25f;

    protected Coroutine wanderCo;

    protected virtual void Start()
    {
        if (enemyData != null)
        {
            Debug.Log($"この敵のタイプは: {enemyData.enemyType} です");
            Debug.Log($"移動スピードは: {enemyData.moveSpeed} です");
            moveSpeed = enemyData.moveSpeed;
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
        enemyRb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (isHijacked)
        {
            UpdateWhenHijacked();
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Vector2 thisPos = transform.position;
        Vector2 playerPos = player.transform.position;
        float distance = CheckDistance(thisPos, playerPos);

        UpdateEnemyBehavior(distance, playerPos);
    }

    /// <summary>
    /// サブクラスで実装。距離とプレイヤー位置に応じた行動を定義する。
    /// </summary>
    protected abstract void UpdateEnemyBehavior(float distance, Vector2 playerPos);

    /// <summary>
    /// 乗っ取り中の追加行動。ShooterEnemyController で弾発射などに使用。
    /// </summary>
    protected virtual void UpdateWhenHijacked() { }

    protected float CheckDistance(Vector2 thisPos, Vector2 playerPos)
    {
        return Vector2.Distance(thisPos, playerPos);
    }

    protected bool RotateToTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle + angleOffset);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        return Quaternion.Angle(transform.rotation, targetRotation) > 0.1f;
    }

    public void StartWander()
    {
        if (wanderCo != null) return;
        isWandering = true;
        wanderCo = StartCoroutine(WanderRoutine());
    }

    public void StopWander()
    {
        if (wanderCo != null)
        {
            StopCoroutine(wanderCo);
            wanderCo = null;
        }
        isWandering = false;

        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    protected IEnumerator WanderRoutine()
    {
        if (agent == null) yield break;

        agent.updateRotation = false;
        agent.updateUpAxis = false;

        while (true)
        {
            if (isHijacked || isAttacking || !agent.enabled || !agent.isOnNavMesh)
            {
                yield return null;
                continue;
            }

            if (!TryGetRandomNavMeshPoint(transform.position, wanderRadius, out Vector3 dest))
            {
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            agent.isStopped = true;
            agent.ResetPath();

            float rotTimer = 0f;
            const float rotTimeout = 1f;

            while (RotateToTarget(dest))
            {
                if (isHijacked || isAttacking || !agent.enabled || !agent.isOnNavMesh)
                    break;
                rotTimer += Time.deltaTime;
                if (rotTimer >= rotTimeout) break;
                yield return null;
            }

            if (isHijacked || isAttacking || !agent.enabled || !agent.isOnNavMesh)
            {
                yield return null;
                continue;
            }

            float wanderSpeed = moveSpeed * wandervelocity;
            agent.speed = wanderSpeed;
            agent.SetDestination(dest);
            agent.isStopped = false;

            float timer = 0f;
            while (true)
            {
                if (isHijacked || isAttacking || !agent.enabled || !agent.isOnNavMesh)
                    break;
                timer += Time.deltaTime;
                if (!agent.pathPending && agent.remainingDistance <= arriveThreshold)
                    break;
                if (timer >= wanderPointTimeout)
                    break;
                yield return null;
            }

            if (agent.enabled)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            yield return new WaitForSeconds(Random.Range(wanderMinWait, wanderMaxWait));
        }
    }

    protected bool TryGetRandomNavMeshPoint(Vector3 origin, float radius, out Vector3 result)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 r = Random.insideUnitCircle * radius;
            Vector3 randomPoint = origin + new Vector3(r.x, r.y, 0f);
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = origin;
        return false;
    }

    public virtual void StopTracking()
    {
        isHijacked = true;
        if (agent != null)
            agent.enabled = false;
        if (enemyRb != null)
            enemyRb.simulated = false;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
        StopAllCoroutines();
        isAttacking = false;
        StopWander();
    }

    public void ReleaseEnemy()
    {
        isHijacked = false;
        transform.SetParent(null);

        if (agent != null)
        {
            agent.enabled = true;
            if (!agent.isOnNavMesh)
                agent.Warp(transform.position);
        }
        if (enemyRb != null)
            enemyRb.simulated = true;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;
    }
}
