using System.Globalization;
using inmobiliaria.Models;
using MySql.Data.MySqlClient;

namespace inmobiliaria.Repositories;

public class RepositorioReservas : RepositorioBase
{
    private const string Columnas = """
        r.IdReserva,
        r.IdInmueble,
        r.IdInquilino,
        r.FechaDesde,
        r.FechaHasta,
        r.MontoDia,
        r.FechaTerminacionAnticipada,
        r.MontoMulta,
        r.IdUsuarioCreador,
        r.IdUsuarioTerminador,
        r.IdReservaOrigen,
        i.IdPropietario,
        i.IdTipoInmueble,
        i.Direccion,
        i.Cupo,
        i.Coordenadas,
        i.PrecioDia,
        i.PorcentajeReserva,
        i.Disponible,
        i.ImagenPortada,
        t.Nombre AS TipoNombre,
        p.Dni AS PropietarioDni,
        p.Nombre AS PropietarioNombre,
        p.Apellido AS PropietarioApellido,
        p.Telefono AS PropietarioTelefono,
        p.Email AS PropietarioEmail,
        iq.Dni AS InquilinoDni,
        iq.Nombre AS InquilinoNombre,
        iq.Apellido AS InquilinoApellido,
        iq.Telefono AS InquilinoTelefono,
        iq.Email AS InquilinoEmail,
        uc.Nombre AS CreadorNombre,
        uc.Apellido AS CreadorApellido,
        uc.Email AS CreadorEmail,
        uc.Rol AS CreadorRol,
        uc.Avatar AS CreadorAvatar,
        ut.Nombre AS TerminadorNombre,
        ut.Apellido AS TerminadorApellido,
        ut.Email AS TerminadorEmail,
        ut.Rol AS TerminadorRol,
        ut.Avatar AS TerminadorAvatar
        """;

    public RepositorioReservas(IConfiguration configuration)
        : base(configuration)
    {
    }

