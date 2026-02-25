using ToGo.Compiler;

if (args.Length == 0)
{
	Console.Error.WriteLine("Usage: ToGo.LanguageServer <path-to-file.tg> [output.dll]");
	return;
}

var input = args[0];
var output = args.Length >= 2
	? args[1]
	: Path.Combine(Path.GetDirectoryName(input) ?? ".", Path.GetFileNameWithoutExtension(input) + ".dll");

var compiler = new ToGoCompiler();
var result = compiler.CompileFile(input, output);

for (int i = 0; i < result.Diagnostics.Count; i++)
{
	Console.Error.WriteLine(result.Diagnostics[i].ToString());
}

if (result.Success)
{
	Console.WriteLine(result.OutputPath);
}
else
{
	Environment.ExitCode = 1;
}
