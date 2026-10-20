# Spotify Toolbox
A set of tools to manage your Spotify library from the CLI.

## 🚀 Running
In order to run the CLI you need to first install .NET 10 or newer which you can obtain
from [here](https://dotnet.microsoft.com/en-us/download). Once you have it you can clone the repo and run:

```bash
dotnet run --project SpotifyToolbox.CLI -- --help
```

This should print out the available commands, but since you haven't added an access token yet it'll print out an error.

## 🔗 Connecting with Spotify
The first thing you'll need is a token to access Spotify resources, so head over to the [Spotify Developers website](https://developer.spotify.com/dashboard/) and create an account if you don't have any.

Once in the Dashboard create an app and then click on `Edit settings` and add a new redirect URI under the **Redirect URIs** section. By default the CLI uses http://127.0.0.1:8000/callback, so add that one unless you specifically went to the code to change it.

Once you've done that you can copy the `Client ID` that the dashboard should show you and re-run the command from before but adding this token to an environment variable:

```bash
SPOTIFY_CLIENT_ID={your client ID} dotnet run --project SpotifyToolbox.CLI -- --help
```

This should now print out the list of commands, which will only be `login` and `logout`. Running `login` should open a browser to help you authenticate your account to the CLI. After login you should see the available commands when running help.

Your credentials are stored in the user's config folder (`~/.config/spotify-toolbox` on Linux and macOS, `%APPDATA%\spotify-toolbox` on Windows). If you need to authenticate again, run `logout` and then `login`.

## 🔨 Available commands
Right now with the toolbox you can do the following things:

### Interactive cleanup of albums and artists
By running:
```bash
dotnet run --project SpotifyToolbox.CLI -- cleanup
```

The CLI will fetch all the albums in your library and show them grouped by artist:

```
  ▸ [ ] Beck (1)
  ▸ [X] Björk (2)
> ▾ [-] Radiohead (1/3)
      [ ] The Bends (1995)
      [X] OK Computer (1997)
      [ ] Kid A (2000)
  ▸ [ ] Thom Yorke (1)
```

- <kbd>↑</kbd>/<kbd>↓</kbd> (and <kbd>PgUp</kbd>/<kbd>PgDn</kbd>/<kbd>Home</kbd>/<kbd>End</kbd>) move the cursor. Typing a letter jumps to the next artist starting with it.
- <kbd>Space</kbd> on an artist selects (or deselects) all of their albums.
- <kbd>→</kbd> expands an artist so you can pick specific albums with <kbd>Space</kbd>, and <kbd>←</kbd> collapses it back keeping the selection. Artists with only some albums selected show as `[-]`.
- <kbd>Enter</kbd> confirms the selection and <kbd>Esc</kbd> cancels without changing anything.

If any of the artists you follow won't have any albums left in your library after the cleanup, the CLI will ask you whether you want to keep following them, unfollow all of them or choose which ones to unfollow. You'll then see a summary of all the changes and nothing will be removed until you confirm it. Add `--dry-run` to go through the whole process without changing anything.

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
If you're missing any feature or want to add a new command feel free to open an issue or implement it yourself! The project uses C# (run the tests with `dotnet test`) and the [.NET Spotify API wrapper](https://johnnycrazy.github.io/SpotifyAPI-NET/) which makes it incredibly easy to interact with the Spotify API and [Spectre Console](https://spectreconsole.net/) to show all those cool animations and colors on the terminal.