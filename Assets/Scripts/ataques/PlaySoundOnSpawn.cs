using UnityEngine;

public class PlaySoundOnSpawn : MonoBehaviour
{
    [Tooltip("Clip de sonido a reproducir al activarse el objeto.")]
    public AudioClip spawnSound;

    [Tooltip("Volumen del sonido.")]
    [Range(0f, 1f)]
    public float volume = 1f;

    void OnEnable()
    {
        if (spawnSound != null)
        {
            AudioSource.PlayClipAtPoint(spawnSound, transform.position, volume);
        }
    }
}