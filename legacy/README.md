# Eski prototipler (derlenmez)

Bu klasör, WPF uygulamasından önceki iki prototipi geçmiş referansı olarak saklar. Hiçbiri ana çözümün
parçası değildir, derlenmez ve WPF uygulaması tarafından kullanılmaz.

| Klasör | İçerik | Çalıştırma |
|---|---|---|
| `WinFormsPrototype/` | İlk WinForms sürümü; Factory/Strategy desen örnekleri (`Patterns/`) | `dotnet run --project legacy/WinFormsPrototype` |
| `WebPrototype/` | Tarayıcı tabanlı demo (`localStorage`) | `legacy/WebPrototype/index.html` dosyasını bir statik sunucu ile açın (ES modülleri `file://` altında çalışmaz) |

Yeni geliştirme yalnızca kök dizindeki WPF uygulamasında yapılmalıdır.
