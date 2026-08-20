using inmobiliaria.Models;
using MySql.Data.MySqlClient;

namespace inmobiliaria.Repositories;

public class RepositorioPropietarios : RepositorioBase
{
    private const string Columnas =
        "IdPropietario, Dni, Nombre, Apellido, Telefono, Email";

    public RepositorioPropietarios(IConfiguration configuration)
        : base(configuration)
    {
    }

    public IList<Propietario> ObtenerLista(
        string? busqueda = null,
        int pagina = 1,
        int tamPagina = 10)
    {
        pagina = Math.Max(pagina, 1);
        tamPagina = Math.Clamp(tamPagina, 1, 10);

        var propietarios = new List<Propietario>();
        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT {Columnas}
            FROM Propietarios
            WHERE Dni LIKE @busqueda
               OR Nombre LIKE @busqueda
               OR Apellido LIKE @busqueda
               OR COALESCE(Telefono, '') LIKE @busqueda
               OR Email LIKE @busqueda
            ORDER BY Apellido, Nombre, IdPropietario
            LIMIT @limite OFFSET @desplazamiento;
            """;

        AgregarParametrosListado(command, busqueda, pagina, tamPagina);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            propietarios.Add(MapearPropietario(reader));
        }

        return propietarios;
    }

    public int ObtenerCantidad(string? busqueda = null)
    {
        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT COUNT(*)
            FROM Propietarios
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

    public Propietario? ObtenerPorId(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT {Columnas}
            FROM Propietarios
            WHERE IdPropietario = @id;
            """;
        command.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

        connection.Open();
        using var reader = command.ExecuteReader();

        return reader.Read() ? MapearPropietario(reader) : null;
    }

    public int Alta(Propietario propietario)
    {
        ArgumentNullException.ThrowIfNull(propietario);

        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO Propietarios (Dni, Nombre, Apellido, Telefono, Email)
            VALUES (@dni, @nombre, @apellido, @telefono, @email);
            SELECT LAST_INSERT_ID();
            """;
        AgregarParametrosPropietario(command, propietario);

        connection.Open();
        propietario.IdPropietario = Convert.ToInt32(command.ExecuteScalar());

        return propietario.IdPropietario;
    }

    public bool Modificacion(Propietario propietario)
    {
        ArgumentNullException.ThrowIfNull(propietario);

        if (propietario.IdPropietario <= 0)
        {
            return false;
        }

        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = """
            UPDATE Propietarios
            SET Dni = @dni,
                Nombre = @nombre,
                Apellido = @apellido,
                Telefono = @telefono,
                Email = @email
            WHERE IdPropietario = @id;
            """;
        AgregarParametrosPropietario(command, propietario);
        command.Parameters.Add("@id", MySqlDbType.Int32).Value = propietario.IdPropietario;

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
            DELETE FROM Propietarios
            WHERE IdPropietario = @id;
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

    private static void AgregarParametrosPropietario(
        MySqlCommand command,
        Propietario propietario)
    {
        command.Parameters.Add("@dni", MySqlDbType.VarChar, 8).Value = propietario.Dni;
        command.Parameters.Add("@nombre", MySqlDbType.VarChar, 100).Value = propietario.Nombre;
        command.Parameters.Add("@apellido", MySqlDbType.VarChar, 100).Value = propietario.Apellido;
        command.Parameters.Add("@telefono", MySqlDbType.VarChar, 30).Value =
            string.IsNullOrWhiteSpace(propietario.Telefono)
                ? DBNull.Value
                : propietario.Telefono;
        command.Parameters.Add("@email", MySqlDbType.VarChar, 150).Value = propietario.Email;
    }

    private static string CrearPatronBusqueda(string? busqueda) =>
        $"%{busqueda?.Trim() ?? string.Empty}%";

    private static Propietario MapearPropietario(MySqlDataReader reader)
    {
        var telefonoOrdinal = reader.GetOrdinal(nameof(Propietario.Telefono));

        return new Propietario
        {
            IdPropietario = reader.GetInt32(nameof(Propietario.IdPropietario)),
            Dni = reader.GetString(nameof(Propietario.Dni)),
            Nombre = reader.GetString(nameof(Propietario.Nombre)),
            Apellido = reader.GetString(nameof(Propietario.Apellido)),
            Telefono = reader.IsDBNull(telefonoOrdinal)
                ? null
                : reader.GetString(telefonoOrdinal),
            Email = reader.GetString(nameof(Propietario.Email))
        };
    }
}
