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
    AbsolutePath CoverageFiles => RootDirectory / "**" / "coverage.cobertura.xml";
    AbsolutePath TestReportDirectory => RootDirectory / "TestReport";
    AbsolutePath DocsPath => RootDirectory / "docfx";
    AbsolutePath DocsSitePath => DocsPath / "_site";

    Target Clean => _ => _
        .Description("Clean project directories")
        .Executes(() => new[] { "src", "tests" }
            .Select(path => RootDirectory / path)
            .SelectMany(dir => dir
                .GlobDirectories("**/bin", "**/obj", "**/TestResults"))
            .Append(TestReportDirectory)
            .ForEach(x => x.CreateOrCleanDirectory()));

    Target Restore => _ => _
        .Description("Run dotnet restore in every project")
        .DependsOn(Clean)
        .Executes(() => DotNetRestore(s => s
            .SetProjectFile(Solution)));

    Target Build => _ => _
        .Description("Builds SDK")
        .DependsOn(Restore)
        .Executes(() =>
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoLogo()
                .EnableNoRestore()
                .SetProperty("UseSharedCompilation", false)
                .SetProcessArgumentConfigurator(args => args.Add("/nodeReuse:false")))
        );

    Target BuildSamples => _ => _
        .Description("Builds SDK and Samples")
        .DependsOn(Restore)
        .Executes(() =>
            DotNetBuild(s => s
                .SetProjectFile(RootDirectory / "Backdash.Samples.sln")
                .SetConfiguration(Configuration)
                .EnableNoLogo()
                .EnableNoRestore()
                .SetProperty("UseSharedCompilation", false)
                .SetProcessArgumentConfigurator(args => args.Add("/nodeReuse:false")))
        );

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
        .Executes(() =>
        {
            DocsSitePath.CreateOrCleanDirectory();
            (DocsPath / "api").CreateOrCleanDirectory();
            DotNetBuild(s => s.SetProjectFile(Solution).SetConfiguration(Configuration.Release));
            DocFX.Build(c => c
                .SetProcessWorkingDirectory(DocsPath)
                .SetProcessEnvironmentVariable(DocFX.DocFXSourceBranchName, MasterBranch)
            );
        });

    Target Docs => _ => _
        .Description("View DocFX")
        .Executes(() =>
        {
            DotNetBuild(s => s.SetProjectFile(Solution).SetConfiguration(Configuration.Release));
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
                .EnableNoRestore()
                .SetProject(Solution.FindProject("Backdash"))
                .SetConfiguration(Configuration.Release)
                .AddProperty("DefineConstants", "AOT_ENABLED")
                .SetProcessArgumentConfigurator(args => args.Add("--use-current-runtime"))
            ));

    public static int Main() => Execute<MainBuild>();

    protected override void OnBuildInitialized() =>
        DotNetToolRestore(c => c.DisableProcessLogOutput());

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