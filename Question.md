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

There might be a way to instruct a JSON parser interpret an object in the form `{ "type": [Type], "properties": { ... }` as a single object (rather than two), but I've not found a way to do this automatically with NSwag or Newtonsoft.

> 3. Assuming my code generator can accommodate this, is there a particular way I must define the response in the OpenAPI schema for it
> to do this correctly?

The way I solved this in the end is as follows. So I can more easily describe my solution, I'm assuming the example JSON in my original question is now as follows:

```
[
  {
    "type": "MyType",
    "properties": {
      "baseProp1": "Alpha",
      "baseProp2": "Bravo",
      "baseProp3": "Charlie"
    }
  },
  {
    "type": "MyTypeVariant",
    "properties": {
      "baseProp1": "Delta",
      "baseProp2": "Echo",
      "baseProp3": "Foxtrot",
      "derivedPropA": "Golf"
    }
  }
]
```

In short, I've changed `"type": "Base",` to `"type": "MyType",` and `"type": "Derived",` to `"type": "MyTypeVariant",`. Hopefully it will become clear why.

### Creating API schema

The first step was to create an OpenAPI/Swagger schema defining 4 schemas (types)

  - `MyTypeBase` as a new type common to the types below
  - `MyType` inheriting from `Base`, but with no addition properties
  - `MyTypeVariant` inheriting from `Base, but with an additional property
  - `MyTypeProps` to define the properties for the `Base` type.

The following is the Open API schema for this, written in YAML.

```yaml
openapi: 3.0.0
info:
  title: My OpenAPI/Swagger schema for StackOverflow question #70791679
  version: 1.0.0
paths:
  /getMyTypes:
    get:
      operationId: getMyTypes
      responses:
        '200':
          description: Gets all MyType objects
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/MyTypeBase'
components:
  schemas:
    MyTypeProps:
      type: object
      description: Properties for MyType and its derivatives
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
    MyTypeBase:
      type: object
      description: TBD
      # allOf:
      #   - $ref: '#/components/schemas/MyTypeProps' # Is this needed?
      properties:
        type:
          type: string
          description: Descriminates the subtype of the MyTypeBase (e.g. MyType, MyTypeVariant)
          enum:
            - MyType
            - MyTypeVariant
        properties:
          type: object
          properties:
            $ref: '#/components/schemas/MyTypeProps'
    MyType:
      type: object
      description: The standard subtype of MyTypeBase
      allOf:
        - $ref: '#/components/schemas/MyTypeProps'
    MyTypeVariant:
      type: object
      description: A variant subtype of MyTypeBase which extends it with an additional property
      allOf:
        - $ref: '#/components/schemas/MyTypeProps'
      properties:
        derivedPropA:
          type: string
          example: Golf
```

### Creating the C# client

The next step was to create a C# schema in Visual Studio. I did this by creating a C# Class Library project and [adding an OpenAPI connected service](https://devblogs.microsoft.com/dotnet/generating-http-api-clients-using-visual-studio-connected-services/) using the above file as a schema. Doing so created generated a code file that defined the following classes:

  - MyApiClient
  - MyTypeProps   # SenseCore
  - MyTypeBase    # Sense
  - MyType        # BabelSense
  - MyTypeVarient # WordNetSense
  - MyTypeBaseType (enum)

I installed the NuGet packaged [`JsonSubTypes`](https://github.com/manuc66/JsonSubTypes). Finally I add the following code file that extends the generated API code:

MyApiClient.cs:
```csharp

```

  [1]: https://tech.signavio.com/2017/json-type-information