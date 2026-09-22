using System.Numerics;
using Raylib_cs;

namespace Aether_NBody_Simulation;
public static class GalaxyBuilder
{
    public sealed record SceneDefinition(string Name, string Description, Func<SimulationScene> Factory);

    private static float CircularOrbitSpeed(float centralMass, float radius)
    {
        // v = sqrt(GM/r): las órbitas se generan con la misma escala que usa el motor físico.
        return MathF.Sqrt(PhysicsEngine.GravitationalConstant * centralMass / MathF.Max(radius, 1f));
    }

    public static IReadOnlyList<SceneDefinition> GetAvailableScenes() => new List<SceneDefinition>
    {
        new("Sistema de prueba", "Un sistema solar simple con una estrella y varios planetas.", () => new SolarSystemScene("Sistema de prueba", "Un sistema solar simple con una estrella y varios planetas.", CreateTestGalaxy())),
        new("Tres cuerpos caótico", "Un sistema compacto con un agujero negro y dos cuerpos en órbita.", () => new SolarSystemScene("Tres cuerpos caótico", "Un sistema compacto con un agujero negro y dos cuerpos en órbita.", CreateChaoticThreeBodySystem2())),
        new("Sistema binario", "Dos estrellas y un planeta que orbita alrededor del par.", () => new SolarSystemScene("Sistema binario", "Dos estrellas y un planeta que orbita alrededor del par.", CreateBinaryStarSystem())),
        new("Cinturón de asteroides", "Un sol central con miles de asteroides en órbita.", () => new GalaxyScene("Cinturón de asteroides", "Un sol central con miles de asteroides en órbita.", CreateAsteroidBeltSystem())),
        new("Anillo orbital", "Un sistema de anillo con satélites alrededor de una estrella central.", () => new GalaxyScene("Anillo orbital", "Un sistema de anillo con satélites alrededor de una estrella central.", CreateOrbitalRingSystem())),
        new("Stress Quadtree 10000", "Escenario de estrés para Barnes-Hut con 10k cuerpos orbitando un núcleo.", () => new GalaxyScene("Stress Quadtree 10000", "Escenario de estrés para Barnes-Hut con 10k cuerpos orbitando un núcleo.", CreateQuadtreeStressSystem(100000))),
        new("Galaxia Espiral Realista", "Cuásar supermasivo, materia oscura, brazos espirales y sistemas solares.", () => new GalaxyScene("Galaxia Espiral Realista", "Galaxia construida alrededor de un cuásar supermasivo.", CreateRealisticSpiralGalaxy())),
        new("Galaxia Espiral Amplia", "Brazos más largos y definidos alrededor de un cuásar central.", () => new GalaxyScene("Galaxia Espiral Amplia", "Perfil de galaxia con brazos grandes y navegación a escala galáctica.", CreateWideSpiralGalaxy())),
        new("Universo Fase 1: galaxias", "Varias galaxias separadas con probabilidad de colisión entre sus cuásares.", () => new GalaxyScene("Universo Fase 1: galaxias", "Universo inicial con galaxias, sistemas planetarios y encuentros gravitatorios.", CreatePhaseOneMultiGalaxyUniverse())),
        new("Universo Fase 2: Big Bang", "Expansión inicial de varias galaxias nacidas de un estado comprimido.", () => new GalaxyScene("Universo Fase 2: Big Bang", "Múltiples galaxias se separan durante una expansión cósmica controlada.", CreateBigBangUniverse())),
        new("Colisión de galaxias", "Dos galaxias de 1000 cuerpos cada una en curso de choque directo.", () => new GalaxyScene("Colisión de galaxias", "Dos galaxias de 1000 cuerpos cada una en curso de choque directo.", CreateGalaxyCollisionSystem())),
        new("Agujero negro: espaguetificación", "Prueba visual del estiramiento, pixelación y absorción de cuerpos.", () => new SolarSystemScene("Agujero negro: espaguetificación", "Prueba visual de absorción gravitatoria.", CreateBlackHoleSpaghettificationSystem())),
        new("Púlsar 2D", "Estrella de neutrones con dos haces de plasma giratorios.", () => new SolarSystemScene("Púlsar 2D", "Púlsar con jets polares animados.", CreatePulsarSystem()))
    };

