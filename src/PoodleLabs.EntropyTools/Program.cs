// <copyright file="Program.cs" company="Poodle Labs LTD">
// Copyright (c) Poodle Labs LTD 2023. All rights reserved.
// </copyright>

namespace PoodleLabs.EntropyTools;

using System.Diagnostics.CodeAnalysis;

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
        WriteLinesInColour(ConsoleColor.Yellow, "Welcome to Poodle Labs Entropy Tools!", string.Empty);
        while (true)
        {
            var entropyBits = ReadFromConsole<ushort>(
            TryParseEntropyBits,
            ConsoleColor.Cyan,
            "Please enter the desired number of bits of entropy:");

            var possibilities = ReadFromConsole<ushort>(
                TryParsePossibilities,
                ConsoleColor.Cyan,
                "Please enter the number of possibilities your random generation method provides.",
                $"For example 2 for a coinflip, 6 for a six-sided dice (max {byte.MaxValue + 1}):");

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
                    "Would you like to apply a Von Neumann filter? You will need twice the inputs, but will erase any bias!");
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
                    $"Given {inputs} inputs, you will achieve  {Math.Log2(actualMax):0.000} bits of entropy.");
            }
            else
            {
                WriteLinesInColour(
                    ConsoleColor.White,
                    $"It will take {(vonNeumann ? $"approximately {inputs * 2}" : inputs)} inputs to reach your desired level of security.");
            }

            if (!Confirm())
            {
                continue;
            }

            WriteLinesInColour(
                ConsoleColor.Yellow,
                $"Generating {entropyBits} bits of entropy with an input size of {possibilities} {(vonNeumann ? "with" : "without")} a Von Neumann filter.",
                string.Empty);

            if (!Confirm())
            {
                continue;
            }

            return;
        }
    }

    /// <summary>
    /// Prompt a user to input a value of the provided type into the console until a value is successfully parsed.
    /// </summary>
    /// <typeparam name="T">The type to parse.</typeparam>
    /// <param name="parser">The parsing/validation method.</param>
    /// <param name="promptColour">The colour to write the prompt in.</param>
    /// <param name="promptLines">The prompt to write before input.</param>
    /// <returns>The input value.</returns>
    private static T ReadFromConsole<T>(
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
                    if (!Confirm())
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
