using System.Numerics;

namespace Aether_NBody_Simulation;

/// <summary>
/// Motor de física del simulador.
/// </summary>
/// <remarks>
/// Por ahora actúa como esqueleto mínimo para dejar lista la integración futura
/// de RK4, cálculo de fuerzas, Barnes-Hut y paralelización.
/// </remarks>
public sealed class PhysicsEngine
{
    private const float GravitationalConstant = 1f; // Constante gravitatoria en unidades simplificadas.
    private const float Epsilon = 5f; // Distancia mínima para evitar singularidades.

    /// <summary>
    /// Actualiza el estado físico de todos los cuerpos durante un paso temporal.
    /// </summary>
    /// <param name="bodies">Lista de cuerpos a actualizar.</param>
    /// <param name="dt">Paso de tiempo, en segundos simulados.</param>
    public void Update(List<Body> bodies, float dt)
    {
        StepRK4(bodies, dt);
    }

    /// <summary>
    /// Calcula la aceleración gravitatoria ejercida sobre cada cuerpo.
    /// </summary>
    /// <param name="positions">Posiciones actuales de todos los cuerpos.</param>
    /// <param name="bodies">Lista de cuerpos del sistema.</param>
    /// <returns>Un array con la aceleración de cada cuerpo.</returns>
    private Vector2[] CalculateAccelerations(Vector2[] positions, List<Body> bodies)
    {
        var accelerations = new Vector2[bodies.Count];

        for (int i = 0; i < bodies.Count; i++)
        {
            Vector2 acc = Vector2.Zero;

            for (int j = 0; j < bodies.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                Vector2 deltaX = positions[j] - positions[i];
                float distanceSquared = deltaX.LengthSquared() + Epsilon * Epsilon;
                float inverseDistanceCubed = 1f / (float)Math.Pow(distanceSquared, 1.5f);

                acc += deltaX * (GravitationalConstant * bodies[j].Mass * inverseDistanceCubed);
            }

            accelerations[i] = acc;
        }

        return accelerations;
    }

    /// <summary>
    /// Avanza el sistema mediante una integración RK4 simple para el movimiento gravitatorio.
    /// </summary>
    /// <param name="bodies">Lista de cuerpos a simular.</param>
    /// <param name="dt">Paso de tiempo.</param>
    public void StepRK4(List<Body> bodies, float dt)
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

        // K1: derivada inicial.
        var k1Position = velocity0;
        var k1Velocity = CalculateAccelerations(position0, bodies);

        // K2: evaluamos en el punto intermedio.
        var position2 = new Vector2[n];
        var velocity2 = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            position2[i] = position0[i] + (dt * 0.5f) * k1Position[i];
            velocity2[i] = velocity0[i] + (dt * 0.5f) * k1Velocity[i];
        }

        var k2Position = velocity2;
        var k2Velocity = CalculateAccelerations(position2, bodies);

        // K3: segunda evaluación intermedia.
        var position3 = new Vector2[n];
        var velocity3 = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            position3[i] = position0[i] + (dt * 0.5f) * k2Position[i];
            velocity3[i] = velocity0[i] + (dt * 0.5f) * k2Velocity[i];
        }

        var k3Position = velocity3;
        var k3Velocity = CalculateAccelerations(position3, bodies);

        // K4: evaluación al final del paso.
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
}