    public static List<Body> CreateTestGalaxy()
    {
        // === SISTEMA DE PRUEBA: 1 SOL + 4 PLANETAS EN ÓRBITA ===

        return new List<Body>
        {
            new StarBody(
                position: Vector2.Zero,
                velocity: Vector2.Zero,
                mass: 4500000f,
                radius: 50f,
                color: new Color(255, 255, 0, 255), // Amarillo para el sol
                isStatic: true
                ), 
                

            new PlanetBody(
                position: new Vector2(420f, 0f),
                velocity: new Vector2(0f, 95f),
                mass: 180f,
                radius: 10f,
                color: new Color(0, 0, 255, 255), // Azul para el planeta principal
                isStatic: false
                ), 

            new PlanetBody(
                position: new Vector2(0f, 360f),
                velocity: new Vector2(-100f, 0f),
                mass: 140f,
                radius: 8f,
                color: new Color(0, 255, 0, 255), // Verde para un planeta secundario
                isStatic: false
                ), 

            new PlanetBody(
                position: new Vector2(-560f, 0f),
                velocity: new Vector2(0f, -82f),
                mass: 110f,
                radius: 7f,
                color: new Color(255, 0, 255, 255), // Magenta para otro planeta
                isStatic: false
                ), 

            new PlanetBody(
                position: new Vector2(0f, -470f),
                velocity: new Vector2(90f, 0f),
                mass: 100f,
                radius: 6f,
                color: new Color(255, 165, 0, 255), // Naranja para el cuarto planeta
                isStatic: false
                ) 
        };
    }

    /// <summary>
    /// Crea una configuración de tres cuerpos pensada para exhibir un comportamiento caótico.
    /// </summary>
    /// 
    /// <summary>
    /// Configuración caótica equilibrada para Leapfrog (Momento total = 0, masas moderadas).
    /// </summary>
    public static List<Body> CreateChaoticThreeBodySystem1()
    {
        // Masas reducidas a 60,000 para evitar que la aceleración explote 
        // cuando dos cuerpos pasan muy cerca con dt constante.
        float mass = 80000f;

        return new List<Body>
    {
        // Cuerpo 1 (Rojo) - Abajo Izquierda
        new PlanetBody(
            position: new Vector2(-280f, -150f),
            velocity: new Vector2(30f, 45f),
            mass: mass,
            radius: 12f,
            color: new Color(255, 110, 110, 255),
            isStatic: false
        ), 

        // Cuerpo 2 (Verde) - Abajo Derecha
        new PlanetBody(
            position: new Vector2(280f, -150f),
            velocity: new Vector2(-27f, 43f),
            mass: mass,
            radius: 12f,
            color: new Color(110, 255, 150, 255),
            isStatic: false
        ), 

        // Cuerpo 3 (Azul) - Arriba Centro
        // Nota: (25 - 22 - 3 = 0) y (40 + 38 - 78 = 0) -> Momento neto cero
        new PlanetBody(
            position: new Vector2(0f, 300f),
            velocity: new Vector2(-8f, -83f),
            mass: mass,
            radius: 12f,
            color: new Color(110, 180, 255, 255),
            isStatic: false
        )
    };
    }
    public static List<Body> CreateChaoticThreeBodySystem2()
    {
        // Las masas ahora son iguales y el triángulo inicial es más compacto.
        // Con eso los cuerpos se mantienen más “pegados” y se influencia entre sí de forma más visible.
        //
        // La pequeña asimetría en las velocidades conserva un comportamiento sensible/caótico,
        // pero sin que el sistema se desarme tan rápido.
        return new List<Body>
        {
            new BlackHoleBody(
                position: new Vector2(-120f, -70f),
                velocity: new Vector2(21f, 41f),
                mass: 320000f,
                radius: 14f,
                color: new Color(255, 120, 120, 255),
                isStatic: false
                ), 

            new PlanetBody(
                position: new Vector2(120f, -70f),
                velocity: new Vector2(-19f, 39f),
                mass: 320000f,
                radius: 14f,
                color: new Color(120, 255, 160, 255),
                isStatic: false
                ), 

            new PlanetBody(
                position: new Vector2(0f, 140f),
                velocity: new Vector2(-2f, -80f),
                mass: 320000f,
                radius: 14f,
                color: new Color(120, 180, 255, 255),
                isStatic: false
                ) 
        };
    }

