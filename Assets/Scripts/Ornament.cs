using UnityEngine;

public class Ornament : MonoBehaviour
{
   public enum OrnamentColor
    {
        Red,
        Green,
        Gold
    }

    private Vector3 startPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Stores the starting position for return
        startPosition = transform.position;

    }

    public void ReturnToStart()
    {
        //Returns the ornament to its starting position
        transform.position = startPosition;
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
