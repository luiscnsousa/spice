namespace Spice.Data
{
    using System.Threading.Tasks;

    public interface IDbInitializer
    {
        Task InitializeAsync();
    }
}