    public IList<Reserva> ObtenerLista(
        string? busqueda = null,
        string? estado = null,
        int pagina = 1,
        int tamPagina = 10)
    {
        pagina = Math.Max(pagina, 1);
        tamPagina = Math.Clamp(tamPagina, 1, 10);

        var reservas = new List<Reserva>();
        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT {Columnas}
            FROM Reservas r
            INNER JOIN Inmuebles i ON i.IdInmueble = r.IdInmueble
            INNER JOIN TiposInmueble t ON t.IdTipoInmueble = i.IdTipoInmueble
            INNER JOIN Propietarios p ON p.IdPropietario = i.IdPropietario
            INNER JOIN Inquilinos iq ON iq.IdInquilino = r.IdInquilino
            INNER JOIN Usuarios uc ON uc.IdUsuario = r.IdUsuarioCreador
            LEFT JOIN Usuarios ut ON ut.IdUsuario = r.IdUsuarioTerminador
            WHERE (
                i.Direccion LIKE @busqueda
                OR t.Nombre LIKE @busqueda
                OR iq.Dni LIKE @busqueda
                OR iq.Nombre LIKE @busqueda
                OR iq.Apellido LIKE @busqueda
            )
            AND (
                @estado = ''
                OR (@estado = 'pendiente' AND r.FechaDesde > CURDATE())
                OR (@estado = 'vigente'
                    AND r.FechaDesde <= CURDATE()
                    AND COALESCE(r.FechaTerminacionAnticipada, r.FechaHasta) >= CURDATE())
                OR (@estado = 'finalizada'
                    AND COALESCE(r.FechaTerminacionAnticipada, r.FechaHasta) < CURDATE())
                OR (@estado = 'anticipada' AND r.FechaTerminacionAnticipada IS NOT NULL)
            )
            ORDER BY r.FechaDesde DESC, r.IdReserva DESC
            LIMIT @limite OFFSET @desplazamiento;
            """;
        AgregarParametrosListado(command, busqueda, estado, pagina, tamPagina);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            reservas.Add(MapearReserva(reader));
        }

        return reservas;
    }

    public int ObtenerCantidad(string? busqueda = null, string? estado = null)
    {
        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT COUNT(*)
            FROM Reservas r
            INNER JOIN Inmuebles i ON i.IdInmueble = r.IdInmueble
            INNER JOIN TiposInmueble t ON t.IdTipoInmueble = i.IdTipoInmueble
            INNER JOIN Inquilinos iq ON iq.IdInquilino = r.IdInquilino
            WHERE (
                i.Direccion LIKE @busqueda
                OR t.Nombre LIKE @busqueda
                OR iq.Dni LIKE @busqueda
                OR iq.Nombre LIKE @busqueda
                OR iq.Apellido LIKE @busqueda
            )
            AND (
                @estado = ''
                OR (@estado = 'pendiente' AND r.FechaDesde > CURDATE())
                OR (@estado = 'vigente'
                    AND r.FechaDesde <= CURDATE()
                    AND COALESCE(r.FechaTerminacionAnticipada, r.FechaHasta) >= CURDATE())
                OR (@estado = 'finalizada'
                    AND COALESCE(r.FechaTerminacionAnticipada, r.FechaHasta) < CURDATE())
                OR (@estado = 'anticipada' AND r.FechaTerminacionAnticipada IS NOT NULL)
            );
            """;
        AgregarParametrosFiltros(command, busqueda, estado);

        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public Reserva? ObtenerPorId(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT {Columnas}
            FROM Reservas r
            INNER JOIN Inmuebles i ON i.IdInmueble = r.IdInmueble
            INNER JOIN TiposInmueble t ON t.IdTipoInmueble = i.IdTipoInmueble
            INNER JOIN Propietarios p ON p.IdPropietario = i.IdPropietario
            INNER JOIN Inquilinos iq ON iq.IdInquilino = r.IdInquilino
            INNER JOIN Usuarios uc ON uc.IdUsuario = r.IdUsuarioCreador
            LEFT JOIN Usuarios ut ON ut.IdUsuario = r.IdUsuarioTerminador
            WHERE r.IdReserva = @id;
            """;
        command.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

        connection.Open();
        using var reader = command.ExecuteReader();

        return reader.Read() ? MapearReserva(reader) : null;
    }

    public bool ExisteSuperposicion(
        int idInmueble,
        DateTime fechaDesde,
        DateTime fechaHasta,
        int? idReservaExcluir = null)
    {
        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM Reservas
                WHERE IdInmueble = @idInmueble
                  AND IdReserva <> COALESCE(@idReservaExcluir, 0)
                  AND FechaDesde <= @fechaHasta
                  AND COALESCE(FechaTerminacionAnticipada, FechaHasta) >= @fechaDesde
            );
            """;
        command.Parameters.Add("@idInmueble", MySqlDbType.Int32).Value = idInmueble;
        command.Parameters.Add("@fechaDesde", MySqlDbType.Date).Value = fechaDesde.Date;
        command.Parameters.Add("@fechaHasta", MySqlDbType.Date).Value = fechaHasta.Date;
        command.Parameters.Add("@idReservaExcluir", MySqlDbType.Int32).Value =
            idReservaExcluir.HasValue ? idReservaExcluir.Value : DBNull.Value;

        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar()) == 1;
    }

    public int Alta(
        Reserva reserva,
        int idUsuarioCreador,
        decimal porcentajeReserva,
        int? idReservaOrigen = null)
    {
        ArgumentNullException.ThrowIfNull(reserva);
        if (porcentajeReserva is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(porcentajeReserva),
                "El porcentaje de pago inicial debe estar entre 1 y 100.");
        }

