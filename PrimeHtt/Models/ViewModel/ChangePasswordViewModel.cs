using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PrimeHtt.Models.ViewModel
{
    public class ChangePasswordViewModel
    {
        public long UserId { get; set; }

        [Required(ErrorMessage = "Username Required", AllowEmptyStrings = false)]
        public string Username { get; set; }

        [Required(ErrorMessage = "New Password Required", AllowEmptyStrings = false)]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm New Password Required", AllowEmptyStrings = false)]
        public string ConfirmNewPassword { get; set; }
    }
}