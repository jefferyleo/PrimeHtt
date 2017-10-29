using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PrimeHtt.Models.ViewModel
{
    public class AddReservationContact
    {
        public long ContactDetailId { get; set; }
        [Required(ErrorMessage = "Please enter Reservation Title.")]
        public string ReservationTitle { get; set; }
        [Required(ErrorMessage = "Please enter Contact Number.")]
        public string ContactNumber { get; set; }
    }
}