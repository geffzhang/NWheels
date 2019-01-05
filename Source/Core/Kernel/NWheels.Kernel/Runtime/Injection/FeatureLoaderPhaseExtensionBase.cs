﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using NWheels.Kernel.Api.Injection;

namespace NWheels.Kernel.Runtime.Injection
{
    [ExcludeFromCodeCoverage]
    public abstract class FeatureLoaderPhaseExtensionBase : IFeatureLoaderPhaseExtension
    {
        public virtual void BeforeContributeConfigSections(IComponentContainer components)
        {
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public virtual void BeforeContributeConfiguration(IComponentContainer components)
        {
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public virtual void BeforeContributeComponents(IComponentContainer components)
        {
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public virtual void BeforeContributeAdapterComponents(IComponentContainer comcomponents)
        {
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public virtual void BeforeCompileComponents(IComponentContainer comcomponents)
        {
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public virtual void BeforeContributeCompiledComponents(IComponentContainer comcomponents)
        {
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public virtual void AfterContributeCompiledComponents(IComponentContainer comcomponents)
        {
        }
    }
}
