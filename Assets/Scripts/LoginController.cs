using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginController : MonoBehaviour
{
    [Header("Campos UI (Figura 11)")]
    public TMP_InputField inputFieldEmail;
    public TMP_InputField inputFieldPassword;
    public Toggle toggleRemember;
    public Button btnLogin;

    [Header("Paneles de la Escena")]
    public GameObject panelLogin;
    public GameObject panelDashboard;

    private void Start()
    {
        Debug.Log("[LoginController] Script iniciado correctamente.");

        if (btnLogin != null)
        {
            // Limpia eventos previos para evitar duplicados y asigna el método
            btnLogin.onClick.RemoveAllListeners();
            btnLogin.onClick.AddListener(EjecutarLogin);
        }
        else
        {
            Debug.LogError("[LoginController] ERROR: 'btnLogin' no está asignado en el Inspector.");
        }
    }

    public void EjecutarLogin()
    {
        Debug.Log("[LoginController] Se hizo clic en el botón Login.");

        // 1. Verificación de referencias de texto
        if (inputFieldEmail == null || inputFieldPassword == null)
        {
            Debug.LogError("[LoginController] ERROR: Faltan asignar los InputFields en el Inspector.");
            return;
        }

        string email = inputFieldEmail.text.Trim();
        string password = inputFieldPassword.text;

        Debug.Log($"[LoginController] Email detectado: '{email}' | Longitud Password: {password.Length}");

        // 2. Validación de campos vacíos
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("[LoginController] AVISO: Debe escribir algo en el correo y contraseña para continuar.");
            return;
        }

        // 3. Verificación de PortalBridgeManager
        if (PortalBridgeManager.Instance == null)
        {
            Debug.LogError("[LoginController] ERROR CRÍTICO: No existe PortalBridgeManager en la escena. Crea un objeto vacío con ese script.");
            return;
        }

        // 4. Ejecución del Login
        PortalBridgeManager.Instance.IniciarSesion(email, password, (exito, rol) =>
        {
            Debug.Log($"[LoginController] Respuesta del Puente -> Éxito: {exito}, Rol: {rol}");

            if (exito)
            {
                if (panelLogin != null)
                {
                    panelLogin.SetActive(false);
                    Debug.Log("[LoginController] Panel Login apagado.");
                }
                else
                {
                    Debug.LogError("[LoginController] ERROR: 'panelLogin' está vacío en el Inspector.");
                }

                if (panelDashboard != null)
                {
                    panelDashboard.SetActive(true);
                    Debug.Log("[LoginController] Panel Dashboard ENCENDIDO.");
                }
                else
                {
                    Debug.LogError("[LoginController] ERROR: 'panelDashboard' está vacío en el Inspector.");
                }
            }
        });
    }
}