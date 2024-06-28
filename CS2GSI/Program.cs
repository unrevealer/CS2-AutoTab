// See https://aka.ms/new-console-template for more information
using CS2GSI;

Console.WriteLine("Hello, World!");

GSI g = new GSI("http://localhost:3001");

if (!g.start())
{
    Console.WriteLine("start failed");
    Environment.Exit(0);
}
Console.WriteLine("Listening...");

g.ReadInput();

