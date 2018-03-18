﻿using System.Collections.Generic;
using System.Linq;
using Fody;
using Mono.Cecil.Cil;

namespace InlineIL.Fody
{
    public class ModuleWeaver : BaseModuleWeaver
    {
        public override bool ShouldCleanReference => true;

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "InlineIL";
        }

        public override void Execute()
        {
            var hasErrors = false;

            foreach (var method in ModuleDefinition.Assembly.Modules.SelectMany(m => m.Types).SelectMany(t => t.Methods))
            {
                try
                {
                    if (MethodWeaver.NeedsProcessing(method))
                        new MethodWeaver(ModuleDefinition, method).Process();
                }
                catch (SequencePointWeavingException ex)
                {
                    AddError(ex.Message, ex.SequencePoint);
                    hasErrors = true;
                }
                catch (WeavingException ex)
                {
                    AddError(ex.Message, null);
                    hasErrors = true;
                }
            }

            if (hasErrors)
                throw new WeavingException("Weaving failed - see logged errors");
        }

        protected virtual void AddError(string message, SequencePoint sequencePoint)
            => LogErrorPoint(message, sequencePoint);
    }
}
