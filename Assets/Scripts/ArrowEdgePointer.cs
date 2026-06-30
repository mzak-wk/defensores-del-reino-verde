using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ArrowEdgePointer : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;
    public Transform target;

    [Header("Ajustes")]
    public float screenMargin = 15f;   // borde de la pantalla
    public float spriteUpOffset = -90f; // corrige la “punta” del sprite si su up no apunta al objetivo

    [Header("Tamaño (px)")]
    public float sizePixels = 96f;     // <— fuerza tamaño de la flecha en píxeles

    RectTransform rt;
    Image img;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        img = GetComponent<Image>();
        // Fuerza tamaño en píxeles independientemente del PPU
        rt.sizeDelta = new Vector2(sizePixels, sizePixels);
        if (img) img.preserveAspect = true;
    }

    void LateUpdate()
    {
        if (!cam || !target) return;

        // ¿El target está dentro del viewport?
        Vector3 vp = cam.WorldToViewportPoint(target.position);
        bool onScreen = (vp.z > 0f && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f);

        // Mostrar/ocultar
        if (img) img.enabled = !onScreen;  // si prefieres SetActive, usa gameObject.SetActive(!onScreen);

        if (onScreen) return;

        // Proyecta al borde
        Vector3 screenCenter = new Vector3(Screen.width, Screen.height, 0f) * 0.5f;
        Vector3 screenPos = cam.WorldToScreenPoint(target.position);
        Vector3 dir = (screenPos - screenCenter).normalized;

        // Posición en borde con margen
        Vector3 half = screenCenter - new Vector3(screenMargin, screenMargin, 0f);
        Vector3 pos = screenCenter + new Vector3(dir.x * half.x, dir.y * half.y, 0f);

        // Coloca en canvas (Screen Space Overlay)
        rt.position = pos;

        // Rotación apuntando al target
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.rotation = Quaternion.Euler(0, 0, angle + spriteUpOffset);
    }
}
