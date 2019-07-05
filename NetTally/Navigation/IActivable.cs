using System.Threading.Tasks;

namespace NetTally.Navigation
{
    /// <summary>
    /// Decoration interface to allow initializing a window with parameters
    /// before showing it.  Required for use with the Navigation Service.
    /// </summary>
    public interface IActivable
    {
        Task ActivateAsync(object? parameter);
    }
}
