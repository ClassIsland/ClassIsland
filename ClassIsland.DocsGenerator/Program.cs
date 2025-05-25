// See https://aka.ms/new-console-template for more information

using XmlDocMarkdown.Core;

namespace ClassIsland.DocsGenerator;

static class Program
{
    public static int Main(string[] args)
    {
        Console.WriteLine("ClassIsland Document Generator");
        return XmlDocMarkdownApp.Run(args);
    }
}