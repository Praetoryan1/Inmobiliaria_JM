using inmobiliaria.Models;
using MySql.Data.MySqlClient;

namespace inmobiliaria.Repositories;

public class RepositorioInformes : RepositorioBase
{
    private const string ColumnasInmueble = """
        i.IdInmueble,
        i.IdPropietario,
        i.IdTipoInmueble,
        i.Direccion,
        i.Cupo,
        i.Coordenadas,
        i.PrecioDia,
        i.PorcentajeReserva,
        i.Disponible,
        i.ImagenPortada,
        p.Dni AS PropietarioDni,
        p.Nombre AS PropietarioNombre,
        p.Apellido AS PropietarioApellido,
        p.Telefono AS PropietarioTelefono,
        p.Email AS PropietarioEmail,
        t.Nombre AS TipoNombre
        """;

    private const string FiltroBusqueda = """
        (
            i.Direccion LIKE @busqueda
            OR i.Coordenadas LIKE @busqueda
            OR p.Dni LIKE @busqueda
            OR p.Nombre LIKE @busqueda
            OR p.Apellido LIKE @busqueda
            OR t.Nombre LIKE @busqueda
        )
        """;

    public RepositorioInformes(IConfiguration configuration)
        : base(configuration)
    {
    }

    public IList<InformeInmueble> ObtenerInmuebles(
        string? busqueda,
        bool? disponible,
        int? idPropietario,
        int pagina,
        int tamPagina)
    {
        pagina = Math.Max(1, pagina);
        tamPagina = Math.Clamp(tamPagina, 1, 10);

        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(
            $"""
            SELECT
                {ColumnasInmueble},
                COALESCE(resumen.CantidadReservas, 0) AS CantidadReservas
            FROM Inmuebles i
            INNER JOIN Propietarios p ON p.IdPropietario = i.IdPropietario
            INNER JOIN TiposInmueble t ON t.IdTipoInmueble = i.IdTipoInmueble
            LEFT JOIN (
                SELECT IdInmueble, COUNT(*) AS CantidadReservas
                FROM Reservas
                GROUP BY IdInmueble
            ) resumen ON resumen.IdInmueble = i.IdInmueble
            WHERE {FiltroBusqueda}
              AND (@disponible IS NULL OR i.Disponible = @disponible)
              AND (@idPropietario IS NULL OR i.IdPropietario = @idPropietario)
            ORDER BY i.Direccion, i.IdInmueble
            LIMIT @limite OFFSET @desplazamiento;
            """,
            conexion);
        AgregarParametrosInmuebles(
            comando,
            busqueda,
            disponible,
            idPropietario,
            pagina,
            tamPagina);
        conexion.Open();

        return LeerResultados(comando);
    }

    public int ObtenerCantidadInmuebles(
        string? busqueda,
        bool? disponible,
        int? idPropietario)
    {
        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(
            $"""
            SELECT COUNT(*)
            FROM Inmuebles i
            INNER JOIN Propietarios p ON p.IdPropietario = i.IdPropietario
            INNER JOIN TiposInmueble t ON t.IdTipoInmueble = i.IdTipoInmueble
            WHERE {FiltroBusqueda}
              AND (@disponible IS NULL OR i.Disponible = @disponible)
              AND (@idPropietario IS NULL OR i.IdPropietario = @idPropietario);
            """,
            conexion);
        AgregarParametrosFiltros(comando, busqueda, disponible, idPropietario);
        conexion.Open();

        return Convert.ToInt32(comando.ExecuteScalar());
    }

    public IList<InformeInmueble> ObtenerMasReservados(
        string? busqueda,
        int pagina,
        int tamPagina)
    {
        pagina = Math.Max(1, pagina);
        tamPagina = Math.Clamp(tamPagina, 1, 10);

        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(
            $"""
            SELECT
                {ColumnasInmueble},
                resumen.CantidadReservas
            FROM Inmuebles i
            INNER JOIN Propietarios p ON p.IdPropietario = i.IdPropietario
            INNER JOIN TiposInmueble t ON t.IdTipoInmueble = i.IdTipoInmueble
            INNER JOIN (
                SELECT IdInmueble, COUNT(*) AS CantidadReservas
                FROM Reservas
                WHERE FechaDesde BETWEEN DATE_SUB(CURDATE(), INTERVAL 365 DAY)
                    AND CURDATE()
                GROUP BY IdInmueble
            ) resumen ON resumen.IdInmueble = i.IdInmueble
            WHERE {FiltroBusqueda}
            ORDER BY resumen.CantidadReservas DESC, i.Direccion, i.IdInmueble
            LIMIT @limite OFFSET @desplazamiento;
            """,
            conexion);
        AgregarParametrosPagina(comando, busqueda, pagina, tamPagina);
        conexion.Open();

        return LeerResultados(comando);
    }

