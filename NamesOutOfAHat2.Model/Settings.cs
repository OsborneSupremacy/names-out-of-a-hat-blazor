using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Model
{
    public record Settings
    {
        public bool SendEmails { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string SenderName { get; set; } = default!;

        [Required(AllowEmptyStrings = false)]
        public string SiteUrl { get; set; } = default!;

        [EmailAddress]
        [Required(AllowEmptyStrings = false)]
        public string SenderEmail { get; set; } = default!;
    }
}
