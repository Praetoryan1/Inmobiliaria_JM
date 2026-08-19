using Microsoft.Extensions.Configuration;


namespace inmobiliaria.Repositories
{
	public abstract class RepositorioBase
	{
		protected readonly IConfiguration configuration;
		protected readonly string connectionString;

		protected RepositorioBase(IConfiguration configuration)
		{
			this.configuration = configuration;
			connectionString = configuration["ConnectionStrings:DefaultConnection"];
			//connectionString = configuration["ConnectionStrings:MySql"];
		}
	}
}