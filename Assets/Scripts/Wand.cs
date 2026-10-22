using UnityEngine;

public class Wand : MonoBehaviour
{
    [SerializeField] private GameObject wandVisual;
    private Vector3 rotationSpeed = new Vector3(0, 90f, 0);
    private void Update()
    {
        wandVisual.transform.Rotate(0f, rotationSpeed.y * Time.deltaTime, 0f, Space.World);
        Debug.Log(wandVisual.transform.localEulerAngles);
    }
}
