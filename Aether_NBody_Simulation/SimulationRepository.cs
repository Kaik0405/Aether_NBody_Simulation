using System.Linq;
using System.Numerics;
using Microsoft.Data.Sqlite;
using Dapper;
using Raylib_cs;

namespace Aether_NBody_Simulation;

public class SimulationRepository
{
    // El archivo se creará automáticamente en la carpeta de tu ejecutable
    private readonly string connectionString = "Data Source=aether.db";

    public SimulationRepository()
    {
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(connectionString);
        
        // Si las tablas no existen, se crean automáticamente al arrancar la aplicación.
        string createTablesSql = @"
            CREATE TABLE IF NOT EXISTS Simulations (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS Bodies (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SimulationId INTEGER,
                Mass REAL,
                PosX REAL, PosY REAL,
                VelX REAL, VelY REAL,
                FOREIGN KEY(SimulationId) REFERENCES Simulations(Id)
            );";
            
        connection.Execute(createTablesSql);

        // Migración incremental para instalaciones viejas:
        // Body ahora necesita Radius y Color (RGBA) para reconstruirse correctamente.
        EnsureColumnExists(connection, "Bodies", "Radius", "REAL", "6.0");
        EnsureColumnExists(connection, "Bodies", "ColorR", "INTEGER", "255");
        EnsureColumnExists(connection, "Bodies", "ColorG", "INTEGER", "255");
        EnsureColumnExists(connection, "Bodies", "ColorB", "INTEGER", "255");
        EnsureColumnExists(connection, "Bodies", "ColorA", "INTEGER", "255");
    }

    private static void EnsureColumnExists(
        SqliteConnection connection,
        string tableName,
        string columnName,
        string columnType,
        string defaultValueSql)
    {
        string pragmaSql = $"PRAGMA table_info({tableName});";
        var columns = connection.Query(pragmaSql).Select(c => (string)c.name).ToHashSet();

        if (!columns.Contains(columnName))
        {
            string alterSql =
                $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType} NOT NULL DEFAULT {defaultValueSql};";
            connection.Execute(alterSql);
        }
    }

    public int SaveSimulation(string name, List<Body> bodies)
    {
        // Guarda la simulación y todos sus cuerpos en una transacción para evitar estados parciales.
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var tx = connection.BeginTransaction();
        
        // 1. Guardamos la Simulación y obtenemos su ID generado
        string insertSim = "INSERT INTO Simulations (Name) VALUES (@Name); SELECT last_insert_rowid();";
        long simId = connection.QuerySingle<long>(insertSim, new { Name = name }, tx);

        // 2. Preparamos los cuerpos para la BD (separando los Vector2 en floats)
        var bodyRecords = bodies.Select(b => new BodyRecord
        {
            SimulationId = (int)simId,
            Mass = b.Mass,
            PosX = b.Position.X,
            PosY = b.Position.Y,
            VelX = b.Velocity.X,
            VelY = b.Velocity.Y,
            Radius = b.Radius,
            ColorR = b.Color.R,
            ColorG = b.Color.G,
            ColorB = b.Color.B,
            ColorA = b.Color.A
        }).ToList();

        // 3. Guardamos todos los cuerpos de golpe (Dapper lo hace super rápido)
        string insertBody = @"INSERT INTO Bodies (SimulationId, Mass, PosX, PosY, VelX, VelY, Radius, ColorR, ColorG, ColorB, ColorA) 
                              VALUES (@SimulationId, @Mass, @PosX, @PosY, @VelX, @VelY, @Radius, @ColorR, @ColorG, @ColorB, @ColorA)";
        connection.Execute(insertBody, bodyRecords, tx);

        tx.Commit();
        return (int)simId;
    }

    public List<Body> LoadSimulation(int simulationId)
    {
        // Recompone los cuerpos desde la tabla plana de la base de datos.
        using var connection = new SqliteConnection(connectionString);
        
        string sql = "SELECT * FROM Bodies WHERE SimulationId = @SimId";
        var records = connection.Query<BodyRecord>(sql, new { SimId = simulationId }).ToList();

        // Reconstruimos la clase Body con sus Vector2 a partir de los datos planos
        var bodies = new List<Body>();
        foreach (var record in records)
        {
            var body = new Body(
                new Vector2(record.PosX, record.PosY),
                new Vector2(record.VelX, record.VelY),
                record.Mass,
                record.Radius,
                new Color(record.ColorR, record.ColorG, record.ColorB, record.ColorA),
                BodyKind.Generic
            );
            bodies.Add(body);
        }
        return bodies;
    }

    public List<SimulationRecord> GetSimulations()
    {
        using var connection = new SqliteConnection(connectionString);
        const string sql = "SELECT Id, Name, CreatedAt FROM Simulations ORDER BY Id DESC";
        return connection.Query<SimulationRecord>(sql).ToList();
    }

    public bool RenameSimulation(int simulationId, string newName)
    {
        // Evita nombres vacíos y actualiza únicamente el nombre de la simulación.
        if (string.IsNullOrWhiteSpace(newName))
        {
            return false;
        }

        using var connection = new SqliteConnection(connectionString);
        const string sql = "UPDATE Simulations SET Name = @Name WHERE Id = @Id";
        int rows = connection.Execute(sql, new { Id = simulationId, Name = newName.Trim() });
        return rows > 0;
    }

    public bool DeleteSimulation(int simulationId)
    {
        // El borrado se hace en una transacción para eliminar tanto la cabecera como sus cuerpos.
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var tx = connection.BeginTransaction();

        connection.Execute("DELETE FROM Bodies WHERE SimulationId = @Id", new { Id = simulationId }, tx);
        int removed = connection.Execute("DELETE FROM Simulations WHERE Id = @Id", new { Id = simulationId }, tx);

        tx.Commit();
        return removed > 0;
    }

    public SimulationRecord? GetLatestSimulation()
    {
        using var connection = new SqliteConnection(connectionString);
        const string sql = "SELECT Id, Name, CreatedAt FROM Simulations ORDER BY Id DESC LIMIT 1";
        return connection.QuerySingleOrDefault<SimulationRecord>(sql);
    }
}