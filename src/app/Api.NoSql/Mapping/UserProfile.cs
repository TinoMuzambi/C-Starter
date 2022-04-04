﻿using Api.NoSql.Models;
using Api.NoSql.Models.User;
using AutoMapper;
using Common.Dal;
using Common.Dal.Documents.User;

namespace Api.NoSql.Mapping
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserViewModel>();
            CreateMap<Page<User>, PageModel<UserViewModel>>();
        }
    }
}