        using var connection = CrearConexion();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            using var crearReserva = connection.CreateCommand();
            crearReserva.Transaction = transaction;
            crearReserva.CommandText = """
                INSERT INTO Reservas
                    (IdInmueble, IdInquilino, FechaDesde, FechaHasta, MontoDia,
                     FechaTerminacionAnticipada, MontoMulta,
                     IdUsuarioCreador, IdUsuarioTerminador, IdReservaOrigen)
                VALUES
                    (@idInmueble, @idInquilino, @fechaDesde, @fechaHasta, @montoDia,
                     NULL, NULL, @idUsuarioCreador, NULL, @idReservaOrigen);
                """;
            AgregarParametrosReserva(crearReserva, reserva);
            crearReserva.Parameters.Add("@idUsuarioCreador", MySqlDbType.Int32).Value =
                idUsuarioCreador;
            crearReserva.Parameters.Add("@idReservaOrigen", MySqlDbType.Int32).Value =
                idReservaOrigen.HasValue ? idReservaOrigen.Value : DBNull.Value;
            crearReserva.ExecuteNonQuery();
            reserva.IdReserva = Convert.ToInt32(crearReserva.LastInsertedId);

            var dias = Math.Max(
                1,
                (reserva.FechaHasta.Date - reserva.FechaDesde.Date).Days);
            var importePagoInicial = decimal.Round(
                dias * reserva.MontoDia * porcentajeReserva / 100m,
                2,
                MidpointRounding.AwayFromZero);

            using var crearPago = connection.CreateCommand();
            crearPago.Transaction = transaction;
            crearPago.CommandText = """
                INSERT INTO Pagos
                    (IdReserva, Concepto, FechaPago, Importe, Anulado,
                     IdUsuarioCreador, IdUsuarioAnulador, FechaAnulacion)
                VALUES
                    (@idReserva, @concepto, @fechaPago, @importe, 0,
                     @idUsuarioCreador, NULL, NULL);
                """;
            crearPago.Parameters.Add("@idReserva", MySqlDbType.Int32).Value =
                reserva.IdReserva;
            crearPago.Parameters.Add("@concepto", MySqlDbType.VarChar, 150).Value =
                $"Pago inicial de reserva ({porcentajeReserva.ToString("0.##", CultureInfo.InvariantCulture)}%)";
            crearPago.Parameters.Add("@fechaPago", MySqlDbType.Date).Value = DateTime.Today;
            crearPago.Parameters.Add("@importe", MySqlDbType.Decimal).Value =
                importePagoInicial;
            crearPago.Parameters.Add("@idUsuarioCreador", MySqlDbType.Int32).Value =
                idUsuarioCreador;
            crearPago.ExecuteNonQuery();

