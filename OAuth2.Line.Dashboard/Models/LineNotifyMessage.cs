using System.ComponentModel.DataAnnotations;

namespace OAuth2.Line.Dashboard.Models;

public class LineNotifyMessage
{
    [Required]
    public string Message { get; set; }
}