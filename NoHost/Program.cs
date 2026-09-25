namespace NoHost;
using InteractiveEditor;
using InteractiveEditor.Model;
using DemoObjects.ClassObjects;
using InteractiveEditor.Binding;

internal class Program
{
    static void Main(string[] args)
    {

        var foo = new Foo();
        Inspector inspector = Inspector.Create<Foo>();
        inspector.bind(foo);

        foreach (var fs in inspector)
        {
            switch (fs)
            {
                case Fieldset fieldset:
                    Console.WriteLine($"Field: {fieldset.Name}, Value: {fieldset.GetValue()}");
                    break;

                case Inspector node:
                    Console.WriteLine($"Inspector: {node.Name}");
                    break;
            }
            //Console.WriteLine($"Fieldset: {fs.Name}   ");
            //Console.WriteLine($"Field: {inspector[fs.Name]}, Value: {inspector[fs.Name].GetValue()}");
        }

        Console.WriteLine("n\n\n\n\n");
     
        foreach (var fs in inspector
    .OfType<Inspector>()
    .First(i => i.Name == "Moo"))
        {
            switch (fs)
            {
                case Fieldset fieldset:
                    Console.WriteLine($"Field: {fieldset.Name}, Value: {fieldset.GetValue()}");
                    break;

                case Inspector node:
                    Console.WriteLine($"Inspector: {node.Name}");
                    break;
            }
        }


        Console.WriteLine("Hello, World!");
    }
}
