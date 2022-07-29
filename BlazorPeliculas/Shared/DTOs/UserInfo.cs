﻿using System.ComponentModel.DataAnnotations;

namespace BlazorPeliculas.Shared.DTOs
{
    public class UserInfo
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
