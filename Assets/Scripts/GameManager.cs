using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Update()
    {
        
        if (Inputs.Instance != null)
        {
            if (Inputs.Instance.ConfirmPressed)
            {
                
                Debug.Log("Confirm action triggered!");
            }

            if (Inputs.Instance.BackPressed)
            {
                Debug.Log("Back action triggered!");
            }

            Vector2 move = Inputs.Instance.MoveInput;
            if (move != Vector2.zero)
            {
                
            }
        }
    }
}
