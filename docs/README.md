# UnityModules Documentation

Open `index.html`. Or continue reading if you're trying to build new docs.

## Rebuilding the docs

First, install Doxygen 1.8.11. **Later versions of Doxygen will not work correctly.**

You can delete the `html` folder if it already exists, as doxygen will regenerate it when run. This cleans out any old files.

Run `doxygen` in this folder (`/docs`).

Documentation will be generated into a folder called `html`. The `index.html` file will redirect to the `html/index.html` file, which is the *actual* landing page.

## Publishing the docs

Replace the contents of the branch `gh-pages` with the contents of the doxygen output, which is configured to dump its output into an `html` folder in this directory. (GitHub Pages is configured to statically serve the contents of the root directory in the `gh-pages` branch.)

## Optional: Dot adds cool diagrams

On macOS,
```
brew install graphviz
```
will get you the `dot` command in your path, which allows Doxygen to generate
much, much more interesting visual diagrams depicting the inter-relation of
classes.