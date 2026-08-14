using System.Numerics;

namespace Aether_NBody_Simulation;

/// <summary>
/// Motor de física del simulador.
/// </summary>

public sealed class PhysicsEngine
{
    private const float GravitationalConstant = 1f; // Constante gravitatoria en unidades simplificadas.
    private const float Epsilon = 5f; // Distancia mínima para evitar singularidades.
    private const float Theta = 0.5f;
    /// <summary>
    /// Actualiza el estado físico de todos los cuerpos durante un paso temporal.
    /// </summary>
    private QuadTreeNode quadTreeNode; // Arbol de particion espacial
    public void Update(List<Body> bodies, float dt)
    {
        BuildQuadTree(bodies);
        StepRK4(bodies, dt);

        foreach (var body in bodies)
        {
            body.RecordTrailPoint();
        }
    }

    /// <summary>
    /// Calcula la aceleración gravitatoria ejercida sobre cada cuerpo.
    /// </summary>
    
    // Metodo recursivo para calcular las aceleraciones con el Quadtree
    private Vector2 GetAccelerarionFromTree(Vector2 position,QuadTreeNode node, float theta, float G, float epsilon)
    {
        if(node == null || node.IsEmpty)
            return Vector2.Zero;

       // Si es hoja y es el mismo cuerpo
        if(node.IsLeaf && node.Body.Position == position)
            return Vector2.Zero;

        Vector2 direction = node.CenterOfMass - position;
        float distanceSq = direction.LengthSquared();
        float distance = MathF.Sqrt(distanceSq);
        float size = node.Boundary.HalfDimension / 2.0f;

        // Criterio theta y si es hoja
        if (node.IsLeaf || (size / distance)< theta)
        {
            //Tratamos al nodo como un punto de masa consentrado
            float forceMagnitude = (G * node.TotalMass) / (distanceSq+epsilon*epsilon);
            Vector2 unitDirection = direction / (distance+0.0001f);

            return unitDirection*forceMagnitude;
        }
        // Si esta muy cerca y no es hoja
        Vector2 totalAcceleration = Vector2.Zero;

        totalAcceleration = GetAccelerarionFromTree(position,node.NE,theta,G,epsilon);
        totalAcceleration = totalAcceleration + GetAccelerarionFromTree(position,node.NW,theta,G,epsilon);
        totalAcceleration = totalAcceleration + GetAccelerarionFromTree(position,node.SE,theta,G,epsilon);
        totalAcceleration = totalAcceleration + GetAccelerarionFromTree(position,node.SW,theta,G,epsilon);
        
        return totalAcceleration;
    }

    // Metodo para calcular todas las aceleraciones de los cuerpos
    private Vector2[] CalculateAccelerations(Vector2[] positions)
    {
        var accelerations = new Vector2[positions.Count];

        for (int i = 0; i < positions.Count; i++)
            accelerations[i] = GetAccelerarionFromTree(positions[i],quadTreeNode,Theta,GravitationalConstant,Epsilon);   

        return accelerations;
    }

    // Metodo que soluciona la EDO que describle el movimiento de los cuerpos en el espacio con posciones y velocidades
    private void StepRK4(List<Body> bodies, float dt)
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
 
    // Funcion para calcular las dimenciones iniciales en las que de se van a dividir los cuadrantes
    private (Vector2,float) CalculeteWorldBounds(List<Body> bodies)
    {
        // Verifica si hay cuerpos
        if (bodies.Count == 0)
            return(Vector2.Zero, 1000f);
        
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (Body b in bodies)
        {
            if(b.Position.X < minX) minX = b.Position.X;
            if(b.Position.Y < minY) minY = b.Position.Y;
            if(b.Position.X > maxX) maxX = b.Position.X;
            if(b.Position.Y > maxY) maxY = b.Position.Y;
        }
        // Determina el vector central usando la mitad de las sumas minimas y maximas de las coordenadas
        Vector2 center = new Vector2((minX+maxX)/2f, (minY+maxY)/2f);
        
        float w = maxX-minX;
        float h = maxY-minY;
        
        // Deter
        float maxDimension = Math.Max(w, h);
        float halfDimension = (maxDimension/2.0f) * 1.05f;
        
        if (halfDimension < 1.0f) halfDimension = 1.0f;

        return (center, halfDimension);
    } 
    
    //Metodo que construye el QuadTree incertando los nodos
    private void BuildQuadTree(List<Body> bodies)
    {
        BoundingBox boundingBox = new BoundingBox(CalculeteWorldBounds(bodies));
        quadTreeNode = new QuadTreeNode(boundingBox); 
        
        foreach (var body in bodies)
            quadTree.Insert(body);
    }
}