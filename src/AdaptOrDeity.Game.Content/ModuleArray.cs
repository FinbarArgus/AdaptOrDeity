using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace AdaptOrDeity.Game.Content
{
    /// <summary>One module instance: a record of a module array (PhLynx keys, which libcuflynx also reads).</summary>
    public sealed class ModuleRecord
    {
        public ModuleRecord(string name, string moduleType, string moduleSubtype, string instance,
            IReadOnlyList<string>? inputs = null, IReadOnlyList<string>? outputs = null)
        {
            if (string.IsNullOrEmpty(name) || name.IndexOfAny(new[] { ' ', '\t', '\n', '\r' }) >= 0)
                throw new ArgumentException($"module name '{name}' must be non-empty with no whitespace", nameof(name));
            Name = name;
            ModuleType = moduleType;
            ModuleSubtype = moduleSubtype;
            Instance = instance;
            Inputs = inputs ?? Array.Empty<string>();
            Outputs = outputs ?? Array.Empty<string>();
        }

        public string Name { get; }

        public string ModuleType { get; }

        /// <summary>The module's version (libcuflynx's BC_type).</summary>
        public string ModuleSubtype { get; }

        /// <summary>The parameter instance of the module's library entry this record starts from.</summary>
        public string Instance { get; }

        /// <summary>Names of the records whose exit ports feed this one, and of those this one feeds.</summary>
        public IReadOnlyList<string> Inputs { get; }

        public IReadOnlyList<string> Outputs { get; }
    }

    /// <summary>A row of a model's parameters file: a value that overrides the module instance's.</summary>
    public sealed class ParameterRow
    {
        public ParameterRow(string name, string units, double value, string reference)
        {
            Name = name;
            Units = units;
            Value = value;
            Reference = reference;
        }

        /// <summary>The module variable's name followed by _ and the record's name, as libcuflynx names instance parameters.</summary>
        public string Name { get; }

        public string Units { get; }

        public double Value { get; }

        public string Reference { get; }
    }

    /// <summary>
    /// A model as libcuflynx, PhLynx and the circulatory-autogen-modules tests describe it: module instances from a module
    /// library (<c>content/assembled</c> for the game's cells) and the parameters that differ from their instances, in
    /// <c>&lt;prefix&gt;_module_array.json</c> and <c>&lt;prefix&gt;_parameters.csv</c>.
    /// </summary>
    public sealed class ModuleArray
    {
        public const string ParametersHeader = "variable_name,units,value,data_reference";

        public List<ModuleRecord> Records { get; } = new List<ModuleRecord>();

        public List<ParameterRow> Parameters { get; } = new List<ParameterRow>();

        public static string ArrayFile(string prefix) => prefix + "_module_array.json";

        public static string ParametersFile(string prefix) => prefix + "_parameters.csv";

        public void Write(string directory, string prefix)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, ArrayFile(prefix)), ToJson());
            File.WriteAllText(Path.Combine(directory, ParametersFile(prefix)), ToCsv());
        }

        public static ModuleArray Read(string directory, string prefix)
        {
            var array = new ModuleArray();
            if (!(Json.Parse(File.ReadAllText(Path.Combine(directory, ArrayFile(prefix)))) is List<object?> records))
                throw new FormatException($"{ArrayFile(prefix)} must be a JSON list of records");
            foreach (var item in records)
            {
                if (!(item is Dictionary<string, object?> r))
                    throw new FormatException($"{ArrayFile(prefix)}: every record must be an object");
                array.Records.Add(new ModuleRecord(Text(r, "name"), Text(r, "module_type"), Text(r, "module_subtype"),
                    r.ContainsKey("instance") ? Text(r, "instance") : "default", Names(r, "inp_instances"),
                    Names(r, "out_instances")));
            }
            foreach (var row in Csv.Rows(File.ReadAllText(Path.Combine(directory, ParametersFile(prefix)))))
                array.Parameters.Add(new ParameterRow(row["variable_name"], row["units"],
                    double.Parse(row["value"], CultureInfo.InvariantCulture), row.TryGetValue("data_reference", out var d) ? d : ""));
            return array;
        }

        public string ToJson()
        {
            var b = new StringBuilder("[\n");
            for (int i = 0; i < Records.Count; i++)
            {
                var r = Records[i];
                b.Append("  {\"name\": ").Append(Quote(r.Name))
                    .Append(", \"module_type\": ").Append(Quote(r.ModuleType))
                    .Append(", \"module_subtype\": ").Append(Quote(r.ModuleSubtype))
                    .Append(", \"instance\": ").Append(Quote(r.Instance))
                    .Append(", \"inp_instances\": ").Append(List(r.Inputs))
                    .Append(", \"out_instances\": ").Append(List(r.Outputs))
                    .Append(i + 1 < Records.Count ? "},\n" : "}\n");
            }
            return b.Append("]\n").ToString();
        }

        public string ToCsv()
        {
            var b = new StringBuilder(ParametersHeader).Append('\n');
            foreach (var p in Parameters)
                b.Append(Csv.Field(p.Name)).Append(',').Append(Csv.Field(p.Units)).Append(',')
                    .Append(p.Value.ToString("R", CultureInfo.InvariantCulture)).Append(',').Append(Csv.Field(p.Reference)).Append('\n');
            return b.ToString();
        }

        private static string Text(Dictionary<string, object?> r, string key) =>
            r.TryGetValue(key, out var v) && v is string s ? s : throw new FormatException($"record has no text '{key}'");

        // A list of names: a JSON list, one space-separated string, or null.
        private static IReadOnlyList<string> Names(Dictionary<string, object?> r, string key)
        {
            if (!r.TryGetValue(key, out var v) || v == null)
                return Array.Empty<string>();
            if (v is string s)
                return s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var names = new List<string>();
            foreach (var n in (List<object?>)v)
                names.Add((string)n!);
            return names;
        }

        private static string List(IReadOnlyList<string> names)
        {
            var parts = new string[names.Count];
            for (int i = 0; i < names.Count; i++)
                parts[i] = Quote(names[i]);
            return "[" + string.Join(", ", parts) + "]";
        }

        private static string Quote(string s)
        {
            var b = new StringBuilder("\"");
            foreach (char c in s)
            {
                if (c == '"' || c == '\\')
                    b.Append('\\').Append(c);
                else if (c < ' ')
                    b.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                else
                    b.Append(c);
            }
            return b.Append('"').ToString();
        }
    }

    /// <summary>
    /// The module library the game's cells come from (content/assembled, embedded): each instance's parameters with their
    /// units, so a model's parameters file can name them as libcuflynx does.
    /// </summary>
    public static class ModuleLibrary
    {
        private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> Cache =
            new Dictionary<string, IReadOnlyDictionary<string, string>>();

        /// <summary>Units of each parameter of an instance of a module in content/assembled.</summary>
        public static IReadOnlyDictionary<string, string> InstanceUnits(string moduleType, string moduleSubtype, string instance)
        {
            string path = $"assembled/{moduleType}/versions/{moduleSubtype}/instances/{instance}/{instance}_parameters.csv";
            lock (Cache)
            {
                if (Cache.TryGetValue(path, out var units))
                    return units;
                var u = new Dictionary<string, string>();
                foreach (var row in Csv.Rows(ContentFiles.Read(path)))
                    u[row["variable_name"]] = row["units"];
                Cache[path] = u;
                return u;
            }
        }
    }

    /// <summary>Comma-separated values with a header row, double-quoted fields allowed (RFC 4180).</summary>
    internal static class Csv
    {
        public static IEnumerable<Dictionary<string, string>> Rows(string text)
        {
            List<string>? header = null;
            int i = 0;
            while (i < text.Length)
            {
                var fields = ReadLine(text, ref i);
                if (fields.Count == 1 && fields[0].Length == 0)
                    continue;
                if (header == null)
                {
                    header = fields;
                    continue;
                }
                var row = new Dictionary<string, string>();
                for (int k = 0; k < header.Count; k++)
                    row[header[k]] = k < fields.Count ? fields[k] : "";
                yield return row;
            }
        }

        public static string Field(string s) =>
            s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? s : "\"" + s.Replace("\"", "\"\"") + "\"";

        private static List<string> ReadLine(string s, ref int i)
        {
            var fields = new List<string>();
            var b = new StringBuilder();
            bool quoted = false;
            for (; i < s.Length; i++)
            {
                char c = s[i];
                if (quoted)
                {
                    if (c != '"')
                        b.Append(c);
                    else if (i + 1 < s.Length && s[i + 1] == '"')
                        b.Append(s[++i]);
                    else
                        quoted = false;
                }
                else if (c == '"')
                    quoted = true;
                else if (c == ',')
                {
                    fields.Add(b.ToString());
                    b.Clear();
                }
                else if (c == '\n' || c == '\r')
                {
                    if (c == '\r' && i + 1 < s.Length && s[i + 1] == '\n')
                        i++;
                    i++;
                    break;
                }
                else
                    b.Append(c);
            }
            fields.Add(b.ToString());
            return fields;
        }
    }
}
