namespace ReqRes.Client.DTOs
{
    /// <summary>
    /// Represents a paginated response containing a list of users and associated metadata.
    /// </summary>
    /// <remarks>This class is typically used to encapsulate the results of a paginated query for user data.
    /// It includes information about the current page, the number of items per page, the total number of items, and the
    /// total number of pages, as well as the list of user data for the current page.</remarks>
    public class UserListResponse
    {
        /// <summary>
        /// Gets or sets the current page number in a paginated collection.
        /// </summary>
        public int Page { get; set; }
        /// <summary>
        /// Gets or sets the number of items to include per page in a paginated result set.
        /// </summary>
        public int Per_Page { get; set; }
        /// <summary>
        /// Gets or sets the total count of items.
        /// </summary>
        public int Total { get; set; }
        /// <summary>
        /// Gets or sets the total number of pages available.
        /// </summary>
        public int Total_Pages { get; set; }
        /// <summary>
        /// Gets or sets the collection of user data. with default initialization to an empty list.
        /// </summary>
        public List<User> Data { get; set; } = new();
    }
}