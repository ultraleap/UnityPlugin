## Pre-Release Tasks

### Pre-release
- [ ] Update package.json versions and dependencies
- [ ] Update client libraries (.dll, .so, .dylib)
- [ ] Update changelog date, release version & client library versions
- [ ] Update Readme if required
- [ ] Apply CI formatting patch
- [ ] Ensure documentation requirements are met e.g., public API is commented.
- [ ] If this is a major release, action any `Obsolete` items and other breaking considerations.
- [ ] Run the required tests for the release

### After Approval
- [ ] Tag the branch (e.g. com.ultraleap.tracking/5.9.0 and com.ultraleap.tracking.preview/5.9.0)
- [ ] Merge to main
- [ ] Create a GitHub Release with the CI artifact for the .unitypackage
- [ ] Resolve any public GitHub issues
- [ ] Merge main to develop (also add [NEXT] - unreleased to changelog)
- [ ] Announce release on various platforms
- [ ] Publish any accompanying docs
