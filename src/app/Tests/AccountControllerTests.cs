using Api.Controllers;
using Api.Core.DAL.Documents.User;
using Api.Core.DAL.Repositories;
using Api.Core.Interfaces.Services.Document;
using Api.Core.Interfaces.Services.Infrastructure;
using Api.Core.Services.Document.Models;
using Api.Core.Services.Infrastructure.Models;
using Api.Core.Settings;
using Api.Core.Utils;
using Api.Models.Account;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using ForgotPasswordModel = Api.Models.Account.ForgotPasswordModel;

namespace Tests
{
    public class AccountControllerTests
    {
        private readonly Mock<IEmailService> _emailService;
        private readonly Mock<IUserService> _userService;
        private readonly Mock<ITokenService> _tokenService;
        private readonly Mock<IAuthService> _authService;
        private readonly Mock<IWebHostEnvironment> _environment;
        private readonly Mock<IOptions<AppSettings>> _appSettingsOptions;
        private readonly Mock<IGoogleService> _googleService;
        private readonly AppSettings _appSettings;

        public AccountControllerTests()
        {
            _emailService = new Mock<IEmailService>();
            _userService = new Mock<IUserService>();
            _tokenService = new Mock<ITokenService>();
            _authService = new Mock<IAuthService>();
            _environment = new Mock<IWebHostEnvironment>();
            _appSettingsOptions = new Mock<IOptions<AppSettings>>();
            _googleService = new Mock<IGoogleService>();

            _appSettings = new AppSettings
            {
                WebUrl = "http://test.com",
                LandingUrl = "http://test-landing.com"
            };
        }

        [Fact]
        public async void SignUpShouldReturnBadRequestWhenUserAlreadyExist()
        {
            // Arrange
            var model = new SignUpModel
            {
                Email = "sample@sample.com"
            };

            _userService.Setup(service => service.FindByEmailAsync(model.Email))
                .ReturnsAsync(new User());

            
            var controller = CreateInstance();

            // Act
            var result = await controller.SignUpAsync(model);

            // Assert
            Assert.IsAssignableFrom<BadRequestResult>(result);
        }

        [Fact]
        public async void SignUpShouldReturnOkWhenUserDoesNotExist()
        {
            // Arrange
            var model = new SignUpModel
            {
                Email = "sample@sample.com"
            };

            _userService.Setup(service => service.FindByEmailAsync(model.Email))
                .ReturnsAsync((User)null);

            var controller = CreateInstance();

            // Act
            var result = await controller.SignUpAsync(model);

            // Assert
            Assert.IsAssignableFrom<OkResult>(result);
        }

        [Fact]
        public async void VerifyEmailShouldReturnBadRequestWhenTokenIsNull()
        {
            // Arrange
            var controller = CreateInstance();

            // Act
            var result = await controller.VerifyEmailAsync(null);

            // Assert
            Assert.IsAssignableFrom<BadRequestResult>(result);
        }

        [Fact]
        public async void VerifyEmailShouldReturnBadRequestWhenTokenIsInvalid()
        {
            // Arrange
            var token = "sample";

            _userService.Setup(service => service.FindOneAsync(It.Is<UserFilter>(filter => filter.SignUpToken == token)))
                .ReturnsAsync((User)null);
            
            var controller = CreateInstance();

            // Act
            var result = await controller.VerifyEmailAsync(token);

            // Assert
            Assert.IsAssignableFrom<BadRequestResult>(result);
        }

        [Fact]
        public async void VerifyEmailShouldMarkEmailAsVerifiedAndReturnRedirectToMainPage()
        {
            // Arrange
            var token = "sample";
            var userId = "user id sample";

            _userService.Setup(service => service.FindOneAsync(It.Is<UserFilter>(filter => filter.SignUpToken == token)))
                .ReturnsAsync(new User { Id = userId });
            
            var controller = CreateInstance();

            // Act
            var result = await controller.VerifyEmailAsync(token);

            // Assert
            _userService.Verify(service => service.MarkEmailAsVerifiedAsync(userId), Times.Once);
            _userService.Verify(service => service.UpdateLastRequestAsync(userId), Times.Once);
            _authService.Verify(service => service.SetTokensAsync(userId), Times.Once);
            Assert.True((result as RedirectResult)?.Url == _appSettings.WebUrl);
        }

