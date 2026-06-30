using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para buscar el Director

public class BossHealth : Enemy // <--- HEREDA DE ENEMY
{
    // Opcional: Para evitar que el boss dé XP/drops normales al morir
    public bool suppressNormalDropsOnBossDeath = true;

    // Sobrescribimos el método Die() de la clase base (Enemy)
    protected override void Die()
    {
        // 1. Notificar al director de escena ANTES de hacer la limpieza
        NotifyTutorialDirector();

        // 2. Si configuramos que el boss suprima sus drops normales, lo aplicamos
        if (suppressNormalDropsOnBossDeath)
        {
            suppressDrops = true;
        }

        // 3. Llamamos al método Die() de la clase base (Enemy) para que haga
        // la limpieza (Invoke OnDeath? / Destroy gameObject)
        base.Die();
    }

    void NotifyTutorialDirector()
    {
        // Buscamos el director de escena.
        // Nota: Busca el Director solo cuando el jefe muere, no en Start/Awake.
        TutorialDirector director = FindFirstObjectByType<TutorialDirector>();

        if (director != null)
        {
            director.OnBossDefeated();
        }
        else
        {
            Debug.LogError("[BossHealth] No se encontró un TutorialDirector en la escena. ¡El portal no se abrirá!");
        }
    }
}