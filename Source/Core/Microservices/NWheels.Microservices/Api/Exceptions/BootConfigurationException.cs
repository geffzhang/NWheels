﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using NWheels.Kernel.Api.Exceptions;
using NWheels.Microservices.Runtime;

namespace NWheels.Microservices.Api.Exceptions
{
    [Serializable]
    public class BootConfigurationException : ExplainableExceptionBase
    {
        private BootConfigurationException(string reason, string moduleName = null, string featureName = null)
            : base(reason)
        {
            this.ModuleName = moduleName;
            this.FeatureName = featureName;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        protected BootConfigurationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public string ModuleName { get; }
        public string FeatureName { get; }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        protected override IEnumerable<KeyValuePair<string, string>> BuildKeyValuePairs()
        {
            yield return new KeyValuePair<string, string>(_s_stringModuleName, this.ModuleName);
            yield return new KeyValuePair<string, string>(_s_stringFeatureName, this.FeatureName);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private readonly static string _s_stringMicroserviceNameNotSpecified = nameof(MicroserviceNameNotSpecified);
        private readonly static string _s_stringKernelModuleItemInvalidLocation = nameof(KernelModuleItemInvalidLocation);
        private readonly static string _s_stringModuleListedMultipleTimes = nameof(ModuleListedMultipleTimes);
        private readonly static string _s_stringModuleName = nameof(ModuleName);
        private readonly static string _s_stringFeatureName = nameof(FeatureName);

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public static BootConfigurationException MicroserviceNameNotSpecified()
        {
            return new BootConfigurationException(_s_stringMicroserviceNameNotSpecified);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public static BootConfigurationException KernelModuleItemInvalidLocation()
        {
            return new BootConfigurationException(
                reason: _s_stringKernelModuleItemInvalidLocation, 
                moduleName: MutableBootConfiguration.KernelAssemblyName);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public static BootConfigurationException ModuleListedMultipleTimes(string moduleName)
        {
            return new BootConfigurationException(
                reason: _s_stringModuleListedMultipleTimes, 
                moduleName: moduleName);
        }
    }
}
