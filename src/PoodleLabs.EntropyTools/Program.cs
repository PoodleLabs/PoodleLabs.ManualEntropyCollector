// <copyright file="Program.cs" company="Poodle Labs LTD">
// Copyright (c) Poodle Labs LTD 2023. All rights reserved.
// </copyright>

namespace PoodleLabs.EntropyTools;

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text;

/// <summary>
/// The entrypoint containing class for the program.
/// </summary>
internal static class Program
{
    /// <summary>
    /// A delegate for parsing console input to arbitrary types.
    /// </summary>
    /// <typeparam name="T">The type to parse to.</typeparam>
    /// <param name="text">The string content to parse.</param>
    /// <param name="value">The parsed value, if parsing succeeds.</param>
    /// <returns>A flag indicating whether parsing succeeded; <paramref name="value"/> should not be accessed if false.</returns>
    private delegate bool TryParse<T>(string text, [MaybeNullWhen(false)] out T value);

    /// <summary>
    /// The entrypoint for the program.
    /// </summary>
    private static void Main()
    {
        while (true)
        {
            WriteLinesInColour(ConsoleColor.Yellow, "Welcome to Poodle Labs Entropy Tools!", string.Empty);
            var entropyBits = ReadFromConsole<ushort>(
                false,
                TryParseEntropyBits,
                ConsoleColor.Cyan,
                "Please enter the desired number of bits of entropy:");

            var possibilities = ReadFromConsole<ushort>(
                false,
                TryParsePossibilities,
                ConsoleColor.Cyan,
                "Please enter the number of possibilities your random generation method provides.",
                $"For example 2 for a coinflip, 6 for a six-sided dice (max {byte.MaxValue + 1}), 256 for a random number generator which outputs individual bytes:");

            bool pow2;
            int inputs;
            bool vonNeumann;
            var targetMax = Math.Pow(2, entropyBits);
            if ((possibilities & (possibilities - 1)) == 0)
            {
                pow2 = true;
                inputs = (int)Math.Ceiling(Math.Log(targetMax, possibilities));
                WriteLinesInColour(
                    ConsoleColor.Yellow,
                    "Your random generation method has a possibility number which is a power of 2!",
                    "Would you like to apply a Von Neumann filter? You will need roughly twice the inputs, but will erase any bias from your entropy source if each round is truly independent.",
                    "This is a good idea for coinflips, dice rolls, and similar methods of entropy generation, but is a bad idea for most computer-generated entropy sources.");
                vonNeumann = Confirm();
                Console.WriteLine();
            }
            else
            {
                inputs = (int)Math.Log(targetMax, possibilities);
                pow2 = vonNeumann = false;
            }

            var actualMax = Math.Pow(possibilities, inputs);
            var missed = targetMax - actualMax;
            if (actualMax < targetMax)
            {
                WriteLinesInColour(
                    ConsoleColor.Yellow,
                    $"The maximum value for the target number of bits of entropy is {targetMax}.",
                    $"The maximum possible value for your input method is {actualMax}.",
                    $"Given {inputs} inputs, you will achieve {Math.Log2(actualMax):0.000} bits of entropy.");
                if (!Confirm())
                {
                    continue;
                }

                Console.WriteLine();
            }

            WriteLinesInColour(
                ConsoleColor.Yellow,
                $"Generating {entropyBits} bits of entropy with an input size of {possibilities} {(vonNeumann ? "with" : "without")} a Von Neumann filter with {(vonNeumann ? $"approximately {inputs * 2}" : inputs)} inputs.",
                string.Empty);
            if (!Confirm())
            {
                continue;
            }

            WriteLinesInColour(
                ConsoleColor.Magenta,
                string.Empty,
                $"You will now be prompted for inputs; you will need to enter a number from 0-{possibilities - 1} (inclusive).",
                "If you are flipping a coin, 0 is heads, 1 is tails. If you're rolling a dice labelled 1-6, 1 is 0, 2 is 1, etc.",
                string.Empty);

            bool TryParseRound(string text, out ushort value) => ushort.TryParse(text, out value) && value < possibilities;
            ushort PromptForRound(string prompt) => ReadFromConsole<ushort>(true, TryParseRound, ConsoleColor.Cyan, prompt);
            var result = new BigInteger(0UL);
            var actualInputs = 0;
            var discarded = 0;
            if (pow2)
            {
                var bitsPerRound = (int)Math.Log2(possibilities);
                for (var i = 0; i < entropyBits;)
                {
                    actualInputs++;
                    var bitStart = i + 1;
                    var bitEnd = Math.Min(i + bitsPerRound, entropyBits);
                    var bitsDescriptor = bitStart == bitEnd ? $"bit {bitStart}" : $"bits {bitStart}-{bitEnd}";
                    if (vonNeumann)
                    {
                        var input1 = PromptForRound($"Enter input 1 for {bitsDescriptor}:");
                        var input2 = PromptForRound($"Enter input 2 for {bitsDescriptor}:");
                        for (var j = bitsPerRound - 1; j >= 0 && i < entropyBits; --j)
                        {
                            var b1 = GetBitAtIndex(input1, j);
                            if (b1 == GetBitAtIndex(input2, j))
                            {
                                WriteLinesInColour(ConsoleColor.Yellow, $"Discarding bit {j + 1}");
                                ++discarded;
                            }
                            else
                            {
                                result = AddTrailingDigit(result, 2, b1);
                                ++i;
                            }
                        }
                    }
                    else
                    {
                        var input = PromptForRound($"Enter input for {bitsDescriptor}:");
                        for (var j = bitsPerRound - 1; j >= 0 && i < entropyBits; --j, ++i)
                        {
                            result = AddTrailingDigit(result, 2, GetBitAtIndex(input, j));
                        }
                    }
                }
            }
            else
            {
                for (var i = 0; i < inputs; ++i)
                {
                    result = AddTrailingDigit(
                        result,
                        possibilities,
                        PromptForRound($"Enter input {i + 1} of {inputs}:"));
                }
            }

            var one = new BigInteger(1UL);
            var binaryStringBuilder = new StringBuilder(entropyBits);
            for (var i = 0; i < entropyBits; ++i)
            {
                _ = binaryStringBuilder.Append(((one << (entropyBits - i - 1)) & result) == 0 ? '0' : '1');
            }

            var binaryString = binaryStringBuilder.ToString();
            WriteLinesInColour(ConsoleColor.Green, "Completed entropy collection:");
            WriteInColour(ConsoleColor.Green, "BASE-10: ");
            WriteLinesInColour(ConsoleColor.Cyan, result.ToString(CultureInfo.CurrentCulture));
            WriteInColour(ConsoleColor.Green, "BASE-16: ");
            WriteLinesInColour(ConsoleColor.Cyan, result.ToString("X", CultureInfo.CurrentCulture));
            WriteInColour(ConsoleColor.Green, "BASE-2: ");
            WriteLinesInColour(ConsoleColor.Cyan, binaryString, string.Empty);

            if (discarded > 0)
            {
                WriteLinesInColour(ConsoleColor.White, $"Discard rate of {(float)discarded / actualInputs}; 0.5 indicates low bias of your input method (with many iterations).");
            }

            WriteLinesInColour(ConsoleColor.White, $"Ratio of {(float)binaryString.Count(c => c == '1') / binaryString.Length}; at high entropy bit counts, this should converge toward 0.5.", string.Empty);
        }
    }

