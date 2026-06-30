using UnityEngine;
using UnityEngine.UI;
using TMPro;            // <- para TMP_Text
using System.Collections;  // <- para IEnumerator y coroutines

public class ShopUI : MonoBehaviour
{
    [Header("Root panel de tienda")]
    [SerializeField] GameObject panel;

    [Header("Botones")]
    [SerializeField] Button btnBuyHP;
    [SerializeField] Button btnBuySpeed;
    [SerializeField] Button btnBuyResist;

    [Header("Textos de precio y nivel")]
    [SerializeField] TMP_Text hpPriceTxt;
    [SerializeField] TMP_Text hpLvlTxt;
    [SerializeField] TMP_Text spdPriceTxt;
    [SerializeField] TMP_Text spdLvlTxt;
    [SerializeField] TMP_Text resPriceTxt;
    [SerializeField] TMP_Text resLvlTxt;

    [Header("Oro")]
    [SerializeField] TMP_Text goldTxt;

    [Header("Niveles máximos")]
    public int maxHpLevel = 50;
    public int maxSpeedLevel = 10;
    public int maxResistLevel = 20;   // luego lo usaremos para el imán

    GameSession gs;

    void Awake()
    {
        gs = GameSession.Instance;

        if (btnBuyHP)    btnBuyHP.onClick.AddListener(OnBuyHP);
        if (btnBuySpeed) btnBuySpeed.onClick.AddListener(OnBuySpeed);
        if (btnBuyResist)btnBuyResist.onClick.AddListener(OnBuyResist);
    }

    void OnEnable()
    {
        RefreshUI();
    }

    // -------------------------
    // COMPRA
    // -------------------------
    void OnBuyHP()
    {
        if (gs == null) return;
        if (gs.hpLevel >= maxHpLevel) return;

        if (gs.ApplyHpUpgrade())
            RefreshUI();
        else
            BlinkPrice(hpPriceTxt);
    }

    void OnBuySpeed()
    {
        if (gs == null) return;
        if (gs.speedLevel >= maxSpeedLevel) return;

        if (gs.ApplySpeedUpgrade())
            RefreshUI();
        else
            BlinkPrice(spdPriceTxt);
    }

    void OnBuyResist()
    {
        if (gs == null) return;
        if (gs.resistLevel >= maxResistLevel) return;

        if (gs.ApplyResistUpgrade())
            RefreshUI();
        else
            BlinkPrice(resPriceTxt);
    }

    // -------------------------
    // UI
    // -------------------------
    public void RefreshUI()
    {
        if (gs == null) gs = GameSession.Instance;
        if (gs == null) return;

        // NIVELES
        if (hpLvlTxt)  hpLvlTxt.text  = $"Nivel {gs.hpLevel}";
        if (spdLvlTxt) spdLvlTxt.text = $"Nivel {gs.speedLevel}";
        if (resLvlTxt) resLvlTxt.text = $"Nivel {gs.resistLevel}";

        // PRECIOS O 'MAX'
        if (hpPriceTxt)
            hpPriceTxt.text  = (gs.hpLevel    >= maxHpLevel)    ? "MAX" : gs.CurrentPriceHP().ToString();
        if (spdPriceTxt)
            spdPriceTxt.text = (gs.speedLevel >= maxSpeedLevel) ? "MAX" : gs.CurrentPriceSpeed().ToString();
        if (resPriceTxt)
            resPriceTxt.text = (gs.resistLevel>= maxResistLevel)? "MAX" : gs.CurrentPriceResist().ToString();

        if (goldTxt)
            goldTxt.text = gs.gold.ToString();

        // BOTONES DESACTIVADOS AL MÁXIMO
        if (btnBuyHP)     btnBuyHP.interactable    = gs.hpLevel    < maxHpLevel;
        if (btnBuySpeed)  btnBuySpeed.interactable = gs.speedLevel < maxSpeedLevel;
        if (btnBuyResist) btnBuyResist.interactable= gs.resistLevel< maxResistLevel;
    }

    // -------------------------
    // Feedback si no alcanza oro
    // -------------------------
    void BlinkPrice(TMP_Text txt)
    {
        if (!txt) return;
        StartCoroutine(BlinkPriceCR(txt));
    }

    IEnumerator BlinkPriceCR(TMP_Text txt)
    {
        string originalText  = txt.text;
        Color  originalColor = txt.color;

        txt.text  = "Sin oro";
        txt.color = Color.red;

        yield return new WaitForSecondsRealtime(0.5f);

        txt.text  = originalText;
        txt.color = originalColor;
    }
}
