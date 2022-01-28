using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JsonSubTypes;
using Newtonsoft.Json;

namespace MyApi
{
    public interface IMyType
    {
        MyTypeBaseType Type { get; }
        string BaseProp1 { get; set; }
        string BaseProp2 { get; set; }
        string BaseProp3 { get; set; }
    }

    [JsonConverter(typeof(JsonSubtypes))]
    [JsonSubtypes.FallBackSubType(typeof(MyType))]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(MyTypeVariant), nameof(MyTypeVariant.DerivedPropA))]
    public abstract partial class MyTypeProps : IMyType
    {
        MyTypeBaseType IMyType.Type => GetSenseType();

        protected abstract MyTypeBaseType GetSenseType();
    }

    public partial class MyType
    {
        protected override MyTypeBaseType GetSenseType() => MyTypeBaseType.MyType;
    }

    public partial class MyTypeVariant
    {
        protected override MyTypeBaseType GetSenseType() => MyTypeBaseType.MyTypeVariant;
    }

    public partial class MyTypeBase : IMyType
    {
        public string BaseProp1 { get => Properties.BaseProp1; set => Properties.BaseProp1 = value; }
        public string BaseProp2 { get => Properties.BaseProp2; set => Properties.BaseProp2 = value; }
        public string BaseProp3 { get => Properties.BaseProp3; set => Properties.BaseProp3 = value; }
    }

    public interface IMyApiClient
    {
        Task<ICollection<IMyType>> GetMyTypesAsync();
        Task<ICollection<IMyType>> GetMyTypesAsync(CancellationToken cancellationToken);
    }

    public partial class MyApiClient : IMyApiClient
    {
        async Task<ICollection<IMyType>> IMyApiClient.GetMyTypesAsync()
        {
            var resp = await GetMyTypesAsync().ConfigureAwait(false);
            return resp.Cast<IMyType>().ToArray();
        }

        async Task<ICollection<IMyType>> IMyApiClient.GetMyTypesAsync(CancellationToken cancellationToken)
        {
            var resp = await GetMyTypesAsync(cancellationToken).ConfigureAwait(false);
            return resp.Cast<IMyType>().ToArray();
        }

        partial void UpdateJsonSerializerSettings(JsonSerializerSettings settings)
        {
            var converter = JsonSubtypesWithPropertyConverterBuilder
                .Of<MyTypeBase>()
                .RegisterSubtypeWithProperty<MyTypeVariant>(nameof(MyTypeVariant.DerivedPropA))
                .SetFallbackSubtype<MyType>()
                .Build();
            settings.Converters.Add(converter);
        }
    }

    class SanityTest
    {
        private async Task Do()
        {
            IMyApiClient client = new MyApiClient("https://example.io", new HttpClient());
             var resp = await client.GetMyTypesAsync();
             IEnumerable<MyTypeVariant> variants =
                 resp.Where(o => o.Type == MyTypeBaseType.MyTypeVariant).Cast<MyTypeVariant>();
        }
    }
}
