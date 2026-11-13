# C# Coding Conventions

This document defines the C# coding conventions for the **mapepire-csharp** project. It is derived from the
[Java coding conventions](https://github.com/Mapepire-IBMi/mapepire-server/blob/main/CODING_CONVENTIONS_JAVA.md)
of mapepire-server. Section numbers match that document, so the two can be compared side by side. Where C#
idiom or the language itself calls for a different rule, the rule here says so and explains why.

It is written for two audiences:

- **Humans** contributing to the project, who should read it once and then follow it by habit.
- **AI coding assistants**, which should treat every rule below as a hard requirement when generating,
  editing, or reviewing C# in this repository.

Where this document is silent, follow the style of the code immediately surrounding your change. Read a
few of the nearby files before writing anything: the rules below describe the intent, and the code you
are working in shows what that intent looks like in practice. No single file in this repository is a
canonical reference implementation, and none should be treated as one. Consult the neighbourhood, not a
designated exemplar.

> **Note on scope:** These rules are about *readability and consistency*, not correctness. A change
> that follows every rule here but breaks the build is still a bad change. Never sacrifice clarity to
> satisfy a formatting rule. If a rule genuinely makes a particular piece of code worse, say so in the
> pull request rather than silently ignoring it.

> **Note on existing code:** Not every file in this repository conforms to the conventions below. Much of
> the SDK under `io/github/mapepire_ibmi` was ported from Java and predates them. Where existing code
> conflicts with this document, **this document wins** for new code. Do not treat a non-conforming file
> as precedent. **Public API is the exception:** never rename an existing public type, member, or
> parameter to satisfy this document. That is a breaking change (see §7).

> **Note on precedence:** Where this document conflicts with a comment on a pull request, **the review
> comment wins.** A reviewer is looking at one specific change with context this document cannot have,
> and their call on that change is final. If the same review comment keeps recurring, that is a signal to
> update this document, not to re-argue the rule on every pull request.

---

## 1. Files and Encoding

### 1.1 Line endings must be Unix-style (LF)

**All source files must use Unix-style line endings (`LF`, `\n`). Windows-style `CRLF` line endings are
not permitted anywhere in the source tree.** This covers `.cs`, `.csproj`, `.sln`, `.json`, and `.md`
files alike.

A file committed with CRLF shows up as a whole-file change in every later diff. That destroys the
usefulness of `git blame` and code review. Much of this project's development happens on Windows, where
editors and `dotnet new` templates default to CRLF, so this needs active attention.

Practical guidance:

- Configure your editor to use LF for this repository. In VS Code: `"files.eol": "\n"`.
- Make sure Git is **not** rewriting line endings on checkout: set `core.autocrlf=false`.
- **AI assistants:** when writing or rewriting a file, emit `\n` only. Never emit `\r\n`. Before
  finishing an edit, verify the file has no `\r` characters.

**Repairing line endings is always in scope.** Section 5 and the checklist in section 9 tell you not to
reformat code you did not otherwise need to touch. Converting `CRLF` to `LF` is the explicit, standing
exception: if you open a file and find CRLF endings, convert the entire file as part of your change. No
file is exempt, however old it is or however large the resulting diff. Mention the conversion in the
pull request description so the reviewer expects whitespace noise alongside the substantive change.

### 1.2 Encoding

Source files are **UTF-8**. New files are written **without a byte-order mark**. A few existing files
carry a BOM. Do not add or strip one as a drive-by change.

Prefer plain ASCII in identifiers, comments, exception messages, and log output. Non-ASCII characters
(emoji, typographic quotes, arrows) render inconsistently in IBM i terminals and job logs, and can be
mangled by EBCDIC-adjacent tooling. The exception is string data that must round-trip through Db2, such
as test values that exercise Unicode columns.

### 1.3 Target frameworks and language features

| Project | Target framework |
| --- | --- |
| `io` (the SDK, assembly `mapepire`) | `net8.0` |
| `Mapepire.Data` | `net8.0` |
| `Mapepire.EntityFrameworkCore` | `net8.0` |
| `UnitTests` | `net9.0` |
| `Mapepire.EntityFrameworkCore.Tests` | `net9.0` |

CI builds with the .NET 9 SDK.

- **Library code must compile and run on `net8.0`.** Do not use APIs that first shipped in a later
  runtime. The compiler catches most of these, but not all: some APIs exist in a newer reference set and
  fail only at runtime on the target framework.
- Do not change a project's `TargetFramework` (or add a target) as part of an unrelated change. That is
  a deliberate decision for the pull request description, not a side effect.
- **Nullable reference types are enabled in every project and stay enabled.** Do not add
  `#nullable disable` or `<Nullable>disable</Nullable>`. See §4.9.
- `ImplicitUsings` is enabled. Project-wide `global using` directives belong in a single
  `GlobalUsings.cs` per project, not scattered across files.

### 1.4 Generated sources are never edited by hand

Never hand-edit a file the build generates: anything under `bin/` or `obj/`, `*.g.cs`, and
`*.Designer.cs`. Any change you make will be silently discarded by the next build. The resulting
confusion, a fix that works locally and vanishes in CI, costs far more time than it saves. If a
generated file needs to change, change its input (the `.csproj`, the template, or the source generator).

EF Core migration files are the exception: they are generated once, then checked in and owned like any
other source file.

---

## 2. Whitespace and Layout

### 2.1 Spaces only, never tabs

**Indentation is 4 spaces. Tab characters must not appear in C# source files.**

This applies to continuation lines and alignment as well. If you are lining something up, use spaces.
A few existing files contain tabs. Convert the lines you touch; do not reformat the rest of the file as a
drive-by change.

### 2.2 Line length

**Individual lines should be no longer than 320 characters where it is reasonable to do so.**

320 matches the limit used by mapepire-server. It is generous on purpose: it is a limit, not a target, and
most lines should be far shorter. The rule exists to catch the pathological cases, such as a long LINQ
chain or an interpolated string with a dozen embedded expressions, that become unreadable in
side-by-side diffs.

When a line must be broken, break it at a point that makes the structure obvious and indent the
continuation by one level. Good break points are before a `.` in a method chain, after a `,` in an
argument list, and before `=>`, `?`, `:`, `&&` or `||`.

There is no formatter profile in this repository yet (§8), so use formatter suppression sparingly.
A file full of `#pragma` or hand-aligned regions is a sign the code needs restructuring.

### 2.3 Braces

> **Differs from Java:** the Java conventions use K&R braces. C# uses **Allman braces**, the near-universal
> .NET convention and the style of the majority of this repository, including `SqlJob`, `Query`, and the
> ADO.NET and EF Core providers.

The opening brace goes on its own line, aligned with the start of the declaration or statement. The
closing brace goes on its own line.

```csharp
public void DoSomething()
{
    if (condition)
    {
        // ...
    }
    else
    {
        // ...
    }
}
```

Some type files under `io/github/mapepire_ibmi/types` use K&R braces. Match the file you are editing
rather than mixing the two styles in one file. New files use Allman.

### 2.4 Braces are required on single-statement blocks

**Every `if`, `else`, `for`, `foreach`, `while`, `do`, `using`, and `lock` body must be wrapped in curly
braces `{}`, even when the body is a single statement and the braces are syntactically unnecessary.**

```csharp
// Correct
if (rule is not null)
{
    result.Add(rule);
}

// WRONG - do not do this
if (rule is not null)
    result.Add(rule);

// WRONG - do not do this either
if (rule is not null) result.Add(rule);
```

The reason is defensive: braceless bodies are where a later "just add one more line" edit silently
changes the control flow. Requiring braces removes the whole class of bug.

This rule is about *statement* bodies. Expression-bodied members (`=>`) are allowed for members whose
whole body is a single expression, such as properties, simple overrides, and one-line delegating methods.
Use a block body as soon as the member needs more than one expression, or when the expression no longer
reads cleanly on one or two lines.

`using` declarations (`using var reader = ...;`) are allowed and preferred over nested `using` blocks.
They are not braceless bodies.

---

## 3. Naming

Naming in this codebase is deliberately explicit about an identifier's *scope and lifetime*. The Java
conventions express this with prefixes on fields and parameters. C# keeps the same goal, distinguishing
fields from parameters and locals at the point of use, but follows .NET conventions, because C# makes
parameter names and member casing part of the public API.

### 3.1 Summary table

| Kind of identifier | Convention | Example |
| --- | --- | --- |
| Namespace | `PascalCase`, dot-separated | `Mapepire.Data`, `Mapepire.EntityFrameworkCore.Query.Internal` |
| Class / struct / record / enum / delegate | `PascalCase` | `MapepireConnection`, `ServerResult` |
| Interface | `I` + `PascalCase` | `IHistoryRepository` |
| Method, property, event | `PascalCase` | `ExecuteReader()`, `FetchSize` |
| Private / internal instance field | `_camelCase` | `_session`, `_settings` |
| Private / internal static field (non-constant) | `s_camelCase` | `s_uniqueIdCounter`, `s_pools` |
| Constant (`const`, or `static readonly` immutable value) | `PascalCase` | `DefaultStringLength`, `Instance` |
| Method parameter | `camelCase` (no prefix) | `connectionString`, `fetchSize` |
| Local variable | `camelCase` (no prefix) | `rowsAffected`, `columnType` |
| Enum member | `PascalCase` | `Open`, `ReadCommitted` |
| Generic type parameter | `T`, or `T` + `PascalCase` | `T`, `TContext`, `TResult` |

Public and protected state is exposed through **properties**, never through fields. That is why only
private and internal fields appear in the table.

Existing public names that predate these conventions keep their names. These include the lowercase
`io.github.mapepire_ibmi` namespace, the `UPPER_SNAKE_CASE` members of `QueryState`, `TransactionEndType`
and `ExplainType`, the `Option.NAMING`-style constants, and `JDBCOptions`. When you add a member to one
of those existing types, match that type's existing style, so a single type is not split between two
conventions.

### 3.2 Instance fields: `_` prefix

> **Differs from Java:** Java uses `m_camelCase` for instance fields. C# uses `_camelCase`, the
> convention of the .NET runtime and of the C# tooling defaults.

**Private and internal instance fields must begin with `_`, followed by a camelCased name.**

```csharp
public sealed class MapepireCommand : DbCommand
{
    private readonly MapepireParameterCollection _parameters = new();
    private string _commandText = string.Empty;
    private MapepireConnection? _connection;
}
```

A field backing a property is named after the property: `_commandText` backs `CommandText`.

### 3.3 Static fields: `s_` prefix (non-constant) or `PascalCase` (constant)

**Non-constant static fields must begin with `s_`, followed by a camelCased name. Constants are
`PascalCase`.**

> **Differs from Java:** Java names constants `UPPER_SNAKE_CASE`. C# names them `PascalCase`, as the .NET
> design guidelines and every framework API do. The `s_` prefix is kept unchanged.

```csharp
internal const int DefaultStringLength = 4000;                              // constant
public static readonly MapepireFactory Instance = new();                    // immutable singleton: constant-like

private static readonly ConcurrentDictionary<string, Queue> s_pools = new(); // shared and mutable
private static int s_uniqueIdCounter;                                       // shared and mutable
```

The distinction is meaningful: `s_` is a warning label. It tells the reader that this value is shared
process-wide *and* can change. In a library used by multi-threaded applications (ASP.NET Core,
connection pools, EF Core context pools), that is something to think carefully about. `PascalCase`
carries no such warning because a constant is safe to read from anywhere.

A `static readonly` reference to a *mutable object*, such as a `Dictionary`, a `List`, a
`ConcurrentDictionary`, or a lock object, is **not** a constant: `readonly` freezes the reference, not
the contents. Name it `s_camelCase`. A `static readonly` reference to a genuinely immutable value, such
as a `string`, a `Regex`, an immutable type mapping, or a singleton with no mutable state, is
constant-like and takes `PascalCase`.

`s_` means **static**. Do not use it on an instance field.

### 3.4 Method parameters: no prefix

> **Differs from Java:** Java prefixes parameters with `_`. **C# parameters are plain `camelCase`.**
> Parameter names are part of a C# method's public contract: callers can use them as named arguments
> (`Open(timeout: 5)`), they appear in IntelliSense and generated documentation, and renaming one breaks
> callers who use named arguments. A `_` prefix would leak into every one of those places.

The Java rule's practical payoff still holds: **a parameter can never shadow a field**, because fields
carry the `_` prefix and parameters do not. `_connection` and `connection` are visibly different
identifiers, so assignments read unambiguously and there is no need for `this.` (see §4.1).

```csharp
public MapepireCommand(string? commandText, MapepireConnection? connection = null)
{
    CommandText = commandText;
    _connection = connection;
}
```

Prefer descriptive parameter names, such as `isolationLevel` over `level` and `fetchSize` over `n`.
Some older code uses abbreviated names; that is not a pattern to copy in new code.

When overriding a method or implementing an interface, keep the parameter names of the base declaration.
Renaming them produces a compiler warning and confuses callers who use named arguments.

### 3.5 Local variables

Local variables take **no prefix** and are plain `camelCase`. The absence of a prefix on a local, unlike a
field, means "this lives and dies inside this method."

```csharp
var rewritten = SqlParameterRewriter.Rewrite(_commandText, name => _parameters.IndexOf(name) >= 0);
var fetchSize = FetchSize ?? connection.FetchSize;
```

### 3.6 Types and members

> **Differs from Java:** Java methods are `camelCase`. **C# methods, properties, and events are
> `PascalCase`** without exception, including private helpers. This is enforced by the expectations of
> every .NET API, analyzer, and reader.

Types are `PascalCase`. Interfaces start with `I`. Attribute classes end in `Attribute`, exception classes
in `Exception`, and EF Core / ADO.NET provider classes follow the established `Mapepire` + base-name
pattern (`MapepireConnection`, `MapepireQuerySqlGenerator`).

### 3.7 Modifier order, acronyms, and other declaration details

**Modifier order follows the .NET default (the order the IDE0036 analyzer expects):**

```
public private protected internal file static extern new virtual abstract sealed override readonly unsafe required volatile async
```

So `private static readonly`, never `static private readonly`. An unusual order makes a declaration snag
the eye for no reason. Always state the access modifier explicitly, including `private`, even though
it is the default.

**Acronyms and initialisms are cased as ordinary words:** `SqlJob`, not `SQLJob`; `JsonValue`, not
`JSONValue`; `GetDbJob()`, not `GetDBJob()`; `Db2Type`. Shouted acronyms become unreadable the moment
two of them collide (`ParseJSONURLID`). This matches the Java rule. Existing names such as `JDBCOptions`
are public API: leave them alone, and do not add new ones.

**Boolean properties and methods begin with `Is`, `Has`, or `Can`:** `IsClosed`, `HasRows`, `IsAutoCommit`.
A boolean named `Results` reads at the call site like a collection.

**Async methods end in `Async`** and return `Task`, `Task<T>`, `ValueTask`, or `ValueTask<T>`.

**Generic type parameters are `T`, or `T` followed by a descriptive name:** `T`, `TContext`, `TResult`.
This differs from the Java rule (single letters only) because the `T` prefix already makes a type
parameter impossible to confuse with a class name, and it is the .NET convention.

**`override` is required by the compiler** and plays the role of Java's `@Override`. Two related rules
the compiler does not enforce:

- Do not hide an inherited member with `new` unless the hiding is deliberate. The established ADO.NET
  pattern of re-declaring `Connection`, `Parameters`, and `Transaction` with stronger types is
  deliberate; give such members their own XML docs saying what they return.
- Members that implement an interface or override a documented base member use `/// <inheritdoc />`
  instead of copying the base documentation (§6.1).

**Seal classes that are not designed for inheritance.** Making a class inheritable is a public-API
commitment. For example, the ADO.NET types in `Mapepire.Data` are `sealed`.

---

## 4. Language Usage

### 4.1 Use `this.` only when it is needed

**Do not prefix member accesses with `this.` unless it is actually required.**

Because fields carry a `_` prefix and parameters do not, shadowing is impossible, so `this.` is almost
always noise:

```csharp
// Correct
public void SetRules(List<AuthRule> rules)
{
    _rules = rules;
}

// WRONG - the "this." adds nothing
public void SetRules(List<AuthRule> rules)
{
    this._rules = rules;
}
```

`this` on its own is fine and often necessary: returning `this` for chaining, passing `this` to a
collaborator, calling an extension method on the current instance, or chaining constructors with
`: this(...)`.

Much of the ported SDK code uses `this.` on every member access. Do not strip it from lines you are not
otherwise changing (§5); new code should not add it.

### 4.2 Prefer `readonly` and immutability wherever possible

> **Differs from Java:** C# has no `final` locals or parameters, so the Java rule "mark every variable
> `final`" maps onto fields, properties, and types instead.

- **Mark fields `readonly` whenever they are not reassigned after construction.** A class whose fields
  are all `readonly` is far easier to reason about when shared across threads.
- Use `const` for compile-time constants, and `static readonly` for values computed once at startup.
- Prefer get-only or `init`-only properties over settable ones where the value is fixed at construction.
  Prefer `record` or `readonly record struct` for small immutable data carriers, such as `ServerColumn`.
- Do not reassign parameters. Introduce a new, well-named local instead.

**`var`:** use `var` when the type is obvious from the right-hand side (a `new` expression, a cast, a
well-named factory method) or is long and uninteresting. Spell out the type when it carries information
the reader cannot see at a glance, such as the result of a method whose return type is not evident from
its name, or a numeric type where width matters.

### 4.3 Null checks and comparisons

> **Differs from Java:** the Java conventions require constant-first ("Yoda") comparisons, mainly so
> that `"literal".equals(value)` cannot throw a `NullPointerException`. That reasoning does not carry over
> to C#: `==` on strings is a null-safe value comparison, and `if (x = null)` does not compile. Yoda
> comparisons are **not** used in C#.

- **Test for null with `is null` and `is not null`**, not `== null` and `!= null`. `is` cannot be
  intercepted by an overloaded `==` operator, so it always means what it says.
- **Compare strings with an explicit `StringComparison`** whenever the comparison is anything other than
  exact, case-sensitive equality:
  `string.Equals(name, other, StringComparison.OrdinalIgnoreCase)`. The static `string.Equals` is also
  null-safe on both sides, which is the C# equivalent of the Java rule's goal.
- Use **ordinal** comparisons for identifiers, keywords, SQL text, and protocol values. Culture-sensitive
  comparison of identifiers is a classic source of bugs, for example with the Turkish dotless "i".

### 4.4 `using` directives

- There are no wildcard imports in C#, but the Java rule's intent applies: a file's `using` directives
  should show what it depends on. **Do not add unused `using` directives**, and remove ones your change
  made unused.
- Order directives as: `System.*`, then `Microsoft.*`, then third-party packages, then project
  namespaces. Within a group, order them alphabetically.
- New files use **file-scoped namespaces** (`namespace Mapepire.Data;`).
- `global using` directives live only in the project's `GlobalUsings.cs` (§1.3).
- Use a `using` alias, or a fully qualified name at the point of use, to resolve a name conflict between
  namespaces. Do not drop one of the namespaces.

### 4.5 Exception handling

- **Throw the most specific exception type that fits:**
  - `ArgumentException`, `ArgumentNullException`, or `ArgumentOutOfRangeException` for bad arguments.
    Use their `ThrowIf...` helpers.
  - `InvalidOperationException` for a call made in the wrong state.
  - `NotSupportedException` for features the provider deliberately does not implement.
  - `MapepireException` or `SqlException` for errors reported by Db2 or the Mapepire server, with the
    SQLCODE and SQLSTATE preserved.

  Much of the ported SDK throws bare `System.Exception`. Do not copy that in new code.
- **Rethrow with `throw;`**, never `throw ex;`, which discards the original stack trace. When you wrap an
  exception, pass the original as `innerException`.
- When synchronous code waits on a task (`.Wait()`, `.Result`), unwrap the `AggregateException` before
  reporting it, so callers see the real error.
- **Never swallow an exception silently.** Rethrow it, convert it into a meaningful error, or, where
  ignoring it is genuinely correct, say why in a comment directly inside the `catch`. The legitimate
  cases are cleanup paths (`Dispose` must not throw) and best-effort operations whose failure does not
  matter:
  ```csharp
  catch (MapepireException)
  {
      // The server may already have closed the cursor; there is nothing left to release.
  }
  ```
- Catch the narrowest exception type that is actually thrown, and use `when` filters rather than catching
  and rethrowing. Catching `Exception` is acceptable at a boundary whose whole purpose is to convert any
  failure into an error result, but not as a general habit.
- Library code does not write to `Console`. Report errors through exceptions; where diagnostics are
  needed, use the caller's logging (for example EF Core's `IDiagnosticsLogger`), not a private output
  channel.

