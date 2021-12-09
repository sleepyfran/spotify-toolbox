# Spotify Toolbox
A set of tools to manage your Spotify library from the CLI.

## 🚀 Running
In order to run the CLI you need to first install the latest version of .NET which you can obtain
from [here](https://dotnet.microsoft.com/en-us/download). Once you have it you can clone the repo and run:

```bash
dotnet run --project SpotifyToolbox.CLI -- --help
```

This should print out the available commands, but since you haven't added an access token yet it'll print out an error.

## 🔗 Connecting with Spotify
The first thing you'll need is a token to access Spotify resources, so head over to the [Spotify Developers website](https://developer.spotify.com/dashboard/) and create an account if you don't have any.

Once in the Dashboard create an app and then click on `Edit settings` and add a new redirect URI under the **Redirect URIs** section. By default the CLI uses http://localhost:8000/callback, so add that one unless you specifically went to the code to change it.

Once you've done that you can copy the `Client ID` that the dashboard should show you and re-run the command from before but adding this token to an environment variable:

```bash
SPOTIFY_CLIENT_ID={your client ID} dotnet run --project SpotifyToolbox.CLI -- --help
```

This should now print out the list of commands, which will only be `login`, and if you run it it should open a browser to help you authenticate your account to the CLI. After login you should see the available commands when running help.

## 🔨 Available commands
Right now with the toolbox you can do the following things:

### Removing all albums
By running:
```bash
dotnet run --project SpotifyToolbox.CLI -- remove albums
```

The CLI will prompt you for confirmation and then proceed to remove **all** the albums on your collection. If instead you run:

```bash
dotnet run --project SpotifyToolbox.CLI -- remove albums --dry-run
```

It should show you instead the list of albums that the app would remove (which should be your entire library).

### Unfollowing all artists
By running:
```bash
dotnet run --project SpotifyToolbox.CLI -- remove artists
```

The CLI will prompt you for confirmation and then proceed to unfollow **all** the artists on your collection. If instead you run:

```bash
dotnet run --project SpotifyToolbox.CLI -- remove artists --dry-run
```

It should show you instead the list of artists that the app would unfollow (which should be all of them).

## 😄 Contributions
If you're missing any feature or want to add a new command feel free to open an issue or implement it yourself! The project uses C# and the [.NET Spotify API wrapper](https://johnnycrazy.github.io/SpotifyAPI-NET/) which makes it incredibly easy to interact with the Spotify API and [Spectre Console](https://spectreconsole.net/) to show all those cool animations and colors on the terminal.