    /// <summary>
    /// Prompt a user to input a value of the provided type into the console until a value is successfully parsed.
    /// </summary>
    /// <typeparam name="T">The type to parse.</typeparam>
    /// <param name="skipConfirm">A value indicating whether confirmations should be skipped.</param>
    /// <param name="parser">The parsing/validation method.</param>
    /// <param name="promptColour">The colour to write the prompt in.</param>
    /// <param name="promptLines">The prompt to write before input.</param>
    /// <returns>The input value.</returns>
    private static T ReadFromConsole<T>(
        bool skipConfirm,
        TryParse<T> parser,
        ConsoleColor promptColour,
        params string[] promptLines)
    {
        while (true)
        {
            WriteLinesInColour(promptColour, promptLines);
            var input = Console.ReadLine() ?? string.Empty;
            Console.WriteLine();

            if (parser(input, out var value))
            {
                WriteInColour(ConsoleColor.Green, "You entered: ");
                WriteInColour(ConsoleColor.Cyan, value!.ToString() ?? string.Empty);
                WriteLinesInColour(ConsoleColor.Green, ".");
                try
                {
                    if (!skipConfirm && !Confirm())
                    {
                        continue;
                    }

                    return value;
                }
                finally
                {
                    Console.WriteLine();
                }
            }

            WriteLinesInColour(ConsoleColor.Red, "Invalid input.", string.Empty);
        }
    }

