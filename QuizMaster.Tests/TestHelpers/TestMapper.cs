using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using QuizMaster.Application.Services.Implementations;

namespace QuizMaster.Tests.TestHelpers
{
	/// <summary>
	/// The real AutoMapper configuration, registered the same way as in Program.cs.
	/// </summary>
	internal static class TestMapper
	{
		private static readonly Lazy<IMapper> Mapper = new(() =>
		{
			var services = new ServiceCollection();
			services.AddLogging();
			services.AddAutoMapper(cfg => cfg.AddMaps(typeof(UserService).Assembly));
			return services.BuildServiceProvider().GetRequiredService<IMapper>();
		});

		public static IMapper Create()
		{
			return Mapper.Value;
		}
	}
}
