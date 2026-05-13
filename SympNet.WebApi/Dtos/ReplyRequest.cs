using System.ComponentModel.DataAnnotations;

namespace SympNet.WebApi.Dtos;

public class ReplyRequest
{
    [Required]
    public string Message { get; set; } = string.Empty;
}