    /// <summary>
    /// Write the provided strings in the specified colour.
    /// </summary>
    /// <param name="colour">The colour to write in.</param>
    /// <param name="parts">The strings to write.</param>
    private static void WriteInColour(ConsoleColor colour, params string[] parts)
    {
        using var handle = new ColourHandle(colour);
        foreach (var part in parts)
        {
            Console.Write(part);
        }
    }

    /// <summary>
    /// Ask the user to confirm whether they should continue.
    /// </summary>
    /// <returns>A value indicating whether the user confirmed.</returns>
    private static bool Confirm()
    {
        WriteLinesInColour(ConsoleColor.Green, "Press Y to confirm, or N to reject.");
        while (true)
        {
            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.Y:
                    return true;
                case ConsoleKey.N:
                    return false;
                default:
                    continue;
            }
        }
    }

    /// <summary>
    /// Get the bit at the specified index in the provided bitfield.
    /// </summary>
    /// <param name="field">The bitfield to extract a bit from.</param>
    /// <param name="index">The index of the bit.</param>
    /// <returns>1 or 0 depending on the value of the specified bit.</returns>
    private static int GetBitAtIndex(ushort field, int index) => ((1 << index) & field) is not 0 ? 1 : 0;

    /// <summary>
    /// Add a trailing digit to the provided big integer for the specified numeric base.
    /// </summary>
    /// <param name="left">The current big integer.</param>
    /// <param name="base">The base for digits.</param>
    /// <param name="right">The digit to add to the end.</param>
    /// <returns>A big integer with the specified digit added to the end.</returns>
    private static BigInteger AddTrailingDigit(BigInteger left, ushort @base, int right) => (left * @base) + right;

    /// <summary>
    /// Parse a ushort larger than 0.
    /// </summary>
    /// <param name="text">The text input to parse.</param>
    /// <param name="value">The returned value if parsing was successful.</param>
    /// <returns>A flag indicating whether parsing was successful.</returns>
    private static bool TryParseEntropyBits(string text, out ushort value)
        => ushort.TryParse(text, out value) && value > 0;

    /// <summary>
    /// Parse a byte larger than 1.
    /// </summary>
    /// <param name="text">The text input to parse.</param>
    /// <param name="value">The returned value if parsing was successful.</param>
    /// <returns>A flag indicating whether parsing was successful.</returns>
    private static bool TryParsePossibilities(string text, out ushort value)
        => ushort.TryParse(text, out value) && value > 1 && value < 257;

    /// <summary>
    /// Write the provided strings each on their own line in the specified colour.
    /// </summary>
    /// <param name="colour">The colour to write in.</param>
    /// <param name="lines">The strings to write on their own lines.</param>
    private static void WriteLinesInColour(ConsoleColor colour, params string[] lines)
    {
        using var handle = new ColourHandle(colour);
        foreach (var line in lines)
        {
            Console.WriteLine(line);
        }
    }

    /// <summary>
    /// A simple ref struct to write in a specific colour with a 'using' statement.
    /// </summary>
    private readonly ref struct ColourHandle
    {
        /// <summary>
        /// The original console foreground colour.
        /// </summary>
        private readonly ConsoleColor _original;

        /// <summary>
        /// Initialises a new instance of the <see cref="ColourHandle"/> struct.
        /// </summary>
        /// <param name="colour">The colour to write in.</param>
        public ColourHandle(ConsoleColor colour)
        {
            _original = Console.ForegroundColor;
            Console.ForegroundColor = colour;
        }

        /// <summary>
        /// Resets the console colour.
        /// </summary>
        public void Dispose() => Console.ForegroundColor = _original;
    }
}
