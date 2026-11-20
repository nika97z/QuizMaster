using Microsoft.Extensions.DependencyInjection;

namespace QuizMaster.UI.Helpers
{
	/// <summary>
	/// Runs each user action in its own DI scope, so every action gets a fresh DbContext and never sees stale data.
	/// </summary>
	public class ServiceRunner
	{
		private readonly IServiceProvider _provider;

		public ServiceRunner(IServiceProvider provider)
		{
			_provider = provider;
		}

		public TResult Get<TService, TResult>(Func<TService, TResult> action) where TService : notnull
		{
			using var scope = _provider.CreateScope();
			return action(scope.ServiceProvider.GetRequiredService<TService>());
		}

		public void Do<TService>(Action<TService> action) where TService : notnull
		{
			using var scope = _provider.CreateScope();
			action(scope.ServiceProvider.GetRequiredService<TService>());
		}
	}
}
