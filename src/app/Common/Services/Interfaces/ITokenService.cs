﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Common.DAL.Documents.Token;
using Common.DAL.Repositories;
using Common.Services.Interfaces.Models;

namespace Common.Services.Interfaces
{
    public interface ITokenService : IDocumentService<Token, TokenFilter>
    {
        Task<List<Token>> CreateAuthTokensAsync(string userId);
        Task<Token> FindAsync(string tokenValue);
        Task<UserTokenModel> GetUserTokenAsync(string accessToken);
        Task DeleteUserTokensAsync(string userId);
    }
}