    /// <summary>
    /// Crea dos estrellas orbitando entre sí con un planeta lejano.
    /// </summary>
    public static List<Body> CreateBinaryStarSystem()
    {
        return new List<Body>
        {
            new StarBody(
                position: new Vector2(-120f, 0f),
                velocity: new Vector2(0f, -55f),
                mass: 1800000f,
                radius: 24f,
                color: new Color(255, 210, 120, 255),
                isStatic: false
                ),

            new StarBody(
                position: new Vector2(120f, 0f),
                velocity: new Vector2(0f, 55f),
                mass: 1800000f,
                radius: 24f,
                color: new Color(255, 160, 80, 255),
                isStatic: false
                ),

            new PlanetBody(
                position: new Vector2(0f, 380f),
                velocity: new Vector2(-88f, 0f),
                mass: 160f,
                radius: 8f,
                color: new Color(120, 190, 255, 255),
                isStatic: false
                )
        };
    }

    /// <summary>
    /// Genera un sistema visualmente rico con un sol central y múltiples asteroides.
    /// </summary>
    public static List<Body> CreateAsteroidBeltSystem()
    {
        var bodies = new List<Body>
        {
            new StarBody(
                position: Vector2.Zero,
                velocity: Vector2.Zero,
                mass: 4000000f,
                radius: 46f,
                color: new Color(255, 240, 140, 255),
                isStatic: true
                )
        };

        // Cinturón con cuerpos pequeños a distintas distancias para dar variedad visual.
        for (int i = 0; i < 1000; i++)
        {
            float angle = i * MathF.Tau / 18f;
            float radius = 220f + (i % 4) * 34f;
            Vector2 position = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            Vector2 tangent = new Vector2(-position.Y, position.X);
            tangent = Vector2.Normalize(tangent);

            float speed = 125f + (i % 5) * 8f;
            bodies.Add(new AsteroidBody(
                position: position,
                velocity: tangent * speed,
                mass: 18f + (i % 3) * 6f,
                radius: 3.5f + (i % 3),
                color: new Color((byte)(120 + (i * 7) % 100), (byte)(120 + (i * 13) % 100), (byte)(120 + (i * 17) % 100), (byte)255),
                isStatic: false));
        }

        return bodies;
    }

    /// <summary>
    /// Añade un sistema visual con un cuerpo central y un anillo de satélites en órbita.
    /// </summary>
    public static List<Body> CreateOrbitalRingSystem()
    {
        var bodies = new List<Body>
        {
            new StarBody(
                position: Vector2.Zero,
                velocity: Vector2.Zero,
                mass: 3000000f,
                radius: 34f,
                color: new Color(140, 220, 255, 255),
                isStatic: true
                )
        };

        for (int i = 0; i < 24; i++)
        {
            float angle = i * MathF.Tau / 24f;
            Vector2 position = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 280f;
            Vector2 tangent = new Vector2(-position.Y, position.X);
            tangent = Vector2.Normalize(tangent);

            bodies.Add(new PlanetBody(
                position: position,
                velocity: tangent * (90f + (i % 4) * 8f),
                mass: 90f,
                radius: 5f + (i % 3),
                color: new Color((byte)(180 + (i * 7) % 60), (byte)(120 + (i * 13) % 70), (byte)(220 - (i * 5) % 40), (byte)255),
                isStatic: false
                ));
        }

        return bodies;
    }

    public static List<Body> CreateQuadtreeStressSystem(int bodyCount)
    {
        // Genera un conjunto grande y reproducible para medir Barnes-Hut con una distribución parecida a un disco galáctico.
        int finalCount = Math.Max(1000, bodyCount);
        var random = new Random(42);

        var bodies = new List<Body>(finalCount + 1)
        {
            new StarBody(
                position: Vector2.Zero,
                velocity: Vector2.Zero,
                mass: 12000000f,
                radius: 56f,
                color: new Color(255, 240, 120, 255),
                isStatic: true
                )
        };

        for (int i = 0; i < finalCount; i++)
        {
            // Cada cuerpo recibe un radio orbital aleatorio y una velocidad tangencial para no caer todos al centro.
            float angle = (float)(random.NextDouble() * MathF.Tau);
            float radius = 240f + (float)random.NextDouble() * 4200f;

            Vector2 radial = new(MathF.Cos(angle), MathF.Sin(angle));
            Vector2 position = radial * radius;
            Vector2 tangent = Vector2.Normalize(new Vector2(-radial.Y, radial.X));

            float speed = 45f + (float)random.NextDouble() * 95f;
            float jitter = ((float)random.NextDouble() - 0.5f) * 3f;

            bodies.Add(new AsteroidBody(
                position: position,
                velocity: tangent * speed + radial * jitter,
                mass: 8f + (float)random.NextDouble() * 24f,
                radius: 1.6f + (float)random.NextDouble() * 2.6f,
                color: new Color((byte)(120 + random.Next(120)), (byte)(120 + random.Next(120)), (byte)(120 + random.Next(120)), (byte)255),
                isStatic: false
                ));
        }

        return bodies;
    }

