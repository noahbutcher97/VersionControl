using UnityEngine;

public class Balloon : MonoBehaviour
{
    public float riseSpeed = 1f;         // Speed at which the balloon rises
    public float chaoticMotionAmount = 0.5f; // Amount of random movement (air currents)
    private Vector3 velocity;            // Velocity of the balloon

    void Start()
    {
        // Initial velocity is mostly upward with some random horizontal motion
        velocity = new Vector3(Random.Range(-chaoticMotionAmount, chaoticMotionAmount), riseSpeed, 0);
    }

    void Update()
    {
        // Update the position based on the velocity
        transform.position += velocity * Time.deltaTime;

        // Optionally, add random wind/air current effects
        ApplyAirCurrents();
    }

    private void ApplyAirCurrents()
    {
        // Simulate random air currents by applying small random changes to the horizontal velocity
        velocity.x += Random.Range(-chaoticMotionAmount, chaoticMotionAmount) * Time.deltaTime;
    }
}
