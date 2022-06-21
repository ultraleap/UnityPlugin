## Pull Request Templates

Switch template by going to preview and clicking the link - note it not work if you've made any changes to the description.

- [default.md](?expand=1) - for contributions to stable packages.
- [lightweight.md](?expand=1&template=lightweight.md) - for contributions to pre-release/preview packages use the lightweight merge request template. The quality threshold for reviewing is lower - it prioritizes a lean review process.
- [release.md](?expand=1&template=release.md) - for release merge requests.

**You are currently using: release.md**

Note: these links work by overwriting query parameters of the current url. If the current url contains any you may want to amend the url with `&template=name.md` instead of using the link. See [query parameter docs](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/using-query-parameters-to-create-a-pull-request) for more information.

## Release Tasks

_These tasks are for the merge request creator to tick off when creating a merge request._

- [ ] Run through the pre-release steps on [Confluence](https://ultrahaptics.atlassian.net/wiki/spaces/SV/pages/3665625233). The rest of the process continues after this merge request is merged.
- [ ] Check any relevant CHANGELOG files have been updated.
- [ ] Ensure documentation requirements are met e.g., public API is commented.
- [ ] Ensure package.json files are updated with new package versions and any changes dependency versions.
- [ ] If this is a major release, action any `Obsolete` items and other breaking considerations.
- [ ] Check that additional release tasks for each MR contributing to this release have been considered.

### Additional Release Tasks

_This task list should be populated from MRs contributing to this release. Can include functionality tests and regression tests such as tests for integration of multiple features as well as any other tasks that should be performed during the release._

- [ ] 

## JIRA Release

_Link to the Jira release for this version. See the [releases page](https://ultrahaptics.atlassian.net/projects/UNITY?selectedItem=com.atlassian.jira.jira-projects-plugin:release-page)._
