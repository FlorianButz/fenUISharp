# FenUI

`FenUI` is the base class which helps setup the app.

## Public Methods

`static void Init()` initializes all window features and loads the default resources. It is recommended to call this before anyhing else in your program.

`static void Shutdown()` is for cleaning up at the end of every program.

`static void SetupAppModel(string appModelId)` sets up an app model for the current process. This is required for some Windows related features.

## Public Fields

`static Version FenUIVersion { get; }` is the current version of FenUISharp.

`static bool HasBeenInitialized { get; }` indicates whether the class has been initialized already or not.