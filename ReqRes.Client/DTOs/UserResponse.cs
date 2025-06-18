namespace ReqRes.Client.DTOs
{
    /// <summary>
    /// Represents the response containing user data.
    /// </summary>
    /// <remarks>This class is typically used to encapsulate the result of an operation that retrieves user
    /// information. The <see cref="Data"/> property holds the user details if the operation is successful, or
    /// <c>null</c> if no user data is available.</remarks>
    public class UserResponse
    {
        /// <summary>
        /// Gets or sets the user data associated with the current operation.
        /// </summary>
        public User? Data { get; set; }
    }
}