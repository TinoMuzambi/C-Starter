﻿using System;
using System.Threading.Tasks;
using Common.DalSql.Entities;
using Common.DalSql.Filters;
using Common.Utils;
using Common.ServicesSql.Domain.Interfaces;
using Common.ServicesSql.Domain.Models;
using Common.ServicesSql.Infrastructure.Interfaces;
using Common.ServicesSql.Infrastructure.Email.Models;
using Common.DalSql.Interfaces;
using Common.Enums;

namespace Common.ServicesSql.Domain;

public class UserService : BaseEntityService<User, UserFilter>, IUserService
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;

    public UserService(
        IEmailService emailService,
        IUserRepository userRepository) : base(userRepository)
    {
        _emailService = emailService;
        _userRepository = userRepository;
    }

    public async Task<User> CreateUserAccountAsync(CreateUserModel model)
    {
        var signUpToken = SecurityUtils.GenerateSecureToken();

        var user = new User
        {
            Role = UserRole.User,
            FirstName = model.FirstName,
            LastName = model.LastName,
            PasswordHash = model.Password.GetHash(),
            Email = model.Email,
            IsEmailVerified = false,
            SignupToken = signUpToken
        };

        await _userRepository.InsertAsync(user);

        _emailService.SendSignUpWelcome(new SignUpWelcomeModel
        {
            Email = model.Email,
            SignUpToken = signUpToken
        });

        return user;
    }

    public async Task VerifyEmailAsync(long id)
    {
        await _userRepository.UpdateOneAsync(id, x => {
            x.IsEmailVerified = true;
            x.LastRequest = DateTime.UtcNow;
        });
    }

    public async Task SignInAsync(long id)
    {
        await _userRepository.UpdateOneAsync(id, x => {
            x.LastRequest = DateTime.UtcNow;
        });
    }

    public async Task<string> SetResetPasswordTokenAsync(long id)
    {
        var user = await _userRepository.FindOneAsync(new UserFilter
        {
            Id = id
        },
        x => new
        {
            x.ResetPasswordToken
        });

        if (user.ResetPasswordToken.HasNoValue())
        {
            await _userRepository.UpdateOneAsync(id, x =>
            {
                x.ResetPasswordToken = SecurityUtils.GenerateSecureToken();
            });
        }

        return user.ResetPasswordToken;
    }

    public async Task UpdatePasswordAsync(long id, string newPassword)
    {
        await _userRepository.UpdateOneAsync(id, x =>
        {
            x.PasswordHash = newPassword.GetHash();
            x.ResetPasswordToken = null;
        });
    }
}