    public static List<Body> CreateBlackHoleSpaghettificationSystem()
    {
        var bodies = new List<Body>
        {
            new BlackHoleBody(
                position: Vector2.Zero,
                velocity: Vector2.Zero,
                mass: 18000000f,
                radius: 58f,
                color: new Color(8, 8, 14, 255),
                isStatic: true)
            {
                TidalRadius = 720f,
                EventHorizonRadius = 72f
            },
            new StarBody(
                position: new Vector2(-680f, -35f),
                velocity: new Vector2(175f, 8f),
                mass: 1200f,
                radius: 18f,
                color: new Color(255, 190, 80, 255),
                isStatic: false),
            new PlanetBody(
                position: new Vector2(-620f, 170f),
                velocity: new Vector2(155f, -12f),
                mass: 160f,
                radius: 12f,
                color: new Color(100, 190, 255, 255),
                isStatic: false),
            new AsteroidBody(
                position: new Vector2(-560f, -250f),
                velocity: new Vector2(145f, 20f),
                mass: 40f,
                radius: 9f,
                color: new Color(255, 100, 160, 255),
                isStatic: false)
        };

        return bodies;
    }

    public static List<Body> CreatePulsarSystem()
    {
        var bodies = new List<Body>
        {
            new PulsarBody(
                position: Vector2.Zero,
                velocity: Vector2.Zero,
                mass: 5500000f,
                radius: 24f,
                color: new Color(180, 225, 255, 255),
                isStatic: true)
            {
                SpinSpeed = 7f,
                JetLength = 125f
            },
            new PlanetBody(
                position: new Vector2(310f, 0f),
                velocity: new Vector2(0f, 120f),
                mass: 120f,
                radius: 8f,
                color: new Color(255, 150, 80, 255),
                isStatic: false),
            new PlanetBody(
                position: new Vector2(-460f, 0f),
                velocity: new Vector2(0f, -96f),
                mass: 90f,
                radius: 7f,
                color: new Color(130, 255, 190, 255),
                isStatic: false)
        };

        return bodies;
    }
    /// <summary>
    /// Escenario de colisión masiva: Dos galaxias de 1000 cuerpos cada una en curso de choque directo.
    /// Utiliza soles supermasivos en el centro de cada disco galáctico.
    /// </summary>
    public static List<Body> CreateGalaxyCollisionSystem()
    {
        // 2 galaxias * (1 sol central + 1000 cuerpos) = 2002 cuerpos en total
        var bodies = new List<Body>(2002);
        var random = new Random(777); // Semilla fija para reproducibilidad

        // ==========================================
        // GALAXIA 1 (Izquierda, viaja hacia la derecha)
        // ==========================================
        Vector2 posG1 = new Vector2(-1200f, 400f);
        Vector2 velG1 = new Vector2(40f, -12f);
        
        // Sol supermasivo central (Galaxia 1)
        bodies.Add(new StarBody(
            position: posG1,
            velocity: velG1,
            mass: 5000000f,
            radius: 48f,
            color: new Color(255, 80, 80, 255),
            isStatic: false)); // Tono anaranjado cálido

        for (int i = 0; i < 5000; i++)
        {
            float angle = (float)(random.NextDouble() * MathF.Tau);
            float radius = 100f + (float)random.NextDouble() * 600f; // Distribución del disco
            
            Vector2 radial = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            Vector2 tangent = new Vector2(-radial.Y, radial.X); // Rotación anti-horaria
            
            float orbitalSpeed = 50f + (float)random.NextDouble() * 70f;
            
            bodies.Add(new AsteroidBody(
                position: posG1 + (radial * radius),
                velocity: velG1 + (tangent * orbitalSpeed),
                mass: 5f + (float)random.NextDouble() * 15f,
                radius: 1.5f + (float)random.NextDouble() * 2f,
                color: new Color((byte)(180 + random.Next(75)),(byte) 120, (byte)200, (byte)200),
                isStatic: false
                ));
        }

        // ==========================================
        // GALAXIA 2 (Derecha, viaja hacia la izquierda)
        // ==========================================
        Vector2 posG2 = new Vector2(1200f, -400f);
        Vector2 velG2 = new Vector2(-40f, 12f);

        // Sol supermasivo central (Galaxia 2)
        bodies.Add(new StarBody(
            position: posG2,
            velocity: velG2,
            mass: 5000000f,
            radius: 48f,
            color: new Color(100, 220, 255, 255),
            isStatic: false
            )); // Tono azul brillante

        for (int i = 0; i < 5000; i++)
        {
            float angle = (float)(random.NextDouble() * MathF.Tau);
            float radius = 100f + (float)random.NextDouble() * 600f;
            
            Vector2 radial = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            Vector2 tangent = new Vector2(radial.Y, -radial.X); // Rotación horaria
            
            float orbitalSpeed = 50f + (float)random.NextDouble() * 70f;
            
            bodies.Add(new AsteroidBody(
                position: posG2 + (radial * radius),
                velocity: velG2 + (tangent * orbitalSpeed),
                mass: 5f + (float)random.NextDouble() * 15f,
                radius: 1.5f + (float)random.NextDouble() * 2f,
                color: new Color((byte)100, (byte)200, (byte)(180 + random.Next(75)), (byte)200),
                isStatic: false
                ));
        }

        return bodies;
    }
    /// <summary>
    /// Crea una galaxia espiral realista con un Agujero Negro Supermasivo, 
    /// un halo de Materia Oscura ESTÁTICO (para estabilidad), bulbo central y brazos anchos.
    /// </summary>
    public static List<Body> CreateRealisticSpiralGalaxy(bool wideArms = false)
    {
        // El perfil amplio aumenta el radio y la continuidad visual de los brazos sin cambiar la física orbital.
        int haloCount = wideArms ? 2600 : 2000;
        int starsPerArm = wideArms ? 700 : 500;
        int dustCount = wideArms ? 2200 : 1500;
        float outerRadius = wideArms ? 9500f : 6500f;
        float armRadius = wideArms ? 9000f : 6000f;
        float armSpread = wideArms ? 0.34f : 0.65f;
        float armWinding = wideArms ? 0.0011f : 0.0015f;

        var bodies = new List<Body>(wideArms ? 10000 : 7000);
        var random = new Random(10101); // Semilla fija para reproducibilidad

        // 1. EL NÚCLEO: Cuásar supermasivo activo
        Vector2 center = Vector2.Zero;
        var superMassiveBlackHole = new QuasarBody(
            position: center,
            velocity: Vector2.Zero,
            mass: 14000000f, 
            radius: 60f,
            color: new Color(20, 20, 20, 255),
            isStatic: true
        );
        bodies.Add(superMassiveBlackHole);

        // 2. EL HALO: Materia Oscura
        // CLAVE DE ESTABILIDAD: isStatic = true. 
        // Esto crea un pozo gravitacional inmutable que mantiene la galaxia unida.
        for (int i = 0; i < haloCount; i++)
        {
            float angle = (float)(random.NextDouble() * MathF.Tau);
            float radius = 500f + (float)random.NextDouble() * outerRadius;
            
            Vector2 radial = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

            bodies.Add(new AsteroidBody(
                position: radial * radius,
                velocity: Vector2.Zero, // No necesitan velocidad porque son estáticos
                // El halo aporta estabilidad, pero no debe superar varias veces la masa del cuásar.
                mass: 2500f, 
                radius: 2f,
                color: new Color(80, 20, 100, 10), // Muy tenue
                isStatic: true // FUNDAMENTAL PARA QUE NO SE DESARME
            ));
        }

        // DIRECCIÓN DE ROTACIÓN (Sentido horario para que los brazos "arrastren")
        // radial.Y, -radial.X es un giro de 90 grados a la derecha.
        Vector2 GetTangent(Vector2 r) => new Vector2(r.Y, -r.X);

        // 3. EL BULBO CENTRAL (Añade "Grosor" masivo al centro de la galaxia)
        for (int i = 0; i < 600; i++)
        {
            float angle = (float)(random.NextDouble() * MathF.Tau);
            float radius = 80f + (float)random.NextDouble() * 800f; // Estrellas muy céntricas
            
            Vector2 radial = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            Vector2 tangent = GetTangent(radial);
            
            float speed = CircularOrbitSpeed(14000000f, radius) * (0.96f + (float)random.NextDouble() * 0.04f);

            bodies.Add(new StarBody(
                position: radial * radius,
                velocity: tangent * speed,
                mass: 1000f + (float)random.NextDouble() * 2000f,
                radius: 5f + (float)random.NextDouble() * 3f,
                color: new Color((byte)255, (byte)(200 + random.Next(55)), (byte)150, (byte)255), // Tonos amarillos/naranjas viejos
                isStatic: false
            ));
        }

        // 4. LA ESPIRAL: Brazos más gruesos y dirección corregida
        int numArms = 4;

        for (int arm = 0; arm < numArms; arm++)
        {
            float armOffset = (MathF.Tau / numArms) * arm;

            for (int i = 0; i < starsPerArm; i++)
            {
                float distFactor = (float)random.NextDouble();
                float radius = 400f + (distFactor * distFactor) * armRadius;

                // Enrollamiento del brazo
                float theta = armOffset + (radius * armWinding);

                // Dispersión para hacer el brazo ancho (con forma de campana)
                float scatterDist = ((float)random.NextDouble() - 0.5f);
                float scatter = scatterDist * armSpread * radius;
                float finalAngle = theta + (scatter / radius);

                Vector2 radial = new Vector2(MathF.Cos(finalAngle), MathF.Sin(finalAngle));
                Vector2 starPos = radial * radius;
                
                // Aplicamos la tangente con la dirección corregida
                Vector2 tangent = GetTangent(radial);

                // Velocidad orbital
                float orbitalSpeed = CircularOrbitSpeed(14000000f, radius) * (0.97f + (float)random.NextDouble() * 0.05f);
                Vector2 starVel = tangent * orbitalSpeed;

                bodies.Add(new StarBody(
                    position: starPos,
                    velocity: starVel,
                    mass: 1000f + (float)random.NextDouble() * 3000f,
                    radius: 6f + (float)random.NextDouble() * 5f,
                    color: new Color((byte)(150 + random.Next(105)), (byte)(180 + random.Next(75)), (byte)255, (byte)255),
                    isStatic: false
                ));

                // 5. SISTEMAS SOLARES (Igual, pero adaptados al nuevo vector)
                if (random.NextDouble() < 0.25)
                {
                    int numPlanets = random.Next(1, 4); 
                    for (int p = 0; p < numPlanets; p++)
                    {
                        float pAngle = (float)(random.NextDouble() * MathF.Tau);
                        float pRadius = 15f + (p * 10f) + (float)random.NextDouble() * 5f; 
                        
                        Vector2 pRadial = new Vector2(MathF.Cos(pAngle), MathF.Sin(pAngle));
                        Vector2 pPos = starPos + (pRadial * pRadius); 
                        Vector2 pTangent = GetTangent(pRadial); // Los planetas giran en el mismo sentido
                        
                        float localOrbitSpeed = CircularOrbitSpeed(2000f, pRadius);
                        Vector2 pVel = starVel + (pTangent * localOrbitSpeed);

                        bodies.Add(new PlanetBody(
                            position: pPos,
                            velocity: pVel,
                            mass: 50f + (float)random.NextDouble() * 50f,
                            radius: 2f + (float)random.NextDouble() * 2.5f,
                            color: new Color((byte)random.Next(50, 255), (byte)random.Next(100, 255), (byte)random.Next(50, 200), (byte)255),
                            isStatic: false
                        ));
                    }
                }
            }
        }

        // 6. POLVO ESTELAR / FONDO (Actualizado al nuevo giro)
        for (int i = 0; i < dustCount; i++)
        {
            float angle = (float)(random.NextDouble() * MathF.Tau);
            float radius = 400f + (float)random.NextDouble() * outerRadius;
            Vector2 radial = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            Vector2 tangent = GetTangent(radial);
            
            float orbitalSpeed = CircularOrbitSpeed(14000000f, radius) * (0.95f + (float)random.NextDouble() * 0.08f);

            bodies.Add(new AsteroidBody(
                position: radial * radius,
                velocity: tangent * orbitalSpeed,
                mass: 5f,
                radius: 1f,
                color: new Color(100, 150, 200, 80), 
                isStatic: false
            ));
        }

        return bodies;
    }

