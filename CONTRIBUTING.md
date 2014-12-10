##<a id="how-to-contribute">How to contribute?</a>
Your contributions to SOVND are very welcome.
If you find a bug, please raise it as an issue.
Even better, fix it and send a pull request.
If you like to help out with existing bugs and feature requests just check out the list of [issues](https://github.com/GeorgeHahn/SOVND/issues) and grab and fix one.
If you don't write C# or aren't running Windows, you might consider writing a client for your platform of choice. See [SPEC.md](SPEC.md) for client implementation details.
If you don't program, you can help fund this project by tipping us with [GratiPay](https://gratipay.com/GeorgeHahn/).

###<a id="contribution-guideline">Contribution guideline</a>
To contribute, fork the repo, preferably create a local branch to avoid conflicts with other activities, fix an issue, and send a PR if the build and tests all pass.

Pull requests are code reviewed. Here is a checklist you should tick through before submitting a pull request:

 - Implementation is clean
 - Code adheres to the existing coding standards; e.g. no curlies for one-line blocks, no redundant empty lines between methods or code blocks, spaces rather than tabs, etc.
 - No ReSharper warnings
 - If the code is copied from StackOverflow (or a blog or OSS) full disclosure is included. That includes required license files and/or file headers explaining where the code came from with proper attribution
 - Your PR is (re)based on top of the latest commits (more info below)
 - Link to the issue(s) you're fixing from your PR description. Use `fixes #<the issue number>`
 - Readme is updated if you change an existing feature or add a new one

Please rebase your code on top of the latest commits.
Before working on your fork make sure you pull the latest so you work on top of the latest commits to avoid merge conflicts.
Also before sending the pull request please rebase your code as there is a chance there have been new commits pushed after you pulled last.
Please refer to [this guide](https://gist.github.com/jbenet/ee6c9ac48068889b0912#the-workflow) if you're new to git.
