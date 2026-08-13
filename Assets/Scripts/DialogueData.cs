using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Opción de diálogo que el jugador puede seleccionar.
/// Cada opción puede ejecutar una acción y/o saltar a otro bloque de diálogo.
/// </summary>
[Serializable]
public class DialogueChoice
{
    [Tooltip("Texto corto que aparece en el botón de opción.")]
    public string optionText;

    [Tooltip("Texto extendido que dirá el personaje al seleccionar esta opción.")]
    [TextArea(2, 5)]
    public string fullResponseText;

    [Tooltip("Acción a ejecutar cuando el jugador selecciona esta opción.")]
    public DialogueActionType actionType = DialogueActionType.None;

    [Tooltip("Clave identificadora para CustomEvent. Permite al DialogueManager o consumidores reaccionar.")]
    public string customEventKey;

    [Tooltip("Salto a otro bloque de diálogo (opcional). Si se asigna, continúa con ese bloque después de la respuesta.")]
    public DialogueData nextDialogueBlock;
}

/// <summary>
/// Línea individual de diálogo con hablante, emoción, texto y opciones opcionales.
/// </summary>
[Serializable]
public class DialogueLine
{
    [Tooltip("Quién habla en esta línea.")]
    public SpeakerType speaker;

    [Tooltip("Emoción/expressión del hablante.")]
    public EmotionType emotion;

    [Tooltip("Contenido del diálogo.")]
    [TextArea(2, 5)]
    public string text;

    [Tooltip("Opciones opcionales que el jugador puede seleccionar después de esta línea. Si está vacío, se avanza con click/tecla.")]
    public List<DialogueChoice> choices = new List<DialogueChoice>();
}

/// <summary>
/// ScriptableObject universal para cualquier tipo de conversación.
/// Reemplaza DialogueData y BossDialogueData con una estructura unificada y extensible.
/// </summary>
[CreateAssetMenu(menuName = "Dialogue/Universal Dialogue Data", fileName = "New Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("Dialogue Content")]
    [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

    public IReadOnlyList<DialogueLine> Lines => lines;
}
