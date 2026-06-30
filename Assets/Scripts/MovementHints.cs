using UnityEngine;
using TMPro;

public class MovementHints : MonoBehaviour
{
    public TextMeshProUGUI hintW;
    public TextMeshProUGUI hintA;
    public TextMeshProUGUI hintS;
    public TextMeshProUGUI hintD;

    GameSession gs;

    void Start()
    {
        gs = GameSession.Instance;

        // Al cargar, aplica los estados guardados
        if (gs)
        {
            if (gs.movedW && hintW) hintW.gameObject.SetActive(false);
            if (gs.movedA && hintA) hintA.gameObject.SetActive(false);
            if (gs.movedS && hintS) hintS.gameObject.SetActive(false);
            if (gs.movedD && hintD) hintD.gameObject.SetActive(false);
        }

        // Si ya hizo todos los movimientos, ocultar el GO completo
        if (gs && gs.movedW && gs.movedA && gs.movedS && gs.movedD)
        {
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (gs == null) gs = GameSession.Instance;

        if (!gs.movedW && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)))
        {
            gs.movedW = true;
            if (hintW) hintW.gameObject.SetActive(false);
        }

        if (!gs.movedA && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)))
        {
            gs.movedA = true;
            if (hintA) hintA.gameObject.SetActive(false);
        }

        if (!gs.movedS && (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)))
        {
            gs.movedS = true;
            if (hintS) hintS.gameObject.SetActive(false);
        }

        if (!gs.movedD && (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)))
        {
            gs.movedD = true;
            if (hintD) hintD.gameObject.SetActive(false);
        }

        if (gs.movedW && gs.movedA && gs.movedS && gs.movedD)
        {
            gameObject.SetActive(false);
        }
    }
}
