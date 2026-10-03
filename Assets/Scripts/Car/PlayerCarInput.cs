using UnityEngine;

// Keyboard controls of the player car:
// W/S (Vertical) = accelerate / brake / reverse, A/D (Horizontal) = steer,
// Space = handbrake, R = back to the last checkpoint.
public class PlayerCarInput : MonoBehaviour
{
    [SerializeField] private CarController car;
    [SerializeField] private Racer racer;

    private void Update()
    {
        float throttle = Input.GetAxis("Vertical");
        float steering = Input.GetAxis("Horizontal");
        bool handbrake = Input.GetKey(KeyCode.Space);
        car.SetInput(throttle, steering, handbrake);

        if (Input.GetKeyDown(KeyCode.R) && car.ControlsEnabled)
        {
            racer.ResetToLastCheckpoint();
        }
    }
}
