## Pre-Release Tasks

[Pre-release](https://ultrahaptics.atlassian.net/wiki/spaces/SV/pages/3665625233/Unity+Plugin+Development+Release+Process#Pre-release-Steps)
- [ ] Update JIRA release version number. _See the [releases page](https://ultrahaptics.atlassian.net/projects/UNITY?selectedItem=com.atlassian.jira.jira-projects-plugin:release-page)._
- [ ] Update package.json versions and dependencies
- [ ] Update client libraries (.dll, .so, .dylib)
- [ ] Update changelog date, release version & client library versions
- [ ] Update Readme if required
- [ ] Apply CI formatting patch
- [ ] Ensure documentation requirements are met e.g., public API is commented.
- [ ] If this is a major release, action any `Obsolete` items and other breaking considerations.

- [ ] Run the required tests for the release
- [ ] Complete the request to release on Cognidox

[After Approval](https://ultrahaptics.atlassian.net/wiki/spaces/SV/pages/3665625233/Unity+Plugin+Development+Release+Process#Release-Steps)
- [ ] Tag the branch (e.g. com.ultraleap.tracking/5.9.0 and com.ultraleap.tracking.preview/5.9.0)
- [ ] Merge to main
- [ ] Crate Github Release with CI artefact for .unitypackage
- [ ] Resolve any public GitHub issues
- [ ] Release version on JIRA and make a new Unity Plugin NEXT version
- [ ] Merge main to develop (also add [NEXT] - unreleased to changelog)
- [ ] Announce release on various platforms
- [ ] Publish any accompanying docs

## Pull Request Templates

Switch template by going to preview and clicking the link - note it not work if you've made any changes to the description.

- [default.md](?expand=1) - for contributions to stable packages.
- [release.md](?expand=1&template=release.md) - for release merge requests.

**You are currently using: release.md**

Note: these links work by overwriting query parameters of the current url. If the current url contains any you may want to amend the url with `&template=name.md` instead of using the link. See [query parameter docs](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/using-query-parameters-to-create-a-pull-request) for more information.
