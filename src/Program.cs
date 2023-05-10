using System.Text.Json;
using DNAOperations.Models;
using DNAOperations.Services;

namespace DNAOperations
{
    public class Program
    {
        static bool onlyOnce = true;
        static User user = new User();

        public async static Task Main(string[] args)
        {
            if (onlyOnce)
            {
                onlyOnce = false;
                Console.Clear();
                Console.WriteLine("## Welcome to DNA Operations ##\n");
                Console.WriteLine("This program will allow you request and solve jobs related to DNA!\n");
                Console.Write("Press any key to continue...");
                Console.ReadKey();
            }
            Console.Clear();

            Console.WriteLine("## DNA Operations ##\n");
            Console.WriteLine("1. Create User");
            Console.WriteLine("2. Request Job");
            Console.WriteLine("3. Exit");

            Console.Write("\n> ");
            string input = Console.ReadLine();

            switch (input)
            {
                case "1": // Create User
                    Console.Clear();
                    Console.WriteLine("## Create User ##\n");
                    Console.Write("Username: ");
                    string username = Console.ReadLine();
                    Console.Write("Email: ");
                    string email = Console.ReadLine();
                    Console.Write("Password: ");
                    string password = Console.ReadLine();

                    user.Username = username;
                    user.Email = email;
                    user.Password = password;

                    UserServiceProvider userService = new UserServiceProvider();
                    var userCreated = await userService.CreateUser(user);
                    if (userCreated)
                    {
                        Console.WriteLine("\nUser created successfully.");
                    }

                    Console.Write("\nPress any key to continue...");
                    Console.ReadKey();
                    break;
                case "2": // Request Job
                    Console.Clear();
                    Console.WriteLine("## Request Job ##\n");

                    if (DateTime.Now >= user.AccessToken.Expiration || user.AccessToken.Id == "")
                    {
                        Console.Write("Username: ");
                        user.Username = Console.ReadLine();
                        Console.Write("Password: ");
                        user.Password = Console.ReadLine();
                        userService = new UserServiceProvider();
                        user.AccessToken.Id = await userService.GetAccessToken(user);
                        user.AccessToken.Expiration = DateTime.Now + TimeSpan.FromMinutes(2);
                    }

                    JobServiceProvider jobService = new JobServiceProvider();
                    JsonElement job = await jobService.RequestJob(user.AccessToken.Id);

                    if (job.GetProperty("code").ToString() == "Success")
                    {
                        Console.WriteLine("\nJob requested successfully.");
                        Console.Write("\nPress any key to continue...");
                        Console.ReadKey();
                        await SolveJob(job);
                    }
                    else
                    {
                        Console.WriteLine("\nJob request failed.");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                    }
                    break;
                case "3": // Exit
                    Console.Clear();
                    Console.WriteLine("Exiting...");
                    Environment.Exit(0);
                    break;
                default: // Invalid input
                    Console.Clear();
                    Console.WriteLine("## Error ##\n");
                    Console.WriteLine("Invalid input.");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    break;
            }
            await Main(args); // Only exits when user chooses
        }

        private static async Task SolveJob(JsonElement jobResponse)
        {
            Console.Clear();
            Console.WriteLine("## Job ##\n");
            var job = jobResponse.GetProperty("job");
            Console.WriteLine("# Inputs #\n");
            Console.WriteLine("Job ID: " + job.GetProperty("id").ToString());
            Console.WriteLine("Job Type: " + job.GetProperty("type").ToString());

            JobServiceProvider jobService = new JobServiceProvider();
            JsonDocument jsonDoc = JsonDocument.Parse("{}");
            JsonElement result = jsonDoc.RootElement;

            if (job.GetProperty("type").ToString() == "DecodeStrand")
            {
                Console.WriteLine("Encoded Strand: " + job.GetProperty("strandEncoded").ToString());
                result = await jobService.DecodeStrand(job, user.AccessToken.Id.ToString());
            }
            else if (job.GetProperty("type").ToString() == "EncodeStrand")
            {
                Console.WriteLine("Strand Decoded: " + job.GetProperty("strand").ToString());
                result = await jobService.EncodeStrand(job, user.AccessToken.Id.ToString());
            }
            else if (job.GetProperty("type").ToString() == "CheckGene")
            {
                Console.WriteLine("Strand Encoded: " + job.GetProperty("strandEncoded").ToString());
                Console.WriteLine("Gene Encoded: " + job.GetProperty("geneEncoded").ToString());
                result = await jobService.CheckGene(job, user.AccessToken.Id.ToString());
            }

            if (result.GetProperty("code").ToString() == "Success")
            {
                Console.WriteLine("\n! SUCCESS !");
            }
            else
            {
                Console.WriteLine("\n! WRONG ANSWER !");
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}