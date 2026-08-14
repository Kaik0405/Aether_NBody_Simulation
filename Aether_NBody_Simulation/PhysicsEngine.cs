using System.Numerics;

namespace Aether_NBody_Simulation;

public enum PhysicsSolverMode
{
    Naive,
    Quadtree
}

/// <summary>
/// Motor de física del simulador.
/// Permite comparar el método clásico O(n²) con Barnes-Hut (Quadtree).
/// </summary>
public sealed class PhysicsEngine
{
    private const float GravitationalConstant = 1f;
    private const float Epsilon = 5f;

    /// <summary>
    /// Valor theta de Barnes-Hut: menor => más preciso, mayor => más rápido.
    /// </summary>
    public float Theta { get; set; } = 0.6f;

    /// <summary>
    /// Modo de resolución de aceleraciones.
    /// </summary>
    public PhysicsSolverMode SolverMode { get; set; } = PhysicsSolverMode.Quadtree;

    /// <summary>
    /// Si está desactivado, se evita el coste de registrar estelas en cada frame.
    /// </summary>
    public bool RecordTrails { get; set; } = false;

    private QuadTreeNode? quadTreeRoot;

    public void Update(List<Body> bodies, float dt)
    {
        // RK4 usa varias evaluaciones de aceleración por paso; el solver elegido decide cómo se calculan.
        StepRK4(bodies, dt);

        if (!RecordTrails)
        {
            return;
        }

        foreach (var body in bodies)
        {
            body.RecordTrailPoint();
        }
    }

    private void StepRK4(List<Body> bodies, float dt)
    {
        int n = bodies.Count;
        if (n == 0 || dt <= 0f)
        {
            return;
        }

        var position0 = new Vector2[n];
        var velocity0 = new Vector2[n];

        for (int i = 0; i < n; i++)
        {
            position0[i] = bodies[i].Position;
            velocity0[i] = bodies[i].Velocity;
        }

        var k1Position = velocity0;
        var k1Velocity = CalculateAccelerations(position0, bodies);

        var position2 = new Vector2[n];
        var velocity2 = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            position2[i] = position0[i] + (dt * 0.5f) * k1Position[i];
            velocity2[i] = velocity0[i] + (dt * 0.5f) * k1Velocity[i];
        }

        var k2Position = velocity2;
        var k2Velocity = CalculateAccelerations(position2, bodies);