        [Fact]
        public async void SignInShouldReturnBadRequestWhenUserDoesNotExist()
        {
            // Arrange
            var model = new SignInModel
            {
                Email = "test@test.com"
            };

            _userService.Setup(service => service.FindByEmailAsync(model.Email))
                .ReturnsAsync((User)null);

            var controller = CreateInstance();

            // Act
            var result = await controller.SignInAsync(model);

            // Assert
            Assert.IsAssignableFrom<BadRequestResult>(result);
        }

        [Fact]
        public async void SignInShouldReturnBadRequestWhenPasswordIsIncorrect()
        {
            // Arrange
            var model = new SignInModel
            {
                Email = "test@test.com",
                Password = "sample2"
            };

            _userService.Setup(service => service.FindByEmailAsync(model.Email))
                .ReturnsAsync(new User { PasswordHash = "sample".GetHash() });

            var controller = CreateInstance();

            // Act
            var result = await controller.SignInAsync(model);

            // Assert
            Assert.IsAssignableFrom<BadRequestResult>(result);
        }

        [Fact]
        public async void SignInShouldReturnBadRequestWhenEmailDoesNotVerified()
        {
            // Arrange
            var password = "sample";
            var user = new User
            {
                PasswordHash = password.GetHash(),
                IsEmailVerified = false
            };
            var model = new SignInModel
            {
                Email = "test@test.com",
                Password = password
            };

            _userService.Setup(service => service.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);

            var controller = CreateInstance();

            // Act
            var result = await controller.SignInAsync(model);

            // Assert
            Assert.IsAssignableFrom<BadRequestResult>(result);
        }

        [Fact]
        public async void SignInShouldReturnJsonResult()
        {
            // Arrange
            var userId = "user id";
            var password = "sample";
            var user = new User
            {
                Id = userId,
                PasswordHash = password.GetHash(),
                IsEmailVerified = true
            };
            var model = new SignInModel
            {
                Email = "test@test.com",
                Password = password
            };

            _userService.Setup(service => service.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);

            var controller = CreateInstance();

            // Act
            var result = await controller.SignInAsync(model);

            // Assert
            _userService.Verify(service => service.UpdateLastRequestAsync(userId), Times.Once);
            _authService.Verify(service => service.SetTokensAsync(userId), Times.Once);
            Assert.IsAssignableFrom<JsonResult>(result);
        }

        [Fact]
        public async void ForgotPasswordShouldReturnBadRequestWhenUserDoesNotExist()
        {
            // Arrange
            var model = new ForgotPasswordModel
            {
                Email = "test@test.com"
            };

            _userService.Setup(service => service.FindByEmailAsync(model.Email))
                .ReturnsAsync((User)null);

            var controller = CreateInstance();

            // Act
            var result = await controller.ForgotPasswordAsync(model);

            // Assert
            Assert.IsAssignableFrom<BadRequestResult>(result);
        }

        [Fact]
        public async void ForgotPasswordShouldGenerateAndSendResetPasswordToken()
        {
            // Arrange
            var user = new User
            {
                Id = "user id",
                Email = "test@test.com"
            };
            var model = new ForgotPasswordModel
            {
                Email = user.Email
            };

            _userService.Setup(service => service.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);

            var controller = CreateInstance();

            // Act
            var result = await controller.ForgotPasswordAsync(model);

            // Assert
            _userService.Verify(service => service.UpdateResetPasswordTokenAsync(user.Id, It.IsAny<string>()), Times.Once);
            _emailService.Verify(service => service.SendForgotPassword(
                It.Is<Api.Core.Services.Infrastructure.Models.ForgotPasswordModel>(m => m.Email == user.Email))
            );

            Assert.IsAssignableFrom<OkResult>(result);
        }

