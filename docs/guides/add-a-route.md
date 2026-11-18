# How to add a route to the API

1. Map it in the endpoints class of its resource, such as `OptionsEndpoints`. A new resource gets a folder, a
   class with a `Map...` method, and a call in `ApiApplicationExtensions.MapRoutes`: a route mapped there asks
   for the token and declares the answers 401 and 500 without naming them.
2. Give it `WithName`, `WithTags` and `WithSummary`. The name is the operation id, which becomes the name of the
   TypeScript function and never changes. The handler is a named static method that returns typed results;
   `ProducesValidationProblem` and `ProducesProblem` declare the error answers that the results do not show.
3. The types that travel are records in the folder of the resource: `required` on a member that always has a
   value, a nullable type on a member that may be absent. Every type that travels is public, in a request and
   in an answer alike, and nothing else becomes public for it. There are two reasons: the validation is
   generated for public types only, and the descriptions of the document, which are the `<summary>` of a type
   and of its members, are read for public types only. Neither says anything about a type that is not public:
   a test of the document fails for it. Write a `<summary>` for a reader of the API, with the names of the
   JSON.
4. Validate with data annotations or `IValidatableObject` on the types and with attributes on the parameters,
   and test each rule through the route: nothing reports a type that the validation does not see.
5. A use case reports an error that the user interface must tell apart by throwing `BravoException`
   (`Bravo.Core.Errors`) with a member of `ErrorCode`, a new one when none says what happened: the answer
   is 409 with the name of the member as its code. An error of the route itself is a typed problem result
   with its status and, as the member `code`, the name of a member of `ApiProblemCode`. The two lists are
   the codes of the contract: a problem with any other code is answered with the code of its status.
6. Test it in `tests/Bravo.Api.Tests` on `TestApi`: the API as the application composes it, on a server in
   memory, with a clock that the test moves.
7. Write the document again and commit `src/Bravo.Api/openapi.json` with the change: until the file matches the
   routes, `OpenApiDocumentTests` fails and says so.

What the API is and where its parts live: [the design](../design/api.md).