### 4.6 Never log credentials or secrets

**No password, passphrase, authentication token, or credential-bearing string may ever be written to a
log, a trace, the console, or an exception message.**

This is the one rule in this document that is a security requirement rather than a style preference.
This library holds IBM i user credentials in memory and sends them to the server. A password that reaches
a log file, a CI transcript, or an exception message that is shown to an end user has leaked.

Concretely, never pass any of the following to a logging or tracing call or into an exception message:

- A user password or passphrase, in any form, including inside a wrapped exception's message.
- A **connection string**, or the output of `MapepireConnectionStringBuilder.ToString()` or
  `ConnectionString`, which contain the password. Log the server, port, and user profile if you need
  them, never the whole string.
- A `DaemonServer` instance, a JDBC property map, or a CA or private-key value.
- An HTTP `Authorization` header or the websocket request that carries one.

Where an object might carry a secret, log the specific fields you actually need rather than the object.
`ToString()` on a connection or configuration object is exactly how credentials end up in logs by
accident. When in doubt, log less.

The same applies to tests and tooling: test credentials come from `unitTestSettings.json` or `MAPEPIRE_*`
environment variables and must not be echoed to test output. Never enable EF Core's
`EnableSensitiveDataLogging()` in committed code outside of a test that specifically needs it.

