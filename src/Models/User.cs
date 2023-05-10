namespace DNAOperations.Models
{
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public APIAccessToken AccessToken { get; set; }

        public User()
        {
            Username = "";
            Password = "";
            Email = "";
            AccessToken = new APIAccessToken();
        }

        public static bool ValidUser(User user, bool creation = false)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user), "The User object cannot be null");
            }
            if (string.IsNullOrEmpty(user.Username))
            {
                throw new ArgumentException("The Username field of the User object is required");
            }
            if (string.IsNullOrEmpty(user.Password))
            {
                throw new ArgumentException("The Password field of the User object is required");
            }
            if (string.IsNullOrEmpty(user.Email) && creation)
            {
                throw new ArgumentException("The Email field of the User object is required");
            }

            return true;
        }

        public static bool ValidLoginData(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user), "The User object cannot be null");
            }
            if (string.IsNullOrEmpty(user.Username))
            {
                throw new ArgumentException("The Username field of the User object is required");
            }
            if (string.IsNullOrEmpty(user.Password))
            {
                throw new ArgumentException("The Password field of the User object is required");
            }

            return true;
        }
    }
}