using UnityEngine;

/// Componente que representa un checkpoint en el juego Frogger.
/// Maneja la ocupación de los checkpoints por parte del jugador cuando llega al final del nivel.
[RequireComponent(typeof(BoxCollider2D))]
public class CheckPoint : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("GameObject de la rana que aparece cuando el checkpoint está ocupado")]
    public GameObject frog;

    [Tooltip("Puntos otorgados al ocupar este checkpoint")]
    [SerializeField] private int pointsReward = 50;

    [Header("Estado")]
    [Tooltip("¿Está actualmente ocupado este checkpoint?")]
    [SerializeField] private bool isOccupied = false;

    // Componente del collider (se obtiene automáticamente)
    private BoxCollider2D boxCollider;

    // Evento que se dispara cuando el checkpoint es ocupado
    public System.Action<CheckPoint> OnCheckPointOccupied;

    /// Inicialización de componentes y estado inicial
    private void Awake()
    {
        // Obtener referencia al collider y configurarlo como trigger
        boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;

        // Validar que la rana esté asignada
        if (frog == null)
        {
            Debug.LogWarning($"[CheckPoint] Falta asignar la rana en {gameObject.name}");
        }

        // Establecer estado inicial, checkpoint vacío, rana oculta
        SetOccupiedState(false);
    }

    /// Detecta cuando el jugador entra en el área del checkpoint
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Solo procesar si es el jugador y el checkpoint está disponible
        if (other.CompareTag("Player") && !isOccupied)
        {
            OccupyHouse();
        }
    }

    /// Ocupa este checkpoint cuando el jugador llega
    public void OccupyHouse()
    {
        // Verificar que el checkpoint esté disponible
        if (isOccupied)
        {
            Debug.LogWarning($"[CheckPoint] El checkpoint {gameObject.name} ya está ocupado");
            return;
        }

        // Marcar como ocupada y mostrar la rana
        SetOccupiedState(true);

        // Notificar al GameManager que un checkpoint fue ocupado
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckPointOccupied();
        }

        // Disparar evento para otros sistemas que lo necesiten
        OnCheckPointOccupied?.Invoke(this);
    }

    /// Libera este checkpoint para permitir nueva ocupación, útil para reset de nivel
    public void ReleaseCheckPoint()
    {
        if (!isOccupied) return;

        SetOccupiedState(false);
    }

    /// Configura el estado visual y funcional de este checkpoint
    private void SetOccupiedState(bool occupied)
    {
        isOccupied = occupied;

        // Mostrar/ocultar la rana según el estado
        if (frog != null)
        {
            frog.SetActive(occupied);
        }

        // Desactivar collider cuando está ocupado para evitar múltiples activaciones
        boxCollider.enabled = !occupied;
    }

    #region Propiedades Públicas

    /// Indica si este checkpoint está actualmente ocupado
    public bool IsOccupied => isOccupied;

    /// Puntos que otorga este checkpoint al ser ocupado
    public int PointsReward => pointsReward;

    #endregion

    #region Editor

    /// Validación en el editor para asegurar configuración correcta
    private void OnValidate()
    {
        // Los puntos no pueden ser negativos
        if (pointsReward < 0)
        {
            pointsReward = 0;
        }
    }

    /// Dibuja el área del checkpoint en el editor para visualización
    private void OnDrawGizmosSelected()
    {
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider2D>();

        // Color verde si está ocupada, amarillo si está libre
        Gizmos.color = isOccupied ? Color.green : Color.yellow;

        // Dibujar el área del collider
        Gizmos.DrawWireCube(transform.position + (Vector3)boxCollider.offset, boxCollider.size);
    }

    #endregion
}
