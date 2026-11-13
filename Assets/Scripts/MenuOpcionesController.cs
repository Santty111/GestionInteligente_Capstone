using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuOpcionesController : MonoBehaviour
{
    [Header("1. Botones y Paneles de Navegación")]
    public GameObject panelOpciones;               // Arrastra: Menú (el contenedor raíz del modal)
    public Button btnAbrirOpciones;                // Opcional: botón único de apertura (Main Menu)
    public Button[] botonesAbrirConfiguracion;     // Arrastra: Btn_Configuración de Panel 3, 4 y 5
    public Button btnVolverJuego;                  // Arrastra: Volver_Juego (cierra modal y vuelve a Panel 3, 4 o 5)
    public Button btnVolverMenu;                   // Opcional: botón volver en Main Menu

    [Header("2. Regreso al Menú Principal (Opcional)")]
    public Button btnSalirAlMainMenu;              // Arrastra: Btn_Salir_Menú
    public GameObject panelMainMenu;               // Arrastra: 2_Panel_MainMenu
    public GameObject[] panelesJuegoACerrar;       // Arrastra: 3_Panel_InicioJuego, 4_Panel_Oficina, 5_Panel_Mision1_Importacion

    [Header("3. Control de Brillo Global (0 - 100)")]
    public Slider sliderBrillo;                    // Arrastra: Brillo
    public TMP_Text txtBrillo;                     // Arrastra: Txt_Brillo
    public Image overlayBrilloGlobal;              // Arrastra: Overlay_Brillo (en el Canvas raíz)

    [Header("4. Control de Volumen General (0 - 100)")]
    public Slider sliderVolumen;                   // Arrastra: Volumen
    public TMP_Text txtVolumen;                    // Arrastra: Txt_Volumen

    private const string KEY_BRILLO = "Config_Brillo_100";
    private const string KEY_VOLUMEN = "Config_Volumen_100";

    private void Awake()
    {
        if (btnAbrirOpciones != null)
        {
            btnAbrirOpciones.onClick.RemoveAllListeners();
            btnAbrirOpciones.onClick.AddListener(AbrirOpciones);
        }

        if (botonesAbrirConfiguracion != null)
        {
            for (int i = 0; i < botonesAbrirConfiguracion.Length; i++)
            {
                if (botonesAbrirConfiguracion[i] != null)
                {
                    botonesAbrirConfiguracion[i].onClick.RemoveAllListeners();
                    botonesAbrirConfiguracion[i].onClick.AddListener(AbrirOpciones);
                }
            }
        }

        if (btnVolverJuego != null)
        {
            btnVolverJuego.onClick.RemoveAllListeners();
            btnVolverJuego.onClick.AddListener(CerrarOpciones);
        }

        if (btnVolverMenu != null)
        {
            btnVolverMenu.onClick.RemoveAllListeners();
            btnVolverMenu.onClick.AddListener(CerrarOpciones);
        }

        if (btnSalirAlMainMenu != null)
        {
            btnSalirAlMainMenu.onClick.RemoveAllListeners();
            btnSalirAlMainMenu.onClick.AddListener(RegresarAMainMenu);
        }

        if (sliderBrillo != null)
        {
            sliderBrillo.minValue = 0f;
            sliderBrillo.maxValue = 100f;
            sliderBrillo.wholeNumbers = true;
            sliderBrillo.onValueChanged.RemoveAllListeners();
            sliderBrillo.onValueChanged.AddListener(CambiarBrillo);
        }

        if (sliderVolumen != null)
        {
            sliderVolumen.minValue = 0f;
            sliderVolumen.maxValue = 100f;
            sliderVolumen.wholeNumbers = true;
            sliderVolumen.onValueChanged.RemoveAllListeners();
            sliderVolumen.onValueChanged.AddListener(CambiarVolumen);
        }
    }

    private void Start()
    {
        SincronizarValoresGuardados();

        if (panelOpciones != null)
            panelOpciones.SetActive(false);
    }

    public void AbrirOpciones()
    {
        SincronizarValoresGuardados();

        if (panelOpciones != null)
            panelOpciones.SetActive(true);
    }

    public void CerrarOpciones()
    {
        PlayerPrefs.Save();

        if (panelOpciones != null)
            panelOpciones.SetActive(false);
    }

    public void RegresarAMainMenu()
    {
        PlayerPrefs.Save();

        if (panelOpciones != null)
            panelOpciones.SetActive(false);

        if (panelesJuegoACerrar != null)
        {
            for (int i = 0; i < panelesJuegoACerrar.Length; i++)
            {
                if (panelesJuegoACerrar[i] != null)
                    panelesJuegoACerrar[i].SetActive(false);
            }
        }

        if (panelMainMenu != null)
            panelMainMenu.SetActive(true);
    }

    private void SincronizarValoresGuardados()
    {
        float brilloGuardado = PlayerPrefs.GetFloat(KEY_BRILLO, 100f);
        float volumenGuardado = PlayerPrefs.GetFloat(KEY_VOLUMEN, 100f);

        if (sliderBrillo != null) sliderBrillo.SetValueWithoutNotify(brilloGuardado);
        if (sliderVolumen != null) sliderVolumen.SetValueWithoutNotify(volumenGuardado);

        CambiarBrillo(brilloGuardado);
        CambiarVolumen(volumenGuardado);
    }

    public void CambiarBrillo(float valor)
    {
        float factorNormalizado = Mathf.Clamp01(valor / 100f);
        float alphaOscuridad = (1f - factorNormalizado) * 0.80f;

        if (overlayBrilloGlobal != null)
        {
            Color colorActual = overlayBrilloGlobal.color;
            colorActual.a = alphaOscuridad;
            overlayBrilloGlobal.color = colorActual;
        }

        if (txtBrillo != null)
            txtBrillo.text = $"Brillo: {Mathf.RoundToInt(valor)}%";

        PlayerPrefs.SetFloat(KEY_BRILLO, valor);
    }

    public void CambiarVolumen(float valor)
    {
        AudioListener.volume = Mathf.Clamp01(valor / 100f);

        if (txtVolumen != null)
            txtVolumen.text = $"Volumen: {Mathf.RoundToInt(valor)}%";

        PlayerPrefs.SetFloat(KEY_VOLUMEN, valor);
    }
}