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

    public async Task<User> FindByEmailAsync(string email)
    {
        return await FindOneAsync(new UserFilter
        {
            Email = email
        });
    }

    public async Task<User> FindBySignupTokenAsync(string token)
    {
        return await FindOneAsync(new UserFilter
        {
            SignupToken = token
        });
    }

    public async Task<User> FindByResetPasswordTokenAsync(string token)
    {
        return await FindOneAsync(new UserFilter
        {
            ResetPasswordToken = token
        });
    }

    public async Task<bool> IsEmailInUseAsync(long userIdToExclude, string email)
    {
        var user = await FindOneAsync(new UserFilter
        {
            IdToExclude = userIdToExclude,
            Email = email,
            AsNoTracking = true
        });

        return user != null;
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
        var user = await _userRepository.FindById(id);
        user.IsEmailVerified = true;
        user.LastRequest = DateTime.UtcNow;

        await _userRepository.UpdateOneAsync(user);
    }

    public async Task SignInAsync(long id)
    {
        var user = await _userRepository.FindById(id);
        user.LastRequest = DateTime.UtcNow;

        await _userRepository.UpdateOneAsync(user);
    }

    public async Task UpdateResetPasswordTokenAsync(long id, string token)
    {
        var user = await _userRepository.FindById(id);
        user.ResetPasswordToken = token;

        await _userRepository.UpdateOneAsync(user);
    }

    public async Task<string> SetResetPasswordTokenAsync(long id)
    {
        var user = await _userRepository.FindById(id);
        if (user.ResetPasswordToken.HasNoValue())
        {
            user.ResetPasswordToken = SecurityUtils.GenerateSecureToken();
            await _userRepository.UpdateOneAsync(user);
        }

        return user.ResetPasswordToken;
    }

    public async Task UpdatePasswordAsync(long id, string newPassword)
    {
        var user = await _userRepository.FindById(id);
        user.PasswordHash = newPassword.GetHash();
        user.ResetPasswordToken = string.Empty;

        await _userRepository.UpdateOneAsync(user);
    }

    public async Task UpdateInfoAsync(long id, string email, string firstName, string lastName)
    {
        var user = await _userRepository.FindById(id);
        user.Email = email;
        user.FirstName = firstName;
        user.LastName = lastName;

        await _userRepository.UpdateOneAsync(user);
    }
}