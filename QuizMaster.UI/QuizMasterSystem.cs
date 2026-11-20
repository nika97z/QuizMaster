using Microsoft.Extensions.DependencyInjection;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.UserDTOs;
using QuizMaster.Domain.Models.Enums;
using QuizMaster.UI.Helpers;
using QuizMaster.UI.Menus;

namespace QuizMaster.UI
{
	/// <summary>
	/// The console shell ("QuizMasterSystem" in the assignment): main menu, registration, login,
	/// and role-based routing to the Lecturer or Student menu.
	/// </summary>
	public class QuizMasterSystem
	{
		private readonly ServiceRunner _services;
		private readonly SafeExecutor _safe;
		private readonly bool _smtpConfigured;

		public QuizMasterSystem(IServiceProvider provider, bool smtpConfigured)
		{
			_services = new ServiceRunner(provider);
			_safe = new SafeExecutor(provider.GetRequiredService<IAppLogger>());
			_smtpConfigured = smtpConfigured;
		}

		public void Run()
		{
			ConsoleHelper.WriteColored("Welcome to QuizMaster!", ConsoleColor.Cyan);
			if (!_smtpConfigured)
			{
				ConsoleHelper.Info("SMTP is not configured, so result emails are shown in the console.");
			}

			while (true)
			{
				ConsoleHelper.Header("Main menu");
				Console.WriteLine("1) Register");
				Console.WriteLine("2) Log in");
				Console.WriteLine("0) Exit");

				switch (ConsoleHelper.ReadInt("Choose", 0, 2))
				{
					case 1:
						_safe.Execute(Register);
						break;
					case 2:
						UserDTO? user = null;
						_safe.Execute(() => user = Login());
						if (user != null)
						{
							OpenMenu(user);
						}
						break;
					case 0:
						Console.WriteLine("Goodbye!");
						return;
				}
			}
		}

		private void Register()
		{
			ConsoleHelper.Header("Register");
			var dto = new RegisterUserDTO
			{
				Username = ConsoleHelper.ReadNonEmpty("Username", 30),
				Email = ConsoleHelper.ReadNonEmpty("Email", 256)
			};

			dto.Password = ConsoleHelper.ReadPassword("Password (at least 6 characters)");

			Console.WriteLine("Role: 1) Student  2) Lecturer");
			dto.Role = ConsoleHelper.ReadInt("Choose", 1, 2) == 2 ? UserRole.Lecturer : UserRole.Student;

			var user = _services.Get<IUserService, UserDTO>(s => s.Register(dto));
			ConsoleHelper.Success($"Account created for {user.Username} ({user.Role}). You can log in now.");
		}

		private UserDTO Login()
		{
			ConsoleHelper.Header("Log in");
			var dto = new LoginDTO
			{
				Username = ConsoleHelper.ReadNonEmpty("Username", 30),
				Password = ConsoleHelper.ReadPassword("Password")
			};

			var user = _services.Get<IUserService, UserDTO>(s => s.Login(dto));
			ConsoleHelper.Success($"Welcome, {user.Username}!");
			return user;
		}

		private void OpenMenu(UserDTO user)
		{
			if (user.Role == UserRole.Lecturer)
			{
				new LecturerMenu(_services, _safe, user).Run();
			}
			else
			{
				new StudentMenu(_services, _safe, user).Run();
			}
		}
	}
}
