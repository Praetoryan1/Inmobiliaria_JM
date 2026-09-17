using inmobiliaria.Models;
using MySql.Data.MySqlClient;

namespace inmobiliaria.Repositories;

public class RepositorioUsuarios : RepositorioBase
{
    public RepositorioUsuarios(IConfiguration configuration)
        : base(configuration)
    {
    }

    public Usuario? ObtenerPorId(int id)
    {
        const string sql = """
            SELECT IdUsuario, Nombre, Apellido, Email, PasswordHash, Rol, Avatar
            FROM Usuarios
            WHERE IdUsuario = @id;
            """;

        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", id);
        conexion.Open();

        using var lector = comando.ExecuteReader();
        return lector.Read() ? Mapear(lector) : null;
    }

    public Usuario? ObtenerPorEmail(string email)
    {
        const string sql = """
            SELECT IdUsuario, Nombre, Apellido, Email, PasswordHash, Rol, Avatar
            FROM Usuarios
            WHERE LOWER(Email) = @email
            LIMIT 1;
            """;

        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant());
        conexion.Open();

        using var lector = comando.ExecuteReader();
        return lector.Read() ? Mapear(lector) : null;
    }

    public bool ExisteEmail(string email, int idUsuarioExcluir)
    {
        const string sql = """
            SELECT EXISTS(
                SELECT 1
                FROM Usuarios
                WHERE LOWER(Email) = @email
                  AND IdUsuario <> @idUsuarioExcluir
            );
            """;

        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant());
        comando.Parameters.AddWithValue("@idUsuarioExcluir", idUsuarioExcluir);
        conexion.Open();

        return Convert.ToBoolean(comando.ExecuteScalar());
    }

    public bool ActualizarPerfil(Usuario usuario, string? nuevoPasswordHash)
    {
        const string sql = """
            UPDATE Usuarios
            SET Nombre = @nombre,
                Apellido = @apellido,
                Email = @email,
                Avatar = @avatar,
                PasswordHash = COALESCE(@nuevoPasswordHash, PasswordHash)
            WHERE IdUsuario = @idUsuario;
            """;

        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@nombre", usuario.Nombre);
        comando.Parameters.AddWithValue("@apellido", usuario.Apellido);
        comando.Parameters.AddWithValue("@email", usuario.Email);
        comando.Parameters.AddWithValue("@avatar", (object?)usuario.Avatar ?? DBNull.Value);
        comando.Parameters.AddWithValue(
            "@nuevoPasswordHash",
            (object?)nuevoPasswordHash ?? DBNull.Value);
        comando.Parameters.AddWithValue("@idUsuario", usuario.IdUsuario);
        conexion.Open();

        return comando.ExecuteNonQuery() > 0;
    }

    public bool ActualizarPasswordHash(int idUsuario, string passwordHash)
    {
        const string sql = """
            UPDATE Usuarios
            SET PasswordHash = @passwordHash
            WHERE IdUsuario = @idUsuario;
            """;

        using var conexion = CrearConexion();
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@passwordHash", passwordHash);
        comando.Parameters.AddWithValue("@idUsuario", idUsuario);
        conexion.Open();

        return comando.ExecuteNonQuery() > 0;
    }

    private static Usuario Mapear(MySqlDataReader lector) => new()
    {
        IdUsuario = lector.GetInt32("IdUsuario"),
        Nombre = lector.GetString("Nombre"),
        Apellido = lector.GetString("Apellido"),
        Email = lector.GetString("Email"),
        PasswordHash = lector.GetString("PasswordHash"),
        Rol = lector.GetString("Rol"),
        Avatar = lector.IsDBNull(lector.GetOrdinal("Avatar"))
            ? null
            : lector.GetString("Avatar")
    };
}
