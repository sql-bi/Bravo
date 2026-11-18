# User interface and host

For 1.1.0 the user interface reaches the host through an HTTP API
([ADR 0012](../decisions/0012-http-on-loopback-between-user-interface-and-host.md)): ASP.NET Core minimal APIs, with the
conventions of [ADR 0014](../decisions/0014-api-conventions-and-a-contract-kept-as-a-file.md). Development and production
use the same path, so a browser reaches the API that the webview reaches. The Visual Studio Code extension keeps
the routes of Template Development that it calls today (backlog B-17).

**Server.** The host serves the API with Kestrel, on plain HTTP, on the loopback address `127.0.0.1` and on a
port that the system chooses at every start. A service of the host gives the address once the host has started.
The server stops with the host: a request that waits for an event is answered when the stop begins, and any
other request has the 5 seconds of the stop to end.

**Protection.** Any program and any page of the machine can reach the port.

- Every request carries the token of the process, as `Authorization: Bearer <token>`: 32 random bytes created
  at the start and kept in memory only. Without it the answer is 401, also on a path that matches no route. A
  route added later is protected with nothing to remember.
- A request names `127.0.0.1` as its host, or the answer is 400: a page that reaches the port through a name of
  its own gets nothing.
- A browser lets a page read an answer only when the page comes from an origin that the application allows.
  The application allows the origin of its page, `https://bravo.invalid`
  ([window and webview](window-and-webview.md)). An allowed origin may send `GET`, `POST`, `PUT` and `DELETE`
  with the `Authorization` and `Content-Type` headers, without credentials. The answer to the request that a
  browser sends first, to ask what is allowed, may be kept for two hours, the longest that Chromium keeps it:
  without a value it is kept for 5 seconds.
- A second token is a second authentication scheme, accepted only by the routes whose policy names it: the one
  of the Visual Studio Code extension is backlog B-17.

**Routes.**

| Route | What it does |
| --- | --- |
| `GET /application` | Reads what the application is: its version, the culture of the user and whether the build is a development build |
| `GET /options` | Reads the options of the user |
| `PUT /options` | Replaces the options of the user, and returns them as they were kept |
| `GET /events?after={number}` | Reads the events of the host after a number, waiting for the next one when there is none |

- A route names a resource. An operation that is not create, read, update or delete is a `POST` on the
  resource with a verb as last segment. A `GET` changes no state, opens no dialog and starts no process.
- An operation runs inside its request, and the user interface cancels it by aborting the request. Only an
  operation that reports progress will be a resource of its own (backlog B-15).
- No route opens a dialog as a side effect: choosing a file or a folder is a request of its own, and the
  operation receives the path.
- The routes have no version: host and user interface ship together.

**JSON.**

- A member name is camelCase. An enum value and an error code are PascalCase names, and an enum never travels
  as a number.
- A number is a JSON number, never text.
- A member without a value is absent: the host never writes `null`. In a request, `null` is read as absent
  where the member is optional and is an error where the member always has a value. The document lists as
  required the members that always have a value.
- A member that the host does not know is ignored.

**Errors.** Every error answer is a problem details body ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457),
`application/problem+json`) with the member `code`: a name that never changes meaning, by which the user
interface chooses what to do. The codes are a closed list
([ADR 0016](../decisions/0016-closed-error-codes-generated-types-and-tests-in-the-user-interface.md)): the document
of the API lists them in the schema `ProblemCode`, and no answer carries another one.

| Status | When | `code` |
| --- | --- | --- |
| 400 | The request cannot be read, or names another host | `InvalidRequest` |
| 400 | A value is not allowed. The member `errors` names each value as it travels, such as `proxy.address` | `ValidationFailed` |
| 401 | The token of the API is missing or wrong, and nothing else: a missing sign-in to the Power BI service is a 409 with its code | `Unauthorized` |
| 404, 405, 415 | No such route, no such method on the route, a body that is not JSON | `NotFound`, `MethodNotAllowed`, `UnsupportedMediaType` |
| 409 | An error that a use case foresaw: the request was right, and the application cannot carry it out as things stand | The code of the error |
| 500 | A defect of the host. The answer tells nothing of the exception | `Unexpected` |
| Any other | The status is all that tells what happened | `HttpError` |

- A use case reports a foreseen error by throwing `BravoException` with a code and a message in English,
  which becomes the `detail` of the problem. The code is a member of one list, which admits no other value.
  No use case reports an error yet: that list is empty, and the codes of the document are the ones of the
  table.
- A route that writes a problem with a code outside the list is answered with the code of its status.

**Events.** The host tells the user interface what happens through numbered events, the first of each process
being 1. The page asks for the events after the last number it has read; the answer has the events and
`next`, the number to send with the next request.

- When there are events after the number, the answer is immediate. Otherwise the request waits: an event that
  arrives answers it at once, and after 30 seconds it is answered with no event.
- The host keeps the last 64 events. A request without a number gets all of them, so a page reads what
  happened before it was ready.
- Numbers have no gaps: when the first event of an answer does not follow the number of the request, the
  events between the two are no longer kept. A number beyond the last one is answered at once, and `next`
  brings the page back.
- An event is one type for every kind: `kind` names the kind, and the member with the data of that kind is
  the one present. The first kind is `Activation`, published at every start of the application, the first
  included, with the server, the database and the errors of the command line.

**Options.** The options are the settings of the user: theme, telemetry, update check, custom templates,
sign-in with the system browser, diagnostic level, proxy, and the options that only the user interface reads,
which the host keeps as any JSON value. A proxy address must be the address of a server, and each entry of the
bypass list a regular expression; blank text is no value. Today the settings start from their defaults at every
start: keeping them is backlog B-4, and the policies that override them backlog B-5.