### 4.7 Dispose resources with `using`

**Anything that implements `IDisposable` or `IAsyncDisposable` is acquired in a `using` declaration or
`using` block (`await using` for async disposal).** This includes connections, commands, data readers,
transactions, streams, sockets, and `JsonDocument`.

```csharp
using var command = connection.CreateCommand();
using var reader = command.ExecuteReader();
```

`using` disposes the resource on every exit path, including the ones you did not think about. A manual
`Dispose()` in a `finally` block is easy to write *almost* correctly. A leaked reader or connection in
this library holds an open cursor or a server job on the IBM i. That is a slow leak that only shows up
under load on a customer's system.

The exception is a resource whose lifetime deliberately outlives the method that creates it. That is a
legitimate pattern here:

- a `MapepireDataReader` keeps a server cursor open across `sqlmore` requests;
- `ServerSession` objects are returned to the `SessionPool` rather than closed;
- `SqlJob` holds its websocket for the life of the job.

In each case the owning object is responsible for releasing the resource, and the code should make it
obvious where that happens.

> **AI assistants:** resource lifetime in this codebase is not always local, and it is not always
> apparent from the method you are editing. Wrapping a resource in `using` can dispose a cursor, session,
> or socket that a *later* call still needs. That turns working code into an intermittent runtime failure
> that no compiler will catch. **Never convert existing resource handling to `using` as an unprompted
> cleanup.** Where you do change how a resource is acquired, disposed, pooled, or returned, including in
> new code, call it out explicitly in your summary and in the pull request, and flag it as requiring
> human review. Do not report such a change as routine.

