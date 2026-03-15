using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class RushEnemyController : BaseEnemyController
{
    [SerializeField] float attackRadius = 3.0f;
    [SerializeField] float dashSpeed = 5.0f;
    [SerializeField] float waitTime = 2f;
    [SerializeField] float attackDuration = 3f;
    [SerializeField] float attackCoolDown = 3f;

    private bool isInRushDamagePhase = false;
    private bool dealtDamageThisRush = false;

    protected override void Start()
    {
        base.Start();
        isAttacking = false;
    }

    protected override void UpdateEnemyBehavior(float distance, Vector2 playerPos)
    {
        Vector3 playerPos3 = new Vector3(playerPos.x, playerPos.y, 0f);

        if (distance < attackRadius)
        {
            StopWander();
            if (!isAttacking)
                StartCoroutine(AttackToTarget(playerPos3));
        }
        else if (distance < serchRadius)
        {
            StopWander();
            if (!isAttacking)
                MoveToTarget(playerPos3);
        }
        else
        {
            if (!isWandering && !isAttacking)
                StartWander();
        }
    }

    void MoveToTarget(Vector3 targetPos)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = false;
        RotateToTarget(targetPos);
        agent.speed = moveSpeed;
        agent.destination = targetPos;
    }

    IEnumerator DashStraightToTarget(Vector3 direction, float speed, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            if (agent != null && agent.enabled)
                agent.Move(direction * speed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator AttackToTarget(Vector3 targetPos)
    {
        Debug.Log("攻撃ループ開始");
        isAttacking = true;

        Vector3 direction = (targetPos - transform.position).normalized;

        while (RotateToTarget(targetPos))
            yield return null;

        Debug.Log("待機");
        yield return new WaitForSeconds(waitTime);

        Debug.Log("突進!!");
        isInRushDamagePhase = true;
        dealtDamageThisRush = false;
        yield return StartCoroutine(DashStraightToTarget(direction, dashSpeed, attackDuration));
        isInRushDamagePhase = false;

        Debug.Log("クールタイム");
        yield return new WaitForSeconds(attackCoolDown);

        if (agent != null)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                agent.enabled = true;
                if (!agent.isOnNavMesh)
                    agent.Warp(hit.position);
            }
            else
            {
                agent.enabled = true;
                Debug.LogWarning($"攻撃終了後、NavMesh上に近い位置が見つかりませんでした。現在位置: {transform.position}");
            }
        }

        isAttacking = false;
        Debug.Log("攻撃ループ終了");
    }

    void StopMove()
    {
        if (agent != null)
            agent.speed = 0f;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isHijacked) return; // 乗っ取り中はプレイヤーにダメージを与えない
        if (!collision.gameObject.CompareTag("Player") || !isInRushDamagePhase || dealtDamageThisRush)
            return;
        var playerController = collision.gameObject.GetComponent<PlayerController>();
        if (playerController == null) return;
        dealtDamageThisRush = true;
        playerController.OnPlayerDamaged();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, serchRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.up * 3f);
    }
}
