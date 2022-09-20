using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Golem : EnemyController
{
    [Header("Skill")]
    public float kickForce = 25;
    public GameObject rockPrefab;
    public Transform handPosition;

    public void KickOff()
    {
        if (attackTarget == null) return;
        if (!transform.IsFacingTarget(attackTarget.transform)) return;

        var targetStates = attackTarget.GetComponent<CharacterStates>();

        Vector3 direction = (attackTarget.transform.position - transform.position).normalized;

        targetStates.GetComponent<NavMeshAgent>().isStopped = true;
        targetStates.GetComponent<NavMeshAgent>().velocity = direction * kickForce;
        targetStates.GetComponent<Animator>().SetTrigger("Dizzy");

        targetStates.TakeDamage(characterStates, targetStates);
    }

    public void ThrowRock()
    {
        if (attackTarget != null)
        {
            var rock = Instantiate(rockPrefab, handPosition.position, Quaternion.identity);
            rock.GetComponent<Rock>().target = attackTarget;
        }
    }
}
