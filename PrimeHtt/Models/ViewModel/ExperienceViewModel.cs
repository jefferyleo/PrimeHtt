using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PrimeHtt.Models.ViewModel
{
    public class ExperienceViewModel
    {
        public long LocationId { get; set; }
        public string LocationName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string CountryName { get; set; }
        public List<ExperienceDetailViewModel> Galleries { get; set; }
    }

    public class AddLocationViewModel
    {
        public long LocationId { get; set; }
        [Required(ErrorMessage = "Please enter Location Name.")]
        public string LocationName { get; set; }

        private double _latitude;
        public double Latitude
        {
            get
            {
                return this._latitude;
            }
            set
            {
                if (value < -90 || value > 90)
                {
                    throw new ArgumentOutOfRangeException("Latitude must be between -90 and 90 degrees inclusive.");
                }
                this._latitude = value;
            }
        }

        private double _longitude;

        public double Longitude {
            get
            {
                return this._longitude;
            }
            set
            {
                if (value < -180 || value > 180)
                {
                    throw new ArgumentOutOfRangeException("Longitude must be between -180 and 180 degrees inclusive.");
                }
                this._longitude = value;
            }
        }
    }
}