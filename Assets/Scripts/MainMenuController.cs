using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Botones del Menú Principal")]
    public Button btnJugar;
    public Button btnOpciones;
    public Button btnSalir;

    [Header("Paneles de Navegación")]
    public GameObject panelMainMenu;
    public GameObject panelInicioJuego;    // Asignar aquí '3_Panel_InicioJuego'
    public GameObject modalOpciones;

    private void Start()
    {
        if (btnJugar != null)
            btnJugar.onClick.AddListener(IniciarJuego);

        if (btnOpciones != null)
            btnOpciones.onClick.AddListener(AbrirOpciones);

        if (btnSalir != null)
            btnSalir.onClick.AddListener(SalirDelJuego);
    }

    public void IniciarJuego()
    {
        // Oculta el menú principal y abre la pantalla de inducción con Melissa
        if (panelMainMenu != null)
            panelMainMenu.SetActive(false);

        if (panelInicioJuego != null)
            panelInicioJuego.SetActive(true);
    }

    public void AbrirOpciones()
    {
        if (modalOpciones != null)
            modalOpciones.SetActive(!modalOpciones.activeSelf);
    }

    public void SalirDelJuego()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}