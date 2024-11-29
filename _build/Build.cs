using System.Collections.Generic;

class MainBuild : NukeBuild
{
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration =
        IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter(List = false)] readonly bool DotnetRunningInContainer;
    [GlobalJson] readonly GlobalJson GlobalJson;

    [Parameter("Don't open the coverage report")]
    readonly bool NoBrowse;

    [Parameter("Production Git Branch")] readonly string MasterBranch = "master";

    [Solution] readonly Solution Solution;
    [Parameter] readonly string TestResultFile = "test_result.xml";

    static AbsolutePath CoverageFiles => RootDirectory / "**" / "coverage.cobertura.xml";
    static AbsolutePath TestReportDirectory => RootDirectory / "TestReport";
    static AbsolutePath DocsPath => RootDirectory / "docfx";
    static AbsolutePath DocsSitePath => DocsPath / "_site";

    static readonly string[] cleanPaths = ["src", "tests", "samples"];

    Target Clean => _ => _
        .Description("Clean project directories")
        .Executes(() => cleanPaths.Select(path => RootDirectory / path)
            .SelectMany(dir => dir
                .GlobDirectories("**/bin", "**/obj", "**/TestResults"))
            .Append(TestReportDirectory)
            .ForEach(x => x.DeleteDirectory()));

    Target Restore => _ => _
        .Description("Run dotnet restore in every project")
        .DependsOn(Clean)
        .Executes(() => DotNetRestore(s => s.SetProjectFile(Solution)));

    IReadOnlyCollection<Output> BuildProj(AbsolutePath project, Configuration configuration) =>
        DotNetBuild(s => s
            .SetProjectFile(project)
            .SetConfiguration(configuration)
            .EnableNoLogo()
            .EnableNoRestore()
            .SetProperty("UseSharedCompilation", false)
            .AddProcessAdditionalArguments("/nodeReuse:false"));

    Target Build => _ => _
        .Description("Builds SDK")
        .DependsOn(Restore)
        .Executes(() => BuildProj(Solution, Configuration));

    Target BuildSamples => _ => _
        .Description("Builds SDK and Samples")
        .Executes(() =>
        {
            var sampleSln = RootDirectory / "Samples" / "Backdash.Samples.sln";
            DotNetRestore(s => s.SetProjectFile(sampleSln));
            BuildProj(sampleSln, Configuration);
        });

    Target BuildAll => _ => _
        .Description("Build All Projects")
        .Triggers(Build, BuildSamples);

    Target Lint => _ => _
        .Description("Check for codebase formatting and analyzers")
        .DependsOn(Build)
        .Executes(() => DotNetFormat(c => c
            .EnableNoRestore()
            .EnableVerifyNoChanges()
            .SetProject(Solution)));

    Target Format => _ => _
        .Description("Try fix codebase formatting and analyzers")
        .DependsOn(Build)
        .Executes(() => DotNetFormat(c => c
            .EnableNoRestore()
            .SetProject(Solution)));

    Target Test => _ => _
        .Description("Run tests with coverage")
        .DependsOn(Build)
        .Executes(() => DotNetTest(s => s
            .SetVerbosity(DotNetVerbosity.minimal)
            .SetFilter("FullyQualifiedName!~Acceptance")
            .EnableNoBuild()
            .EnableNoRestore()
            .SetConfiguration(Configuration)
            .SetProjectFile(Solution)
            .SetLoggers($"trx;LogFileName={TestResultFile}")
            .SetSettingsFile(RootDirectory / "coverlet.runsettings")
        ))
        .Executes(() =>
        {
            ReportGenerator(r => r
                .SetReports(CoverageFiles)
                .SetTargetDirectory(TestReportDirectory)
                .SetReportTypes(ReportTypes.TextSummary));
            (TestReportDirectory / "Summary.txt")
                .ReadAllLines()
                .ForEach(l => Console.WriteLine(l));
        });

    Target GenerateReport => _ => _
        .Description("Generate test coverage report")
        .After(Test)
        .OnlyWhenDynamic(() => CoverageFiles.GlobFiles().Any())
        .Executes(() =>
            ReportGenerator(r => r
                .SetReports(CoverageFiles)
                .SetTargetDirectory(TestReportDirectory)
                .SetReportTypes(
                    ReportTypes.Html,
                    ReportTypes.Clover,
                    ReportTypes.Cobertura,
                    ReportTypes.MarkdownSummary
                )));

    Target BrowseReport => _ => _
        .Description("Open coverage report")
        .OnlyWhenStatic(() => !NoBrowse && !DotnetRunningInContainer)
        .After(GenerateReport, GenerateBadges)
        .Unlisted()
        .Executes(() =>
        {
            var path = TestReportDirectory / "index.htm";
            OpenBrowser(path);
        });

    Target Report => _ => _
        .Description("Run tests and generate coverage report")
        .DependsOn(Test)
        .Triggers(GenerateReport, BrowseReport);

    Target GenerateBadges => _ => _
        .Description("Generate cool badges for readme")
        .After(Test)
        .Requires(() => CoverageFiles.GlobFiles().Any())
        .Executes(() =>
        {
            var output = RootDirectory / "docfx" / "_site";
            if (!output.DirectoryExists()) output.CreateDirectory();
            Badges.ForCoverage(output, CoverageFiles);
            Badges.ForDotNetVersion(output, GlobalJson);
            Badges.ForTests(output, TestResultFile);
        });

    Target BuildDocs => _ => _
        .Description("Build DocFX")
        .DependsOn(Restore)
        .Executes(() =>
        {
            DocsSitePath.CreateOrCleanDirectory();
            (DocsPath / "api").CreateOrCleanDirectory();
            BuildProj(Solution, Configuration.Release);
            DocFX.Build(c => c
                .SetProcessWorkingDirectory(DocsPath)
                .SetProcessEnvironmentVariable(DocFX.DocFXSourceBranchName, MasterBranch)
            );
        });

    Target Docs => _ => _
        .Description("View DocFX")
        .DependsOn(Restore)
        .Executes(() =>
        {
            BuildProj(Solution, Configuration.Release);
            DocFX.Serve(c => c
                .SetProcessWorkingDirectory(DocsPath)
                .SetProcessEnvironmentVariable(DocFX.DocFXSourceBranchName, MasterBranch));
        });

    Target UpdateTools => _ => _
        .Description("Update all project .NET tools")
        .Executes(UpdateLocalTools);

    Target BuildNative => _ => _
        .Description("Builds as an AOT Native library")
        .DependsOn(Restore)
        .Executes(() =>
            DotNetPublish(s => s
                .EnableNoLogo()
                .SetProject(Solution.FindProject("Backdash"))
                .SetConfiguration(Configuration.Release)
                .AddProperty("DefineConstants", "AOT_ENABLED")
                .AddProcessAdditionalArguments("--use-current-runtime"))
        );


    public static int Main() => Execute<MainBuild>();

    protected override void OnBuildInitialized() =>
        DotNetToolRestore(c => c.DisableProcessOutputLogging());

    protected override void OnBuildFinished()
    {
        try
        {
            DotNet("build-server shutdown");
        }
        catch (Exception ex)
        {
            Log.Warning("Failure shutting build server down:{Error}", ex.Message);
        }
    }
}