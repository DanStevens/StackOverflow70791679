using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JsonSubTypes;
using Newtonsoft.Json;

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

    // Use a JsonConverter provided by JsonSubtypes, which deserializes a Base object as a Derived
    // subtype when it contains a property named 'DerivedPropA'
    [JsonConverter(typeof(JsonSubtypes))]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Derived), nameof(Derived.DerivedPropA))]
    public partial class Base : IBase {}

    public partial class Derived : IDerived {}

    public partial class BaseResponse : IBase
    {
        public string BaseProp1 { get => ((IBase)Properties).BaseProp1; set => ((IBase)Properties).BaseProp1 = value; }
        public string BaseProp2 { get => ((IBase)Properties).BaseProp2; set => ((IBase)Properties).BaseProp2 = value; }
        public string BaseProp3 { get => ((IBase)Properties).BaseProp3; set => ((IBase)Properties).BaseProp3 = value; }
    }

    public partial class DerivedResponse : IDerived
    {
        public string DerivedPropA { get => ((IDerived)Properties).DerivedPropA; set => ((IDerived)Properties).DerivedPropA = value; }
    }

    public partial class MyApiClient : IMyApiClient
    {
        async Task<ICollection<IBase>> IMyApiClient.GetAllAsync(CancellationToken cancellationToken)
        {
            var resp = await GetAllAsync(cancellationToken).ConfigureAwait(false);
            return resp.Select(o => (IBase) o.Properties).ToList();
        }
    }
}
