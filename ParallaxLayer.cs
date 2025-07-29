using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Configura��o da Esteira")]
    public float speed = 1f;
    public Transform[] beltSegments; // Segmentos da esteira (normalmente 2)

    private float spriteWidth;
    private Vector3[] initialPositions;
    private Camera mainCamera;
    private float leftCameraEdge;

    // NOVO: Vari�vel para controlar se a camada deve se mover
    private bool podeMover = false;

    void Start()
    {
        mainCamera = Camera.main;

        initialPositions = new Vector3[beltSegments.Length];
        for (int i = 0; i < beltSegments.Length; i++)
        {
            initialPositions[i] = beltSegments[i].position;
        }

        SpriteRenderer sr = beltSegments[0].GetComponent<SpriteRenderer>();
        spriteWidth = sr != null ? sr.bounds.size.x : Mathf.Abs(beltSegments[1].position.x - beltSegments[0].position.x);
    }

    void Update()
    {
        // Se n�o puder mover, simplesmente sai do Update
        if (!podeMover) return;

        CalculateCameraEdge();

        for (int i = 0; i < beltSegments.Length; i++)
        {
            if (!beltSegments[i].gameObject.activeSelf) continue;

            Transform segment = beltSegments[i];
            segment.Translate(Vector3.left * speed * Time.deltaTime);

            if (segment.position.x + spriteWidth / 2f < leftCameraEdge)
            {
                int outroIndex = i == 0 ? 1 : 0;
                segment.position = new Vector3(
                    beltSegments[outroIndex].position.x + spriteWidth,
                    initialPositions[i].y,
                    initialPositions[i].z
                );
            }
        }
    }

    void CalculateCameraEdge()
    {
        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        leftCameraEdge = mainCamera.transform.position.x - cameraWidth / 2f;
    }

    // NOVO: M�todo p�blico para controlar o movimento da camada
    public void SetPodeMover(bool mover)
    {
        this.podeMover = mover;
    }
}