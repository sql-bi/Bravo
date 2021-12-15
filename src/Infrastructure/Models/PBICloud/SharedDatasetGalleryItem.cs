using System;

#nullable disable

namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    /// <summary>
    /// v201901 - metadata/v201901/gallery/sharedDatasets
    /// </summary>
    public class SharedDatasetGalleryItem
    {
        public long Id { get; set; }

        public string Description { get; set; }

        public int Status { get; set; }
#nullable enable
        public string? IconUrl { get; set; }
#nullable disable
        public int? Stage { get; set; }

        // Not sure about the data type, it's always null in the sample analyzed
        //public string Disabled { get; set; }

        public DateTime? CertificationTime { get; set; }

        public long? CertifyingUserId { get; set; }
#nullable enable
        public SharedDatasetUser? CertifyingUser { get; set; }
#nullable disable
    }
}
