# Container builds with GitHub Packages

The Docker workflow restores `Marshall.Authentication.Google` from the
`Justinmarkmarshall` GitHub NuGet registry and all other packages from nuget.org.
The exact package source mapping prevents this package being resolved from a
different public feed.

## One-time package access

On GitHub, open the `Marshall.Authentication.Google` package settings. Under
**Manage Actions access**, add **Justinmarkmarshall/FinancialPlanner.Blazor**
with **Read** access. This lets this repository's `GITHUB_TOKEN` restore the package.
The workflow already grants `packages: write` because it also pushes the image
to GHCR. A personal access token is not needed for the workflow.

Without this package access grant, restore fails even though the workflow has
package permissions. See [GitHub package access documentation](https://docs.github.com/en/packages/learn-github-packages/configuring-a-packages-access-control-and-visibility).

## Workflow behavior

- Pushes to `main` or `master` build and publish a SHA-tagged image. The default
  branch also publishes `latest`.
- Same-repository pull requests targeting `main`, `master`, or `develop` build
  without publishing.
- Manual runs build without publishing.
- Fork pull requests and Dependabot runs skip the image job because it requires
  authenticated package access. Do not switch to `pull_request_target` to build
  untrusted pull request code with credentials.

The token is passed through a BuildKit secret mount during `dotnet restore`.
The NuGet configuration contains no credentials, and `dotnet publish` uses
`--no-restore`. Only published application files are copied into the runtime
image. `.dockerignore` excludes local build outputs and common secret/data files.

## Local Docker build (PowerShell)

Set `GITHUB_NUGET_TOKEN` in your local environment to a GitHub classic token with
`read:packages` and access to the package, then run:

```powershell
docker build --secret id=github_nuget_token,env=GITHUB_NUGET_TOKEN -t financialplanner:local .
```

Do not put the token in the Dockerfile, build arguments, or committed config.
See [Docker secret mounts in GitHub Actions](https://docs.docker.com/build/ci/github-actions/secrets/).