            transaction.Commit();
            return reserva.IdReserva;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public bool TerminarAnticipadamente(
        int idReserva,
        DateTime fechaTerminacion,
        decimal montoMulta,
        int idUsuarioTerminador)
    {
        using var connection = CrearConexion();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            using var actualizarReserva = connection.CreateCommand();
            actualizarReserva.Transaction = transaction;
            actualizarReserva.CommandText = """
                UPDATE Reservas
                SET FechaTerminacionAnticipada = @fechaTerminacion,
                    MontoMulta = @montoMulta,
                    IdUsuarioTerminador = @idUsuarioTerminador
                WHERE IdReserva = @idReserva
                  AND FechaTerminacionAnticipada IS NULL;
                """;
            actualizarReserva.Parameters.Add("@fechaTerminacion", MySqlDbType.Date).Value =
                fechaTerminacion.Date;
            actualizarReserva.Parameters.Add("@montoMulta", MySqlDbType.Decimal).Value =
                montoMulta;
            actualizarReserva.Parameters.Add("@idUsuarioTerminador", MySqlDbType.Int32).Value =
                idUsuarioTerminador;
            actualizarReserva.Parameters.Add("@idReserva", MySqlDbType.Int32).Value =
                idReserva;

            if (actualizarReserva.ExecuteNonQuery() != 1)
            {
                transaction.Rollback();
                return false;
            }

            using var registrarPago = connection.CreateCommand();
            registrarPago.Transaction = transaction;
            registrarPago.CommandText = """
                INSERT INTO Pagos
                    (IdReserva, Concepto, FechaPago, Importe, Anulado,
                     IdUsuarioCreador, IdUsuarioAnulador, FechaAnulacion)
                VALUES
                    (@idReserva, @concepto, @fechaPago, @importe, 0,
                     @idUsuarioCreador, NULL, NULL);
                """;
            registrarPago.Parameters.Add("@idReserva", MySqlDbType.Int32).Value = idReserva;
            registrarPago.Parameters.Add("@concepto", MySqlDbType.VarChar, 150).Value =
                "Multa por terminación anticipada";
            registrarPago.Parameters.Add("@fechaPago", MySqlDbType.Date).Value = DateTime.Today;
            registrarPago.Parameters.Add("@importe", MySqlDbType.Decimal).Value = montoMulta;
            registrarPago.Parameters.Add("@idUsuarioCreador", MySqlDbType.Int32).Value =
                idUsuarioTerminador;
            registrarPago.ExecuteNonQuery();

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public bool Modificacion(Reserva reserva)
    {
        ArgumentNullException.ThrowIfNull(reserva);

        if (reserva.IdReserva <= 0)
        {
            return false;
        }

        using var connection = CrearConexion();
        using var command = connection.CreateCommand();

        command.CommandText = """
            UPDATE Reservas
            SET IdInmueble = @idInmueble,
                IdInquilino = @idInquilino,
                FechaDesde = @fechaDesde,
                FechaHasta = @fechaHasta,
                MontoDia = @montoDia
            WHERE IdReserva = @id;
            """;
        AgregarParametrosReserva(command, reserva);
        command.Parameters.Add("@id", MySqlDbType.Int32).Value = reserva.IdReserva;

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
            DELETE FROM Reservas
            WHERE IdReserva = @id;
            """;
        command.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

        connection.Open();
        return command.ExecuteNonQuery() > 0;
    }

    private static void AgregarParametrosListado(
        MySqlCommand command,
        string? busqueda,
        string? estado,
        int pagina,
        int tamPagina)
    {
        AgregarParametrosFiltros(command, busqueda, estado);
        command.Parameters.Add("@limite", MySqlDbType.Int32).Value = tamPagina;
        command.Parameters.Add("@desplazamiento", MySqlDbType.Int32).Value =
            (pagina - 1) * tamPagina;
    }

    private static void AgregarParametrosFiltros(
        MySqlCommand command,
        string? busqueda,
        string? estado)
    {
        command.Parameters.Add("@busqueda", MySqlDbType.VarChar, 202).Value =
            $"%{busqueda?.Trim() ?? string.Empty}%";
        command.Parameters.Add("@estado", MySqlDbType.VarChar, 20).Value =
            estado?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static void AgregarParametrosReserva(MySqlCommand command, Reserva reserva)
    {
        command.Parameters.Add("@idInmueble", MySqlDbType.Int32).Value = reserva.IdInmueble;
        command.Parameters.Add("@idInquilino", MySqlDbType.Int32).Value = reserva.IdInquilino;
        command.Parameters.Add("@fechaDesde", MySqlDbType.Date).Value = reserva.FechaDesde.Date;
        command.Parameters.Add("@fechaHasta", MySqlDbType.Date).Value = reserva.FechaHasta.Date;
        command.Parameters.Add("@montoDia", MySqlDbType.Decimal).Value = reserva.MontoDia;
    }

    private static Reserva MapearReserva(MySqlDataReader reader)
    {
        var terminacionOrdinal = reader.GetOrdinal(nameof(Reserva.FechaTerminacionAnticipada));
        var multaOrdinal = reader.GetOrdinal(nameof(Reserva.MontoMulta));
        var imagenOrdinal = reader.GetOrdinal(nameof(Inmueble.ImagenPortada));
        var propietarioTelefonoOrdinal = reader.GetOrdinal("PropietarioTelefono");
        var inquilinoTelefonoOrdinal = reader.GetOrdinal("InquilinoTelefono");
        var terminadorOrdinal = reader.GetOrdinal(nameof(Reserva.IdUsuarioTerminador));

        return new Reserva
        {
            IdReserva = reader.GetInt32(nameof(Reserva.IdReserva)),
            IdInmueble = reader.GetInt32(nameof(Reserva.IdInmueble)),
            IdInquilino = reader.GetInt32(nameof(Reserva.IdInquilino)),
            FechaDesde = reader.GetDateTime(nameof(Reserva.FechaDesde)),
            FechaHasta = reader.GetDateTime(nameof(Reserva.FechaHasta)),
            MontoDia = reader.GetDecimal(nameof(Reserva.MontoDia)),
            FechaTerminacionAnticipada = reader.IsDBNull(terminacionOrdinal)
                ? null
                : reader.GetDateTime(terminacionOrdinal),
            MontoMulta = reader.IsDBNull(multaOrdinal)
                ? null
                : reader.GetDecimal(multaOrdinal),
            IdUsuarioCreador = reader.GetInt32(nameof(Reserva.IdUsuarioCreador)),
            IdUsuarioTerminador = reader.IsDBNull(terminadorOrdinal)
                ? null
                : reader.GetInt32(terminadorOrdinal),
            IdReservaOrigen = reader.IsDBNull(reader.GetOrdinal(nameof(Reserva.IdReservaOrigen)))
                ? null
                : reader.GetInt32(nameof(Reserva.IdReservaOrigen)),
            Inmueble = new Inmueble
            {
                IdInmueble = reader.GetInt32(nameof(Reserva.IdInmueble)),
                IdPropietario = reader.GetInt32(nameof(Inmueble.IdPropietario)),
                IdTipoInmueble = reader.GetInt32(nameof(Inmueble.IdTipoInmueble)),
                Direccion = reader.GetString(nameof(Inmueble.Direccion)),
                Cupo = reader.GetInt32(nameof(Inmueble.Cupo)),
                Coordenadas = reader.GetString(nameof(Inmueble.Coordenadas)),
                PrecioDia = reader.GetDecimal(nameof(Inmueble.PrecioDia)),
                PorcentajeReserva = reader.GetDecimal(nameof(Inmueble.PorcentajeReserva)),
                Disponible = reader.GetBoolean(nameof(Inmueble.Disponible)),
                ImagenPortada = reader.IsDBNull(imagenOrdinal)
                    ? null
                    : reader.GetString(imagenOrdinal),
                TipoInmueble = new TipoInmueble
                {
                    IdTipoInmueble = reader.GetInt32(nameof(Inmueble.IdTipoInmueble)),
                    Nombre = reader.GetString("TipoNombre")
                },
                Propietario = new Propietario
                {
                    IdPropietario = reader.GetInt32(nameof(Inmueble.IdPropietario)),
                    Dni = reader.GetString("PropietarioDni"),
                    Nombre = reader.GetString("PropietarioNombre"),
                    Apellido = reader.GetString("PropietarioApellido"),
                    Telefono = reader.IsDBNull(propietarioTelefonoOrdinal)
                        ? null
                        : reader.GetString(propietarioTelefonoOrdinal),
                    Email = reader.GetString("PropietarioEmail")
                }
            },
            Inquilino = new Inquilino
            {
                IdInquilino = reader.GetInt32(nameof(Reserva.IdInquilino)),
                Dni = reader.GetString("InquilinoDni"),
                Nombre = reader.GetString("InquilinoNombre"),
                Apellido = reader.GetString("InquilinoApellido"),
                Telefono = reader.IsDBNull(inquilinoTelefonoOrdinal)
                    ? null
                    : reader.GetString(inquilinoTelefonoOrdinal),
                Email = reader.GetString("InquilinoEmail")
            },
            UsuarioCreador = MapearUsuario(reader, "Creador", "IdUsuarioCreador"),
            UsuarioTerminador = reader.IsDBNull(terminadorOrdinal)
                ? null
                : MapearUsuario(reader, "Terminador", "IdUsuarioTerminador")
        };
    }

    private static Usuario MapearUsuario(
        MySqlDataReader reader,
        string prefijo,
        string columnaId) => new()
    {
        IdUsuario = reader.GetInt32(columnaId),
        Nombre = reader.GetString($"{prefijo}Nombre"),
        Apellido = reader.GetString($"{prefijo}Apellido"),
        Email = reader.GetString($"{prefijo}Email"),
        Rol = reader.GetString($"{prefijo}Rol"),
        Avatar = reader.IsDBNull(reader.GetOrdinal($"{prefijo}Avatar"))
            ? null
            : reader.GetString($"{prefijo}Avatar")
    };
}