    public static List<Body> CreateWideSpiralGalaxy()
    {
        // La escena de prueba reutiliza el generador estable con brazos de mayor escala.
        return CreateRealisticSpiralGalaxy(wideArms: true);
    }

    public static List<Body> CreatePhaseOneMultiGalaxyUniverse(
        int galaxyCount = 3,
        int starsPerGalaxy = 560,
        float collisionProbability = 0.45f,
        int seed = 424242)
    {
        // Fase 1: galaxias separadas que nacen coherentes y pueden encontrarse más adelante.
        galaxyCount = Math.Clamp(galaxyCount, 2, 6);
        starsPerGalaxy = Math.Clamp(starsPerGalaxy, 20, 600);
        collisionProbability = Math.Clamp(collisionProbability, 0f, 1f);

        var random = new Random(seed);
        var bodies = new List<Body>(galaxyCount * (starsPerGalaxy + 20));
        bool collisionPair = random.NextDouble() < collisionProbability;
        float separation = 5200f;

        for (int galaxyIndex = 0; galaxyIndex < galaxyCount; galaxyIndex++)
        {
            Vector2 center = GetGalaxyCenter(galaxyIndex, galaxyCount, separation, collisionPair);
            Vector2 drift = GetGalaxyDrift(galaxyIndex, galaxyCount, collisionPair);
            float centralMass = 9000000f + (float)random.NextDouble() * 2500000f;

            // Cada galaxia tiene un cuásar dinámico para que los encuentros entre núcleos sean posibles.
            var quasar = new QuasarBody(
                position: center,
                velocity: drift,
                mass: centralMass,
                radius: 48f,
                color: GalaxyColor(galaxyIndex),
                isStatic: false)
            {
                TidalRadius = 260f,
                EventHorizonRadius = 58f,
                JetLength = 220f,
                JetWidth = 18f
            };
            bodies.Add(quasar);

            for (int starIndex = 0; starIndex < starsPerGalaxy; starIndex++)
            {
                // Cuatro brazos logarítmicos: más densidad cerca del bulbo y extensión visible hacia fuera.
                int arm = starIndex % 4;
                float armProgress = (starIndex / 4f) / MathF.Max(1f, starsPerGalaxy / 4f);
                float angle = arm * MathF.Tau / 4f
                    + armProgress * 2.4f
                    + ((float)random.NextDouble() - 0.5f) * 0.16f;
                float radius = 260f + MathF.Sqrt((float)random.NextDouble()) * 3400f;
                Vector2 radial = new(MathF.Cos(angle), MathF.Sin(angle));
                Vector2 position = center + radial * radius;
                Vector2 tangent = new(radial.Y, -radial.X);
                float orbitalSpeed = CircularOrbitSpeed(centralMass, radius) * (0.97f + (float)random.NextDouble() * 0.06f);

                var star = new StarBody(
                    position: position,
                    velocity: drift + Vector2.Normalize(tangent) * orbitalSpeed,
                    mass: 900f + (float)random.NextDouble() * 2600f,
                    radius: 4f + (float)random.NextDouble() * 5f,
                    color: new Color((byte)(190 + random.Next(65)), (byte)(170 + random.Next(85)), (byte)(120 + random.Next(100)), (byte)255),
                    isStatic: false);
                bodies.Add(star);

                // Una parte de las estrellas recibe un sistema planetario local.
                if (random.NextDouble() < 0.3)
                {
                    int planetCount = 1 + random.Next(3);
                    for (int planetIndex = 0; planetIndex < planetCount; planetIndex++)
                    {
                        float localRadius = 16f + planetIndex * 14f + (float)random.NextDouble() * 8f;
                        float localAngle = (float)random.NextDouble() * MathF.Tau;
                        Vector2 localRadial = new(MathF.Cos(localAngle), MathF.Sin(localAngle));
                        Vector2 localTangent = new(localRadial.Y, -localRadial.X);
                        float localSpeed = CircularOrbitSpeed(star.Mass, localRadius);

                        bodies.Add(new PlanetBody(
                            position: position + localRadial * localRadius,
                            velocity: star.Velocity + Vector2.Normalize(localTangent) * localSpeed,
                            mass: 20f + (float)random.NextDouble() * 80f,
                            radius: 1.5f + (float)random.NextDouble() * 2.5f,
                            color: new Color((byte)random.Next(80, 255), (byte)random.Next(100, 255), (byte)random.Next(120, 255), (byte)255),
                            isStatic: false));
                    }
                }
            }

            // El polvo rellena visualmente los brazos sin añadir masas grandes que desestabilicen la galaxia.
            int dustCount = starsPerGalaxy * 3;
            for (int dustIndex = 0; dustIndex < dustCount; dustIndex++)
            {
                int arm = dustIndex % 4;
                float armProgress = (dustIndex / 4f) / MathF.Max(1f, dustCount / 4f);
                float angle = arm * MathF.Tau / 4f
                    + armProgress * 2.4f
                    + ((float)random.NextDouble() - 0.5f) * 0.28f;
                float radius = 320f + MathF.Sqrt((float)random.NextDouble()) * 3600f;
                Vector2 radial = new(MathF.Cos(angle), MathF.Sin(angle));
                Vector2 tangent = new(radial.Y, -radial.X);
                float orbitalSpeed = CircularOrbitSpeed(centralMass, radius) * 0.99f;

                bodies.Add(new AsteroidBody(
                    position: center + radial * radius,
                    velocity: drift + Vector2.Normalize(tangent) * orbitalSpeed,
                    mass: 2f,
                    radius: 2f + (float)random.NextDouble() * 1.5f,
                    color: new Color((byte)100, (byte)150, (byte)220, (byte)150),
                    isStatic: false));
            }

            // Un objeto compacto por galaxia añade eventos locales sin saturar la escena.
            Vector2 compactPosition = center + new Vector2(-420f, 300f);
            bodies.Add(new PulsarBody(
                position: compactPosition,
                velocity: drift + new Vector2(0f, CircularOrbitSpeed(centralMass, 520f)),
                mass: 250000f,
                radius: 13f,
                color: new Color(130, 215, 255, 255),
                isStatic: false));
        }

        return bodies;
    }