### 4.8 Mutable static state needs a thread-safety review

**Every non-`readonly` static field, and every static field that refers to a mutable object, requires
human review of how it is accessed from multiple threads.**

This library runs inside applications that use it from many threads at once, such as web servers,
connection pools, and EF Core context pools. A static flag written by one thread and read by another is
not guaranteed to be seen by the reader unless the field is `volatile`, accessed through `Interlocked` or
`Volatile`, or protected by a `lock`. A static collection that is mutated without synchronization can be
corrupted. The failure is silent and intermittent: nothing in the code or the compiler says so.

Existing examples handle this explicitly: `SqlJob.GetNewUniqueId` and `Query.GlobalQueryList` take a
`lock`, and `SessionPool` uses concurrent collections. Whether new state needs the same depends on who
writes it and when. A value set once during type initialization does not; a flag toggled at runtime
almost certainly does. That is a judgement about the program's threading, not something visible from the
declaration.

> **AI assistants:** when you add mutable static state, or touch the declaration or the writes of
> existing static state, call it out explicitly in your summary and in the pull request as needing human
> review for thread safety. Do not add or remove `volatile`, `lock`, or `Interlocked` on existing code as
> an unprompted cleanup, and do not report such a change as routine.

### 4.9 Nullable reference types

*This rule has no counterpart in the Java conventions.*

