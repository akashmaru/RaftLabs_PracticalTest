using ReqRes.Client.DTOs;

namespace ReqRes.Client.Interface
{
    /// <summary>
    /// Defines methods for interacting with an external user service to retrieve user information.
    /// </summary>
    /// <remarks>This interface provides asynchronous methods to retrieve user data, including fetching all
    /// users or retrieving a specific user by their unique identifier. Implementations of this interface are expected
    /// to handle communication with an external data source or API.</remarks>
    public interface IExternalUserService
    {
        /// <summary>
        /// Asynchronously retrieves all users from the data source.
        /// </summary>
        /// <remarks>This method does not filter or paginate the results. It retrieves all users available
        /// in the data source. Use this method when you need a complete list of users.</remarks>
        /// <returns>A task that represents the asynchronous operation. The task result contains an  IEnumerable{T} of User
        /// objects representing all users.  The collection will be empty if no users are found.</returns>
        Task<IEnumerable<User>> GetAllUsersAsync();
        /// <summary>
        /// Asynchronously retrieves a user by their unique identifier.
        /// </summary>
        /// <remarks>This method performs an asynchronous operation to fetch user details. Ensure that the
        /// caller awaits the returned task.</remarks>
        /// <param name="userId">The unique identifier of the user to retrieve. Must be a positive integer.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="User"/> object
        /// corresponding to the specified <paramref name="userId"/>, or <see langword="null"/> if no user is found.</returns>
        Task<User> GetUserByIdAsync(int userId);
    }
}