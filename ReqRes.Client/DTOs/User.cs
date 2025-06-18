namespace ReqRes.Client.DTOs
{
    /// <summary>
    /// Represents a user with identifying and contact information.
    /// </summary>
    /// <remarks>This class provides properties to store user details such as their unique identifier,  email
    /// address, name, and avatar URL. It is typically used to model user data in applications  that require user
    /// management or profile information.</remarks>
    public class User
    {
        /// <summary>
        /// Gets or sets the unique identifier for the entity.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the email address associated with the user.
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// Gets or sets the first name of the individual.
        /// </summary>
        public string First_Name { get; set; }
        /// <summary>
        /// Gets or sets the last name of a person.
        /// </summary>
        public string Last_Name { get; set; }
        /// <summary>
        /// Gets or sets the URL of the user's avatar image.
        /// </summary>
        public string Avatar { get; set; }
    }
}