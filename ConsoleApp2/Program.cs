using Microsoft.AspNetCore.Components.Rendering;
using System;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var test = new Index();
            test.SetParametersAsync(Microsoft.AspNetCore.Components.ParameterView.Empty);
        }
    }
}
