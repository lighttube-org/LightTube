# Contributing to LightTube

Before submitting a pull request, please make sure that it abides by the following rules:

## Proper base

Make sure that your pull request is based off of the latest `master` branch.

## PR titles / content

Please make sure to follow the default PR template (available in [.github/pull_request_template.md](https://github.com/kuylar/lighttube/blob/master/.github/pull_request_template.md)).

Also, make sure that your PR titles describe what it does in a brief but understandable way.

## Commenting

Make sure to document any public methods in your code using XMLDocs.

## Code style

Make sure that your code follows the [Microsoft's C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions), with the following exceptions:

- Using tabs over 4 spaces
- Not using the `s_` or `t_` prefixes for private static fields
- Implicitly typed variables are strongly discouraged

# Non-code changes

Make sure to prefix your PR titles with `[skip ci]` if it's not a code change (for example, documentation articles)
