using UnityEngine;

/// Componente para animar sprites de forma automática
/// Requiere un SpriteRenderer en el mismo GameObject
[RequireComponent(typeof(SpriteRenderer))]
public class Animated : MonoBehaviour
{
    [Header("Configuración de Animación")]
    [Tooltip("Array de sprites que forman la animación")]
    public Sprite[] sprites = new Sprite[0];

    [Tooltip("Tiempo entre cada frame de animación en segundos")]
    [Range(0.01f, 5.0f)]
    public float animationTime = 0.5f;

    [Tooltip("¿Debe repetirse la animación en bucle?")]
    public bool loop = true;

    [Tooltip("¿Iniciar la animación automáticamente?")]
    public bool playOnStart = true;

    [Header("Estado de la Animación")]
    [Tooltip("Frame actual de la animación (solo para lectura)")]
    [SerializeField] private int animationFrame;

    [Tooltip("¿Está la animación actualmente en reproducción?")]
    [SerializeField] private bool isPlaying;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Validar que tenemos sprites para animar
        if (sprites.Length == 0)
        {
            Debug.LogWarning($"[Animated] No hay sprites asignados en {gameObject.name}");
        }
    }

    /// Configuración inicial y inicio de la animación
    private void Start()
    {
        // Inicializar el frame en -1 para que Advance() lo configure correctamente
        animationFrame = -1;

        // Solo iniciar la animación si está habilitado y tenemos sprites
        if (playOnStart && sprites.Length > 0)
        {
            StartAnimation();
        }
    }

    /// Avanza al siguiente frame de la animación
    private void Advance()
    {
        // No animar si el renderer está deshabilitado o no hay sprites
        if (!spriteRenderer.enabled || sprites.Length == 0)
        {
            return;
        }

        // Avanzar al siguiente frame
        animationFrame++;

        // Manejar el final de la animación
        if (animationFrame >= sprites.Length)
        {
            if (loop)
            {
                // Reiniciar la animación desde el principio
                animationFrame = 0;
            }
            else
            {
                // Detener la animación en el último frame
                animationFrame = sprites.Length - 1;
                StopAnimation();
                return;
            }
        }

        // Aplicar el sprite actual si el índice es válido
        if (animationFrame >= 0 && animationFrame < sprites.Length)
        {
            spriteRenderer.sprite = sprites[animationFrame];
        }
    }

    #region Métodos Públicos

    /// Reinicia la animación desde el primer frame
    public void Restart()
    {
        animationFrame = -1;
        StartAnimation();
        Advance(); // Mostrar inmediatamente el primer frame
    }

    /// Inicia o reanuda la animación
    public void StartAnimation()
    {
        if (sprites.Length == 0)
        {
            Debug.LogWarning($"[Animated] No se puede iniciar animación sin sprites en {gameObject.name}");
            return;
        }

        if (!isPlaying)
        {
            isPlaying = true;
            InvokeRepeating(nameof(Advance), animationTime, animationTime);
        }
    }

    /// Pausa la animación
    public void StopAnimation()
    {
        if (isPlaying)
        {
            isPlaying = false;
            CancelInvoke(nameof(Advance));
        }
    }

    /// Pausa o reanuda la animación según su estado actual
    public void ToggleAnimation()
    {
        if (isPlaying)
        {
            StopAnimation();
        }
        else
        {
            StartAnimation();
        }
    }

    /// Establece un frame específico de la animación
    public void SetFrame(int frameIndex)
    {
        if (frameIndex >= 0 && frameIndex < sprites.Length)
        {
            animationFrame = frameIndex;
            spriteRenderer.sprite = sprites[animationFrame];
        }
        else
        {
            Debug.LogWarning($"[Animated] Índice de frame inválido: {frameIndex}. Debe estar entre 0 y {sprites.Length - 1}");
        }
    }

    #endregion

    #region Propiedades Públicas

    /// Indica si la animación está actualmente reproduciéndose
    public bool IsPlaying => isPlaying;

    /// Frame actual de la animación
    public int CurrentFrame => animationFrame;

    /// Número total de frames en la animación
    public int FrameCount => sprites.Length;

    #endregion

    #region Métodos de Unity Editor (Solo en desarrollo)

    private void OnValidate()
    {
        // Asegurar que animationTime no sea demasiado pequeño
        if (animationTime < 0.01f)
        {
            animationTime = 0.01f;
        }

        // Validar sprites array
        if (sprites != null)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null)
                {
                    Debug.LogWarning($"[Animated] Sprite en índice {i} es null en {gameObject.name}");
                }
            }
        }
    }

    #endregion
}
