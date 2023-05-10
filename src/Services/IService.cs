namespace DNAOperations.Services
{
    public class IService
    {
        public readonly string BASE_PATH = "https://gene.lacuna.cc/api/";
        public HttpClient Client { get; set; }

        public IService()
        {
            Client = new HttpClient();
            Client.BaseAddress = new Uri(BASE_PATH);
        }
    }
}