    public int ObtenerCantidadMasReservados(string? busqueda)
    {
        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(
            $"""
            SELECT COUNT(*)
            FROM Inmuebles i
            INNER JOIN Propietarios p ON p.IdPropietario = i.IdPropietario
            INNER JOIN TiposInmueble t ON t.IdTipoInmueble = i.IdTipoInmueble
            WHERE {FiltroBusqueda}
              AND EXISTS (
                  SELECT 1
                  FROM Reservas r
                  WHERE r.IdInmueble = i.IdInmueble
                    AND r.FechaDesde BETWEEN DATE_SUB(CURDATE(), INTERVAL 365 DAY)
                        AND CURDATE()
              );
            """,
            conexion);
        AgregarParametroBusqueda(comando, busqueda);
        conexion.Open();

        return Convert.ToInt32(comando.ExecuteScalar());
    }

    public IList<InformeInmueble> ObtenerSinReservas(
        string? busqueda,
        int dias,
        int pagina,
        int tamPagina)
    {
        dias = Math.Clamp(dias, 1, 3650);
        pagina = Math.Max(1, pagina);
        tamPagina = Math.Clamp(tamPagina, 1, 10);

        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(
            $"""
            SELECT
                {ColumnasInmueble},
                COALESCE(resumen.CantidadReservas, 0) AS CantidadReservas
            FROM Inmuebles i
            INNER JOIN Propietarios p ON p.IdPropietario = i.IdPropietario
            INNER JOIN TiposInmueble t ON t.IdTipoInmueble = i.IdTipoInmueble
            LEFT JOIN (
                SELECT IdInmueble, COUNT(*) AS CantidadReservas
                FROM Reservas
                GROUP BY IdInmueble
            ) resumen ON resumen.IdInmueble = i.IdInmueble
            WHERE {FiltroBusqueda}
              AND NOT EXISTS (
                  SELECT 1
                  FROM Reservas r
                  WHERE r.IdInmueble = i.IdInmueble
                    AND r.FechaDesde <= CURDATE()
                    AND COALESCE(r.FechaTerminacionAnticipada, r.FechaHasta)
                        >= DATE_SUB(CURDATE(), INTERVAL @dias DAY)
              )
            ORDER BY i.Direccion, i.IdInmueble
            LIMIT @limite OFFSET @desplazamiento;
            """,
            conexion);
        AgregarParametrosPagina(comando, busqueda, pagina, tamPagina);
        comando.Parameters.Add("@dias", MySqlDbType.Int32).Value = dias;
        conexion.Open();

        return LeerResultados(comando);
    }

    public int ObtenerCantidadSinReservas(string? busqueda, int dias)
    {
        dias = Math.Clamp(dias, 1, 3650);
        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(
            $"""
            SELECT COUNT(*)
            FROM Inmuebles i
            INNER JOIN Propietarios p ON p.IdPropietario = i.IdPropietario
            INNER JOIN TiposInmueble t ON t.IdTipoInmueble = i.IdTipoInmueble
            WHERE {FiltroBusqueda}
              AND NOT EXISTS (
                  SELECT 1
                  FROM Reservas r
                  WHERE r.IdInmueble = i.IdInmueble
                    AND r.FechaDesde <= CURDATE()
                    AND COALESCE(r.FechaTerminacionAnticipada, r.FechaHasta)
                        >= DATE_SUB(CURDATE(), INTERVAL @dias DAY)
              );
            """,
            conexion);
        AgregarParametroBusqueda(comando, busqueda);
        comando.Parameters.Add("@dias", MySqlDbType.Int32).Value = dias;
        conexion.Open();

        return Convert.ToInt32(comando.ExecuteScalar());
    }

