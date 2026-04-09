using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Core.Models.Dtos;

[Index(nameof(Type))]
[Index(nameof(PublishedAt))]
[PrimaryKey(nameof(Id))]
public class ReleaseDto
{
    [Key] public int Id { get; set; }
    
    [MaxLength(64)]
    public required string Version { get; set; }
    
    [MaxLength(5)]
    public required string Type { get; set; }
    
    [MaxLength(128)]
    public required string FileName { get; set; }
    
    [MaxLength(40)]
    public required string SHA1 { get; set; }
    
    [MaxLength(64)]
    public required string SHA256 { get; set; }
    
    public long Size { get; set; }
    
    public bool Prerelease { get; set; }

    public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;
}