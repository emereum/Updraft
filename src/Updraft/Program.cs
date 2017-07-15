using Topshelf;

namespace Updraft
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.UseNLog();

                x.Service<UpdraftService>(s =>
                {
                    s.ConstructUsing(name => new UpdraftService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });

                x.RunAsLocalSystem();

                x.SetDescription("Updraft");
                x.SetDisplayName("Updraft");
                x.SetServiceName("Updraft");
            });
        }
    }
}
