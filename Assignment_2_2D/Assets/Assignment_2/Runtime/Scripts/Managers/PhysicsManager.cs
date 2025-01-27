using System.Collections.Generic;
using UnityEngine;

public class PhysicsManager : MonoBehaviour
{
    public struct PhysicsObject
    {
        public GameObject gameObject;
        public Vector3 position;
        public Vector3 previousPosition;
        public Vector3 velocity;
        public float radius;
        public ObjectType objectType;
        public CollisionMethod collisionMethod;
        public List<CollisionBehavior> possibleCollisionBehaviors;
        public float mass;
        public float chaoticMotionAmount;  // Chaos factor for horizontal jitter
    }

    public List<PhysicsObject> harpoons = new List<PhysicsObject>();
    public List<PhysicsObject> balloons = new List<PhysicsObject>();

    private SceneManager sceneManager;
    public float gravity = -9.8f;
    public bool enableDebugging = true;

    void Start()
    {
        sceneManager = FindObjectOfType<SceneManager>();
        if (sceneManager == null)
        {
            Debug.LogError("SceneManager not found in the scene.");
        }
    }

    void Update()
    {
        // Update harpoons
        for (int i = 0; i < harpoons.Count; i++)
        {
            PhysicsObject harpoon = harpoons[i];
            UpdateHarpoonPhysics(ref harpoon, Time.deltaTime);
            harpoon.gameObject.transform.position = harpoon.position;
            UpdateRotation(ref harpoon);
            harpoons[i] = harpoon;
        }

        // Update balloons
        for (int i = 0; i < balloons.Count; i++)
        {
            PhysicsObject balloon = balloons[i];
            UpdateBalloonPhysics(ref balloon, Time.deltaTime);
            balloon.gameObject.transform.position = balloon.position;
            balloons[i] = balloon;
        }

        // Check collisions
        CheckCollisions();

        // Debug visuals if enabled
        if (enableDebugging)
        {
            DrawDebuggingVisuals();
        }
    }
    private void UpdateRotation(ref PhysicsObject obj)
    {
        if (obj.velocity.magnitude > 0)
        {
            float angle = Mathf.Atan2(obj.velocity.y, obj.velocity.x) * Mathf.Rad2Deg;
            obj.gameObject.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void UpdateHarpoonPhysics(ref PhysicsObject obj, float deltaTime)
    {
        obj.velocity.y += gravity * deltaTime;
        obj.position += obj.velocity * deltaTime;

        if (enableDebugging)
        {
            Debug.DrawLine(obj.previousPosition, obj.position, Color.red);
        }

        obj.previousPosition = obj.position;
    }

    private void UpdateBalloonPhysics(ref PhysicsObject obj, float deltaTime)
    {
        // Apply chaotic motion and ensure the balloons rise
        obj.velocity.x += Random.Range(-obj.chaoticMotionAmount, obj.chaoticMotionAmount) * deltaTime;
        obj.velocity.y = 1.0f;  // Balloons should rise constantly

        obj.position += obj.velocity * deltaTime;

        if (enableDebugging)
        {
            Debug.DrawLine(obj.previousPosition, obj.position, Color.green);
        }

        obj.previousPosition = obj.position;
    }

    private void CheckCollisions()
    {
        for (int i = 0; i < harpoons.Count; i++)
        {
            for (int j = 0; j < balloons.Count; j++)
            {
                PhysicsObject harpoon = harpoons[i];
                PhysicsObject balloon = balloons[j];

                if (IsColliding(harpoon, balloon))
                {
                    // Visualize the collision
                    if (enableDebugging)
                    {
                        Debug.DrawLine(harpoon.position, balloon.position, Color.red, 1.0f);
                        DrawDebugCircle(balloon.position, balloon.radius, Color.green);
                        DrawDebugCircle(harpoon.position, harpoon.radius, Color.blue);
                    }
                    HandleCollision(harpoon, balloon);
                }
            }
        }

        // Similar for balloons and terrain, as well as balloon-to-balloon collisions
    }

    private void DrawDebugCircle(Vector3 center, float radius, Color color)
    {
        int segments = 36;  // Number of line segments to represent the circle
        float angleStep = 360f / segments;

        Vector3 previousPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);

            Debug.DrawLine(previousPoint, newPoint, color, 1.0f);
            previousPoint = newPoint;
        }
    }

