# PoodleLabs.ManualEntropyCollector

**ARCHIVED; see https://github.com/PoodleLabs/PoodleLabs.BST**

A simple console app for collecting entropy from various sources, like coins and dice, with support for Von Neumann filtering on power-of-2 input methods (eg: coinflip, D4, D8).

## How to use it.

Install the .NET SDK, run build.ps1 on Windows (or Linux/Mac with PowerShell installed) or just manually run `dotnet publish` as the ps1 script does, then run the output.

You'll need some source of entropy. A real-life source is recommended; something like a coin to flip, or a dice to roll. Powers of 2 are by far the best, especially when applying a Von Neumann filter. It's more work, but you'll get much better entropy.

## The Von Neumann Filter

This filter applies Von Neumann entropy skew correction to erase any bias independent inputs have. This means if you have a loded dice, as long as each roll is independent, you'll still wind up with true, unbiased entropy. If your dice/coin/whatever is (roughly) unbiased, you can expect to double the number of inputs you'll need for a given amount of entropy. The more biased it is, the more inputs you'll need. Using this filter with CSPRNGs is highly inadvisable, but if you have a coin, a D4, D8, or a rarer D16, you can get unbiased, secure random number generation using this tool even if you don't trust your coin/dice.

**Note:** while you can use many coins/dice/etc, roll/flip them all at once, and enter them individually without a Von Neumann filter, you should use a *single* coin or dice when using a Von Neumann filter.

## Note on Usage

This is a small proof of concept, a version of which be rolled into a larger suite of airgap-oriented security tools which can be run on Windows, MacOS, Linux, and (the recommended usage) as a UEFI application. If you've suggestions for improvements to this tool, or for inclusions in such a suite, add an issue, or, better yet, open a PR.
