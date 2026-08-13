/// <summary>
/// Tipos de hablante disponibles en el sistema de diálogos universal.
/// </summary>
public enum SpeakerType
{
    Jugador,
    NPC,
    Boss,
    Sistema
}

/// <summary>
/// Emociones expresivas del hablante para el sistema de diálogos.
/// </summary>
public enum EmotionType
{
    Serio,
    Feliz,
    Triste,
    Enojado
}

/// <summary>
/// Acciones ejecutables al seleccionar una opción de diálogo.
/// Reemplaza UnityEvent que no funciona en ScriptableObjects.
/// </summary>
public enum DialogueActionType
{
    None,
    OpenShop,
    OpenWeaponChoice,
    CloseDialogue,
    CustomEvent
}
