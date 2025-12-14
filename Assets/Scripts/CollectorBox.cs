using UnityEngine;

public class CollectorBox : MonoBehaviour
{
    [SerializeField] private OrnamentColor boxColor;

    void OnTriggerStay2D(Collider2D other)
    {
        DragOrnaments ornament = other.GetComponent<DragOrnaments>();

        if (ornament != null && !ornament.isDragging)
        {
            if (ornament.ornamentColor == boxColor)
            {
                Debug.Log("Correct!");
                Destroy(ornament.gameObject);
            }
            else
            {
                Debug.Log("Wrong color!");
            }
        }
    }
}