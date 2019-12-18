﻿using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public abstract class BaseController : ControllerBase
    {
        protected string CurrentUserId => User.Identity.Name;

        protected virtual object GetErrorsFromModelState(ModelStateDictionary modelState)
        {
            var errors = modelState.ToDictionary(x => x.Key, y => y.Value.Errors.FirstOrDefault()?.ErrorMessage);

            return new { errors };
        }

        protected virtual object GetErrorsModel(params object[] errors)
        {
            return new { errors };
        }
    }
}
