## Pull Request Templates

Switch template by going to preview and clicking the link - note it not work if you've made any changes to the description.

- [default.md](?expand=1) - for contributions to stable packages.
- [lightweight.md](?expand=1&template=lightweight.md) - for contributions to pre-release/preview packages use the lightweight merge request template. The quality threshold for reviewing is lower - it prioritizes a lean review process.
- [release.md](?expand=1&template=release.md) - for release merge requests.

**You are currently using: default.md**

Note: these links work by overwriting query parameters of the current url. If the current url contains any you may want to amend the url with `&template=name.md` instead of using the link. See [query parameter docs](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/using-query-parameters-to-create-a-pull-request) for more information.

## Summary

_Summary of the purpose of this merge request._

## Contributor Tasks

_These tasks are for the merge request creator to tick off when creating a merge request._

- [ ] Pair review with a member of the QA team.
- [ ] Add any release testing considerations to the MR for the next release.
- [ ] Check any relevant CHANGELOG files have been updated.
- [ ] Ensure documentation requirements are met e.g., public API is commented.
- [ ] Consider any licensing/other legal implications for this MR e.g. notices required by any new libraries.
- [ ] Add any relevant labels such as `breaking` to this MR.
- [ ] If this MR closes a Jira issue, make sure the fix version on the JIRA issue is set to the correct one.

## Reviewer Tasks

_Add any instructions or tasks for the reviewer such as specific test considerations before this can be merged._

[Use emojis in review threads to communicate intent and help contributors.](https://github.com/ultraleap/UnityPlugin/blob/develop/CONTRIBUTING.md#review-threads)

- [ ] Code reviewed.
- [ ] Non-code assets e.g. Unity assets/scenes reviewed.
- [ ] Documentation has been reviewed. Includes checking documentation requirements are met and not missing e.g., public API is commented.
- [ ] Checked and agree with release testing considerations added to MR for the next release.

## Closes JIRA Issue

_If this MR closes any JIRA issues list them below in the form `Closes PROJECT-#`_