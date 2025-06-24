namespace FileFlux.Contracts
{
    using FileFlux.Model;

    public interface IOriginGuard
    {
        /// <summary>
        /// Validates if the download resource is valid.
        /// </summary>
        /// <param name="download">The download object containing the resource to validate.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the resource is valid.</returns>    
        Task<bool> IsResourceValidAsync(Download download);
    }
}
