using System.Numerics;

namespace Aether_NBody_Simulation;

public struct BoundingBox
{
    public Vector2 Center;
    public float HalfDimension; // La mitad del ancho/alto del cuadrante

    public BoundingBox(Vector2 center, float halfDimension)
    {
        Center = center;
        HalfDimension = halfDimension;
    }

    // Verifica si una posición (como la de un Body) está dentro de este cuadrante
    public bool Contains(Vector2 point)
    {
        return point.X >= Center.X - HalfDimension &&
               point.X <= Center.X + HalfDimension &&
               point.Y >= Center.Y - HalfDimension &&
               point.Y <= Center.Y + HalfDimension;
    }
}

public class QuadTreeNode
{
    public BoundingBox Boundary { get; private set; }

    // Propiedades físicas para Barnes-Hut.
    public float TotalMass { get; set; }
    public Vector2 CenterOfMass { get; set; }

    // Si el nodo es hoja, guarda una única partícula.
    public int BodyIndex { get; private set; } = -1;
    public Vector2 BodyPosition { get; private set; }
    public float BodyMass { get; private set; }

    // Los 4 hijos
    public QuadTreeNode? NW { get; private set; }
    public QuadTreeNode? NE { get; private set; }
    public QuadTreeNode? SW { get; private set; }
    public QuadTreeNode? SE { get; private set; }

    // Un nodo es hoja si no tiene hijos
    public bool IsLeaf => NW == null;
    public bool IsEmpty => TotalMass <= 0f;

    public QuadTreeNode(BoundingBox boundary)
    {
        Boundary = boundary;
    }

    // Método recursivo para insertar una partícula en el árbol.
    public bool Insert(int bodyIndex, Vector2 position, float mass)
    {
        // Si la partícula cae fuera de este cuadrante, se descarta para que el algoritmo recorra el hijo correcto.
        if (!Boundary.Contains(position))
            return false;

        if (IsLeaf && BodyIndex < 0)
        {
            BodyIndex = bodyIndex;
            BodyPosition = position;
            BodyMass = mass;
            return true;
        }

        // Si el nodo es hoja y ya tiene una partícula, subdividimos.
        if (IsLeaf)
        {
            // Cuando un nodo hoja ya contiene una partícula, se divide para poder seguir insertando.
            // Evita subdivisiones infinitas por posiciones idénticas.
            if (Vector2.Distance(BodyPosition, position) < 0.0001f)
            {
                position += new Vector2(0.0003f, -0.0002f);
            }

            Subdivide();

            InsertIntoChildren(BodyIndex, BodyPosition, BodyMass);
            BodyIndex = -1;
            BodyPosition = Vector2.Zero;
            BodyMass = 0f;
        }

        return InsertIntoChildren(bodyIndex, position, mass);
    }

    // Parte el nodo en 4 cuadrantes iguales.
    private void Subdivide()
    {
        float quarter = Boundary.HalfDimension / 2f;
        Vector2 c = Boundary.Center;

        NW = new QuadTreeNode(new BoundingBox(new Vector2(c.X - quarter, c.Y - quarter), quarter));
        NE = new QuadTreeNode(new BoundingBox(new Vector2(c.X + quarter, c.Y - quarter), quarter));
        SW = new QuadTreeNode(new BoundingBox(new Vector2(c.X - quarter, c.Y + quarter), quarter));
        SE = new QuadTreeNode(new BoundingBox(new Vector2(c.X + quarter, c.Y + quarter), quarter));
    }

    private bool InsertIntoChildren(int bodyIndex, Vector2 position, float mass)
    {
        if (NW!.Insert(bodyIndex, position, mass)) return true;
        if (NE!.Insert(bodyIndex, position, mass)) return true;
        if (SW!.Insert(bodyIndex, position, mass)) return true;
        if (SE!.Insert(bodyIndex, position, mass)) return true;

        return false;
    }

    public void RecomputeMassDistribution()
    {
        // Las hojas conservan su masa y posición; los nodos internos las agregan desde los hijos.
        if (IsLeaf)
        {
            if (BodyIndex >= 0)
            {
                TotalMass = BodyMass;
                CenterOfMass = BodyPosition;
            }
            else
            {
                TotalMass = 0f;
                CenterOfMass = Vector2.Zero;
            }

            return;
        }

        NW!.RecomputeMassDistribution();
        NE!.RecomputeMassDistribution();
        SW!.RecomputeMassDistribution();
        SE!.RecomputeMassDistribution();

        TotalMass = 0f;
        Vector2 weightedSum = Vector2.Zero;

        // Cada hijo aporta su masa total y su centro de masa ponderado.
        AccumulateFromChild(NW, ref weightedSum);
        AccumulateFromChild(NE, ref weightedSum);
        AccumulateFromChild(SW, ref weightedSum);
        AccumulateFromChild(SE, ref weightedSum);

        if (TotalMass > 0f)
        {
            CenterOfMass = weightedSum / TotalMass;
        }
        else
        {
            CenterOfMass = Vector2.Zero;
        }
    }

    private void AccumulateFromChild(QuadTreeNode? child, ref Vector2 weightedSum)
    {
        if (child is null || child.TotalMass <= 0f)
        {
            return;
        }

        TotalMass += child.TotalMass;
        weightedSum += child.CenterOfMass * child.TotalMass;
    }
}