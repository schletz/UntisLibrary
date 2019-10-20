# .NET Core 3 Bibliothek für den Zugriff auf WebUntis

## Erstellen der DLL für die Verwendung in eigenen Projekten
Die DLL kann mit folgendem Befehl kompiliert werden. Danach kann der Ordner *netcoreapp3.0/publish*
in das eigene Projekt kopiert werden und auf die DLL Datei verwiesen werden.
```
.../UntisLibrary.Api> dotnet publish -c Release
```

## Starten des Testprogrammes
Das Testprogramm in *UntisLibrary.Testapp* kann in 3 Varianten direkt ausgeführt werden:
```
.../UntisLibrary.Testapp> dotnet run
.../UntisLibrary.Testapp> dotnet run (username)
.../UntisLibrary.Testapp> dotnet run (username) (password)
```

