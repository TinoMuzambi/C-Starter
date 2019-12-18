﻿using System;
using System.Threading.Tasks;
using Api.Core.DbViews.User;
using Api.Core.Interfaces.DAL;
using Api.Core.Interfaces.Services.App;
using Api.Core.Interfaces.Services.Infrastructure;
using Api.Core.Services.App.Models;
using Api.Core.Services.Infrastructure.Models;
using Api.Core.Utils;

namespace Api.Core.Services.App
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;

        public UserService(IUserRepository userRepository,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _emailService = emailService;
        }

        public User FindByEmail(string email)
        {
            return _userRepository.FindOne(x => x.Email == email);
        }

        public User FindBySignupToken(string signupToken)
        {
            return _userRepository.FindOne(x => x.SignupToken == signupToken);
        }

        public User FindByResetPasswordToken(string resetPasswordToken)
        {
            return _userRepository.FindOne(x => x.ResetPasswordToken == resetPasswordToken);
        }

        public async Task MarkEmailAsVerifiedAsync(string id)
        {
            await _userRepository.UpdateAsync(id, x => new User { IsEmailVerified = true });
        }

        public async Task UpdateLastRequestAsync(string id)
        {
            await _userRepository.UpdateAsync(id, x => new User { LastRequest = DateTime.UtcNow });
        }

        public async Task UpdateResetPasswordTokenAsync(string id, string token)
        {
            await _userRepository.UpdateAsync(id, x => new User { ResetPasswordToken = token });
        }

        public async Task UpdatePasswordAsync(string id, string newPassword)
        {
            var hash = newPassword.GetHash();

            await _userRepository.UpdateAsync(id, x => new User
            {
                PasswordHash = hash,
                ResetPasswordToken = string.Empty
            }); 
        }

        public async Task UpdateInfoAsync(string id, string email, string firstName, string lastName)
        {
            await _userRepository.UpdateAsync(id, x => new User
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName
            });
        }

        public async Task<User> CreateUserAccountAsync(CreateUserModel model)
        {
            var hash = model.Password.GetHash();
            var signupToken = SecurityUtils.GenerateSecureToken();

            var newUser = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                PasswordHash = hash,
                Email = model.Email,
                IsEmailVerified = false,
                SignupToken = signupToken,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            };

            await _userRepository.InsertAsync(newUser);

            _emailService.SendSignupWelcome(new SignupWelcomeModel
            {
                Email = model.Email,
                SignupToken = signupToken
            });

            return newUser;
        }

        public async Task<User> CreateUserAccountAsync(CreateUserGoogleModel model)
        {
            var newUser = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                IsEmailVerified = true,
                OAuth = new User.OAuthSettings
                {
                    Google = true
                }
            };

            await _userRepository.InsertAsync(newUser);

            return newUser;
        }

        public async Task EnableGoogleAuthAsync(string id)
        {
            await _userRepository.UpdateAsync(
                id,
                u => new User {OAuth = new User.OAuthSettings {Google = true}});
        }
    }
}
