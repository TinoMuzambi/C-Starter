﻿using System.ComponentModel.DataAnnotations;

namespace Api.NoSql.Models.Account
{
    public class ForgotPasswordModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; }
    }
}