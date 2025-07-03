# Reflexor

![Reflexor](https://raw.githubusercontent.com/sadicangel/reflexor/refs/heads/main/icon.png)

_Reflexor_ is a C# source generator that produces **mutable proxy types** for immutable `record` and `class` types.

Whether you're working with immutable data models and need editable views, UI bindings, or draft mutations, **Reflexor** lets you work with a mutable representation of your readonly types — without writing boilerplate code.

---

## ✨ Features

- 🧬 Generates mutable `struct` proxies for immutable `record` and `class` types.
- 🛠️ Keeps property names, types, and structure aligned.
- 💨 Designed for performance and minimal memory overhead.
- ✅ Supports `init`-only and positional parameters.

---

## 🚀 Example

### Original Record

```csharp
[GenerateProxy]
public record User(string UserName, int Age);
```

### Generated Proxy

```csharp
public partial struct UserProxy
{
    public string UserName { get; set; }
    public int Age { get; set; }
}
// Property implementations omitted for brevity
```

```csharp
var user = new User("John_Doe", 29);
// user.UserName = "Jane_Doe"; error CS8852

var proxy = new UserProxy(user);
proxy.UserName = "Jane_Doe";

Console.WriteLine(user.UserName);
// "Jane_Doe"
```

---

## 🧩 Usage

### 1. Install the NuGet Package

> Coming soon to [NuGet](https://www.nuget.org/)

```bash
dotnet add package Reflexor
```

### 2. Annotate Your Types

Just add a `[GenerateProxy]` attribute to any immutable `record` or `class`.

```csharp
[GenerateProxy]
public record Order(int Id, DateTime Date, string Customer);
```

That's it — Reflexor will generate a proxy type automatically!

---

## 🛡️ Why Use Proxies?

Working with immutable models is great for safety, but tricky for things like:

- UI data binding (e.g., Blazor, WPF, MAUI)
- Form editing and validation
- Intermediate "draft" state management
- Patch or delta generation

_Reflexor bridges this gap by creating mutable counterparts on the fly._

---

## 👷‍♂️ Contributing

Contributions are welcome! If you have any ideas, suggestions, or bug reports, please open an issue or submit a pull request.

---

## 📄 License


This project is licensed under the MIT License. See the [LICENSE](https://github.com/sadicangel/reflexor/blob/main/LICENSE) file for details.
