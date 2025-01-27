using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PhysicsManagerSimple : MonoBehaviour
{
    // -----------------------------
    // STRUCT: PhysicsObject
    // -----------------------------
    // Holds the basic physics info for either a Harpoon or a Balloon:
    // - GameObject reference
    // - Position & Velocity
    // - Radius (for collisions)
    // - ObjectType enum
    // - (NEW) Rotation & Angular Velocity
    public struct PhysicsObject
    {
        public GameObject gameObject;    // The actual GameObject in the scene
        public Vector3 position;         // 2D position in the world
        public Vector3 velocity;         // 2D linear velocity
        public float radius;             // Used for collision checks
        public ObjectType objectType;    // Harpoon or Balloon?

        // NEW: Rotation fields
        // We'll measure these in degrees around the Z axis.
        public float rotation;           // Current rotation in degrees
        public float angularVelocity;    // Rotation speed in deg/sec
    }


    // -----------------------------
    // PUBLIC FIELDS
    // -----------------------------

    // UI for displaying player’s score
    public Text scoreUI;

    // Lists tracking all active Harpoons / Balloons in the scene
    public List<PhysicsObject> harpoons = new List<PhysicsObject>();
    public List<PhysicsObject> balloons = new List<PhysicsObject>();

    // Toggle debug lines/logging on or off
    public bool enableDebugging = true;

    // Gravity for harpoons (balloons have their own buoyancy approach)
    public float gravity = -9.8f;

    // Bounciness factor for balloon–balloon collisions: 0 = inelastic, 1 = fully elastic
    public float restitution = 0.8f;

    // The minimum upward velocity for balloons; ensures they always float up eventually
    public float minUpwardVelocity = 0.5f;

    // Factor for how strongly balloons push apart when overlapping
    public float softBodyElasticity = 0.5f;

    // (Optional) additional uniform damping if collisions feel too “floaty”
    public float softBodyDamping = 0.5f;

    // Distinguishes direct vs. tangential collisions for harpoon–balloon
    public float tangentialThreshold = 6f;

    // Strength of impulse on a tangential harpoon–balloon collision
    public float rigidImpulseStrength = 0.05f;

    // Toggle whether direct collisions destroy harpoon + balloon
    public bool enableDestruction = true;

    // Current score
    public int score = 0;

    // Balloon pop effect prefab
    public GameObject balloonPopPrefab;

    // -----------------------------
    // FRICTION & AIR-RESISTANCE
    // -----------------------------
    // For balloon–balloon collisions
    [Range(0f, 1f)]
    public float balloonCollisionFriction = 0.2f;

    [Range(0f, 1f)]
    public float balloonCollisionXDecay = 0.95f;  // Damping x-velocity after collision
    [Range(0f, 1f)]
    public float balloonCollisionYDecay = 1.0f;   // Damping y-velocity after collision

    // Continuous air resistance (mostly reducing horizontal velocity) each frame
    public float balloonAirResistance = 0.1f;

    // Buoyant recovery – how quickly balloon recovers upward speed if below minUpwardVelocity
    public float floatRecoveryRate = 1.0f;

    // Max upward speed to prevent infinite acceleration
    public float maxFloatVelocity = 2.0f;

    // Amplitude of the horizontal jitter. Increase to make the balloon wiggle more.
    public float balloonOscillationAmplitude = 0.05f;

    // Speed at which the horizontal jitter changes. Higher = faster variations.
    public float balloonOscillationSpeed = 1.0f;


    // -----------------------------
    // (NEW) ROTATIONAL TUNING
    // -----------------------------
    // How strongly collisions spin the balloon
    public float balloonTorqueFactor = 5f;

    // How quickly balloon “uprights” itself (angle -> 0)
    public float rotationRecoveryFactor = 1f;

    // Rotational drag factor: 0 = no drag, 1 = very high
    [Range(0f, 1f)]
    public float rotationDrag = 0.1f;

    // Max spin speed in deg/sec
    public float maxAngularSpeed = 360f;


    // -----------------------------
    // REFERENCES & INTERNAL LISTS
    // -----------------------------
    private SceneManager sceneManager;      // For terrain queries
    private Camera mainCamera;              // For checking offscreen culls

    // For cleaning up collisions each frame
    private List<PhysicsObject> harpoonsToDestroy = new List<PhysicsObject>();
    private List<PhysicsObject> balloonsToDestroy = new List<PhysicsObject>();
    private Dictionary<int, HashSet<int>> harpoonBalloonCollisions = new Dictionary<int, HashSet<int>>();


    // -----------------------------
    // UNITY LIFECYCLE
    // -----------------------------

    void Start()
    {
        // Attempt to find references in the scene
        sceneManager = FindObjectOfType<SceneManager>();
        mainCamera = Camera.main;

        if (sceneManager == null)
        {
            Debug.LogError("SceneManager not found.");
        }
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found.");
        }
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;

        // 1) Harpoon updates
        for (int i = 0; i < harpoons.Count; i++)
        {
            PhysicsObject harpoon = harpoons[i];
            UpdateHarpoonPhysics(ref harpoon, deltaTime);
            // Apply final position
            harpoon.gameObject.transform.position = harpoon.position;
            // Save it back
            harpoons[i] = harpoon;
        }

        // 2) Balloon updates
        for (int i = 0; i < balloons.Count; i++)
        {
            PhysicsObject balloon = balloons[i];
            UpdateBalloonPhysics(ref balloon, deltaTime);
            balloons[i] = balloon;
        }

        // 3) Balloon–Balloon collisions
        ApplySoftBodyCorrections();

        // 4) Harpoon–Balloon collisions
        CheckHarpoonBalloonCollisions();

        // 5) Destroy objects flagged for removal
        DestroyObjects();

        // 6) Remove balloons that float off screen
        RemoveOffscreenBalloons();
    }


    // -----------------------------
    // HARPOON PHYSICS
    // -----------------------------
    private void UpdateHarpoonPhysics(ref PhysicsObject obj, float deltaTime)
    {
        // Gravity
        obj.velocity.y += gravity * deltaTime;

        // Integrate position
        obj.position += obj.velocity * deltaTime;

        // Rotate harpoon sprite to face direction of travel
        // We'll reuse the old "UpdateRotation" approach for harpoons,
        // as harpoons don't have an 'upright' orientation to recover.
        if (obj.velocity.magnitude > 0.001f && obj.gameObject != null)
        {
            float angle = Mathf.Atan2(obj.velocity.y, obj.velocity.x) * Mathf.Rad2Deg;
            obj.gameObject.transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        // Check if harpoon hits the ground
        CheckHarpoonTerrainCollision(ref obj);
    }


    // -----------------------------
    // BALLOON PHYSICS
    // -----------------------------
    private void UpdateBalloonPhysics(ref PhysicsObject obj, float deltaTime)
    {
        if (obj.gameObject != null)
        {
            // We use the balloon's unique ID so each balloon "wobbles" differently
            int balloonID = obj.gameObject.GetInstanceID();

            // Perlin noise ranges from 0..1, so subtract 0.5f to get -0.5..+0.5
            float noiseValue = Mathf.PerlinNoise(
                Time.time * balloonOscillationSpeed + balloonID * 0.1f,  // x-coordinate in Perlin space
                0f) - 0.5f;

            // Scale by amplitude
            float xChaos = noiseValue * balloonOscillationAmplitude;

            // Add to balloon's horizontal velocity
            obj.velocity.x += xChaos;
        }
        // 1) Air resistance for linear velocity
        Vector3 v = obj.velocity;
        v.x *= (1f - balloonAirResistance * deltaTime);
        // If you also want vertical drag, do:
        // v.y *= (1f - 0.5f * balloonAirResistance * deltaTime);
        obj.velocity = v;

        // 2) Integrate balloon position
        obj.position += obj.velocity * deltaTime;

        // 3) Buoyant recovery for vertical speed
        if (obj.velocity.y < minUpwardVelocity)
        {
            // Slowly accelerate upward
            obj.velocity = new Vector3(
                obj.velocity.x,
                obj.velocity.y + (floatRecoveryRate * deltaTime),
                0f
            );

            // Clamp to minUpwardVelocity
            if (obj.velocity.y > minUpwardVelocity)
            {
                obj.velocity = new Vector3(
                    obj.velocity.x,
                    minUpwardVelocity,
                    0f
                );
            }
        }
        // (Optional) limit top speed
        if (obj.velocity.y > maxFloatVelocity)
        {
            obj.velocity = new Vector3(obj.velocity.x, maxFloatVelocity, 0f);
        }

        // 4) Terrain collisions
        CheckBalloonTerrainCollision(ref obj);
        HandleBalloonInterpenetration(ref obj);

        // 5) (NEW) Rotational physics for the balloon

        // 5a) Apply rotational drag
        obj.angularVelocity *= (1f - rotationDrag * deltaTime);

        // 5b) Limit max spin
        obj.angularVelocity = Mathf.Clamp(obj.angularVelocity, -maxAngularSpeed, maxAngularSpeed);

        // 5c) Rotational buoyancy: balloon tries to return to rotation=0 (upright).
        //     We treat the difference from 0 as a "spring" that adds torque.
        float angleDiff = -obj.rotation;  // If rotation>0, angleDiff is negative, etc.
        float springTorque = angleDiff * rotationRecoveryFactor;
        // Apply this torque as an acceleration to angularVelocity
        obj.angularVelocity += springTorque * deltaTime;

        // 5d) Integrate rotation
        obj.rotation += obj.angularVelocity * deltaTime;

        // 5e) Apply rotation to the sprite’s transform
        if (obj.gameObject != null)
        {
            // We assume the pivot is at the balloon's tie in the Sprite Editor
            obj.gameObject.transform.rotation = Quaternion.Euler(0f, 0f, obj.rotation);
            obj.gameObject.transform.position = obj.position;
        }
    }


    // -----------------------------
    // BALLOON–BALLOON COLLISIONS
    // -----------------------------
    private void ApplySoftBodyCorrections()
    {
        for (int i = 0; i < balloons.Count; i++)
        {
            for (int j = i + 1; j < balloons.Count; j++)
            {
                PhysicsObject balloonA = balloons[i];
                PhysicsObject balloonB = balloons[j];

                // Check collision
                if (IsColliding(balloonA, balloonB))
                {
                    // 1) Overlap & collision normal
                    Vector3 normal = (balloonB.position - balloonA.position).normalized;
                    float dist = Vector3.Distance(balloonA.position, balloonB.position);
                    float penetrationDepth = (balloonA.radius + balloonB.radius) - dist;

                    // 2) Separate if overlapping
                    if (penetrationDepth > 0)
                    {
                        Vector3 correction = normal * penetrationDepth * softBodyElasticity;
                        balloonA.position -= correction * 0.5f;
                        balloonB.position += correction * 0.5f;

                        // 3) Resolve with restitution-based bounce + friction
                        ResolveBalloonCollision(ref balloonA, ref balloonB, normal);

                        // (Optional) You could do an extra clamp if you never want them to go below minUpwardVelocity
                    }

                    // Save changes
                    balloons[i] = balloonA;
                    balloons[j] = balloonB;
                }
            }
        }
    }


    /// <summary>
    /// Performs restitution-based collision impulses for balloon–balloon,
    /// then adds friction, damping, and now spin torque to each balloon.
    /// </summary>
    private void ResolveBalloonCollision(ref PhysicsObject balloonA, ref PhysicsObject balloonB, Vector3 normal)
    {
        normal.Normalize();

        // Relative velocity
        Vector3 relVel = balloonA.velocity - balloonB.velocity;
        float relNormalVel = Vector3.Dot(relVel, normal);

        // If > 0, they're separating
        if (relNormalVel > 0f) return;

        float massA = 1f;
        float massB = 1f;

        // 1) Restitution impulse
        float j = -(1f + restitution) * relNormalVel / (1f / massA + 1f / massB);
        Vector3 impulse = j * normal;

        // Apply linear impulse
        balloonA.velocity += impulse / massA;
        balloonB.velocity -= impulse / massB;

        // 2) Tangential friction
        Vector3 tangent = relVel - (relNormalVel * normal);
        if (tangent.sqrMagnitude > 0.0001f)
        {
            tangent.Normalize();
            float relTangentVel = Vector3.Dot(relVel, tangent);
            float frictionImpulseMag = relTangentVel * balloonCollisionFriction;
            Vector3 frictionImpulse = frictionImpulseMag * tangent;

            balloonA.velocity -= frictionImpulse / massA;
            balloonB.velocity += frictionImpulse / massB;
        }

        // 3) Decay velocity in X & Y
        balloonA.velocity = new Vector3(
            balloonA.velocity.x * balloonCollisionXDecay,
            balloonA.velocity.y * balloonCollisionYDecay,
            0f
        );
        balloonB.velocity = new Vector3(
            balloonB.velocity.x * balloonCollisionXDecay,
            balloonB.velocity.y * balloonCollisionYDecay,
            0f
        );

        // 4) (Optional) softBodyDamping if you want an extra slow-down
        // balloonA.velocity *= softBodyDamping;
        // balloonB.velocity *= softBodyDamping;

        // 5) (NEW) Spin the balloons! 
        // We can approximate torque from tangential velocity:
        // Check how "sideways" the relative velocity is w.r.t. normal.
        // If normal is collision axis, tangent is side axis => we can
        // add angular velocity based on that tangent speed * balloonTorqueFactor.

        float tangentSpeed = Vector3.Dot(relVel, Vector3.Cross(normal, Vector3.forward).normalized);
        // The sign of 'tangentSpeed' tells direction of spin
        // Scale it up by balloonTorqueFactor for both balloons
        // Usually you'd apply torque to each balloon differently if you consider separate masses
        float spinA = tangentSpeed * balloonTorqueFactor;
        float spinB = -tangentSpeed * balloonTorqueFactor;

        balloonA.angularVelocity += spinA;
        balloonB.angularVelocity += spinB;
    }


    // -----------------------------
    // HARPOON–BALLOON COLLISIONS
    // -----------------------------
    private void CheckHarpoonBalloonCollisions()
    {
        for (int i = 0; i < harpoons.Count; i++)
        {
            PhysicsObject harpoon = harpoons[i];
            if (harpoon.gameObject == null) continue;
            // Get or create the set of balloons already hit by this harpoon
            int harpoonID = harpoon.gameObject.GetInstanceID();
            if (!harpoonBalloonCollisions.ContainsKey(harpoonID))
            {
                harpoonBalloonCollisions[harpoonID] = new HashSet<int>();
            }
            var hitBalloonsSet = harpoonBalloonCollisions[harpoonID];

            for (int j = 0; j < balloons.Count; j++)
            {
                PhysicsObject balloon = balloons[j];
                if (balloon.gameObject == null) continue;
                int balloonID = balloon.gameObject.GetInstanceID();

                // --- A) Skip if this harpoon has already hit this balloon ---
                if (hitBalloonsSet.Contains(balloonID))
                {
                    // Already collided before, do nothing
                    continue;
                }


                if (IsColliding(harpoon, balloon))
                {
                    // Record that this harpoon has now hit this balloon at least once
                    hitBalloonsSet.Add(balloonID);
                    // Normal from harpoon to balloon
                    Vector3 normal = (balloon.position - harpoon.position).normalized;
                    float relativeVelocity = Vector3.Dot(harpoon.velocity, normal);

                    if (relativeVelocity > tangentialThreshold)
                    {
                        // Head-on collision
                        if (enableDestruction)
                        {
                            // Destroy balloon (and harpoon if you want)
                            balloonsToDestroy.Add(balloon);
                            //harpoonsToDestroy.Add(harpoon); // If you want the harpoon also gone

                            score += 1;
                            scoreUI.text = "Score: " + score.ToString();

                            SpawnBalloonPopEffect(balloon.position);

                            //Debug.Log(score.ToString());

                            // Respawn a new balloon
                            SpawnNewBalloon();
                        }
                    }
                    else
                    {
                        // Tangential collision => balloon is pushed, harpoon continues
                        ApplyTangentialImpulse(ref balloon, ref harpoon);
                        // Keep harpoon going
                        harpoon.velocity = harpoon.velocity.normalized * Mathf.Max(1.0f, harpoon.velocity.magnitude);

                        // Debug line
                        if (enableDebugging)
                        {
                            Debug.DrawLine(harpoon.position, balloon.position, Color.yellow, 0.5f);
                        }
                    }

                    // Save updates
                    harpoons[i] = harpoon;
                    balloons[j] = balloon;
                }
            }
        }
    }

    /// <summary>
    /// On tangential harpoon–balloon collision, push balloon and add some spin.
    /// </summary>
    private void ApplyTangentialImpulse(ref PhysicsObject balloon, ref PhysicsObject harpoon)
    {
        // 1) Apply a small linear impulse
        Vector3 impulse = harpoon.velocity * rigidImpulseStrength;
        balloon.velocity += impulse;

        // 2) Add spin
        // For a simple approach, scale spin by harpoon velocity and 'balloonTorqueFactor'
        float crossZ = Vector3.Cross(harpoon.velocity, Vector3.up).z;
        float sign = Mathf.Sign(crossZ);
        float spinStrength = harpoon.velocity.magnitude * balloonTorqueFactor;
        balloon.angularVelocity += sign * spinStrength;

        if (enableDebugging)
        {
            Debug.DrawRay(balloon.position, balloon.velocity, Color.green, 0.5f);
        }
    }


    // -----------------------------
    // TERRAIN COLLISIONS
    // -----------------------------
    private void CheckHarpoonTerrainCollision(ref PhysicsObject harpoon)
    {
        Vector3 groundPoint = sceneManager.GetGroundPositionAtPoint(harpoon.position.x);

        if (harpoon.position.y <= groundPoint.y)
        {
            // Harpoon hits terrain
            harpoonsToDestroy.Add(harpoon);

            if (enableDebugging)
            {
                Debug.DrawLine(harpoon.position, groundPoint, Color.blue, 0.5f);
            }
        }
    }

    private void CheckBalloonTerrainCollision(ref PhysicsObject balloon)
    {
        Vector3 terrainPoint = sceneManager.GetTerrainPositionAtPoint(balloon.position.x);
        float distanceToTerrain = balloon.position.y - balloon.radius - terrainPoint.y;

        if (distanceToTerrain <= 0)
        {
            // Reflect velocity around the terrain normal
            Vector3 terrainNormal = CalculateTerrainNormal(balloon.position.x);
            Vector3 reflectedVelocity = Vector3.Reflect(balloon.velocity, terrainNormal);

            balloon.velocity = reflectedVelocity;
            balloon.position = new Vector3(balloon.position.x, terrainPoint.y + balloon.radius, 0);

            if (enableDebugging)
            {
                Debug.DrawLine(balloon.position, terrainPoint, Color.green, 0.5f);
            }
        }
    }

    // If the balloon center is "inside" terrain geometry, push it out
    private void HandleBalloonInterpenetration(ref PhysicsObject balloon)
    {
        Vector3 closestTerrainPoint = sceneManager.GetClosestTerrainPoint(balloon.position);
        float distanceToTerrain = Vector3.Distance(balloon.position, closestTerrainPoint);

        if (distanceToTerrain < balloon.radius / 2)
        {
            Vector3 directionToCorrect = (balloon.position - closestTerrainPoint).normalized;
            balloon.position = closestTerrainPoint + directionToCorrect * balloon.radius;
            balloon.velocity = Vector3.Reflect(balloon.velocity, directionToCorrect) * 0.5f;

            if (enableDebugging)
            {
                Debug.DrawLine(balloon.position, closestTerrainPoint, Color.yellow, 0.5f);
            }
        }
    }

    // Estimates terrain slope near xPosition
    private Vector3 CalculateTerrainNormal(float xPosition)
    {
        Vector3 terrainPointA = sceneManager.GetTerrainPositionAtPoint(xPosition - 0.01f);
        Vector3 terrainPointB = sceneManager.GetTerrainPositionAtPoint(xPosition + 0.01f);
        Vector3 slope = terrainPointB - terrainPointA;
        return new Vector3(-slope.y, slope.x, 0).normalized;
    }


    // -----------------------------
    // OBJECT DESTRUCTION & REMOVAL
    // -----------------------------
    private void DestroyObjects()
    {
        // Remove destroyed harpoons
        foreach (var harpoon in harpoonsToDestroy)
        {
            if (harpoon.gameObject != null)
            {
                int harpoonID = harpoon.gameObject.GetInstanceID();
                Destroy(harpoon.gameObject);
                // Clean up our dictionary
                if (harpoonBalloonCollisions.ContainsKey(harpoonID))
                {
                    harpoonBalloonCollisions.Remove(harpoonID);
                }
            }

            harpoons.Remove(harpoon);
        }
        harpoonsToDestroy.Clear();

        // Remove destroyed balloons
        foreach (var balloon in balloonsToDestroy)
        {
            if (balloon.gameObject != null)
            {
                Destroy(balloon.gameObject);
            }
            balloons.Remove(balloon);
        }
        balloonsToDestroy.Clear();
    }

    private void RemoveOffscreenBalloons()
    {
        for (int i = balloons.Count - 1; i >= 0; i--)
        {
            PhysicsObject balloon = balloons[i];
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(balloon.position);

            // If balloon goes above y=1 or off left/right, remove it
            if (viewportPos.y > 1f || viewportPos.x < 0f || viewportPos.x > 1f)
            {
                balloonsToDestroy.Add(balloon);
            }
        }
        DestroyObjects();
    }


    // -----------------------------
    // COLLISION HELPERS
    // -----------------------------
    private bool IsColliding(PhysicsObject objA, PhysicsObject objB)
    {
        float distance = Vector3.Distance(objA.position, objB.position);
        return distance < (objA.radius + objB.radius);
    }


    // -----------------------------
    // OBJECT ADD / SPAWN
    // -----------------------------
    public void AddPhysicsObject(GameObject obj, Vector3 initialVelocity, float radius, ObjectType objectType)
    {
        PhysicsObject newObj = new PhysicsObject
        {
            gameObject = obj,
            position = obj.transform.position,
            velocity = initialVelocity,
            radius = radius,
            objectType = objectType,

            // Initialize rotation for balloon: start upright with no spin
            rotation = 0f,
            angularVelocity = 0f
        };

        if (objectType == ObjectType.Harpoon)
        {
            harpoons.Add(newObj);
        }
        else if (objectType == ObjectType.Balloon)
        {
            balloons.Add(newObj);
        }
    }

    // Called when you want to spawn a new balloon in the valley
    private void SpawnNewBalloon()
    {
        // Implementation depends on your spawner logic. 
        // Possibly call a "BalloonSpawnerSimple" or something similar.
    }

    private void SpawnBalloonPopEffect(Vector3 spawnPosition)
    {
        // Assume you have a public reference to the BalloonPopEffect prefab:
        // public GameObject balloonPopPrefab;

        if (balloonPopPrefab == null) return;

        // Instantiate at the balloon’s position, no rotation
        Instantiate(balloonPopPrefab, spawnPosition, Quaternion.identity);
    }
}
