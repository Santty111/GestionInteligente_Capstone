using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TogglePasswordVisibility : MonoBehaviour
{
    [Header("Referencias")]
    public TMP_InputField inputFieldPassword;
    public Button btnToggle;
    public Image iconoBoton;

    [Header("Sprites (Opcional)")]
    public Sprite iconoOjoAbierto;
    public Sprite iconoOjoCerrado;

    private bool mostrarPassword = false;

    private void Start()
    {
        if (btnToggle != null)
        {
            btnToggle.onClick.AddListener(AlternarVisibilidad);
        }

        ActualizarEstadoVisual();
    }

    public void AlternarVisibilidad()
    {
        mostrarPassword = !mostrarPassword;

        if (inputFieldPassword != null)
        {
            // Alterna el modo del campo entre Password y texto estándar
            inputFieldPassword.contentType = mostrarPassword 
                ? TMP_InputField.ContentType.Standard 
                : TMP_InputField.ContentType.Password;

            // Fuerza a TextMeshPro a redibujar los caracteres de inmediato
            inputFieldPassword.ForceLabelUpdate();
        }

        ActualizarEstadoVisual();
    }

    private void ActualizarEstadoVisual()
    {
        if (iconoBoton != null)
        {
            if (mostrarPassword && iconoOjoAbierto != null)
            {
                iconoBoton.sprite = iconoOjoAbierto;
            }
            else if (!mostrarPassword && iconoOjoCerrado != null)
            {
                iconoBoton.sprite = iconoOjoCerrado;
            }
        }
    }
}