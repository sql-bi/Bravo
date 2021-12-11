using System;

#nullable disable

namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    public class SharedDatasetGalleryItem
    {
        public long Id { get; set; }

        public string Description { get; set; }

        public int Status { get; set; }

        public string? IconUrl { get; set; }

        public int? Stage { get; set; }

        // Not sure about the data type, it's always null in the sample analyzed
        //public string Disabled { get; set; }

        public DateTime? CertificationTime { get; set; }

        public long? CertifyingUserId { get; set; }

        public SharedDatasetUser? CertifyingUser { get; set; }
    }
}
