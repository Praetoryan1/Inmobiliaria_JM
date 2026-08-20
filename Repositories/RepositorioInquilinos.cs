using inmobiliaria.Models;
using MySql.Data.MySqlClient;

namespace inmobiliaria.Repositories;

public class RepositorioInquilinos : RepositorioBase
{
    private const string Columnas =
        "IdInquilino, Dni, Nombre, Apellido, Telefono, Email";

    public RepositorioInquilinos(IConfiguration configuration)
        : base(configuration)
    {
    }

    public IList<Inquilino> ObtenerLista(
        string? busqueda = null,
        int pagina = 1,
        int tamPagina = 10)
    {
        pagina = Math.Max(pagina, 1);
        tamPagina = Math.Clamp(tamPagina, 1, 10);

        var inquilinos = new List<Inquilino>();
        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT {Columnas}
            FROM Inquilinos
            WHERE Dni LIKE @busqueda
               OR Nombre LIKE @busqueda
               OR Apellido LIKE @busqueda
               OR COALESCE(Telefono, '') LIKE @busqueda
               OR Email LIKE @busqueda
            ORDER BY Apellido, Nombre, IdInquilino
            LIMIT @limite OFFSET @desplazamiento;
            """;

        AgregarParametrosListado(command, busqueda, pagina, tamPagina);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            inquilinos.Add(MapearInquilino(reader));
        }

        return inquilinos;
    }

    public int ObtenerCantidad(string? busqueda = null)
    {
        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT COUNT(*)
            FROM Inquilinos
            WHERE Dni LIKE @busqueda
               OR Nombre LIKE @busqueda
               OR Apellido LIKE @busqueda
               OR COALESCE(Telefono, '') LIKE @busqueda
               OR Email LIKE @busqueda;
            """;
        command.Parameters.Add("@busqueda", MySqlDbType.VarChar, 152).Value =
            CrearPatronBusqueda(busqueda);

        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public Inquilino? ObtenerPorId(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT {Columnas}
            FROM Inquilinos
            WHERE IdInquilino = @id;
            """;
        command.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

        connection.Open();
        using var reader = command.ExecuteReader();

        return reader.Read() ? MapearInquilino(reader) : null;
    }

    public int Alta(Inquilino inquilino)
    {
        ArgumentNullException.ThrowIfNull(inquilino);

        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO Inquilinos (Dni, Nombre, Apellido, Telefono, Email)
            VALUES (@dni, @nombre, @apellido, @telefono, @email);
            SELECT LAST_INSERT_ID();
            """;
        AgregarParametrosInquilino(command, inquilino);

        connection.Open();
        inquilino.IdInquilino = Convert.ToInt32(command.ExecuteScalar());

        return inquilino.IdInquilino;
    }

    public bool Modificacion(Inquilino inquilino)
    {
        ArgumentNullException.ThrowIfNull(inquilino);

        if (inquilino.IdInquilino <= 0)
        {
            return false;
        }

        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = """
            UPDATE Inquilinos
            SET Dni = @dni,
                Nombre = @nombre,
                Apellido = @apellido,
                Telefono = @telefono,
                Email = @email
            WHERE IdInquilino = @id;
            """;
        AgregarParametrosInquilino(command, inquilino);
        command.Parameters.Add("@id", MySqlDbType.Int32).Value = inquilino.IdInquilino;

        connection.Open();
        return command.ExecuteNonQuery() > 0;
    }

    public bool Baja(int id)
    {
        if (id <= 0)
        {
            return false;
        }

        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = """
            DELETE FROM Inquilinos
            WHERE IdInquilino = @id;
            """;
        command.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

        connection.Open();
        return command.ExecuteNonQuery() > 0;
    }

    private static void AgregarParametrosListado(
        MySqlCommand command,
        string? busqueda,
        int pagina,
        int tamPagina)
    {
        command.Parameters.Add("@busqueda", MySqlDbType.VarChar, 152).Value =
            CrearPatronBusqueda(busqueda);
        command.Parameters.Add("@limite", MySqlDbType.Int32).Value = tamPagina;
        command.Parameters.Add("@desplazamiento", MySqlDbType.Int32).Value =
            (pagina - 1) * tamPagina;
    }

    private static void AgregarParametrosInquilino(
        MySqlCommand command,
        Inquilino inquilino)
    {
        command.Parameters.Add("@dni", MySqlDbType.VarChar, 8).Value = inquilino.Dni;
        command.Parameters.Add("@nombre", MySqlDbType.VarChar, 100).Value = inquilino.Nombre;
        command.Parameters.Add("@apellido", MySqlDbType.VarChar, 100).Value = inquilino.Apellido;
        command.Parameters.Add("@telefono", MySqlDbType.VarChar, 30).Value =
            string.IsNullOrWhiteSpace(inquilino.Telefono)
                ? DBNull.Value
                : inquilino.Telefono;
        command.Parameters.Add("@email", MySqlDbType.VarChar, 150).Value = inquilino.Email;
    }

    private static string CrearPatronBusqueda(string? busqueda) =>
        $"%{busqueda?.Trim() ?? string.Empty}%";

    private static Inquilino MapearInquilino(MySqlDataReader reader)
    {
        var telefonoOrdinal = reader.GetOrdinal(nameof(Inquilino.Telefono));

        return new Inquilino
        {
            IdInquilino = reader.GetInt32(nameof(Inquilino.IdInquilino)),
            Dni = reader.GetString(nameof(Inquilino.Dni)),
            Nombre = reader.GetString(nameof(Inquilino.Nombre)),
            Apellido = reader.GetString(nameof(Inquilino.Apellido)),
            Telefono = reader.IsDBNull(telefonoOrdinal)
                ? null
                : reader.GetString(telefonoOrdinal),
            Email = reader.GetString(nameof(Inquilino.Email))
        };
    }
}
