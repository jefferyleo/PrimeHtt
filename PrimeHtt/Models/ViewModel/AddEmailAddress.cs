using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PrimeHtt.Models.ViewModel
{
    public class AddEmailAddress
    {
        public long ContactEmailId { get; set; }
        [Required(ErrorMessage = "Please enter Email Address.")]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        public string ContactEmailAddress { get; set; }
    }
}