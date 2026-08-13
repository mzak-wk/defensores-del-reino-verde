using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FreezeExplosion : MonoBehaviour
{
    [Header("AoE Configuration")]
    [SerializeField] private float explosionDuration = 1.0f;
    [SerializeField] private LayerMask enemyMask;

    [Header("Slow Effect Configuration")]
    [SerializeField] private float slowMultiplier = 0.5f;
    [SerializeField] private float slowDuration = 2.0f;
    [SerializeField] private Color freezeColor = new Color(0.5f, 0.8f, 1f);

    [HideInInspector] public float damage;

    private HashSet<Enemy> alreadyHit = new HashSet<Enemy>();

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnEnable()
    {
        alreadyHit.Clear();
        if (explosionDuration > 0f)
        {
            StartCoroutine(DieAfterDelay(explosionDuration));
        }
    }

    private IEnumerator DieAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (enemyMask.value != 0 && (enemyMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null || alreadyHit.Contains(enemy))
            return;

        alreadyHit.Add(enemy);

        // Aplicar daño
        enemy.TakeDamage(damage);

        // Aplicar efecto slow/freeze
        enemy.ApplySlow(slowMultiplier, slowDuration, freezeColor);
    }
}
