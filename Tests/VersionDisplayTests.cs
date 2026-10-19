using UnattendLocal.Desktop;

static class VersionDisplayTests
{
    public static int Run()
    {
        // The test assembly has its own version, so a literal release label fails here.
        if (new MainViewModel().Status != "就绪 · " + ProductIdentity.VersionLabel)
            throw new Exception("Startup status does not follow the assembly version.");
        Console.WriteLine("PASS startup status follows the assembly version");
        return 1;
    }
}
