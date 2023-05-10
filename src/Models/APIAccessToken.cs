namespace DNAOperations.Models
{
    public class APIAccessToken
    {
        public string Id { get; set; }
        public DateTime Expiration { get; set; }
        public APIAccessToken()
        {
            Id = "";
            Expiration = DateTime.Now + TimeSpan.FromMinutes(2);
        }
    }
}