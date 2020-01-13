﻿using System.Collections.Generic;
using Api.Core.DAL.Documents.Token;
using Api.Core.Interfaces.DAL;
using Api.Core.Utils;
using MongoDB.Driver;

namespace Api.Core.DAL.Repositories
{
    public class TokenRepository : BaseRepository<Token, TokenFilter>, ITokenRepository
    {
        public TokenRepository(IDbContext context, IIdGenerator idGenerator)
            : base(context, idGenerator, dbContext => dbContext.Tokens)
        { }

        protected override IEnumerable<FilterDefinition<Token>> GetFilterQueries(TokenFilter filter)
        {
            var builder = Builders<Token>.Filter;

            if (filter.Value.HasValue())
            {
                yield return builder.Eq(u => u.Value, filter.Value);
            }

            if (filter.UserId.HasValue())
            {
                yield return builder.Eq(u => u.UserId, filter.UserId);
            }
        }
    }

    public class TokenFilter : BaseFilter
    {
        public string Value { get; set; }
        public string UserId { get; set; }
    }
}