# Codex and CI Setup

## Enable ordinary CI first

1. In the GitHub repository, open **Settings** > **Actions** > **General**.
2. Enable Actions and permit the official GitHub actions used by `.github/workflows/ci.yml`.
3. Push the workflow to GitHub. It runs for pushes to `main` and `coding/v0.1`, and for pull requests.
4. To run it manually, open **Actions** > **TidyMind CI** > **Run workflow**, select the branch, and choose **Run workflow**.

Confirm a successful run by checking that the Restore, Build, and Test steps all pass. Download the `test-results` artifact from the run when test-result details are needed. Do not enable Codex automation until this ordinary CI workflow is green.

## Add the OpenAI API key when automation is introduced

The repository does not yet contain a Codex automation workflow. When one is intentionally added, open **Settings** > **Secrets and variables** > **Actions**, create a repository secret named `OPENAI_API_KEY`, and paste the API key as its value. Never commit the key, place it in workflow YAML, or print it in logs.

OpenAI API usage is billed separately from ChatGPT subscriptions. Ensure the API organization and project have an appropriate billing method and usage limits before enabling automation.

## Future permissions for Codex-created branches and pull requests

When a future automation is authorized to create branches or pull requests, configure only the permissions it needs: `contents: write` to create and push a branch, and `pull-requests: write` to open or update a pull request. Review the repository's Actions workflow permissions and any branch-protection rules before granting them. The current CI workflow intentionally uses only `contents: read`.

## Disable automation immediately

If automation must be stopped, open **Actions**, choose the relevant workflow, use the workflow menu to select **Disable workflow**, and remove or rotate `OPENAI_API_KEY` under **Settings** > **Secrets and variables** > **Actions**. For an immediate repository-wide stop, disable GitHub Actions in **Settings** > **Actions** > **General**. Re-enable only after the cause is understood and ordinary CI is green.
