using System.Collections.Generic;

namespace AdaptOrDeity.Sim.Modules
{
    /// <summary>Role of a module variable, from <c>variables_and_units</c> in the module config.</summary>
    public enum VariableRole
    {
        Variable,
        BoundaryCondition,
        Constant,
        GlobalConstant,
    }

    /// <summary>Which port list of the module config a port came from.</summary>
    public enum PortDirection
    {
        Entrance,
        Exit,
        General,
    }

    /// <summary>
    /// How a port takes several connections (libcuflynx <c>multi_port</c>): <see cref="None"/> allows one
    /// connection; <see cref="Many"/> fans out; <see cref="Sum"/> sums neighbours' values into the input.
    /// </summary>
    public enum MultiPortMode
    {
        None,
        Many,
        Sum,
        Multiply,
    }

    /// <summary>One variable of a kernel, in the units of its CellML module.</summary>
    public sealed class VariableInfo
    {
        public VariableInfo(string name, string units, double siMultiplier, VariableRole role)
        {
            Name = name;
            Units = units;
            SiMultiplier = siMultiplier;
            Role = role;
        }

        /// <summary>CellML variable name, unchanged.</summary>
        public string Name { get; }

        /// <summary>CellML units name.</summary>
        public string Units { get; }

        /// <summary>Value in canonical SI = value in module units × this.</summary>
        public double SiMultiplier { get; }

        public VariableRole Role { get; }
    }

    /// <summary>A port of a module: a typed, ordered list of variables matched positionally when coupling.</summary>
    public sealed class PortInfo
    {
        public PortInfo(string portType, PortDirection direction, IReadOnlyList<string> variables, MultiPortMode multiPort)
        {
            PortType = portType;
            Direction = direction;
            Variables = variables;
            MultiPort = multiPort;
        }

        public string PortType { get; }

        public PortDirection Direction { get; }

        public IReadOnlyList<string> Variables { get; }

        public MultiPortMode MultiPort { get; }
    }
}
