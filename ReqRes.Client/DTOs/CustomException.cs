namespace ReqRes.Client.DTOs
{
    public class UserNotFoundException : Exception
    {
        /// <summary>
        /// To throw custom exception
        /// </summary>
        /// <param name="userId"></param>
        public UserNotFoundException(int userId)
            : base($"User with ID {userId} was not found.")
        { }
    }   
}