    public static List<Body> CreateBigBangUniverse(
        int galaxyCount = 5,
        int starsPerGalaxy = 360,
        int seed = 9001)
    {
        // La fase 2 parte de galaxias ya formadas para que la expansión sea legible y estable.
        var bodies = CreatePhaseOneMultiGalaxyUniverse(
            galaxyCount,
            starsPerGalaxy,
            collisionProbability: 0f,
            seed);

        var random = new Random(seed);
        const float expansionSpeed = 3.5f;

        foreach (Body body in bodies)
        {
            // La velocidad radial es pequeña frente a la orbital: expande el universo sin deshacer las galaxias.
            Vector2 radial = body.Position.LengthSquared() > 0.001f
                ? Vector2.Normalize(body.Position)
                : new Vector2(1f, 0f);
            float variation = 0.85f + (float)random.NextDouble() * 0.3f;
            body.Velocity += radial * expansionSpeed * variation;
        }

        return bodies;
    }

    private static Vector2 GetGalaxyCenter(int index, int count, float separation, bool collisionPair)
    {
        if (collisionPair && index < 2)
        {
            return index == 0 ? new Vector2(-separation, 0f) : new Vector2(separation, 0f);
        }

        float angle = index * MathF.Tau / count;
        float radius = collisionPair ? separation * 1.8f : separation * 1.35f;
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
    }

    private static Vector2 GetGalaxyDrift(int index, int count, bool collisionPair)
    {
        if (collisionPair && index < 2)
        {
            return index == 0 ? new Vector2(24f, 2f) : new Vector2(-24f, -2f);
        }

        float angle = index * MathF.Tau / count;
        return new Vector2(-MathF.Sin(angle), MathF.Cos(angle)) * 8f;
    }

    private static Color GalaxyColor(int index)
    {
        Color[] colors =
        {
            new Color(255, 150, 70, 255),
            new Color(100, 200, 255, 255),
            new Color(210, 120, 255, 255),
            new Color(120, 255, 190, 255),
            new Color(255, 220, 100, 255),
            new Color(180, 180, 255, 255)
        };

        return colors[index % colors.Length];
    }
}