        [Fact]
        public async void ResetPasswordShouldReturnBadRequestWhenTokenIsInvalid()
        {
            // Arrange
            var model = new ResetPasswordModel
            {
                Token = "test token"
            };

            _userService.Setup(service => service.FindOneAsync(It.Is<UserFilter>(filter => filter.ResetPasswordToken == model.Token)))
                .ReturnsAsync((User)null);

            var controller = CreateInstance();

            // Act
            var result = await controller.ResetPasswordAsync(model);

            // Assert
            Assert.IsAssignableFrom<BadRequestResult>(result);
        }

        [Fact]
        public async void ResetPasswordShouldUpdatePassword()
        {
            // Arrange
            var model = new ResetPasswordModel
            {
                Token = "test token",
                Password = "new password"
            };
            var user = new User
            {
                Id = "user id"
            };

            _userService.Setup(service => service.FindOneAsync(It.Is<UserFilter>(filter => filter.ResetPasswordToken == model.Token)))
                .ReturnsAsync(user);

            var controller = CreateInstance();

            // Act
            var result = await controller.ResetPasswordAsync(model);

            // Assert
            _userService.Verify(service => service.UpdatePasswordAsync(user.Id, model.Password));
            Assert.IsAssignableFrom<OkResult>(result);
        }

        [Fact]
        public async void ResendVerificationShouldSendSignUpEmail()
        {
            // Arrange
            var model = new ResendVerificationModel
            {
                Email = "test@test.com"
            };
            var user = new User
            {
                SignupToken = "test token"
            };

            _userService.Setup(service => service.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);

            var controller = CreateInstance();

            // Act
            var result = await controller.ResendVerificationAsync(model);

            // Assert
            _emailService.Verify(service => service.SendSignUpWelcome(
                It.Is<SignUpWelcomeModel>(m => m.Email == model.Email && m.SignUpToken == user.SignupToken))
            );
            Assert.IsAssignableFrom<OkResult>(result);
        }

        [Fact]
        public async void RefreshTokenShouldReturnUnauthorizedWhenUserIsNotFound()
        {
            // Arrange

            _tokenService.Setup(service => service.FindUserIdByTokenAsync(It.IsAny<string>()))
                .ReturnsAsync((string)null);

            var controller = CreateInstance();

            // Act
            var result = await controller.RefreshTokenAsync();

            // Assert
            Assert.IsAssignableFrom<UnauthorizedResult>(result);
        }

