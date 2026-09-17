using inmobiliaria.Models;
using MySql.Data.MySqlClient;

namespace inmobiliaria.Repositories;

public class RepositorioInmuebleImagenes : RepositorioBase
{
    public RepositorioInmuebleImagenes(IConfiguration configuration)
        : base(configuration)
    {
    }

    public IList<InmuebleImagen> ObtenerPorInmueble(int idInmueble)
    {
        var imagenes = new List<InmuebleImagen>();
        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(
            """
            SELECT ii.IdInmuebleImagen, ii.IdInmueble, ii.Ruta
            FROM InmuebleImagenes ii
            WHERE ii.IdInmueble = @idInmueble
            ORDER BY ii.IdInmuebleImagen;
            """,
            conexion);
        comando.Parameters.Add("@idInmueble", MySqlDbType.Int32).Value = idInmueble;
        conexion.Open();

        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            imagenes.Add(Mapear(lector));
        }

        return imagenes;
    }

    public InmuebleImagen? ObtenerPorId(int id)
    {
        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(
            """
            SELECT
                ii.IdInmuebleImagen,
                ii.IdInmueble,
                ii.Ruta,
                i.Direccion
            FROM InmuebleImagenes ii
            INNER JOIN Inmuebles i ON i.IdInmueble = ii.IdInmueble
            WHERE ii.IdInmuebleImagen = @id;
            """,
            conexion);
        comando.Parameters.Add("@id", MySqlDbType.Int32).Value = id;
        conexion.Open();

        using var lector = comando.ExecuteReader();
        if (!lector.Read())
        {
            return null;
        }

        var imagen = Mapear(lector);
        imagen.Inmueble = new Inmueble
        {
            IdInmueble = imagen.IdInmueble,
            Direccion = lector.GetString(nameof(Inmueble.Direccion))
        };
        return imagen;
    }

    public void AltaVarias(int idInmueble, IEnumerable<string> rutas)
    {
        var rutasValidas = rutas
            .Where(ruta => !string.IsNullOrWhiteSpace(ruta))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (rutasValidas.Count == 0)
        {
            return;
        }

        using var conexion = CrearConexion();
        conexion.Open();
        using var transaccion = conexion.BeginTransaction();

        try
        {
            foreach (var ruta in rutasValidas)
            {
                using var comando = new MySqlCommand(
                    """
                    INSERT INTO InmuebleImagenes (IdInmueble, Ruta)
                    VALUES (@idInmueble, @ruta);
                    """,
                    conexion,
                    transaccion);
                comando.Parameters.Add("@idInmueble", MySqlDbType.Int32).Value = idInmueble;
                comando.Parameters.Add("@ruta", MySqlDbType.VarChar, 255).Value = ruta;
                comando.ExecuteNonQuery();
            }

            transaccion.Commit();
        }
        catch
        {
            transaccion.Rollback();
            throw;
        }
    }

    public bool Baja(int id)
    {
        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(
            """
            DELETE FROM InmuebleImagenes
            WHERE IdInmuebleImagen = @id;
            """,
            conexion);
        comando.Parameters.Add("@id", MySqlDbType.Int32).Value = id;
        conexion.Open();

        return comando.ExecuteNonQuery() > 0;
    }

    private static InmuebleImagen Mapear(MySqlDataReader lector) => new()
    {
        IdInmuebleImagen = lector.GetInt32(nameof(InmuebleImagen.IdInmuebleImagen)),
        IdInmueble = lector.GetInt32(nameof(InmuebleImagen.IdInmueble)),
        Ruta = lector.GetString(nameof(InmuebleImagen.Ruta))
    };
}