- Annotate honestly. A reference that can be null is declared `T?`; one that cannot is declared `T`.
- Validate public arguments (`ArgumentNullException.ThrowIfNull(connection)`) rather than relying on
  annotations alone. Callers may have nullable warnings switched off.
- Avoid the null-forgiving operator `!`. Use it only where an invariant guarantees the value is non-null
  and that invariant is obvious from the surrounding code, or explained in a short comment. Never use it
  merely to silence a warning you have not understood.
- Do not introduce new nullable warnings. A change that adds warnings to a project that builds without
  them is incomplete.

### 4.10 Asynchronous code

*This rule has no counterpart in the Java conventions.*

The SDK transport (`SqlJob`) is synchronous: it blocks on its websocket. The ADO.NET async overrides
therefore move the synchronous work onto the thread pool rather than pretending to be truly
asynchronous. Within that constraint:

- Do not add new sync-over-async calls (`.Wait()`, `.Result`, `.GetAwaiter().GetResult()`) outside the
  SDK transport layer. They risk deadlocks and thread-pool starvation in the applications that host this
  library.
- `await` in library code uses `ConfigureAwait(false)`.
- Methods that accept a `CancellationToken` take it as the last parameter, defaulting to `default`,
  and honour it: at minimum, check it before starting work.
- Never use `async void` except for event handlers.

