using Moq;
using QuizMaster.Application.Exceptions;
using QuizMaster.Application.Services.Implementations;
using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.UserDTOs;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;
using QuizMaster.Tests.TestHelpers;

namespace QuizMaster.Tests.Services
{
	public class UserServiceTests
	{
		private readonly Mock<IUserRepository> _userRepository = new();
		private readonly Mock<IUnitOfWork> _unitOfWork = new();
		private readonly Mock<IAppLogger> _logger = new();
		private readonly UserService _userService;

		public UserServiceTests()
		{
			_userService = new UserService(_userRepository.Object, TestMapper.Create(), _unitOfWork.Object, _logger.Object);
		}

		private static RegisterUserDTO ValidRegistration(UserRole role = UserRole.Student)
		{
			return new RegisterUserDTO { Username = "nino", Email = "nino@example.com", Password = "pass123", Role = role };
		}

		[Fact]
		public void Register_ValidStudent_SavesABCryptHash()
		{
			User? saved = null;
			_userRepository.Setup(r => r.AddUser(It.IsAny<User>())).Callback<User>(u => saved = u);

			var result = _userService.Register(ValidRegistration());

			var student = Assert.IsType<Student>(saved);
			Assert.Equal("nino", student.Username);
			Assert.NotEqual("pass123", student.PasswordHash);
			Assert.True(BCrypt.Net.BCrypt.Verify("pass123", student.PasswordHash));
			_unitOfWork.Verify(u => u.SaveChanges(), Times.Once);
			Assert.Equal(UserRole.Student, result.Role);
		}

		[Fact]
		public void Register_DuplicateUsername_IsRejected()
		{
			_userRepository.Setup(r => r.GetUserByUsername("nino")).Returns(TestData.Student(5, "nino"));

			Assert.Throws<DuplicateUserException>(() => _userService.Register(ValidRegistration()));

			_userRepository.Verify(r => r.AddUser(It.IsAny<User>()), Times.Never);
			_unitOfWork.Verify(u => u.SaveChanges(), Times.Never);
		}

		[Fact]
		public void Register_DuplicateEmail_IsRejected()
		{
			_userRepository.Setup(r => r.GetUserByEmail("nino@example.com")).Returns(TestData.Student(5, "other"));

			Assert.Throws<DuplicateUserException>(() => _userService.Register(ValidRegistration()));
			_userRepository.Verify(r => r.AddUser(It.IsAny<User>()), Times.Never);
		}

		[Fact]
		public void Register_InvalidData_ReportsEveryProblem()
		{
			var dto = new RegisterUserDTO { Username = "ab", Email = string.Empty, Password = "123" };

			var ex = Assert.Throws<DomainValidationException>(() => _userService.Register(dto));

			Assert.Equal(3, ex.Errors.Count);
		}

		// Email format checking is turned off for now to make testing easier. Change this test when it comes back.
		[Fact]
		public void Register_AnyEmailText_IsAcceptedForNow()
		{
			var dto = ValidRegistration();
			dto.Email = "test";

			var result = _userService.Register(dto);

			Assert.Equal("test", result.Email);
		}

		[Fact]
		public void Register_Lecturer_CreatesALecturer()
		{
			var result = _userService.Register(ValidRegistration(UserRole.Lecturer));

			_userRepository.Verify(r => r.AddUser(It.IsAny<Lecturer>()), Times.Once);
			Assert.Equal(UserRole.Lecturer, result.Role);
		}

		[Fact]
		public void Login_WrongPassword_IsRejectedAndLogged()
		{
			var user = TestData.Student(2, "luka");
			user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("right-password");
			_userRepository.Setup(r => r.GetUserByUsername("luka")).Returns(user);

			Assert.Throws<InvalidCredentialsException>(() => _userService.Login(new LoginDTO { Username = "luka", Password = "wrong" }));

			_logger.Verify(l => l.Log(LogEventType.LoginFailed, It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
		}

		[Fact]
		public void Login_CorrectPassword_ReturnsTheUser()
		{
			var user = TestData.Student(2, "luka");
			user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("right-password");
			_userRepository.Setup(r => r.GetUserByUsername("luka")).Returns(user);

			var result = _userService.Login(new LoginDTO { Username = "luka", Password = "right-password" });

			Assert.Equal(2, result.Id);
			Assert.Equal(UserRole.Student, result.Role);
		}

		[Fact]
		public void GetStudents_ForALecturer_ReturnsTheStudents()
		{
			_userRepository.Setup(r => r.GetUserById(1)).Returns(TestData.Lecturer(1));
			_userRepository.Setup(r => r.GetAllStudents()).Returns(new List<Student> { TestData.Student(2, "luka"), TestData.Student(3, "nino") });

			var students = _userService.GetStudents(1);

			Assert.Equal(new[] { "luka", "nino" }, students.Select(s => s.Username));
			Assert.All(students, s => Assert.Equal(UserRole.Student, s.Role));
		}

		[Fact]
		public void GetStudents_ForAStudent_IsRejected()
		{
			_userRepository.Setup(r => r.GetUserById(2)).Returns(TestData.Student(2));

			Assert.Throws<UnauthorizedActionException>(() => _userService.GetStudents(2));
		}

		[Fact]
		public void DeleteStudent_ByALecturer_DeletesTheStudentAndLogsIt()
		{
			var student = TestData.Student(2, "luka");
			_userRepository.Setup(r => r.GetUserById(1)).Returns(TestData.Lecturer(1));
			_userRepository.Setup(r => r.GetStudentWithAttempts(2)).Returns(student);

			_userService.DeleteStudent(1, 2);

			_userRepository.Verify(r => r.DeleteStudent(student), Times.Once);
			_unitOfWork.Verify(u => u.SaveChanges(), Times.Once);
			_logger.Verify(l => l.Log(LogEventType.StudentDeleted, It.IsAny<string>(), 1, It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
		}

		[Fact]
		public void DeleteStudent_ByAStudent_IsRejected()
		{
			_userRepository.Setup(r => r.GetUserById(2)).Returns(TestData.Student(2));

			Assert.Throws<UnauthorizedActionException>(() => _userService.DeleteStudent(2, 3));

			_userRepository.Verify(r => r.DeleteStudent(It.IsAny<Student>()), Times.Never);
			_unitOfWork.Verify(u => u.SaveChanges(), Times.Never);
		}

		// GetStudentWithAttempts finds only students, so an unknown id and a lecturer's id both end here.
		[Fact]
		public void DeleteStudent_NotAStudent_IsNotFound()
		{
			_userRepository.Setup(r => r.GetUserById(1)).Returns(TestData.Lecturer(1));

			Assert.Throws<NotFoundException>(() => _userService.DeleteStudent(1, 5));

			_userRepository.Verify(r => r.DeleteStudent(It.IsAny<Student>()), Times.Never);
			_unitOfWork.Verify(u => u.SaveChanges(), Times.Never);
		}
	}
}
