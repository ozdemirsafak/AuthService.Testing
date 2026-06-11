# ?? AuthService.Testing — Unit & Integration Tests

JWT tabanl? kullan?c? kay?t/giri? servisinin xUnit ile yaz?lm?? Unit ve Integration testleri.

## Teknolojiler
- .NET 9 Web API
- xUnit + FluentAssertions + Moq
- WebApplicationFactory (Integration testler)
- EF Core InMemory
- BCrypt ?ifre hashleme + JWT

## Testler (21 adet)
- **14 Unit Test:** Servis katman? — kay?t validasyonlar?, duplicate email, ?ifre hashleme, JWT üretimi, case-insensitive email
- **7 Integration Test:** Gerçek HTTP istekleriyle endpoint testleri — status kodlar?, tam kay?t?login ak???

## Çal??t?rma
```bash
dotnet test
```

## Not
Testler sayesinde **Türkçe locale bug'?** yakaland?: `ToLower()` Türkçe sistemlerde `I ? ?` (noktas?z) dönü?ümü yapt??? için email e?le?mesi bozuluyordu. Çözüm: `ToLowerInvariant()`.