---

## 5. Member Ordering

**Where practical, order the members of a type as follows** (the order used by StyleCop rules SA1201 and
SA1202, and by most .NET codebases):

1. Constant and static fields
2. Instance fields
3. Constructors (and finalizer, if any)
4. Delegates and events
5. Properties and indexers
6. Methods
7. Nested types

Within each category, order by accessibility: `public`, then `internal`, then `protected`, then
`private`. Within that, place static members before instance members.

**This rule is a preference, not a hard requirement.** Two caveats:

- Do not reorder the members of an existing file as a drive-by change. It produces an enormous diff
  that buries the actual change and destroys `git blame` history for the file. Order members when you
  are creating a new file, or when you are already substantially rewriting one. The line-ending repair
  described in §1.1 is the only exception to this "no drive-by changes" principle. A large
  whitespace-only diff is an acceptable price for getting a file onto LF.
- Where a deliberate grouping communicates more than the standard order does, keep the deliberate
  grouping. Examples are a set of related overloads, a field placed next to the property it backs, or
  the ADO.NET pattern of a strongly typed property next to the `protected override` it shadows.

---

## 6. Documentation and Comments

### 6.1 XML documentation comments

> **Differs from Java:** Javadoc becomes C# **XML documentation comments** (`///`). Much of the ported
> SDK uses Javadoc-style `/** ... */` blocks with `@param` tags. C# tooling ignores those, so they never
> reach IntelliSense or the generated documentation. Use `///` in new code. Convert an existing block
> only when you are already changing that member.

Write XML docs on public API: public types, and public or protected members whose purpose is not obvious
from the signature. `Mapepire.Data` and `Mapepire.EntityFrameworkCore` set `GenerateDocumentationFile`, so these docs ship to
consumers in IntelliSense.

**Any method whose body is longer than 4 lines must have appropriate documentation**, regardless of its
visibility, private helpers included. A method of that size is doing enough that a reader benefits from a
statement of its purpose, its parameters, what it returns, and what it throws, without having to read
the body. "Appropriate" means the documentation describes intent and contract; it does not narrate the
implementation line by line.

Use the standard tags:

