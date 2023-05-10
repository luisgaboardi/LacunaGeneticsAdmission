using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DNAOperations.Services
{
    public class JobServiceProvider : IService
    {
        public JobServiceProvider() { }

        internal async Task<JsonElement> RequestJob(string accessToken)
        {
            var accessTokenJson = JsonSerializer.Serialize(accessToken);
            var accessTokenString = new StringContent(accessTokenJson, Encoding.UTF8, "application/json");
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.ToString());
            var response = await Client.GetAsync(BASE_PATH + "dna/jobs/");
            response.EnsureSuccessStatusCode();
            var responseData = await response.Content.ReadAsStringAsync();
            var content = JsonSerializer.Deserialize<JsonElement>(responseData);
            if (content.GetProperty("code").ToString() == "Success")
            {
                return content;
            }
            JsonDocument jsonDoc = JsonDocument.Parse("{}");
            JsonElement emptyJsonElement = jsonDoc.RootElement;
            return emptyJsonElement;
        }

        public async Task<JsonElement> DecodeStrand(JsonElement jobData, string accessToken)
        {
            try
            {
                // Validate the input parameters
                if (jobData.ValueKind != JsonValueKind.Object)
                {
                    throw new ArgumentException("The jobData parameter must be a JSON object");
                }
                if (string.IsNullOrEmpty(accessToken))
                {
                    throw new ArgumentException("The accessToken parameter cannot be null or empty");
                }

                // Get the strandEncoded field from the jobData parameter
                var strandEncoded = jobData.GetProperty("strandEncoded");

                // Set the Authorization header to the Bearer token
                Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // Decode the strandEncoded string using the Decode() method
                var decodedStrand = Decode(strandEncoded.GetString());

                // Serialize the decoded strand to a JSON object
                var decodedStrandJson = JsonSerializer.Serialize(new Dictionary<string, string>()
                {
                    { "strand", decodedStrand }
                });

                // Send a POST request to the API to decode the strand
                var jobId = jobData.GetProperty("id");
                var decodedStrandString = new StringContent(decodedStrandJson, Encoding.UTF8, "application/json");
                var response = await Client.PostAsync(BASE_PATH + "dna/jobs/" + jobId + "/decode", decodedStrandString);
                response.EnsureSuccessStatusCode();

                // Read the response as a string and deserialize it into a JsonElement object
                var responseData = await response.Content.ReadAsStringAsync();
                var content = JsonSerializer.Deserialize<JsonElement>(responseData);

                // Check if the API response contains the "code" field with a value of "Success"
                if (content.TryGetProperty("code", out JsonElement code) && code.GetString() == "Success")
                {
                    return content;
                }
                else
                {
                    Console.WriteLine("Error decoding strand: " + responseData);
                    // Return an empty JsonElement object if the API response does not contain the expected "code" field
                    return JsonDocument.Parse("{}").RootElement;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and return an empty JsonElement object
                Console.WriteLine("Error decoding strand: " + ex.Message);
                return JsonDocument.Parse("{}").RootElement;
            }
        }


        private static string Decode(string encodedString)
        {
            // Convert Base64 string to byte array
            byte[] bytes = Convert.FromBase64String(encodedString);

            // Convert byte array to bits string (Big-Endian)
            var bitsStringBuilder = new StringBuilder();
            foreach (byte b in bytes)
            {
                bitsStringBuilder.Append(Convert.ToString(b, 2).PadLeft(8, '0'));
            }
            string bitsString = bitsStringBuilder.ToString();

            // Convert bits string to original string
            var originalStringBuilder = new StringBuilder();
            var nucleobaseMappings = new List<(string, string)>
            {
                ("00", "A"),
                ("01", "C"),
                ("10", "G"),
                ("11", "T")
            };
            for (int i = 0; i < bitsString.Length; i += 2)
            {
                if (i + 2 > bitsString.Length)
                {
                    // The last group of bits has less than 2 digits, exit loop
                    break;
                }
                string nucleobases = bitsString.Substring(i, 2);
                string nucleotide = nucleobaseMappings.FirstOrDefault(x => x.Item1 == nucleobases).Item2;
                if (nucleotide == null)
                {
                    // Invalid nucleobases, return empty string
                    return "";
                }
                originalStringBuilder.Append(nucleotide);
            }

            return originalStringBuilder.ToString();
        }


        public async Task<JsonElement> EncodeStrand(JsonElement jobData, string accessToken)
        {
            try
            {
                // Validate the input parameters
                if (jobData.ValueKind != JsonValueKind.Object)
                {
                    throw new ArgumentException("The jobData parameter must be a JSON object");
                }
                if (string.IsNullOrEmpty(accessToken))
                {
                    throw new ArgumentException("The accessToken parameter cannot be null or empty");
                }
                
                var strand = jobData.GetProperty("strand").GetString();
                var jobId = jobData.GetProperty("id").GetString();

                if (string.IsNullOrEmpty(strand) || string.IsNullOrEmpty(jobId))
                {
                    throw new ArgumentException("Invalid job data");
                }

                var encodedStrand = Encode(strand);
                var encodedStrandJson = JsonSerializer.Serialize(new Dictionary<string, string>()
                {
                    { "strandEncoded", encodedStrand }
                });

                Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await Client.PostAsync($"{BASE_PATH}dna/jobs/{jobId}/encode", new StringContent(encodedStrandJson, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    var content = JsonSerializer.Deserialize<JsonElement>(responseData);
                    if (content.GetProperty("code").GetString() == "Success")
                    {
                        return content;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while encoding: {ex.Message}");
            }

            return JsonDocument.Parse("{}").RootElement;
        }

        private static string Encode(string strand)
        {
            // Use StringBuilder to construct the bit string
            StringBuilder bitStringBuilder = new StringBuilder();
            foreach (char c in strand)
            {
                switch (c)
                {
                    case 'A':
                        bitStringBuilder.Append("00");
                        break;
                    case 'C':
                        bitStringBuilder.Append("01");
                        break;
                    case 'T':
                        bitStringBuilder.Append("11");
                        break;
                    case 'G':
                        bitStringBuilder.Append("10");
                        break;
                    default:
                        throw new ArgumentException("Invalid character in input string");
                }
            }
            string bitString = bitStringBuilder.ToString();

            // Pad the bit string with zeros if necessary
            int padding = bitString.Length % 8;
            if (padding > 0)
            {
                bitString = bitString.PadRight(bitString.Length + 8 - padding, '0');
            }

            // Convert the bit string to a byte array using LINQ
            byte[] bytes = Enumerable.Range(0, bitString.Length / 8)
                .Select(i => Convert.ToByte(bitString.Substring(i * 8, 8), 2))
                .ToArray();

            // Convert the byte array to a Base64 string
            string base64 = Convert.ToBase64String(bytes);

            return base64;
        }

        public async Task<JsonElement> CheckGene(JsonElement jobData, string accessToken)
        {
            try
            {
                // Verifica se as propriedades "geneEncoded" e "strandEncoded" existem no objeto jobData
                if (!jobData.TryGetProperty("geneEncoded", out var geneEncoded) || !jobData.TryGetProperty("strandEncoded", out var strandEncoded))
                {
                    throw new ArgumentException("Properties 'geneEncoded' and 'strandEncoded' are required.");
                }

                // Decodifica os valores de "geneEncoded" e "strandEncoded" para as respectivas strings "gene" e "strand"
                string gene = Decode(geneEncoded.GetString());
                string strand = Decode(strandEncoded.GetString());

                // Verifica se o gene está ativado no strand
                bool isActivated = IsGeneActivated(gene, strand);

                // Cria um objeto JSON contendo a propriedade "isActivated" com o valor booleano de "isActivated"
                var isActivatedJson = JsonSerializer.Serialize(new Dictionary<string, bool>()
                {
                    { "isActivated", isActivated }
                });

                // Envia uma requisição POST para o servidor com o objeto JSON
                jobData.TryGetProperty("id", out var jobId);
                Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var isActivatedString = new StringContent(isActivatedJson, Encoding.UTF8, "application/json");
                var response = await Client.PostAsync($"{BASE_PATH}dna/jobs/{jobId}/gene", isActivatedString);
                response.EnsureSuccessStatusCode();
                // Verifica se a resposta foi bem-sucedida e retorna o conteúdo em formato JSON
                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    var content = JsonSerializer.Deserialize<JsonElement>(responseData);
                    if (content.GetProperty("code").GetString() == "Success")
                    {
                        return content;
                    }
                }

                // Retorna um objeto JSON vazio em caso de erro ou falha na requisição
                return JsonDocument.Parse("{}").RootElement;
            }
            catch (Exception ex)
            {
                // Em caso de exceção, retorna um objeto JSON com a propriedade "error" contendo a mensagem de erro
                return JsonDocument.Parse("{\"error\":\"" + ex.Message + "\"}").RootElement;
            }
        }

        private static bool IsGeneActivated(string gene, string dnaTemplate)
        {
            // Verifica se o tamanho do gene é menor ou igual ao tamanho do dnaTemplate
            if (gene.Length > dnaTemplate.Length)
            {
                return false;
            }

            // Verifica se o gene é uma string vazia
            if (string.IsNullOrEmpty(gene))
            {
                return false;
            }

            int matchCount = 0;

            // Alterado o loop para evitar subtração a cada iteração
            for (int i = 0, maxIndex = dnaTemplate.Length - gene.Length; i <= maxIndex; i++)
            {
                int tempMatchCount = 0;

                // Comparação de strings é feita utilizando o método String.CompareOrdinal
                for (int j = 0; j < gene.Length; j++)
                {
                    if (string.CompareOrdinal(gene[j].ToString(), dnaTemplate[i + j].ToString()) == 0)
                    {
                        tempMatchCount++;
                    }
                }
                matchCount = Math.Max(tempMatchCount, matchCount);
            }

            double matchPercentage = (double)matchCount / gene.Length * 100;
            return matchPercentage > 50;
        }
    }

}