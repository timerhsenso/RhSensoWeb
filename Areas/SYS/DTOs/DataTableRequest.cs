namespace RhSensoWeb.Areas.SYS.Taux1.DTOs
{
    public class DataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public string? Search { get; set; }
        public string? OrderColumn { get; set; }
        public string? OrderDir { get; set; }
    }
}
