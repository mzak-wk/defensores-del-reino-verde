using UnityEngine;

public class CameraBounds2D : MonoBehaviour
{
    public static CameraBounds2D Instance { get; private set; }
    Camera cam;

    private void Awake()
    {
        Instance = this;
        cam = Camera.main;
    }

    public Bounds GetWorldBounds(float extraPadding = 0f)
    {
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        Vector3 center = cam.transform.position;
        var b = new Bounds(center, new Vector3(width, height, 0f));
        b.Expand(new Vector3(extraPadding * 2f, extraPadding * 2f, 0f));
        return b;
    }

    // Devuelve un punto aleatorio fuera de cámara (anillo rectangular)
    public Vector2 RandomPointOffscreen(float margin = 2f)
    {
        Bounds inner = GetWorldBounds(0f);
        Bounds outer = GetWorldBounds(margin);

        int side = Random.Range(0, 4); // 0=izq,1=der,2=abajo,3=arriba
        float x = 0, y = 0;

        switch (side)
        {
            case 0: // izquierda
                x = Random.Range(outer.min.x, inner.min.x);
                y = Random.Range(outer.min.y, outer.max.y);
                break;
            case 1: // derecha
                x = Random.Range(inner.max.x, outer.max.x);
                y = Random.Range(outer.min.y, outer.max.y);
                break;
            case 2: // abajo
                x = Random.Range(outer.min.x, outer.max.x);
                y = Random.Range(outer.min.y, inner.min.y);
                break;
            case 3: // arriba
                x = Random.Range(outer.min.x, outer.max.x);
                y = Random.Range(inner.max.y, outer.max.y);
                break;
        }
        return new Vector2(x, y);
    }
}