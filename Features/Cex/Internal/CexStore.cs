namespace Vault.Features.Cex
{
    public class CexStore
    {
        public string Name { get; set; } = string.Empty; 
        public bool IsPriority { get; set; }             
        public string? Region { get; set; }              
        public int? Number { get; set; }                 
    }
}
