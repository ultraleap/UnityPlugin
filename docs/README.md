# Unity Modules Documentation

## (Re)building the Docs

1. Install [Python][python]. Then install the Python dependencies via `pip`:
```
pip install sphinx breathe
```

2. You'll also need [Doxygen][doxygen] installed appropriately for your platform, the latest version should suffice. (Latest used: `1.8.13`.)

- For macOS, one easy option is [homebrew][homebrew]:
  ```
  brew install doxygen
  ```
- For Windows, you may have to download an installer. [Chocolatey][choco] would be a good option, as it would a one-liner. As of writing, this command **installs an older version of Doxygen**, `1.8.11`, so your mileage may vary:
  ```
  choco install doxygen
  ```

3. (*Optional.*) If you're using [VS Code][vscode], you may also want to install the [Python][pythonVSCodeExtension] and [reStructuredText][rstVSCodeExtension] extensions and set them up. The live preview functionality is very helpful when authoring anything new.

4. Make any changes to configuration or `content` files as you like.

5. Run the Makefile or make.bat for macOS/Windows.
- macOS:
  ```
  make html
  ```
- Windows:
  ```
  ./make.bat html
  ```

6. (Not yet done) The `_build` folder is in a .gitignore so it can be a working doc generation space. When you're done, clean everything out and move the output so that `index.html` is right in the `docs` folder, which is specifically for Github Pages support.
// TODO: This really can and should be a single-line command.

[python]: https://www.python.org/
[vscode]: https://code.visualstudio.com/
[pythonVSCodeExtension]: https://marketplace.visualstudio.com/items?itemName=ms-python.python
[rstVSCodeExtension]: https://marketplace.visualstudio.com/items?itemName=lextudio.restructuredtext
[doxygen]: http://www.stack.nl/~dimitri/doxygen/
[homebrew]: https://brew.sh/
[choco]: https://chocolatey.org/