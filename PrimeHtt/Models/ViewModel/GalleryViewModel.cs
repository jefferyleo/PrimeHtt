using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PrimeHtt.Models.ViewModel
{
    public class GalleryViewModel
    {
        public long LocationId { get; set; }
        public string LocationName { get; set; }
        public List<ExperienceDetailViewModel> Galleries { get; set; }
    }

    public class ExperienceDetailViewModel
    {
        public long LocationDetailId { get; set; }
        public bool LocationContentType { get; set; }

        public string ContentType => LocationContentType ? "Video" : "Image";
        public string LocationContent { get; set; }
        public int LocationIndex { get; set; }

        public string CreatedBy { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public System.DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public long LocationId { get; set; }
        public string LocationName { get; set; }
    
    }

    public class AddGalleryViewModel
    {
        public long LocationDetailId { get; set; }
        public bool LocationContentType { get; set; }

        public string ContentType => LocationContentType ? "Video" : "Image";
        public string LocationContent { get; set; }
        public int LocationIndex { get; set; }
        public long LocationId { get; set; }
        public string Location { get; set; }
        public HttpPostedFileBase ContentImage { get; set; }
        public List<SelectListItem> LocationName { get; set; }        
    }
}