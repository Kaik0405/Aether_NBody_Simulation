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
    
    // Propiedades físicas para Barnes-Hut (las calcularemos después)
    public float TotalMass { get; set; }
    public Vector2 CenterOfMass { get; set; }

    // Si el nodo es una hoja, almacenará el cuerpo aquí. Si se subdivide, esto será null.
    public Body Body { get; private set; }

    // Los 4 hijos
    public QuadTreeNode NW { get; private set; }
    public QuadTreeNode NE { get; private set; }
    public QuadTreeNode SW { get; private set; }
    public QuadTreeNode SE { get; private set; }

    // Un nodo es hoja si no tiene hijos
    public bool IsLeaf => NW == null;

    public QuadTreeNode(BoundingBox boundary)
    {
        Boundary = boundary;
    }

    // Método recursivo para insertar cuerpos
    public bool Insert(Body newBody)
    {
        // 1. Si el cuerpo no está en los límites de este nodo, lo rechazamos
        if (!Boundary.Contains(newBody.Position))
            return false;

        // 2. Si el nodo es hoja y está vacío, el cuerpo se queda aquí
        if (IsLeaf && Body == null)
        {
            Body = newBody;
            return true;
        }

        // 3. Si el nodo es hoja PERO ya tiene un cuerpo, hay un choque.
        if (IsLeaf)
        {
            // Trampa de seguridad: Si por error dos cuerpos tienen la misma posición exacta, 
            // el QuadTree se subdividiría infinitamente hasta explotar la memoria RAM.
            if (Vector2.Distance(Body.Position, newBody.Position) < 0.01f) 
                return false; 

            // Como hay choque, dividimos este nodo en 4
            Subdivide();

            // Pasamos el cuerpo viejo a los nuevos hijos
            InsertIntoChildren(Body);
            
            // Este nodo ya es "padre", no puede tener cuerpo propio
            Body = null; 
        }

        // 4. Insertamos el cuerpo nuevo en los hijos (como ya es padre, caerá donde toque)
        return InsertIntoChildren(newBody);
    }

    // Parte el nodo en 4 cuadrantes iguales (Recuerda: en Raylib Y crece hacia abajo)
    private void Subdivide()
    {
        float quarter = Boundary.HalfDimension / 2f;
        Vector2 c = Boundary.Center;

        NW = new QuadTreeNode(new BoundingBox(new Vector2(c.X - quarter, c.Y - quarter), quarter));
        NE = new QuadTreeNode(new BoundingBox(new Vector2(c.X + quarter, c.Y - quarter), quarter));
        SW = new QuadTreeNode(new BoundingBox(new Vector2(c.X - quarter, c.Y + quarter), quarter));
        SE = new QuadTreeNode(new BoundingBox(new Vector2(c.X + quarter, c.Y + quarter), quarter));
    }

    // Intenta meter el cuerpo en los hijos hasta que uno lo acepte
    private bool InsertIntoChildren(Body b)
    {
        if (NW.Insert(b)) return true;
        if (NE.Insert(b)) return true;
        if (SW.Insert(b)) return true;
        if (SE.Insert(b)) return true;
        
        return false;
    }
    private void CalculateMassCenter()
    {
        // Es hoja (un solo cuerpo)
        if(IsLeaf && Body != null)
        {
            TotalMass = Body.Mass;
            CenterOfMass = Body.Position;
            return;    
        }
        // Es un nodo interno con hijos
        TotalMass = 0f;
        Vector2 weightSum = Vector2.Zero;

        NW.CalculateMassCenter();
        NE.CalculateMassCenter();
        SW.CalculateMassCenter();
        SE.CalculateMassCenter();
        
        TotalMass = NW.TotalMass + NE.TotalMass + SW.TotalMass + SE.TotalMass;
        
        weightSum = (NW.CenterOfMass * NW.TotalMass) + (NE.CenterOfMass * NE.TotalMass) +
                    (SW.CenterOfMass + SW.TotalMass) + (SE.CenterOfMass * SE.TotalMass);

        if (TotalMass > 0f)
            CenterOfMass = weightSum / TotalMass;
    }
}