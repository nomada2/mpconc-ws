﻿using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Functional;
using System.Threading.Tasks;
using static System.Console;

namespace ReactiveAgent.Agents
{
    // TODO : 5.5
    // implement a function or a design/approach to compose Agents
    // rewrite the example composing the Agent as pipeline
    // be sure to have completed    TODO : 5.4 first
    public class WordCountAgentsExample
    {
        static IAgent<string> printer = Agent.Start((string msg) =>
            WriteLine($"{msg} on thread {Thread.CurrentThread.ManagedThreadId}"));

        //   Producer/consumer using TPL Dataflow
        static IAgent<string> reader =
            Agent.Start(async (string filePath) =>
            {
                await printer.Send("reader received message");
                var lines = await File.ReadAllLinesAsync(filePath);
                lines.ForEach(async line => await parser.Send(line));
            });

        static char[] punctuation = Enumerable.Range(0, 256).Select(c => (char)c).Where(c => Char.IsWhiteSpace(c) || Char.IsPunctuation(c)).ToArray();

        static IAgent<string> parser =
            Agent.Start(async (string line) =>
            {
                await printer.Send("parser received message");
                line.Split(punctuation).ForEach(async word =>
                    await counter.Send(word.ToUpper()));
            });

        static IReplyAgent<string, (string, int)> counter =
            Agent.Start(ImmutableDictionary<string, int>.Empty,
                (ImmutableDictionary<string, int> state, string word) =>
                {
                    printer.Post("counter received message");
                    int count;
                    if (state.TryGetValue(word, out count))
                        return state.Add(word, count++);
                    else return state.Add(word, 1);
                }, (state, word) => (state, (word, state[word])));

        public async Task Run()
        {
            foreach (var filePath in Directory.EnumerateFiles(@".\Data", "*.txt"))
            {
                if (System.IO.File.Exists(filePath))
                    reader.Post(filePath);
            }

            Console.ReadLine();

            var wordCount_This = await counter.Ask("this");
            var wordCount_Wind = await counter.Ask("wind");
        }
    }
}
