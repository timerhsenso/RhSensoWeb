using System.ComponentModel.DataAnnotations;

namespace RhSensoWeb.Areas.SYS.DTOs
{
    public class Taux1Dto
    {
        [Required]
        [StringLength(2)]
        public string Cdtptabela { get; set; } = "";

        [Required]
        [StringLength(60)]
        public string Dctabela { get; set; } = "";
    }
}
