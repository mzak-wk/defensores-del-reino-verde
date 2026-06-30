using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PortalTeleporter : MonoBehaviour
{
    public string playerTag = "Player";

    [Header("Sprites")]
    public SpriteRenderer portalSprite; // <-- arrastra aquí el SpriteRenderer del Portal
    public Sprite closedSprite;         // <-- sprite asset “cerrado”
    public Sprite openSprite;           // <-- sprite asset “abierto”

    [Header("Trigger")]
    public Collider2D triggerCollider;  // <-- tu BoxCollider2D (IsTrigger = true)

    [Header("Estado inicial")]
    public bool startOpen = false;

    bool isOpen;
    TutorialDirector director;

    void Awake()
    {
        if (!triggerCollider) triggerCollider = GetComponent<Collider2D>();
        director = FindFirstObjectByType<TutorialDirector>();
        ApplyState(startOpen); // pinta sprite y activa/desactiva trigger
    }

    public void SetOpen(bool open)
    {
        ApplyState(open);
        // aquí puedes reproducir FX/sonido de apertura
    }

    void ApplyState(bool open)
    {
        isOpen = open;
        if (triggerCollider) triggerCollider.enabled = open;
        if (portalSprite)
            portalSprite.sprite = open ? openSprite : closedSprite;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isOpen) return;
        if (other.CompareTag(playerTag))
            director?.OnPlayerEnteredPortal();
    }
}