**Contract.** `src/Bravo.Api/openapi.json` is the OpenAPI 3.1 document of the API. A test produces it from the
routes and fails when the file differs, so a change of the API is a change of the file in the same commit.
The application does not serve the document and ships nothing that produces it: what produces it belongs to
the tests of the API. The descriptions of the document are the comments of the types that travel, which are
public: the validation and the descriptions are generated for public types only.

Every build of the user interface generates its TypeScript client from that file, with
[Orval](https://orval.dev), into a folder that git ignores: a function for each route, named as the route is
named in the document, and the types that travel. The build then checks the user interface against them.

- Each function calls the host through one function of the user interface, which adds the address and the
  token and lets the caller abort the request.
- The codes of the errors are one of those types. The user interface has one table from code to text, and
  the build fails for a code that has no text there. For a value outside the list, which only something that
  is not this host can send, it has the text of an error that nothing describes.
- A value that travels has in the user interface its generated type, with the names of the host, and nothing
  converts a name into a number: today the theme, the diagnostic level and the type of the proxy, later the
  others, each with its feature. A number in the options that only the user interface reads stays a number:
  1.0.x saved it.
- Today the application data, the options and the events go through the client. Every other call of the user
  interface belongs to a feature that has not moved: nothing is sent, and the call fails at once with an
  error that says that the feature is not available yet. Each one moves to the client with its feature.

**Start of the page.** The page is the same in the window and in a browser, with no branch for either: it
reads the address and the token of the API from `window.bravoHost`
([window and webview](window-and-webview.md)), whoever set it. A page that cannot start, because the object is
missing, the host does not answer or one of its parts fails, says so in place of the user interface: in the
language of the browser, in English where it has no text in that language, with what went wrong below and a
button that loads the page again.

- Before any of its parts exists, the page asks the API what the application is and for the options of the
  user. Until those answers arrive it follows the color scheme that it is told: in the window the theme of the
  user ([window and webview](window-and-webview.md)), in a browser the color mode of the system.
- It saves no option when it starts: what it shows is what the options that it has just read say.
- It starts with no policy (backlog B-5) and no telemetry settings (backlog B-9): until then no option is
  locked, and telemetry sends nothing.
- It has no message channel with the window: what the host tells it arrives as events.
- It reads the events one request after the other. The number to ask next is kept in the session storage of
  the page, with the address of the host it belongs to: a page that is loaded again goes on from there, and
  a page of another host starts from the beginning. A page that goes away aborts its request. After an error
  the page waits before it asks again: one second, then twice as long each time, up to 30 seconds.
- An activation without a server does nothing in the page. The errors of a command line go to the diagnostic
  log. An activation with a server is logged, and opens no report until the host can find it (backlog B-10).
- Four calls that the page starts by itself have no route yet: the sign-in with the account that the page
  remembers, the update check, the reading of the diagnostics of the host, and the list of the reports that
  are open in Power BI Desktop. They fail like every call without a route, and the page shows nothing and
  writes nothing in its console for them.

## In the code

`Bravo.Api` holds everything of the HTTP API that the user interface calls. Its conventions are
[ADR 0014](../decisions/0014-api-conventions-and-a-contract-kept-as-a-file.md), and
this document describes them above as they are built. Where each part lives:

| Part | Path |
| --- | --- |
| What the application calls: `AddApi`, `UseApi`, `ApiOptions` | `src/Bravo.Api/` |
| Kestrel on the loopback address, and the address it was given | `src/Bravo.Api/Server/` |
| The token and its authentication scheme, the fallback policy, host filtering, CORS | `src/Bravo.Api/Security/` |
| Problem details and their codes, and the answer to a `BravoException` | `src/Bravo.Api/Problems/` |
| The error of a use case, and the codes that it can carry | `src/Bravo.Core/Errors/` |
| How JSON is read and written | `src/Bravo.Api/Json/` |
| The events for the user interface: their log, their publisher, their route | `src/Bravo.Api/Events/` |
| The routes of a resource, with the types that travel | A folder for each resource, such as `src/Bravo.Api/Options/` |
| The OpenAPI document | `src/Bravo.Api/openapi.json` |
| What produces the document, with the rules applied to it, and the test that compares it with the file | `tests/Bravo.Api.Tests/Contract/` |

- A new error code has no text in the user interface, and no build of `web/` succeeds until
  `web/model/problem-texts.ts` has one.
- An event for the user interface is a new value of `HostEventKind`, an optional member of `HostEvent` with
  its data, and a method of `IHostEventPublisher`. The kinds are not derived types: the document that .NET 10
  produces for a list of derived types is wrong.
- `AddApi` registers the services of the API and `UseLoopbackServer` its server. A use case that a route calls
  is registered by `Bravo.Desktop`, and by `TestApi` for the tests.
- Only a test produces the document, so what produces it is in the test project: `Bravo.Api` has no package
  reference, and the application ships nothing of the OpenAPI package. A rule on how the document describes
  the JSON goes in `ContractSchema` or `ContractDocument`.
- `OpenApiDocumentTests.WriteTheFileOfTheRepository` is not a test: it writes the document, runs only when
  asked for, and shows as skipped in every other run.
- `ErrorCode` has no member yet, so no test can throw a `BravoException`. The two tests of an error that a
  use case foresaw, in `ProblemDetailsTests` and in `BravoExceptionTests`, take every member as their data:
  they show as skipped until the first member exists, and run for it without a change.
