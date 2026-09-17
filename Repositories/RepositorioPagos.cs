using inmobiliaria.Models;
using MySql.Data.MySqlClient;

namespace inmobiliaria.Repositories;

public class RepositorioPagos : RepositorioBase
{
    private const string Columnas = """
        p.IdPago,
        p.IdReserva,
        p.Concepto,
        p.FechaPago,
        p.Importe,
        p.Anulado,
        p.IdUsuarioCreador,
        p.IdUsuarioAnulador,
        p.FechaAnulacion,
        uc.Nombre AS CreadorNombre,
        uc.Apellido AS CreadorApellido,
        uc.Email AS CreadorEmail,
        uc.Rol AS CreadorRol,
        uc.Avatar AS CreadorAvatar,
        ua.Nombre AS AnuladorNombre,
        ua.Apellido AS AnuladorApellido,
        ua.Email AS AnuladorEmail,
        ua.Rol AS AnuladorRol,
        ua.Avatar AS AnuladorAvatar
        """;

    public RepositorioPagos(IConfiguration configuration)
        : base(configuration)
    {
    }

    public IList<Pago> ObtenerListaPorReserva(
        int idReserva,
        string? busqueda,
        int pagina,
        int tamPagina)
    {
        pagina = Math.Max(1, pagina);
        tamPagina = Math.Clamp(tamPagina, 1, 10);

        const string sqlBase = """
            FROM Pagos p
            INNER JOIN Usuarios uc ON uc.IdUsuario = p.IdUsuarioCreador
            LEFT JOIN Usuarios ua ON ua.IdUsuario = p.IdUsuarioAnulador
            WHERE p.IdReserva = @idReserva
              AND p.Concepto LIKE @busqueda
            """;

        var pagos = new List<Pago>();
        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(
            $"""
            SELECT {Columnas}
            {sqlBase}
            ORDER BY p.FechaPago DESC, p.IdPago DESC
            LIMIT @limite OFFSET @desplazamiento;
            """,
            conexion);
        AgregarParametrosListado(comando, idReserva, busqueda, pagina, tamPagina);
        conexion.Open();

        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            pagos.Add(Mapear(lector));
        }