```csharp
/// <summary>
/// Verifies that the given user is permitted to connect from the given address.
/// </summary>
/// <param name="user">The user profile requesting the connection.</param>
/// <param name="ipAddress">The client IP address the connection originated from.</param>
/// <exception cref="MapepireException">
/// The connection is refused by a security rule, or the rules cannot be read.
/// </exception>
public void Verify(string user, string ipAddress)
{
    // ...
}
```

- Use `<see cref="..."/>` to refer to types and members, so references survive renames.
- Use `<inheritdoc />` on overrides and interface implementations instead of copying the base
  documentation. Write a fresh `<summary>` only where the behaviour differs from the base contract in a
  way callers need to know, such as "Not supported; does nothing".
- Use `<remarks>` for behaviour that is specific to Db2 for i or the Mapepire server and that would
  surprise someone who knows another ADO.NET or EF Core provider.

Do not write documentation that merely restates the signature (`/// <summary>Gets the name.</summary>`
on `Name`). Blank filler documentation is worse than none: it costs a reader time and tells them nothing.

### 6.2 Inline comments

Explain **why**, not **what**. The code already says what it does.

```csharp
// Db2 for i rejects EXISTS directly inside CASE WHEN (SQL0104), but accepts it inside a scalar subquery.
```

Mark known deficiencies with `// TODO:` and a short description of what needs doing.

Do **not** leave commentary about the editing process in the code. Markers like `// FIXED:`, `// FIX:`,
`// NEW:`, `// CHANGED:`, or checkmark emoji describe a moment in the repository's history, not the
code, and they are stale as soon as they are committed. That information belongs in the commit message.

Code ported from another project keeps its license header and states where it came from in a comment at
the top of the file.

---

## 7. Wire Protocol and Public API Compatibility

### 7.1 The Mapepire wire protocol

The JSON exchanged with mapepire-server is a **public contract** shared with the server and with every
other client library: mapepire-js, mapepire-python, mapepire-java, and others. This client does not own
it.

- **JSON property names are protocol, not identifiers.** The names in `[JsonPropertyName("...")]`
  attributes and in hand-written `Utf8JsonWriter` code (`"has_results"`, `"cont_id"`, `"sql_rc"`) must
  match the server exactly. They stay `lower_snake_case` even though the C# property they map to is
  `PascalCase`. Never "tidy" one to match the C# name.
- **Request `type` values** (`"sql"`, `"prepare_sql_execute"`, `"sqlmore"`, `"sqlclose"`, `"cl"`, and so
  on) are protocol. Do not change them.
- **Be tolerant when reading.** The server evolves by adding fields. Parsing code must ignore fields it
  does not recognize and cope with optional fields being absent. It must not fail because a reply
  contains something new.
- New protocol features are added by the server first. The client follows the server's field names and
  types; it never invents its own.

### 7.2 The library's public API

This repository also publishes .NET APIs: the SDK, the ADO.NET provider, and the EF Core provider.
Applications compile against them, so treat the following as **breaking changes**, not refactors:

- Renaming, removing, or changing the type of a public or protected type, member, or **parameter**.
  Parameter renames break callers who use named arguments.
- Changing the meaning or default of an existing behaviour, such as a connection-string keyword's
  default, a type mapping, or the generated SQL for an existing translation.
- Removing or renaming a connection-string keyword or one of its synonyms.

Additive changes are the way to evolve the API: add a new member or overload alongside the old one, keep
the old one working, and mark it `[Obsolete("Use X instead.")]` if it should eventually go.

If a breaking change is genuinely necessary, it is a deliberate, versioned decision recorded in the pull
request description. It is never a drive-by rename, and never something an AI assistant decides on its
own while cleaning up nearby code.

---

## 8. Formatting Tooling

**There is no formatter profile or `.editorconfig` checked into this repository yet.** Until there is,
the rules in sections 1 and 2 are checked by hand, using the commands in §9.1.

- **Do not run `dotnet format`, or your editor's "Format Document", across files you are not otherwise
  changing.** With no project configuration, those tools apply their own defaults, and a whole-file
  reformat in a pull request is very hard to review.
- Configure your editor to format only your selection, and match the surrounding code by hand.
- VS Code users: `.vscode/settings.json` sets the default solution (`io.sln`) for the C# extension. Add
  `"files.eol": "\n"` to your user settings for this workspace (§1.1).

> **TODO:** add an `.editorconfig` that encodes these conventions (LF, 4 spaces, Allman braces, required
> braces, naming rules) so the IDE and `dotnet format` can check them automatically. Introduce it in its
> own pull request, without reformatting existing files in the same change.

---

## 9. Checklist for AI Coding Assistants

Before completing any C# edit in this repository, verify all of the following:

**Layout and encoding**

- [ ] File uses **LF** line endings only, with no `\r` anywhere. If the file arrived with CRLF, the whole
      file was converted (§1.1) and the conversion is called out in the summary.
