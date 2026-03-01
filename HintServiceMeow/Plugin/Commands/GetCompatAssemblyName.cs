namespace HintServiceMeow.Plugin.Commands
{
    using System;
    using System.Text;
    using CommandSystem;
    using HintServiceMeow.Core.Utilities;
    using HintServiceMeow.Core.Utilities.Pools;

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    internal class GetCompatAssemblyName : ICommand
    {
        public string Command => "GetCompatAssemblyName";

        public string[] Aliases => Array.Empty<string>();

        public string Description => "Get the name of all the assemblies that are using Compatibility Adaptor in HintServiceMeow";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            StringBuilder sb = StringBuilderPool.Instance.Rent();

            sb.AppendLine("The following assemblies are using Compatibility Adaptor in HintServiceMeow:");

            foreach (string name in CompatibilityAdaptor.RegisteredAssemblies)
            {
                sb.Append("- ");
                sb.AppendLine(name);
            }

            response = StringBuilderPool.Instance.ToStringReturn(sb);
            return true;
        }
    }
}
