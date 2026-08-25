using System;

/// <summary>
/// Causa de muerte o condición de Game Over del jugador.
/// </summary>
public enum DeathReason
{
    Generic,
    EnemyDamage,
    TemperatureLimit,
    CO2Limit,
    HazardLimit
}
