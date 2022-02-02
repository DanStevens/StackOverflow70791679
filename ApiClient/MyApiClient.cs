using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JsonSubTypes;
using Newtonsoft.Json;

namespace MyApi
{
    public interface IMyApiClient
    {
        Task<ICollection<Base>> GetAllAsync(CancellationToken cancellationToken = default);
    }

    // Use a JsonConverter provided by JsonSubtypes, which deserializes a Base object as a Derived
    // subtype when it contains a property named 'DerivedPropA'
    [JsonConverter(typeof(JsonSubtypes))]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Derived), nameof(Derived.DerivedPropA))]
    public partial class Base  {}

    // Use a JsonConverter provided by JsonSubtypes, which deserializes a BaseResponse object as
    // a DerivedResponse subtype when the Type property is `ObjectType.Derived`
    [JsonConverter(typeof(JsonSubtypes), nameof(Type))]
    [JsonSubtypes.KnownSubType(typeof(DerivedResponse), ObjectType.Derived)]
    public partial class BaseResponse
    {
        public virtual Base Object => Properties;
    }

    public partial class DerivedResponse
    {
        public override Derived Object => Properties;
    }

    public partial class MyApiClient : IMyApiClient
    {
        async Task<ICollection<Base>> IMyApiClient.GetAllAsync(CancellationToken cancellationToken)
        {
            var resp = await GetAllAsync(cancellationToken).ConfigureAwait(false);
            return resp.Select(o => o.Object).ToList();
        }
    }
}
