using inmobiliaria.Models;
using MySql.Data.MySqlClient;

namespace inmobiliaria.Repositories;

public class RepositorioInmuebles : RepositorioBase
{
    private const string Columnas = """
        i.IdInmueble,
        i.IdPropietario,
        i.IdTipoInmueble,
        i.Direccion,
        i.Cupo,
        i.Coordenadas,
        i.PrecioDia,
        i.Disponible,
        i.ImagenPortada,
        p.Dni AS PropietarioDni,
        p.Nombre AS PropietarioNombre,
        p.Apellido AS PropietarioApellido,
        p.Telefono AS PropietarioTelefono,
        p.Email AS PropietarioEmail,
        t.Nombre AS TipoNombre
        """;

    public RepositorioInmuebles(IConfiguration configuration)
        : base(configuration)
    {
    }

    public IList<Inmueble> ObtenerLista(
        string? busqueda = null,
        bool? disponible = null,
        int pagina = 1,
        int tamPagina = 10)
    {
        pagina = Math.Max(pagina, 1);
        tamPagina = Math.Clamp(tamPagina, 1, 10);

        var inmuebles = new List<Inmueble>();
        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT {Columnas}
            FROM Inmuebles i
            INNER JOIN Propietarios p ON p.IdPropietario = i.IdPropietario
            INNER JOIN TiposInmueble t ON t.IdTipoInmueble = i.IdTipoInmueble
            WHERE (
                i.Direccion LIKE @busqueda
                OR i.Coordenadas LIKE @busqueda
                OR p.Dni LIKE @busqueda
                OR p.Nombre LIKE @busqueda
                OR p.Apellido LIKE @busqueda
                OR t.Nombre LIKE @busqueda
            )
            AND (@disponible IS NULL OR i.Disponible = @disponible)
            ORDER BY i.Direccion, i.IdInmueble
            LIMIT @limite OFFSET @desplazamiento;
            """;
        AgregarParametrosListado(command, busqueda, disponible, pagina, tamPagina);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            inmuebles.Add(MapearInmueble(reader));
        }

        return inmuebles;
    }

    public int ObtenerCantidad(string? busqueda = null, bool? disponible = null)
    {
        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT COUNT(*)
            FROM Inmuebles i
            INNER JOIN Propietarios p ON p.IdPropietario = i.IdPropietario
            INNER JOIN TiposInmueble t ON t.IdTipoInmueble = i.IdTipoInmueble
            WHERE (
                i.Direccion LIKE @busqueda
                OR i.Coordenadas LIKE @busqueda
                OR p.Dni LIKE @busqueda
                OR p.Nombre LIKE @busqueda
                OR p.Apellido LIKE @busqueda
                OR t.Nombre LIKE @busqueda
            )
            AND (@disponible IS NULL OR i.Disponible = @disponible);
            """;
        AgregarParametrosFiltros(command, busqueda, disponible);

        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public Inmueble? ObtenerPorId(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT {Columnas}
            FROM Inmuebles i
            INNER JOIN Propietarios p ON p.IdPropietario = i.IdPropietario
            INNER JOIN TiposInmueble t ON t.IdTipoInmueble = i.IdTipoInmueble
            WHERE i.IdInmueble = @id;
            """;
        command.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

        connection.Open();
        using var reader = command.ExecuteReader();

        return reader.Read() ? MapearInmueble(reader) : null;
    }

    public int Alta(Inmueble inmueble)
    {
        ArgumentNullException.ThrowIfNull(inmueble);

        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO Inmuebles
                (IdPropietario, IdTipoInmueble, Direccion, Cupo,
                 Coordenadas, PrecioDia, Disponible, ImagenPortada)
            VALUES
                (@idPropietario, @idTipoInmueble, @direccion, @cupo,
                 @coordenadas, @precioDia, @disponible, @imagenPortada);
            SELECT LAST_INSERT_ID();
            """;
        AgregarParametrosInmueble(command, inmueble);

        connection.Open();
        inmueble.IdInmueble = Convert.ToInt32(command.ExecuteScalar());

        return inmueble.IdInmueble;
    }

    public bool Modificacion(Inmueble inmueble)
    {
        ArgumentNullException.ThrowIfNull(inmueble);

        if (inmueble.IdInmueble <= 0)
        {
            return false;
        }

        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = """
            UPDATE Inmuebles
            SET IdPropietario = @idPropietario,
                IdTipoInmueble = @idTipoInmueble,
                Direccion = @direccion,
                Cupo = @cupo,
                Coordenadas = @coordenadas,
                PrecioDia = @precioDia,
                Disponible = @disponible,
                ImagenPortada = @imagenPortada
            WHERE IdInmueble = @id;
            """;
        AgregarParametrosInmueble(command, inmueble);
        command.Parameters.Add("@id", MySqlDbType.Int32).Value = inmueble.IdInmueble;

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
            DELETE FROM Inmuebles
            WHERE IdInmueble = @id;
            """;
        command.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

        connection.Open();
        return command.ExecuteNonQuery() > 0;
    }

    private static void AgregarParametrosListado(
        MySqlCommand command,
        string? busqueda,
        bool? disponible,
        int pagina,
        int tamPagina)
    {
        AgregarParametrosFiltros(command, busqueda, disponible);
        command.Parameters.Add("@limite", MySqlDbType.Int32).Value = tamPagina;
        command.Parameters.Add("@desplazamiento", MySqlDbType.Int32).Value =
            (pagina - 1) * tamPagina;
    }

    private static void AgregarParametrosFiltros(
        MySqlCommand command,
        string? busqueda,
        bool? disponible)
    {
        command.Parameters.Add("@busqueda", MySqlDbType.VarChar, 202).Value =
            $"%{busqueda?.Trim() ?? string.Empty}%";
        command.Parameters.Add("@disponible", MySqlDbType.Byte).Value =
            disponible.HasValue ? disponible.Value : DBNull.Value;
    }

    private static void AgregarParametrosInmueble(
        MySqlCommand command,
        Inmueble inmueble)
    {
        command.Parameters.Add("@idPropietario", MySqlDbType.Int32).Value =
            inmueble.IdPropietario;
        command.Parameters.Add("@idTipoInmueble", MySqlDbType.Int32).Value =
            inmueble.IdTipoInmueble;
        command.Parameters.Add("@direccion", MySqlDbType.VarChar, 200).Value =
            inmueble.Direccion.Trim();
        command.Parameters.Add("@cupo", MySqlDbType.Int32).Value = inmueble.Cupo;
        command.Parameters.Add("@coordenadas", MySqlDbType.VarChar, 100).Value =
            inmueble.Coordenadas.Trim();
        command.Parameters.Add("@precioDia", MySqlDbType.Decimal).Value = inmueble.PrecioDia;
        command.Parameters.Add("@disponible", MySqlDbType.Byte).Value = inmueble.Disponible;
        command.Parameters.Add("@imagenPortada", MySqlDbType.VarChar, 255).Value =
            string.IsNullOrWhiteSpace(inmueble.ImagenPortada)
                ? DBNull.Value
                : inmueble.ImagenPortada;
    }

    private static Inmueble MapearInmueble(MySqlDataReader reader)
    {
        var imagenOrdinal = reader.GetOrdinal(nameof(Inmueble.ImagenPortada));
        var telefonoOrdinal = reader.GetOrdinal("PropietarioTelefono");

        return new Inmueble
        {
            IdInmueble = reader.GetInt32(nameof(Inmueble.IdInmueble)),
            IdPropietario = reader.GetInt32(nameof(Inmueble.IdPropietario)),
            IdTipoInmueble = reader.GetInt32(nameof(Inmueble.IdTipoInmueble)),
            Direccion = reader.GetString(nameof(Inmueble.Direccion)),
            Cupo = reader.GetInt32(nameof(Inmueble.Cupo)),
            Coordenadas = reader.GetString(nameof(Inmueble.Coordenadas)),
            PrecioDia = reader.GetDecimal(nameof(Inmueble.PrecioDia)),
            Disponible = reader.GetBoolean(nameof(Inmueble.Disponible)),
            ImagenPortada = reader.IsDBNull(imagenOrdinal)
                ? null
                : reader.GetString(imagenOrdinal),
            Propietario = new Propietario
            {
                IdPropietario = reader.GetInt32(nameof(Inmueble.IdPropietario)),
                Dni = reader.GetString("PropietarioDni"),
                Nombre = reader.GetString("PropietarioNombre"),
                Apellido = reader.GetString("PropietarioApellido"),
                Telefono = reader.IsDBNull(telefonoOrdinal)
                    ? null
                    : reader.GetString(telefonoOrdinal),
                Email = reader.GetString("PropietarioEmail")
            },
            TipoInmueble = new TipoInmueble
            {
                IdTipoInmueble = reader.GetInt32(nameof(Inmueble.IdTipoInmueble)),
                Nombre = reader.GetString("TipoNombre")
            }
        };
    }
}
