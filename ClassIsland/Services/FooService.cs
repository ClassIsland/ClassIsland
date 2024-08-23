using System;
using ClassIsland.Shared.IPC.Abstractions.Services;

namespace ClassIsland.Services;

public class FooService : IFooService
{
    public void DoSomething()
    {
        Console.WriteLine("Foobar");
    }
}