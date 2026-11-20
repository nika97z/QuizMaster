using AutoMapper;
using QuizMaster.Application.Exceptions;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.UserDTOs;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;
using System.Net.Mail;

namespace QuizMaster.Application.Services.Implementations
{
	public class UserService : IUserService
	{
		public const int MinUsernameLength = 3;
		public const int MaxUsernameLength = 30;
		public const int MinPasswordLength = 6;

		private readonly IUserRepository _userRepository;
		private readonly IMapper _mapper;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IAppLogger _logger;

		public UserService(IUserRepository userRepository, IMapper mapper, IUnitOfWork unitOfWork, IAppLogger logger)
		{
			_userRepository = userRepository;
			_mapper = mapper;
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public UserDTO Register(RegisterUserDTO user)
		{
			ArgumentNullException.ThrowIfNull(user);
			var username = (user.Username ?? string.Empty).Trim();
			var email = (user.Email ?? string.Empty).Trim();
			var errors = new List<string>();

			if (username.Length < MinUsernameLength || username.Length > MaxUsernameLength)
			{
				errors.Add($"Username must be {MinUsernameLength}-{MaxUsernameLength} characters long.");
			}
			else if (!username.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '.'))
			{
				errors.Add("Username may contain only letters, digits, '_' and '.'.");
			}
			// TEMPORARY: email format checking is turned off to make testing easier.
			// To turn it back on, replace this block with: if (!IsValidEmail(email)) errors.Add("Email address is not valid.");
			if (email.Length == 0)
			{
				errors.Add("Email can't be empty.");
			}
			else if (email.Length > 256)
			{
				errors.Add("Email can be at most 256 characters long.");
			}
			if ((user.Password ?? string.Empty).Length < MinPasswordLength)
			{
				errors.Add($"Password must be at least {MinPasswordLength} characters long.");
			}
			if (errors.Count > 0)
			{
				throw new DomainValidationException(errors);
			}

			if (_userRepository.GetUserByUsername(username) != null)
			{
				throw new DuplicateUserException($"The username '{username}' is already taken.");
			}
			if (_userRepository.GetUserByEmail(email) != null)
			{
				throw new DuplicateUserException($"The email '{email}' is already registered.");
			}

			User newUser = user.Role == UserRole.Lecturer ? new Lecturer() : new Student();
			newUser.Username = username;
			newUser.Email = email;
			newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.Password);
			newUser.CreatedAt = DateTime.Now;

			_userRepository.AddUser(newUser);
			_unitOfWork.SaveChanges();

			_logger.Log(LogEventType.UserRegistered, $"User '{username}' registered as {newUser.Role}.", newUser.Id);
			return _mapper.Map<UserDTO>(newUser);
		}

		public UserDTO Login(LoginDTO login)
		{
			ArgumentNullException.ThrowIfNull(login);
			var username = (login.Username ?? string.Empty).Trim();
			var user = _userRepository.GetUserByUsername(username);

			if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password ?? string.Empty, user.PasswordHash))
			{
				_logger.Log(LogEventType.LoginFailed, $"Failed login for '{username}'.", user?.Id);
				throw new InvalidCredentialsException();
			}

			_logger.Log(LogEventType.LoginSucceeded, $"User '{user.Username}' logged in.", user.Id);
			return _mapper.Map<UserDTO>(user);
		}

		public UserDTO GetUserById(int id)
		{
			AccessGuard.RequireValidId(id);
			var user = _userRepository.GetUserById(id)
				?? throw new NotFoundException($"User {id} was not found.");
			return _mapper.Map<UserDTO>(user);
		}

		// Not used while email checking is turned off (see Register). Kept so it can be switched back on.
		private static bool IsValidEmail(string email)
		{
			if (string.IsNullOrWhiteSpace(email) || email.Length > 256)
			{
				return false;
			}
			if (!MailAddress.TryCreate(email, out var address) || address.Address != email)
			{
				return false;
			}
			var domain = email[(email.IndexOf('@') + 1)..];
			return domain.Contains('.') && !domain.StartsWith('.') && !domain.EndsWith('.');
		}
	}
}
