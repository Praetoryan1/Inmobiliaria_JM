using inmobiliaria.Models;
using MySql.Data.MySqlClient;

namespace inmobiliaria.Repositories;

public class RepositorioTiposInmueble : RepositorioBase
{
    private const string Columnas = "IdTipoInmueble, Nombre";

    public RepositorioTiposInmueble(IConfiguration configuration)
        : base(configuration)
    {
    }

    public IList<TipoInmueble> ObtenerLista(
        string? busqueda = null,
        int pagina = 1,
        int tamPagina = 10)
    {
        pagina = Math.Max(pagina, 1);
        tamPagina = Math.Clamp(tamPagina, 1, 10);

        var tipos = new List<TipoInmueble>();
        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT {Columnas}
            FROM TiposInmueble
            WHERE Nombre LIKE @busqueda
            ORDER BY Nombre, IdTipoInmueble
            LIMIT @limite OFFSET @desplazamiento;
            """;
        AgregarParametrosListado(command, busqueda, pagina, tamPagina);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tipos.Add(MapearTipo(reader));
        }

        return tipos;
    }

    public int ObtenerCantidad(string? busqueda = null)
    {
        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT COUNT(*)
            FROM TiposInmueble
            WHERE Nombre LIKE @busqueda;
            """;
        command.Parameters.Add("@busqueda", MySqlDbType.VarChar, 82).Value =
            CrearPatronBusqueda(busqueda);

        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public TipoInmueble? ObtenerPorId(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT {Columnas}
            FROM TiposInmueble
            WHERE IdTipoInmueble = @id;
            """;
        command.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

        connection.Open();
        using var reader = command.ExecuteReader();

        return reader.Read() ? MapearTipo(reader) : null;
    }

    public int Alta(TipoInmueble tipo)
    {
        ArgumentNullException.ThrowIfNull(tipo);

        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO TiposInmueble (Nombre)
            VALUES (@nombre);
            SELECT LAST_INSERT_ID();
            """;
        command.Parameters.Add("@nombre", MySqlDbType.VarChar, 80).Value =
            tipo.Nombre.Trim();

        connection.Open();
        tipo.IdTipoInmueble = Convert.ToInt32(command.ExecuteScalar());

        return tipo.IdTipoInmueble;
    }

    public bool Modificacion(TipoInmueble tipo)
    {
        ArgumentNullException.ThrowIfNull(tipo);

        if (tipo.IdTipoInmueble <= 0)
        {
            return false;
        }

        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = """
            UPDATE TiposInmueble
            SET Nombre = @nombre
            WHERE IdTipoInmueble = @id;
            """;
        command.Parameters.Add("@nombre", MySqlDbType.VarChar, 80).Value =
            tipo.Nombre.Trim();
        command.Parameters.Add("@id", MySqlDbType.Int32).Value = tipo.IdTipoInmueble;

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
            DELETE FROM TiposInmueble
            WHERE IdTipoInmueble = @id;
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
        command.Parameters.Add("@busqueda", MySqlDbType.VarChar, 82).Value =
            CrearPatronBusqueda(busqueda);
        command.Parameters.Add("@limite", MySqlDbType.Int32).Value = tamPagina;
        command.Parameters.Add("@desplazamiento", MySqlDbType.Int32).Value =
            (pagina - 1) * tamPagina;
    }

    private static string CrearPatronBusqueda(string? busqueda) =>
        $"%{busqueda?.Trim() ?? string.Empty}%";

    private static TipoInmueble MapearTipo(MySqlDataReader reader) =>
        new()
        {
            IdTipoInmueble = reader.GetInt32(nameof(TipoInmueble.IdTipoInmueble)),
            Nombre = reader.GetString(nameof(TipoInmueble.Nombre))
        };
}
