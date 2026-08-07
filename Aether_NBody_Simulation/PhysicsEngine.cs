using System.Numerics;

namespace Aether_NBody_Simulation;

/// <summary>
/// Motor de física del simulador.
/// </summary>

public sealed class PhysicsEngine
{
    private const float GravitationalConstant = 1f; // Constante gravitatoria en unidades simplificadas.
    private const float Epsilon = 5f; // Distancia mínima para evitar singularidades.

    /// <summary>
    /// Actualiza el estado físico de todos los cuerpos durante un paso temporal.
    /// </summary>

    public void Update(List<Body> bodies, float dt)
    {
        StepRK4(bodies, dt);

        foreach (var body in bodies)
        {
            body.RecordTrailPoint();
        }
    }

    /// <summary>
    /// Calcula la aceleración gravitatoria ejercida sobre cada cuerpo.
    /// </summary>
    
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

                // Vector de distancia relativa (de i hacia j).
                Vector2 deltaX = positions[j] - positions[i];

                // Distancia euclidiana con regularización:
                // distanceSquared = dx² + dy² + ε²
                // El Epsilon evita división entre cero cuando r=0.
                float distanceSquared = deltaX.LengthSquared() + Epsilon * Epsilon;

                // Inversa de r³ para la ley de gravitación.
                float inverseDistanceCubed = 1f / (float)Math.Pow(distanceSquared, 1.5f);

                // Aplicamos la ley: a⃗ += G * m_j * (r⃗ / r³)
                acc += deltaX * (GravitationalConstant * bodies[j].Mass * inverseDistanceCubed);
            }

            // Aceleración total sobre el cuerpo i = suma de atracciones de todos los j.
            accelerations[i] = acc;
        }

        return accelerations;
    }

    /// <summary>
    /// Avanza el sistema mediante una integración RK4 simple para el movimiento gravitatorio.
    /// </summary>

    public void StepRK4(List<Body> bodies, float dt)
    {
        int n = bodies.Count;

        // Sanidad: si no hay cuerpos o dt es cero, no hay nada que simular.
        if (n == 0 || dt <= 0f)
        {
            return;
        }

        // === FASE 1: GUARDAR ESTADO INICIAL ===
        // Necesitamos el estado (posición, velocidad) en t=0 para todas las evaluaciones de RK4.
        var position0 = new Vector2[n];
        var velocity0 = new Vector2[n];

        for (int i = 0; i < n; i++)
        {
            position0[i] = bodies[i].Position;
            velocity0[i] = bodies[i].Velocity;
        }

        // === K1: Evaluación en t=0 (estado actual) ===
        // K1Position = dr/dt = v(0)
        var k1Position = velocity0;
        // K1Velocity = dv/dt = a(r(0))
        var k1Velocity = CalculateAccelerations(position0, bodies);

        // === K2: Evaluación en t=dt/2 usando K1 ===
        // Estimamos dónde estaremos a mitad del paso de tiempo.
        var position2 = new Vector2[n];
        var velocity2 = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            // r(t + dt/2) ≈ r(t) + (dt/2) * v(t)
            position2[i] = position0[i] + (dt * 0.5f) * k1Position[i];
            // v(t + dt/2) ≈ v(t) + (dt/2) * a(t)
            velocity2[i] = velocity0[i] + (dt * 0.5f) * k1Velocity[i];
        }

        // Derivadas en el punto intermedio (usando K1).
        var k2Position = velocity2;
        var k2Velocity = CalculateAccelerations(position2, bodies);

        // === K3: Segunda evaluación en t=dt/2 usando K2 ===
        // Refinamos la aproximación intermedia (ahora con K2 en lugar de K1).
        var position3 = new Vector2[n];
        var velocity3 = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            // r(t + dt/2) ≈ r(t) + (dt/2) * v(t + dt/2)  [usando K2, más preciso]
            position3[i] = position0[i] + (dt * 0.5f) * k2Position[i];
            // v(t + dt/2) ≈ v(t) + (dt/2) * a(t + dt/2)
            velocity3[i] = velocity0[i] + (dt * 0.5f) * k2Velocity[i];
        }

        // Derivadas con la aproximación refinada de K2.
        var k3Position = velocity3;
        var k3Velocity = CalculateAccelerations(position3, bodies);

        // === K4: Evaluación en t=dt (final del paso) usando K3 ===
        // Estimamos el estado al final del paso de tiempo completo.
        var position4 = new Vector2[n];
        var velocity4 = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            // r(t + dt) ≈ r(t) + dt * v(t + dt/2)  [usando K3]
            position4[i] = position0[i] + dt * k3Position[i];
            // v(t + dt) ≈ v(t) + dt * a(t + dt/2)
            velocity4[i] = velocity0[i] + dt * k3Velocity[i];
        }

        // Derivadas finales en el punto predicho t+dt.
        var k4Position = velocity4;
        var k4Velocity = CalculateAccelerations(position4, bodies);

        // === COMBINACIÓN FINAL (ponderada): ===
        // La fórmula de RK4 combina los 4 puntos con pesos (1,2,2,1) / 6.
        for (int i = 0; i < n; i++)
        {
            // y_nuevo = y_viejo + (dt/6) * (K1 + 2*K2 + 2*K3 + K4)
            // Esto da una aproximación muy precisa del cambio en posición y velocidad.
            bodies[i].Position = position0[i] + (dt / 6f) * (k1Position[i] + 2f * k2Position[i] + 2f * k3Position[i] + k4Position[i]);
            bodies[i].Velocity = velocity0[i] + (dt / 6f) * (k1Velocity[i] + 2f * k2Velocity[i] + 2f * k3Velocity[i] + k4Velocity[i]);
            // Guardamos la aceleración final como valor de referencia (útil en debug/telemetría).
            bodies[i].Acceleration = k4Velocity[i];
        }
    }
}