    private bool IsColliding(PhysicsObject objA, PhysicsObject objB)
    {
        float distance = Vector3.Distance(objA.position, objB.position);
        return distance < (objA.radius + objB.radius);
    }

    private bool IsCollidingWithTerrain(PhysicsObject obj)
    {
        //Vector3 terrainPos = sceneManager.GetTerrainHeightAtPosition(obj.position.x);
        //return obj.position.y <= terrainPos.y;
        return true;
    }

    private void HandleCollision(PhysicsObject harpoon, PhysicsObject balloon)
    {
        Destroy(harpoon.gameObject);
        Destroy(balloon.gameObject);
        harpoons.Remove(harpoon);
        balloons.Remove(balloon);
    }

    private void HandleBalloonTerrainCollision(ref PhysicsObject balloon)
    {
        if (balloon.possibleCollisionBehaviors.Contains(CollisionBehavior.Bounce))
        {
            balloon.velocity.y = Mathf.Abs(balloon.velocity.y);
        }
    }
  
    private void HandleHarpoonTerrainCollision(PhysicsObject harpoon)
    {
        Destroy(harpoon.gameObject);
        harpoons.Remove(harpoon);
    }

    private void ResolveBalloonBalloonCollision(ref PhysicsObject balloon1, ref PhysicsObject balloon2)
    {
        Vector3 normal = (balloon1.position - balloon2.position).normalized;
        float relativeVelocity = Vector3.Dot(balloon1.velocity - balloon2.velocity, normal);

        if (relativeVelocity > 0) return;

        float restitution = 0.9f;

        float j = -(1 + restitution) * relativeVelocity;
        j /= 1 / balloon1.mass + 1 / balloon2.mass;

        Vector3 impulse = j * normal;
        balloon1.velocity += impulse / balloon1.mass;
        balloon2.velocity -= impulse / balloon2.mass;
    }

    private void DrawDebuggingVisuals()
    {
        foreach (var harpoon in harpoons)
        {
            Debug.DrawLine(harpoon.previousPosition, harpoon.position, Color.red);
        }

        foreach (var balloon in balloons)
        {
            DebugDrawCircle(balloon.position, balloon.radius, Color.green);
        }
    }

    private void DebugDrawCircle(Vector3 center, float radius, Color color)
    {
        int segments = 20;
        float angle = 2 * Mathf.PI / segments;

        for (int i = 0; i < segments; i++)
        {
            float theta1 = i * angle;
            float theta2 = (i + 1) * angle;

            Vector3 p1 = new Vector3(center.x + Mathf.Cos(theta1) * radius, center.y + Mathf.Sin(theta1) * radius, center.z);
            Vector3 p2 = new Vector3(center.x + Mathf.Cos(theta2) * radius, center.y + Mathf.Sin(theta2) * radius, center.z);

            Debug.DrawLine(p1, p2, color);
        }
    }

    public void AddPhysicsObject(GameObject obj, Vector3 initialVelocity, float radius, ObjectType objectType, CollisionMethod collisionMethod, List<CollisionBehavior> collisionBehaviors, float mass, float chaoticMotionAmount)
    {
        PhysicsObject newObj = new PhysicsObject
        {
            gameObject = obj,
            position = obj.transform.position,
            previousPosition = obj.transform.position,
            velocity = initialVelocity,
            radius = radius,
            objectType = objectType,
            collisionMethod = collisionMethod,
            possibleCollisionBehaviors = collisionBehaviors,
            mass = mass,
            chaoticMotionAmount = chaoticMotionAmount
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
}