        var position3 = new Vector2[n];
        var velocity3 = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            position3[i] = position0[i] + (dt * 0.5f) * k2Position[i];
            velocity3[i] = velocity0[i] + (dt * 0.5f) * k2Velocity[i];
        }

        var k3Position = velocity3;
        var k3Velocity = CalculateAccelerations(position3, bodies);

        var position4 = new Vector2[n];
        var velocity4 = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            position4[i] = position0[i] + dt * k3Position[i];
            velocity4[i] = velocity0[i] + dt * k3Velocity[i];
        }

        var k4Position = velocity4;
        var k4Velocity = CalculateAccelerations(position4, bodies);

        for (int i = 0; i < n; i++)
        {
            bodies[i].Position = position0[i] + (dt / 6f) * (k1Position[i] + 2f * k2Position[i] + 2f * k3Position[i] + k4Position[i]);
            bodies[i].Velocity = velocity0[i] + (dt / 6f) * (k1Velocity[i] + 2f * k2Velocity[i] + 2f * k3Velocity[i] + k4Velocity[i]);
            bodies[i].Acceleration = k4Velocity[i];
        }
    }

    private Vector2[] CalculateAccelerations(Vector2[] positions, List<Body> bodies)
    {
        // El mismo integrador puede usar el método exacto O(n²) o Barnes-Hut según el modo seleccionado.
        return SolverMode == PhysicsSolverMode.Naive
            ? CalculateAccelerationsNaive(positions, bodies)
            : CalculateAccelerationsFromTree(positions, bodies);
    }

    private Vector2[] CalculateAccelerationsNaive(Vector2[] positions, List<Body> bodies)
    {
        // Versión clásica: cada cuerpo interactúa con todos los demás.
        int n = bodies.Count;
        var accelerations = new Vector2[n];

        for (int i = 0; i < n; i++)
        {
            Vector2 acc = Vector2.Zero;
            Vector2 positionI = positions[i];

            for (int j = 0; j < n; j++)
            {
                if (i == j)
                {
                    continue;
                }

                Vector2 direction = positions[j] - positionI;
                float distanceSq = direction.LengthSquared() + Epsilon * Epsilon;
                float distance = MathF.Sqrt(distanceSq);
                if (distance <= 0.0001f)
                {
                    continue;
                }

                float scale = GravitationalConstant * bodies[j].Mass / (distanceSq * distance);
                acc += direction * scale;
            }

            accelerations[i] = acc;
        }

        return accelerations;
    }

    private Vector2[] CalculateAccelerationsFromTree(Vector2[] positions, List<Body> bodies)
    {
        // Barnes-Hut: primero se construye el árbol espacial y luego se consulta la masa agregada.
        BuildQuadTree(positions, bodies);

        int n = positions.Length;
        var accelerations = new Vector2[n];
        if (quadTreeRoot is null)
        {
            return accelerations;
        }

        for (int i = 0; i < n; i++)
        {
            accelerations[i] = GetAccelerationFromTree(positions[i], i, quadTreeRoot);
        }

        return accelerations;
    }

    private Vector2 GetAccelerationFromTree(Vector2 position, int bodyIndex, QuadTreeNode node)
    {
        // Si el nodo está vacío no aporta fuerza alguna.
        if (node.IsEmpty)
        {
            return Vector2.Zero;
        }

        if (node.IsLeaf)
        {
            if (node.BodyIndex < 0 || node.BodyIndex == bodyIndex)
            {
                return Vector2.Zero;
            }

            return PairAcceleration(position, node.CenterOfMass, node.TotalMass);
        }

        Vector2 direction = node.CenterOfMass - position;
        float distance = direction.Length();
        if (distance <= 0.0001f)
        {
            return Vector2.Zero;
        }

        float size = node.Boundary.HalfDimension * 2f;
        // Si el nodo está lo bastante lejos respecto a su tamaño, se aproxima como una única masa.
        if ((size / distance) < Theta)
        {
            return PairAcceleration(position, node.CenterOfMass, node.TotalMass);
        }

        Vector2 total = Vector2.Zero;
        if (node.NW is not null) total += GetAccelerationFromTree(position, bodyIndex, node.NW);
        if (node.NE is not null) total += GetAccelerationFromTree(position, bodyIndex, node.NE);
        if (node.SW is not null) total += GetAccelerationFromTree(position, bodyIndex, node.SW);
        if (node.SE is not null) total += GetAccelerationFromTree(position, bodyIndex, node.SE);
        return total;
    }

    private static Vector2 PairAcceleration(Vector2 from, Vector2 to, float mass)
    {
        // Esta ayuda centraliza el cálculo gravitatorio para no duplicar la misma fórmula en dos rutas.
        Vector2 direction = to - from;
        float distanceSq = direction.LengthSquared() + Epsilon * Epsilon;
        float distance = MathF.Sqrt(distanceSq);
        if (distance <= 0.0001f)
        {
            return Vector2.Zero;
        }

        float scale = GravitationalConstant * mass / (distanceSq * distance);
        return direction * scale;
    }

    private static (Vector2 center, float halfDimension) CalculateWorldBounds(Vector2[] positions)
    {
        // Ajusta el árbol al área mínima razonable para que la subdivisión no crezca de más.
        if (positions.Length == 0)
        {
            return (Vector2.Zero, 1000f);
        }

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < positions.Length; i++)
        {
            Vector2 p = positions[i];
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;
        }

        Vector2 center = new((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        float width = maxX - minX;
        float height = maxY - minY;
        float maxDimension = MathF.Max(width, height);
        float halfDimension = MathF.Max(1f, (maxDimension * 0.5f) * 1.1f);

        return (center, halfDimension);
    }

    private void BuildQuadTree(Vector2[] positions, List<Body> bodies)
    {
        // Se crea el árbol con los límites del conjunto actual y luego se cargan todas las partículas.
        var (center, halfDimension) = CalculateWorldBounds(positions);
        quadTreeRoot = new QuadTreeNode(new BoundingBox(center, halfDimension));

        for (int i = 0; i < positions.Length; i++)
        {
            quadTreeRoot.Insert(i, positions[i], bodies[i].Mass);
        }

        quadTreeRoot.RecomputeMassDistribution();
    }
}