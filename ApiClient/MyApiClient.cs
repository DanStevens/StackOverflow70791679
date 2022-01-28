using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyApi
{
    public interface IBase
    {
        string BaseProp1 { get; set; }
        string BaseProp2 { get; set; }
        string BaseProp3 { get; set; }
    }
    public interface IDerived : IBase
    {
        string DerivedPropA { get; set; }
    }

    public interface IMyApiClient
    {
        Task<ICollection<IBase>> GetAllAsync(CancellationToken cancellationToken = default);
    }

    public partial class Base : IBase {}

    public partial class Derived : IDerived {}

    public partial class MyApiClient : IMyApiClient
    {
        async Task<ICollection<IBase>> IMyApiClient.GetAllAsync(CancellationToken cancellationToken)
        {
            var resp = await GetAllAsync(cancellationToken).ConfigureAwait(false);
            return resp.Select(o => (IBase) o.Properties).ToList();
        }
    }
}
