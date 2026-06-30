using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RayTelegraph : MonoBehaviour
{
    public float length = 10f;
    public float width = 0.2f;
    public float duration = 1.5f;
    public Color color = Color.white;

    LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.startWidth = lr.endWidth = width;

        // garantiza material simple para que la línea se vea
        if (lr.sharedMaterial == null)
            lr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

        lr.startColor = lr.endColor = color;
    }

    /// <summary>
    /// Muestra la línea orientada desde origin hacia direction (unit vector), durante "duration" y ejecuta onFinish.
    /// </summary>
    public void Show(Vector2 origin, Vector2 direction, Action onFinish = null)
    {
        StopAllCoroutines();
        StartCoroutine(CoShow(origin, direction.normalized, onFinish));
    }

    IEnumerator CoShow(Vector2 origin2, Vector2 dir2, Action onFinish)
    {
        // convertimos a Vector3 para evitar ambigüedades al sumar
        Vector3 origin = new Vector3(origin2.x, origin2.y, 0f);
        Vector3 dir = new Vector3(dir2.x, dir2.y, 0f);

        float t = 0f;
        while (t < duration)
        {
            Vector3 a = origin;
            Vector3 b = origin + dir * length;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            t += Time.deltaTime;
            yield return null;
        }

        // limpiar la línea (opcional)
        lr.SetPosition(0, origin);
        lr.SetPosition(1, origin);
        onFinish?.Invoke();
    }

    void OnValidate()
    {
        if (lr == null) lr = GetComponent<LineRenderer>();
        if (lr)
        {
            lr.startWidth = lr.endWidth = width;
            lr.startColor = lr.endColor = color;
        }
    }
}
