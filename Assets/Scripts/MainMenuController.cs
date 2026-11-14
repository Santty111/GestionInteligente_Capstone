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
    public GameObject panelOficina;        // Asignar aquí '4_Panel_Oficina'
    public GameObject modalOpciones;

    [Header("Control de Modo Usuario")]
    [Tooltip("Si está activo, siempre mostrará el diálogo de bienvenida (útil para pruebas en Editor).")]
    public bool forzarModoNuevo = false;

    private void Awake()
    {
        // Auto-asignar panelOficina si aún no está enlazado en el Inspector
        if (panelOficina == null)
        {
            var oficina = FindAnyObjectByType<OficinaSistemaTMSController>(FindObjectsInactive.Include);
            if (oficina != null)
            {
                panelOficina = oficina.gameObject;
            }
            else
            {
                panelOficina = GameObject.Find("4_Panel_Oficina");
            }
        }
    }

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
        // Oculta el menú principal
        if (panelMainMenu != null)
            panelMainMenu.SetActive(false);

        // Verificar si el usuario es nuevo o ya es ingresado (recurrente)
        bool esNuevo = forzarModoNuevo || EsUsuarioNuevo();

        if (esNuevo)
        {
            Debug.Log("<color=yellow>[MainMenuController] Usuario NUEVO -> Mostrando Diálogo de Bienvenida (Melissa).</color>");
            if (panelInicioJuego != null)
            {
                panelInicioJuego.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[MainMenuController] 'panelInicioJuego' no está asignado.");
            }
        }
        else
        {
            Debug.Log("<color=green>[MainMenuController] Usuario YA INGRESADO -> Saltando directamente a Oficina / Sistema TMS.</color>");
            if (panelOficina != null)
            {
                panelOficina.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[MainMenuController] 'panelOficina' no asignado. Intentando buscar '4_Panel_Oficina'...");
                var ofi = GameObject.Find("4_Panel_Oficina");
                if (ofi != null)
                {
                    ofi.SetActive(true);
                }
                else if (panelInicioJuego != null)
                {
                    panelInicioJuego.SetActive(true);
                }
            }
        }
    }

    private bool EsUsuarioNuevo()
    {
        // 1. Consultar a PortalBridgeManager si hay sesión activa
        if (PortalBridgeManager.Instance != null && PortalBridgeManager.Instance.usuarioActual != null)
        {
            // Si el puente ya determinó que no es nuevo (por partidas en Neon, etc.)
            if (!PortalBridgeManager.Instance.usuarioActual.esNuevo)
            {
                return false;
            }

            string correo = PortalBridgeManager.Instance.usuarioActual.correo;
            string uid = PortalBridgeManager.Instance.usuarioActual.usuarioIdGuid;

            // Revisar PlayerPrefs
            if (!string.IsNullOrEmpty(correo) && PlayerPrefs.GetInt("BienvenidaCompletada_" + correo.ToLower(), 0) == 1)
                return false;

            if (!string.IsNullOrEmpty(uid) && PlayerPrefs.GetInt("BienvenidaCompletada_" + uid, 0) == 1)
                return false;

            // Revisar base de datos SQLite local
            if (DatabaseManager.Instancia != null && !DatabaseManager.Instancia.EsUsuarioNuevo(uid, correo))
                return false;
        }

        // 2. Revisión de bandera global en PlayerPrefs (por si se jugó sin login o sesión guardada)
        if (PlayerPrefs.GetInt("BienvenidaCompletada_Global", 0) == 1)
        {
            return false;
        }

        return true;
    }

    [ContextMenu("Resetear Estado de Bienvenida (Para Pruebas)")]
    public void ResetearBienvenida()
    {
        PlayerPrefs.DeleteKey("BienvenidaCompletada_Global");
        if (PortalBridgeManager.Instance?.usuarioActual != null)
        {
            string correo = PortalBridgeManager.Instance.usuarioActual.correo;
            string uid = PortalBridgeManager.Instance.usuarioActual.usuarioIdGuid;
            if (!string.IsNullOrEmpty(correo)) PlayerPrefs.DeleteKey("BienvenidaCompletada_" + correo.ToLower());
            if (!string.IsNullOrEmpty(uid)) PlayerPrefs.DeleteKey("BienvenidaCompletada_" + uid);
            PortalBridgeManager.Instance.usuarioActual.esNuevo = true;
        }
        PlayerPrefs.Save();
        Debug.Log("[MainMenuController] Estado de bienvenida reseteado. El próximo inicio se considerará usuario nuevo.");
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
