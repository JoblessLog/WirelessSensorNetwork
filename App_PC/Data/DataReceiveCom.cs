using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace wsn_keboo.Data;

public partial class DataReceiveCom
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdEnumerate { get; set; }

    public DateTime? Time { get; set; }

    public string? Id { get; set; }

    public string? Temperature { get; set; }

    public string? Rssi { get; set; }

    public string? Snr { get; set; }

   
}