    private static IList<InformeInmueble> LeerResultados(MySqlCommand comando)
    {
        var resultados = new List<InformeInmueble>();
        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            resultados.Add(Mapear(lector));
        }

        return resultados;
    }

    private static void AgregarParametrosInmuebles(
        MySqlCommand comando,
        string? busqueda,
        bool? disponible,
        int? idPropietario,
        int pagina,
        int tamPagina)
    {
        AgregarParametrosFiltros(comando, busqueda, disponible, idPropietario);
        AgregarParametrosPagina(comando, pagina, tamPagina);
    }

    private static void AgregarParametrosFiltros(
        MySqlCommand comando,
        string? busqueda,
        bool? disponible,
        int? idPropietario)
    {
        AgregarParametroBusqueda(comando, busqueda);
        comando.Parameters.Add("@disponible", MySqlDbType.Byte).Value =
            disponible.HasValue ? disponible.Value : DBNull.Value;
        comando.Parameters.Add("@idPropietario", MySqlDbType.Int32).Value =
            idPropietario.HasValue ? idPropietario.Value : DBNull.Value;
    }

    private static void AgregarParametrosPagina(
        MySqlCommand comando,
        string? busqueda,
        int pagina,
        int tamPagina)
    {
        AgregarParametroBusqueda(comando, busqueda);
        AgregarParametrosPagina(comando, pagina, tamPagina);
    }

    private static void AgregarParametrosPagina(
        MySqlCommand comando,
        int pagina,
        int tamPagina)
    {
        comando.Parameters.Add("@limite", MySqlDbType.Int32).Value = tamPagina;
        comando.Parameters.Add("@desplazamiento", MySqlDbType.Int32).Value =
            (pagina - 1) * tamPagina;
    }

    private static void AgregarParametroBusqueda(MySqlCommand comando, string? busqueda)
    {
        comando.Parameters.Add("@busqueda", MySqlDbType.VarChar, 202).Value =
            $"%{busqueda?.Trim() ?? string.Empty}%";
    }

    private static InformeInmueble Mapear(MySqlDataReader lector)
    {
        var imagenOrdinal = lector.GetOrdinal(nameof(Inmueble.ImagenPortada));
        var telefonoOrdinal = lector.GetOrdinal("PropietarioTelefono");

        return new InformeInmueble
        {
            Inmueble = new Inmueble
            {
                IdInmueble = lector.GetInt32(nameof(Inmueble.IdInmueble)),
                IdPropietario = lector.GetInt32(nameof(Inmueble.IdPropietario)),
                IdTipoInmueble = lector.GetInt32(nameof(Inmueble.IdTipoInmueble)),
                Direccion = lector.GetString(nameof(Inmueble.Direccion)),
                Cupo = lector.GetInt32(nameof(Inmueble.Cupo)),
                Coordenadas = lector.GetString(nameof(Inmueble.Coordenadas)),
                PrecioDia = lector.GetDecimal(nameof(Inmueble.PrecioDia)),
                PorcentajeReserva = lector.GetDecimal(nameof(Inmueble.PorcentajeReserva)),
                Disponible = lector.GetBoolean(nameof(Inmueble.Disponible)),
                ImagenPortada = lector.IsDBNull(imagenOrdinal)
                    ? null
                    : lector.GetString(imagenOrdinal),
                Propietario = new Propietario
                {
                    IdPropietario = lector.GetInt32(nameof(Inmueble.IdPropietario)),
                    Dni = lector.GetString("PropietarioDni"),
                    Nombre = lector.GetString("PropietarioNombre"),
                    Apellido = lector.GetString("PropietarioApellido"),
                    Telefono = lector.IsDBNull(telefonoOrdinal)
                        ? null
                        : lector.GetString(telefonoOrdinal),
                    Email = lector.GetString("PropietarioEmail")
                },
                TipoInmueble = new TipoInmueble
                {
                    IdTipoInmueble = lector.GetInt32(nameof(Inmueble.IdTipoInmueble)),
                    Nombre = lector.GetString("TipoNombre")
                }
            },
            CantidadReservas = lector.GetInt32(nameof(InformeInmueble.CantidadReservas))
        };
    }
}
