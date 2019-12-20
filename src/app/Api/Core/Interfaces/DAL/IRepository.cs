﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Api.Core.DAL;
using Api.Core.DAL.Views;

namespace Api.Core.Interfaces.DAL
{
    public interface IRepository<TModel, in TFilter> 
        where TModel : BaseView
        where TFilter : BaseFilter
    {
        Task InsertAsync(TModel model);
        Task InsertManyAsync(IEnumerable<TModel> models);

        Task<TModel> FindOneAsync(TFilter filter);
        Task<TModel> FindByIdAsync(string id);

        Task<bool> UpdateAsync(string id, Expression<Func<TModel, TModel>> updateExpression);

        Task DeleteManyAsync(Expression<Func<TModel, bool>> deleteExpression);
    }
}
