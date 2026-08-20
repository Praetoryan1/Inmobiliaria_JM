using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace inmobiliaria.Repositories
{
	public abstract class RepositorioBase
	{
		protected readonly string connectionString;

		protected RepositorioBase(IConfiguration configuration)
		{
			connectionString = configuration.GetConnectionString("DefaultConnection")
				?? throw new InvalidOperationException(
					"No se configuró la cadena de conexión 'DefaultConnection'.");
		}

		protected MySqlConnection CrearConexion() => new(connectionString);
	}
}
