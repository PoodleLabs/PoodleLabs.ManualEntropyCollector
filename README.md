# PoodleLabs.EntropyTools

A simple console app for collecting entropy from various sources, like coins and dice, with support for Von Neumann filtering on power-of-2 input methods (eg: coinflip, D4, D8).

## How to use it.

Install the .NET SDK, run build.ps1 on Windows (or Linux/Mac with PowerShell installed) or just manually run `dotnet publish` as the ps1 script does, then run the output.

You'll need some source of entropy. A real-life source is recommended; something like a coin to flip, or a dice to roll. Powers of 2 are by far the best, especially when applying a Von Neumann filter. It's more work, but you'll get much better entropy.
