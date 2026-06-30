using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class HealthBarFollow : MonoBehaviour
{
    public Transform target;                // asigna andrius_0 en el inspector
    public Vector3 worldOffset = new Vector3(0f, -0.3f, 0f); // ajustar para quedar sobre la cabeza
    public Vector2 screenOffset = Vector2.zero; // offset en píxeles si quieres

    RectTransform rectTransform;
    Canvas parentCanvas;
    Camera camForWorldToScreen; // cámara para WorldToScreenPoint
    Camera camForUI;           // cámara a pasar a RectTransformUtility (null para Overlay)

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
            Debug.LogError("HealthBarFollow: no hay Canvas padre.");

        // Usa la cámara principal para convertir posiciones del mundo a pantalla.
        camForWorldToScreen = Camera.main;

        // Para ScreenSpace-Camera debes pasar la camera del canvas; para Overlay pasar null.
        camForUI = (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) 
                    ? null 
                    : parentCanvas.worldCamera;
    }

    void LateUpdate()
    {
        if (target == null || parentCanvas == null) return;

        Vector3 worldPos = target.position + worldOffset;
        Vector3 screenPoint = camForWorldToScreen.WorldToScreenPoint(worldPos);

        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        Vector2 localPoint;
        // Convierte la posición de pantalla al espacio local del rectTransform del Canvas
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, camForUI, out localPoint))
        {
            rectTransform.anchoredPosition = localPoint + screenOffset;
        }
    }
}
