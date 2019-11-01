# Symbolic Regex Matcher (SRM)

SRM is a high-performance regular expression matching engine with predictable performance characteristics. SRM implements a fully compatible subset of the .NET regex language, which mainly omits non-regular features. It provides comparable throughput to popular native libraries, such as RE2, with a pure C# codebase.

SRM combines advanced symbolic reasoning with a regex derivatives based matching approach. For an overview of the theory behind SRM please see:
[Olli Saarikivi, Margus Veanes, Tiki Wan, Eric Xu. *Symbolic Regex Matcher*. In TACAS 2019.](https://doi.org/10.1007/978-3-030-17462-0_24)

# Usage

The API mostly follows that of `System.Text.RegularExpressions`:

```
using Microsoft.Automata;
...
string input = "Hello World!";
var regex = new Regex(".l*.");
bool hasLs = regex.IsMatch(input); // True
var matches = regex.Matches(input); // list of Match structs for "ello" and "rld"
```

# Building and running tests

The library is built and tested with [.NET Core 2.2](https://dotnet.microsoft.com/download/dotnet-core/2.2). To build the project and run the tests run:

```
dotnet build
dotnet test
```

# Regenerate unicode character tables

SRM uses unicode character tables recovered from the .NET runtime. To regenerate them for a new version of the runtime run:

```
cd unicode_table_gen
dotnet run ../srm/unicode
```