        [Fact]
        public async void RefreshTokenShouldSetToken()
        {
            // Arrange
            var userId = "user id";

            _tokenService.Setup(service => service.FindUserIdByTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(userId);

            var controller = CreateInstance();

            // Act
            var result = await controller.RefreshTokenAsync();

            // Assert
            _authService.Verify(service => service.SetTokensAsync(userId), Times.Once);
            Assert.IsAssignableFrom<OkResult>(result);
        }

        [Fact]
        public async void LogoutShouldReturnUnauthorizedWhenUserIsNotLoggedIn()
        {
            // Arrange
            var controller = CreateInstance();

            // Act
            var result = await controller.LogoutAsync();

            // Assert
            Assert.IsAssignableFrom<UnauthorizedResult>(result);
        }

        [Fact]
        public async void SignInGoogleWithCodeShouldReturnNotFoundWhenPayloadIsNull()
        {
            // Arrange
            var model = new SignInGoogleModel {Code = "test code"};
            _googleService.Setup(service => service.ExchangeCodeForTokenAsync(model.Code))
                .ReturnsAsync((GooglePayloadModel)null);

            var controller = CreateInstance();

            // Act
            var result = await controller.SignInGoogleWithCodeAsync(model);

            // Assert
            Assert.IsAssignableFrom<NotFoundResult>(result);
        }

        [Fact]
        public async void SignInGoogleWithCodeShouldCreateUserWhenItDoesNotExist()
        {
            // Arrange
            var model = new SignInGoogleModel { Code = "test code" };
            var payload = new GooglePayloadModel
            {
                Email = "test@test.com",
                GivenName = "Test",
                FamilyName = "Tester"
            };

            _userService.Setup(service => service.FindByEmailAsync(payload.Email))
                .ReturnsAsync((User)null);

            _userService.Setup(service => service.CreateUserAccountAsync(It.IsAny<CreateUserGoogleModel>()))
                .ReturnsAsync(new User());

            _googleService.Setup(service => service.ExchangeCodeForTokenAsync(model.Code))
                .ReturnsAsync(payload);

            var controller = CreateInstance();

            // Act
            await controller.SignInGoogleWithCodeAsync(model);

            // Assert
            _userService.Verify(service => service.CreateUserAccountAsync(
                It.Is<CreateUserGoogleModel>(m => m.Email == payload.Email &&  m.FirstName == payload.GivenName && m.LastName == payload.FamilyName)),
                Times.Once
            );
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async void SignInGoogleWithCodeShouldShouldEnableGoogleAuthWhenItDoesNotEnabled(bool isEnabled)
        {
            // Arrange
            var model = new SignInGoogleModel { Code = "test code" };
            var payload = new GooglePayloadModel
            {
                Email = "test@test.com",
                GivenName = "Test",
                FamilyName = "Tester"
            };
            var user = new User
            {
                Id = "test id",
                Email = payload.Email,
                OAuth = new User.OAuthSettings
                {
                    Google = isEnabled
                }
            };

            _userService.Setup(service => service.FindByEmailAsync(payload.Email))
                .ReturnsAsync(user);

            _userService.Setup(service => service.CreateUserAccountAsync(It.IsAny<CreateUserGoogleModel>()))
                .ReturnsAsync(new User());

            _googleService.Setup(service => service.ExchangeCodeForTokenAsync(model.Code))
                .ReturnsAsync(payload);

            var controller = CreateInstance();

            // Act
            await controller.SignInGoogleWithCodeAsync(model);

            // Assert
            _userService.Verify(service => service.CreateUserAccountAsync(It.IsAny<CreateUserGoogleModel>()), Times.Never);
            _userService.Verify(service => service.EnableGoogleAuthAsync(user.Id), isEnabled ? Times.Never() : Times.Once());
        }

        [Fact]
        public async void SignInGoogleWithCodeShouldShouldSetTokenAndReturnRedirect()
        {
            // Arrange
            var model = new SignInGoogleModel { Code = "test code" };
            var payload = new GooglePayloadModel
            {
                Email = "test@test.com",
                GivenName = "Test",
                FamilyName = "Tester"
            };
            var user = new User
            {
                Id = "test id",
                Email = payload.Email,
                OAuth = new User.OAuthSettings
                {
                    Google = false
                }
            };

            _userService.Setup(service => service.FindByEmailAsync(payload.Email))
                .ReturnsAsync(user);

            _googleService.Setup(service => service.ExchangeCodeForTokenAsync(model.Code))
                .ReturnsAsync(payload);


            var controller = CreateInstance();

            // Act
            var result = await controller.SignInGoogleWithCodeAsync(model);

            // Assert
            _userService.Verify(service => service.UpdateLastRequestAsync(user.Id), Times.Once);
            _authService.Verify(service => service.SetTokensAsync(user.Id), Times.Once);
            Assert.True(result is RedirectResult redirectResult && redirectResult.Url == _appSettings.WebUrl);
        }

        private AccountController CreateInstance()
        {
            _appSettingsOptions.Setup(options => options.Value)
                .Returns(_appSettings);

            return new AccountController(_emailService.Object,_userService.Object, _tokenService.Object,
                _authService.Object, _environment.Object, _appSettingsOptions.Object, _googleService.Object);
        }
    }
}
