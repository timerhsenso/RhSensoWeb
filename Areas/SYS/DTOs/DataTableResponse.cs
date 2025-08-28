using System.Collections.Generic;

namespace RhSensoWeb.Areas.SYS.Taux1.DTOs
{
    public class DataTableResponse<T>
    {
        public int Draw { get; set; }
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public string? Error { get; set; }
    }
}
