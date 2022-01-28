After some time researching and experimenting, here are my answers to my own questions:

> 1. I notice that the properties for the objects are wrapped in a "properties" property. Is the example above a JSON compliant way for
> an HTTP API to serialize polymorphic arrays?

In terms of JSON convention(s) for include type data with an object, I struggled to find a definitive one - it appears to be [up to the developer what convention to use][1]. 

> 2. Will a typical API code generator be able to deserialize this
> correctly? The particular code generator I'm using is Visual Studio's
> 2022 build-in OpenAPI 'connected service' generator (essentially
> NSwag). Ideally I would want the generated classes for Base and
> Derived not to expose the fact that the JSON they were deserialized
> from had their properties wrapped in "properties" (i.e. BaseProp1,
> BaseProp2, etc. are defined on the class itself)

There might be a way to instruct a JSON parser interpret an object in the form `{ "type": [Type], "properties": { ... }` as a single object (rather than two nested ones), but I've not found a way to do this automatically with NSwag or Newtonsoft.

> 3. Assuming my code generator can accommodate this, is there a particular way I must define the response in the OpenAPI schema for it
> to do this correctly?

Yes, I've found a way to do this in a way that works how I want. It does involve some customisations to generated client code. The way I solved this in the end is as follows.

### Creating API schema

The first step was to create an OpenAPI/Swagger schema that defines the following:

  - A schema named `Base` of type `object`
  - A schema named `Derived` of type `object` that derives from `Base`
  - A schema named `GetAllResponseItem` of type `object` that wraps `Base` objects and their derivatives
  - A schema named `ObjectType` of type `string` that is a enum with values `Base` and `Derived`.
  - A `get` operation with the path `getAll` that returns an array of `GetAllResponseItem` objects

Here is schema for this, written in YAML.

```yaml
openapi: 3.0.0
info:
  title: My OpenAPI/Swagger schema for StackOverflow question #70791679
  version: 1.0.0
paths:
  /getAll:
    get:
      operationId: getAll
      responses:
        '200':
          description: Gets all objects
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/GetAllResponseItem'
components:
  schemas:
    Base:
      type: object
      description: Base type
      properties:
        baseProp1:
          type: string
          example: Alpha
        baseProp2:
          type: string
          example: Bravo
        baseProp3:
          type: string
          example: Charlie
    Derived:
      type: object
      description: Derived type that extends Base
      allOf:
        - $ref: '#/components/schemas/Base'
      properties:
        derivedPropA:
          type: string
          example: Golf
    GetAllResponseItem:
      type: object
      description: An item in the array type response for getAll operation
      properties:
        type:
          $ref: '#/components/schemas/ObjectType'
        properties:
          $ref: '#/components/schemas/Base'
    ObjectType:
      type: string
      description: Discriminates the type of object (e.g. Base, Derived) the item is holding
      enum:
        - Base
        - Derived
```

### Creating the C# client

The next step was to create a C# schema in Visual Studio. I did this by creating a C# Class Library project and [adding an OpenAPI connected service](https://devblogs.microsoft.com/dotnet/generating-http-api-clients-using-visual-studio-connected-services/) using the above file as a schema. Doing so created generated a code file that defined the following partial classes:

  - `MyApiClient`
  - `Base`
  - `Derived` (inherits `Base`)
  - `GetAllResponseItem` (with a `Type` property of type `ObjectType` and a `Properties` property of type `Base`)
  - `ObjectType` (an enum with items `Base` and `Derived`)
  - `ApiException` (not important for this discussion)

Next I installed the [JsonSubtypes](https://github.com/manuc66/JsonSubTypes) nuget package. This will allow us to instruct the JSON deserializer in the API client, when it is expecting a `Base` object, to instead provide a `Derived` object when the JSON has the `DerivedPropA` property.

Next, I add the following code file that extends the generated API code:

MyApiClient.cs:
```csharp
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

    public partial class MyApiClient : IMyApiClient
    {
        async Task<ICollection<IBase>> IMyApiClient.GetAllAsync(CancellationToken cancellationToken)
        {
            var resp = await GetAllAsync(cancellationToken).ConfigureAwait(false);
            return resp.Select(o => (IBase) o.Properties).ToList();
        }
    }
}
```

The interfaces `IBase`, `IDerived`, and `IMyApiClient` attempt to hide from consumers of `IMyApiClient` the fact that the actual response from the API uses type `ICollection<GetAllResponseItem>` and instead provides the type `ICollection<IBase>`. This isn't perfect since nothing forces the usage of `IMyApiClient` and `GetAllResponseItem` class is declared as public. It may be possible to encapsulate this further, but it would probably involve customising the client code generation.

Finally, here's some test code to demonstrate usage:

Tests.cs:

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MyApi;
using NUnit.Framework;

namespace ApiClientTests
{
    public class Tests
    {
        private readonly IBase[] _allObjects = {
            new Base {
                BaseProp1 = "Alpha", BaseProp2 = "Bravo", BaseProp3 = "Charlie"
            },
            new Derived {
                BaseProp1 = "Delta", BaseProp2 = "Echo", BaseProp3 = "Foxtrot",
                DerivedPropA = "Golf"
            }
        };

        [Test]
        public void ShouldBeAbleToAccessPropertiesOnBaseAndDerivedTypes()
        {
            IBase baseObject = _allObjects[0];
            Assert.That(baseObject, Is.TypeOf<Base>());
            Assert.That(baseObject.BaseProp1, Is.EqualTo("Alpha"));

            IDerived derivedObject = (IDerived)_allObjects[1];
            Assert.That(derivedObject, Is.TypeOf<Derived>());
            Assert.That(derivedObject.DerivedPropA, Is.EqualTo("Golf"));
        }

        [Test]
        public void ShouldBeAbleToDiscriminateDerivativeTypesUsingTypeCasting()
        {
            IDerived[] derivatives = _allObjects.OfType<IDerived>().ToArray();
            Assert.That(derivatives.Length, Is.EqualTo(1));
            Assert.That(derivatives[0], Is.SameAs(_allObjects[1]));
        }


        [Ignore("Example usage only - API host doesn't exist")]
        [Test]
        public async Task TestGetAllOperation()
        {
            using var httpClient = new HttpClient();
            IMyApiClient apiClient =
                new MyApiClient("https://example.io/", httpClient);
            var resp = await apiClient.GetAllAsync();
            Assert.That(resp, Is.TypeOf<ICollection<IBase>>());

            IBase[] allObjects = resp.ToArray();
            Assert.That(allObjects.Length, Is.EqualTo(2));
            Assert.That(allObjects[0].BaseProp1, Is.EqualTo("Alpha"));
            Assert.That(((IDerived)allObjects[1]).DerivedPropA, Is.EqualTo("Golf"));
        }
    }
}

```

The source code is available in GitHub: https://github.com/DanStevens/StackOverflow70791679

I appreciate this may have been a fairly niche question and answer, but writing up the question has really helped me come to the simplest solution (indeed by [first attempt](https://github.com/DanStevens/StackOverflow70791679/tree/attempt1) was more complex than my [second](https://github.com/DanStevens/StackOverflow70791679/tree/attempt2)). Perhaps this question might be useful to someone else.

Lastly, the actual project that initiated this question, where I will be applying what I've learnt, is available also on GitHub: https://github.com/DanStevens/BabelNetApiClient

  [1]: https://tech.signavio.com/2017/json-type-information