using System.Text;
using System.Text.Json;
using DNAOperations.Models;

namespace DNAOperations.Services
{
    public class UserServiceProvider : IService
    {
        public UserServiceProvider() { }

        internal async Task<bool> CreateUser(User user)
        {
            if (User.ValidUser(user, creation:true) == false)
            {
                return false;
            }
            try
            {
                // Serialize the User object to JSON
                var userDataJson = JsonSerializer.Serialize(user);

                // Create a StringContent object with the serialized JSON
                var userDataString = new StringContent(userDataJson, Encoding.UTF8, "application/json");

                // Send the POST request to the API
                var response = await Client.PostAsync(BASE_PATH + "users/create", userDataString);
                response.EnsureSuccessStatusCode(); // Throws an exception if the status code is not in the 2xx range

                // Read the response as a string and deserialize it into a JsonElement object
                var responseData = await response.Content.ReadAsStringAsync();
                var content = JsonSerializer.Deserialize<JsonElement>(responseData);

                // Check if the API response contains the "code" field with a value of "Success"
                if (content.TryGetProperty("code", out JsonElement code) && code.GetString() == "Success")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and return false
                Console.WriteLine($"Error creating user: {ex.Message}");
                return false;
            }
        }

        internal async Task<string> GetAccessToken(User user)
        {
            if (User.ValidUser(user) == false)
            {
                return "";
            }
            try
            {
                // Serialize the User object to JSON
                var userDataJson = JsonSerializer.Serialize(user);

                // Create a StringContent object with the serialized JSON
                var userDataString = new StringContent(userDataJson, Encoding.UTF8, "application/json");

                // Send the POST request to the API
                var response = await Client.PostAsync(BASE_PATH + "users/login", userDataString);
                response.EnsureSuccessStatusCode(); // Throws an exception if the status code is not in the 2xx range

                // Read the response as a string and deserialize it into a JsonElement object
                var responseData = await response.Content.ReadAsStringAsync();
                var content = JsonSerializer.Deserialize<JsonElement>(responseData);

                // Check if the API response contains the "code" field with a value of "Success"
                if (content.TryGetProperty("code", out JsonElement code) && code.GetString() == "Success")
                {
                    // Try to get the accessToken field from the API response
                    if (content.TryGetProperty("accessToken", out JsonElement accessToken))
                    {
                        return accessToken.GetString();
                    }
                    else
                    {
                        throw new Exception("The API response did not contain the expected 'accessToken' field");
                    }
                }
                else
                {
                    throw new Exception("The API response did not contain the expected 'code' field with a value of 'Success'");
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and return an empty string
                Console.WriteLine($"Error getting access token: {ex.Message}");
                return "";
            }
        }
    }
}