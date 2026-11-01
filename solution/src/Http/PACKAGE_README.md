<br/><br/>
<p align="center">
  <image src="https://raw.githubusercontent.com/osjimenez/Fuxion/refs/heads/main/solution/.config/full_logo.svg" alt="Fuxion logo" width="300px">
</p>
<br/><br/>

Install command:
```bash
dotnet add package Fuxion.Http
```

## Native AOT

From .NET 11 on, compatible with trimming and native AOT: give `FuxionHttpClientOptions.JsonOptions` options that chain
a context of your own (your types and the responses you read) and `FuxionJsonContext`.

**🔨 Work in progress..**