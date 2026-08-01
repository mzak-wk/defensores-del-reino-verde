using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Boss Attack Data", fileName = "New Boss Attack Data")]
public class BossAttackData : ScriptableObject
{
    [Header("Identificación")]
    public string attackName = "Attack";

    [Header("Prefab del Ataque")]
    [Tooltip("Prefab que se instancia (ImpactArea, AttackCollider, etc.).")]
    public GameObject attackPrefab;

    [Header("Configuración de Tiempo")]
    public float cooldown = 2f;
    public float castTime = 0.5f;

    [Header("Selección")]
    [Tooltip("Peso probabilístico para la selección aleatoria (mayor = más probable).")]
    public float weight = 1f;

    [Header("Visual")]
    [Tooltip("Prefab de la señal visual previa al ataque.")]
    public GameObject telegraphPrefab;

    [Header("Audio")]
    public AudioClip sfx;

    [Header("Escala")]
    public Vector3 spawnScale = Vector3.one;
}