        return pagos;
    }

    public int ObtenerCantidadPorReserva(int idReserva, string? busqueda)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM Pagos
            WHERE IdReserva = @idReserva
              AND Concepto LIKE @busqueda;
            """;

        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(sql, conexion);
        AgregarParametrosFiltro(comando, idReserva, busqueda);
        conexion.Open();

        return Convert.ToInt32(comando.ExecuteScalar());
    }

    public decimal ObtenerTotalRegistrado(int idReserva)
    {
        const string sql = """
            SELECT COALESCE(SUM(Importe), 0)
            FROM Pagos
            WHERE IdReserva = @idReserva
              AND Anulado = 0;
            """;

        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.Add("@idReserva", MySqlDbType.Int32).Value = idReserva;
        conexion.Open();

        return Convert.ToDecimal(comando.ExecuteScalar());
    }

    public Pago? ObtenerPorId(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(
            $"""
            SELECT {Columnas}
            FROM Pagos p
            INNER JOIN Usuarios uc ON uc.IdUsuario = p.IdUsuarioCreador
            LEFT JOIN Usuarios ua ON ua.IdUsuario = p.IdUsuarioAnulador
            WHERE p.IdPago = @id;
            """,
            conexion);
        comando.Parameters.Add("@id", MySqlDbType.Int32).Value = id;
        conexion.Open();

        using var lector = comando.ExecuteReader();
        return lector.Read() ? Mapear(lector) : null;
    }

    public int Alta(Pago pago)
    {
        ArgumentNullException.ThrowIfNull(pago);

        const string sql = """
            INSERT INTO Pagos
                (IdReserva, Concepto, FechaPago, Importe, Anulado,
                 IdUsuarioCreador, IdUsuarioAnulador, FechaAnulacion)
            VALUES
                (@idReserva, @concepto, @fechaPago, @importe, 0,
                 @idUsuarioCreador, NULL, NULL);
            SELECT LAST_INSERT_ID();
            """;

        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.Add("@idReserva", MySqlDbType.Int32).Value = pago.IdReserva;
        comando.Parameters.Add("@concepto", MySqlDbType.VarChar, 150).Value = pago.Concepto;
        comando.Parameters.Add("@fechaPago", MySqlDbType.Date).Value = pago.FechaPago.Date;
        comando.Parameters.Add("@importe", MySqlDbType.Decimal).Value = pago.Importe;
        comando.Parameters.Add("@idUsuarioCreador", MySqlDbType.Int32).Value =
            pago.IdUsuarioCreador;
        conexion.Open();

        pago.IdPago = Convert.ToInt32(comando.ExecuteScalar());
        return pago.IdPago;
    }

    public bool ModificarConcepto(int idPago, string concepto)
    {
        const string sql = """
            UPDATE Pagos
            SET Concepto = @concepto
            WHERE IdPago = @idPago
              AND Anulado = 0;
            """;

        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.Add("@concepto", MySqlDbType.VarChar, 150).Value = concepto;
        comando.Parameters.Add("@idPago", MySqlDbType.Int32).Value = idPago;
        conexion.Open();

        return comando.ExecuteNonQuery() > 0;
    }

    public bool Anular(int idPago, int idUsuarioAnulador)
    {
        const string sql = """
            UPDATE Pagos
            SET Anulado = 1,
                IdUsuarioAnulador = @idUsuarioAnulador,
                FechaAnulacion = NOW()
            WHERE IdPago = @idPago
              AND Anulado = 0;
            """;

        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.Add("@idUsuarioAnulador", MySqlDbType.Int32).Value =
            idUsuarioAnulador;
        comando.Parameters.Add("@idPago", MySqlDbType.Int32).Value = idPago;
        conexion.Open();

        return comando.ExecuteNonQuery() > 0;
    }

    private static void AgregarParametrosListado(
        MySqlCommand comando,
        int idReserva,
        string? busqueda,
        int pagina,
        int tamPagina)
    {
        AgregarParametrosFiltro(comando, idReserva, busqueda);
        comando.Parameters.Add("@limite", MySqlDbType.Int32).Value = tamPagina;
        comando.Parameters.Add("@desplazamiento", MySqlDbType.Int32).Value =
            (pagina - 1) * tamPagina;
    }

    private static void AgregarParametrosFiltro(
        MySqlCommand comando,
        int idReserva,
        string? busqueda)
    {
        comando.Parameters.Add("@idReserva", MySqlDbType.Int32).Value = idReserva;
        comando.Parameters.Add("@busqueda", MySqlDbType.VarChar, 152).Value =
            $"%{busqueda?.Trim() ?? string.Empty}%";
    }

    private static Pago Mapear(MySqlDataReader lector)
    {
        var anuladorOrdinal = lector.GetOrdinal(nameof(Pago.IdUsuarioAnulador));
        var fechaAnulacionOrdinal = lector.GetOrdinal(nameof(Pago.FechaAnulacion));

        return new Pago
        {
            IdPago = lector.GetInt32(nameof(Pago.IdPago)),
            IdReserva = lector.GetInt32(nameof(Pago.IdReserva)),
            Concepto = lector.GetString(nameof(Pago.Concepto)),
            FechaPago = lector.GetDateTime(nameof(Pago.FechaPago)),
            Importe = lector.GetDecimal(nameof(Pago.Importe)),
            Anulado = lector.GetBoolean(nameof(Pago.Anulado)),
            IdUsuarioCreador = lector.GetInt32(nameof(Pago.IdUsuarioCreador)),
            IdUsuarioAnulador = lector.IsDBNull(anuladorOrdinal)
                ? null
                : lector.GetInt32(anuladorOrdinal),
            FechaAnulacion = lector.IsDBNull(fechaAnulacionOrdinal)
                ? null
                : lector.GetDateTime(fechaAnulacionOrdinal),
            UsuarioCreador = MapearUsuario(lector, "Creador"),
            UsuarioAnulador = lector.IsDBNull(anuladorOrdinal)
                ? null
                : MapearUsuario(lector, "Anulador")
        };
    }

    private static Usuario MapearUsuario(MySqlDataReader lector, string prefijo) => new()
    {
        IdUsuario = lector.GetInt32($"IdUsuario{prefijo}"),
        Nombre = lector.GetString($"{prefijo}Nombre"),
        Apellido = lector.GetString($"{prefijo}Apellido"),
        Email = lector.GetString($"{prefijo}Email"),
        Rol = lector.GetString($"{prefijo}Rol"),
        Avatar = lector.IsDBNull(lector.GetOrdinal($"{prefijo}Avatar"))
            ? null
            : lector.GetString($"{prefijo}Avatar")
    };
}
