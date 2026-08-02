using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: MigrationDecompiler <assembly> <type> <migration-directory>");
    return 1;
}

var settings = new DecompilerSettings(LanguageVersion.Latest)
{
    ThrowOnAssemblyResolveErrors = false
};
var decompiler = new CSharpDecompiler(args[0], settings);
var source = decompiler.DecompileTypeAsString(new FullTypeName(args[1]));
var up = ExtractMethod(source, "protected override void Up(");
var down = ExtractMethod(source, "protected override void Down(");
var target = ExtractMethod(source, "protected override void BuildTargetModel(");
var usings = source[..source.IndexOf("namespace ", StringComparison.Ordinal)];
const string migrationName = "AddMotorCompatibility";
const string migrationId = "20260802034236_AddMotorCompatibility";
var migrationSource = $"{usings}namespace StockSense.Infrastructure.Migrations;\n\npublic partial class {migrationName} : Migration\n{{\n{Indent(up)}\n\n{Indent(down)}\n}}\n";
var designerSource = $"{usings}namespace StockSense.Infrastructure.Migrations;\n\n[DbContext(typeof(ApplicationDbContext))]\n[Migration(\"{migrationId}\")]\npublic partial class {migrationName}\n{{\n{Indent(target)}\n}}\n";
Directory.CreateDirectory(args[2]);
File.WriteAllText(Path.Combine(args[2], $"{migrationId}.cs"), migrationSource);
File.WriteAllText(Path.Combine(args[2], $"{migrationId}.Designer.cs"), designerSource);
Console.WriteLine($"Recovered {migrationId} from {args[0]}");
return 0;

static string ExtractMethod(string source, string signature)
{
    var start = source.IndexOf(signature, StringComparison.Ordinal);
    if (start < 0) throw new InvalidOperationException($"Method not found: {signature}");
    var openBrace = source.IndexOf('{', start);
    var depth = 0;
    for (var index = openBrace; index < source.Length; index++)
    {
        if (source[index] == '{') depth++;
        if (source[index] != '}') continue;
        depth--;
        if (depth == 0) return source[start..(index + 1)];
    }
    throw new InvalidOperationException($"Unbalanced method: {signature}");
}

static string Indent(string value) => string.Join('\n', value.Replace("\r\n", "\n").Split('\n').Select(line => $"\t{line}"));
