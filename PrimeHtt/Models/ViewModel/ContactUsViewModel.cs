using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PrimeHtt.Models.ViewModel
{
    public class ContactUsViewModel
    {
        [AllowHtml]
        [Required(ErrorMessage = "Please enter Contact Us Title.")]
        public string ContactTitle { get; set; }
        public long ContactId { get; set; }

        [Required(ErrorMessage = "Please enter Contact Us Name.")]
        public string ContactName { get; set; }

        [AllowHtml]
        [Required(ErrorMessage = "Please enter Contact Address.")]
        public string ContactAddress { get; set; }
        public double ContactLatitude { get; set; }
        public double ContactLongitude { get; set; }
        public string CreatedBy { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public System.DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsContactExist { get; set; }
        public List<ContactDetailViewModel> ContactDetails { get; set; }
        public List<ContactEmailViewModel> ContactEmails { get; set; }
    }


    public class ContactDetailViewModel
    {
        public long ContactDetailId { get; set; }
        [Required(ErrorMessage = "Please enter Reservation Title.")]
        public string ReservationTitle { get; set; }
        [Required(ErrorMessage = "Please enter Reservation Contact Number.")]
        public string ContactNumber { get; set; }
        public string CreatedBy { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public System.DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class ContactEmailViewModel
    {
        public long ContactEmailId { get; set; }
        [Required(ErrorMessage = "Please enter Contact Email Address.")]
        public string ContactEmailAddress { get; set; }
        public string CreatedBy { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public System.DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}