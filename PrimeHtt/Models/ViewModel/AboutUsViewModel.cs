using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PrimeHtt.Models.ViewModel
{
    public class AboutUsViewModel
    {
        public long AboutUsId { get; set; }

        [Required(ErrorMessage = "Please enter About Us Title.")]
        [AllowHtml]
        public string AboutUsTitle { get; set; }

        [Required(ErrorMessage = "Please enter the About Us Top Description.")]
        [AllowHtml]
        public string AboutUsTopDescription { get; set; }

        [Required(ErrorMessage = "Please enter the About Us Bottom Description.")]
        [AllowHtml]
        public string AboutUsBottomDescription { get; set; }
        public string AboutUsImageUrl { get; set; }
        public HttpPostedFileBase AboutUsImage { get; set; }
    }
}