- [ ] Indentation is **4 spaces**; the lines you wrote or changed contain no tab characters.
- [ ] No line exceeds **320 characters** without good reason.
- [ ] No generated file was hand-edited (§1.4).
- [ ] Library code still targets and runs on `net8.0`; no `TargetFramework` was changed as a side
      effect (§1.3).

**Naming**

- [ ] Types, methods, properties, and events are `PascalCase`; interfaces start with `I`.
- [ ] Private and internal instance fields are `_camelCase`.
- [ ] Non-constant static fields, including static references to mutable objects, are `s_camelCase`.
- [ ] Constants are `PascalCase`, except when you add to an existing type that uses another style (§3.1).
- [ ] Parameters and locals are `camelCase` with no prefix.
- [ ] Modifiers are in the .NET default order (`private static readonly`, not `static private
      readonly`), and access modifiers are explicit.
- [ ] Acronyms are cased as words (`SqlJob`, not `SQLJob`); boolean members start with `Is`/`Has`/`Can`;
      async methods end in `Async`.
- [ ] No existing public type, member, or parameter was renamed (§7.2).

**Language usage**

- [ ] `this.` appears only where it is actually required.
- [ ] Every field that is never reassigned after construction is `readonly`.
- [ ] Every `if` / `else` / `for` / `foreach` / `while` / `do` / `using` / `lock` body is wrapped in `{}`,
      including single-statement bodies.
- [ ] Null checks use `is null` / `is not null`; non-trivial string comparisons state a
      `StringComparison`.
- [ ] No unused `using` directives were added or left behind.
- [ ] Every method with a body longer than 4 lines has appropriate XML documentation (§6.1).
- [ ] No exception is swallowed without a comment explaining why. New code throws specific exception
      types, not `System.Exception`, and rethrows with `throw;`.
- [ ] No new nullable warnings were introduced, and `!` is not used to silence one.
- [ ] No new sync-over-async (`.Wait()`, `.Result`) was added outside the SDK transport.
- [ ] **No password, connection string, `DaemonServer`, credentialed property map, or `Authorization`
      header is passed to any logging, tracing, console, or exception-message call** (§4.6).
- [ ] Any change to how a resource is acquired, disposed, pooled, or returned is explicitly flagged for
      human review. No existing resource handling was converted to `using` as an unprompted cleanup (§4.7).
- [ ] Any mutable static state that was added or touched is flagged for human review of thread safety
      (§4.8).

**Protocol and scope**

- [ ] No JSON property name, request `type` value, or field type or meaning was renamed, removed, or
      changed (§7.1).
- [ ] No process-narration comments (`// FIXED:`, `// NEW:`) were added.
- [ ] The change is scoped to what was asked: no drive-by reformatting, member reordering, or `this.`
      removal in untouched code, with the single exception of line-ending repair.

### 9.1 Verification commands

Several of the items above are mechanically checkable. Run these before reporting a C# change complete,
and report what they actually returned rather than asserting compliance:

```bash
# Must produce no output. There are no exempt files.
git ls-files --eol | grep -E 'i/(crlf|mixed)|w/(crlf|mixed)'   # CRLF line endings, committed or in the working tree
git ls-files '*.cs' | xargs grep -lP '.{321}'                  # lines over 320 characters

# Lists files containing tabs. A few existing files do; your change must not add any new tabs.
git ls-files '*.cs' | xargs grep -lP '\t'

# Must succeed with no new warnings.
dotnet build io.sln

# Must pass. Live tests are reported as skipped when no Mapepire server is configured.
dotnet test Mapepire.EntityFrameworkCore.Tests
```

On Git Bash for Windows, run `export LC_ALL=C.UTF-8` first; otherwise `grep -P` refuses to run. Do not
check line endings with `grep` there: it reads files in text mode and silently drops the `\r`, so CRLF files
look clean. `git ls-files --eol` reports what is actually stored in the index (`i/`) and the working tree
(`w/`).

Scripts that rewrite files must write `\n` explicitly. For example, Python's `open(path, 'w')` writes CRLF
on Windows unless you pass `newline='\n'`.

The equivalent in PowerShell:

```powershell
git ls-files '*.cs' '*.csproj' '*.sln' '*.json' '*.md' | Where-Object {
    $t = [System.IO.File]::ReadAllText((Resolve-Path $_))
    ($t -match "`r") -or (($t -split "`n" | Where-Object { $_.Length -gt 320 }).Count -gt 0)
}
```

`UnitTests` needs a live Mapepire server (see `UnitTests/unitTestSettings.json`). Run it as well when your
change touches the SDK under `io/`.

A non-empty result from the CRLF or line-length checks, or a new tab in a line you changed, is a defect in
the change, not a pre-existing condition to be excused. See §1.1 on line endings and §2.1 on tabs.
